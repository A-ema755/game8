#!/usr/bin/env bash
# Claude Code Game Studios — Status Line (no jq dependency)
# Receives JSON on stdin, outputs a single-line status.
#
# Format: Model (CtxSize) | Ctx: X% | In: X.XK Out: X.XK | $X.XX | HH:MM:SS | 5h: X% 7d: X% | +X/-X

input=$(cat)

# ---------------------------------------------------------------------------
# 1. Parse JSON values via grep/sed (no jq needed)
# ---------------------------------------------------------------------------
grab() {
  echo "$input" | grep -o "\"$1\"[[:space:]]*:[[:space:]]*[^,}\"]*" | head -1 | sed 's/.*:[[:space:]]*//' | tr -d ' '
}
grab_str() {
  echo "$input" | grep -o "\"$1\"[[:space:]]*:[[:space:]]*\"[^\"]*\"" | head -1 | sed 's/.*:[[:space:]]*"//' | tr -d '"'
}

model_name=$(grab_str 'display_name')
model_id=$(grab_str 'id')
ctx_size=$(grab 'context_window_size')
used_pct=$(grab 'used_percentage')
total_in=$(grab 'total_input_tokens')
total_out=$(grab 'total_output_tokens')
cur_in=$(grab 'input_tokens')
cur_cache_write=$(grab 'cache_creation_input_tokens')
cur_cache_read=$(grab 'cache_read_input_tokens')
session_id=$(grab_str 'session_id')
cwd=$(grab_str 'current_dir')
five_h=""
seven_d=""

# Rate limits need context-aware parsing since used_percentage appears multiple times
rate_block=$(echo "$input" | grep -o '"rate_limits"[^}]*}[^}]*}' | head -1)
if [ -n "$rate_block" ]; then
  fh_block=$(echo "$rate_block" | grep -o '"five_hour"[^}]*}' | head -1)
  sd_block=$(echo "$rate_block" | grep -o '"seven_day"[^}]*}' | head -1)
  five_h=$(echo "$fh_block" | grep -o '"used_percentage"[[:space:]]*:[[:space:]]*[0-9.]*' | grep -o '[0-9.]*$')
  seven_d=$(echo "$sd_block" | grep -o '"used_percentage"[[:space:]]*:[[:space:]]*[0-9.]*' | grep -o '[0-9.]*$')
fi

# Defaults
[ -z "$model_name" ] && model_name="Unknown"
[ -z "$ctx_size" ] && ctx_size=0
[ -z "$total_in" ] && total_in=0
[ -z "$total_out" ] && total_out=0
[ -z "$cur_cache_write" ] && cur_cache_write=0
[ -z "$cur_cache_read" ] && cur_cache_read=0

# Normalize Windows paths
cwd=$(echo "$cwd" | sed 's|\\|/|g')
[ -z "$cwd" ] && cwd="."

# ---------------------------------------------------------------------------
# 2. Model name + context window size label
# ---------------------------------------------------------------------------
if [ "$ctx_size" -ge 1000000 ] 2>/dev/null; then
  ctx_size_label="$(echo "$ctx_size" | awk '{printf "%gM", $1/1000000}') context"
elif [ "$ctx_size" -ge 1000 ] 2>/dev/null; then
  ctx_size_label="$(echo "$ctx_size" | awk '{printf "%gK", $1/1000}') context"
else
  ctx_size_label="${ctx_size} context"
fi

model_segment="${model_name} (${ctx_size_label})"

# ---------------------------------------------------------------------------
# 3. Context usage percentage
# ---------------------------------------------------------------------------
if [ -n "$used_pct" ]; then
  ctx_segment="Ctx: $(printf '%.0f' "$used_pct")%"
else
  ctx_segment="Ctx: --%"
fi

# ---------------------------------------------------------------------------
# 4. Token counts
# ---------------------------------------------------------------------------
format_k() {
  local val="$1"
  if [ "$val" -ge 1000 ] 2>/dev/null; then
    echo "$val" | awk '{printf "%.1fK", $1/1000}'
  else
    echo "${val}"
  fi
}

in_label=$(format_k "$total_in")
out_label=$(format_k "$total_out")
tokens_segment="In: ${in_label} Out: ${out_label}"

# ---------------------------------------------------------------------------
# 5. Estimated session cost
# ---------------------------------------------------------------------------
get_pricing() {
  local id="$1"
  case "$id" in
    *opus-4* | *opus-5*)   echo "15 75 18.75 1.50" ;;
    *sonnet-4* | *sonnet-5*) echo "3 15 3.75 0.30" ;;
    *haiku-3-5* | *haiku-4*) echo "0.80 4 1.00 0.08" ;;
    *)                       echo "3 15 3.75 0.30" ;;
  esac
}

pricing=$(get_pricing "$model_id")
p_in=$(echo "$pricing" | awk '{print $1}')
p_out=$(echo "$pricing" | awk '{print $2}')
p_cw=$(echo "$pricing" | awk '{print $3}')
p_cr=$(echo "$pricing" | awk '{print $4}')

cost=$(echo "$total_in $total_out $cur_cache_write $cur_cache_read $p_in $p_out $p_cw $p_cr" | \
  awk '{cost = ($1*$5 + $2*$6 + $3*$7 + $4*$8) / 1000000; printf "%.2f", cost}')
cost_segment="\$${cost}"

# ---------------------------------------------------------------------------
# 6. Session elapsed time
# ---------------------------------------------------------------------------
timer_dir="/tmp/claude-statusline"
mkdir -p "$timer_dir" 2>/dev/null

elapsed_segment="--:--:--"
if [ -n "$session_id" ]; then
  timer_file="${timer_dir}/${session_id}.start"
  now=$(date +%s)
  if [ ! -f "$timer_file" ]; then
    echo "$now" > "$timer_file"
  fi
  start_epoch=$(cat "$timer_file" 2>/dev/null)
  if [ -n "$start_epoch" ]; then
    elapsed=$(( now - start_epoch ))
    hh=$(( elapsed / 3600 ))
    mm=$(( (elapsed % 3600) / 60 ))
    ss=$(( elapsed % 60 ))
    elapsed_segment=$(printf "%02d:%02d:%02d" "$hh" "$mm" "$ss")
  fi
fi

# ---------------------------------------------------------------------------
# 7. Rate limit segments
# ---------------------------------------------------------------------------
rate_segment=""
if [ -n "$five_h" ] || [ -n "$seven_d" ]; then
  parts=""
  [ -n "$five_h" ] && parts="5h: $(printf '%.0f' "$five_h")%"
  [ -n "$seven_d" ] && parts="${parts:+$parts }7d: $(printf '%.0f' "$seven_d")%"
  rate_segment="$parts"
fi

# ---------------------------------------------------------------------------
# 8. Git diff stats
# ---------------------------------------------------------------------------
git_segment="+0/-0"
if [ -d "$cwd/.git" ] || git -C "$cwd" rev-parse --git-dir &>/dev/null 2>&1; then
  diff_stat=$(GIT_OPTIONAL_LOCKS=0 git -C "$cwd" diff --shortstat HEAD 2>/dev/null || true)
  if [ -n "$diff_stat" ]; then
    adds=$(echo "$diff_stat" | grep -oE '[0-9]+ insertion' | grep -oE '[0-9]+' || echo "0")
    dels=$(echo "$diff_stat" | grep -oE '[0-9]+ deletion' | grep -oE '[0-9]+' || echo "0")
    [ -z "$adds" ] && adds=0
    [ -z "$dels" ] && dels=0
    git_segment="+${adds}/-${dels}"
  fi
fi

# ---------------------------------------------------------------------------
# 9. Assemble
# ---------------------------------------------------------------------------
output="${model_segment} | ${ctx_segment} | ${tokens_segment} | ${cost_segment} | ${elapsed_segment}"
[ -n "$rate_segment" ] && output="${output} | ${rate_segment}"
output="${output} | ${git_segment}"

printf "%s" "$output"
