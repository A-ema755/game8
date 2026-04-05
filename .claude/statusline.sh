#!/usr/bin/env bash
# Claude Code Game Studios — Status Line
# Receives JSON on stdin, outputs a single-line status.
#
# Format: Model (CtxSize) | Ctx: X% | In: X.XK Out: X.XK | $X.XX | HH:MM:SS | 5h: X% 7d: X% | +X/-X

input=$(cat)

# ---------------------------------------------------------------------------
# 1. Parse JSON
# ---------------------------------------------------------------------------
model_name=$(echo "$input" | jq -r '.model.display_name // "Unknown"')
model_id=$(echo "$input" | jq -r '.model.id // ""')
ctx_size=$(echo "$input" | jq -r '.context_window.context_window_size // 0')
used_pct=$(echo "$input" | jq -r '.context_window.used_percentage // empty')
total_in=$(echo "$input" | jq -r '.context_window.total_input_tokens // 0')
total_out=$(echo "$input" | jq -r '.context_window.total_output_tokens // 0')
cur_in=$(echo "$input" | jq -r '.context_window.current_usage.input_tokens // 0')
cur_cache_write=$(echo "$input" | jq -r '.context_window.current_usage.cache_creation_input_tokens // 0')
cur_cache_read=$(echo "$input" | jq -r '.context_window.current_usage.cache_read_input_tokens // 0')
session_id=$(echo "$input" | jq -r '.session_id // ""')
cwd=$(echo "$input" | jq -r '.workspace.current_dir // .cwd // ""')
five_h=$(echo "$input" | jq -r '.rate_limits.five_hour.used_percentage // empty')
seven_d=$(echo "$input" | jq -r '.rate_limits.seven_day.used_percentage // empty')

# Normalize Windows paths
cwd=$(echo "$cwd" | sed 's|\\|/|g')
[ -z "$cwd" ] && cwd="."

# ---------------------------------------------------------------------------
# 2. Model name + context window size label
# ---------------------------------------------------------------------------
# Format context window size as human-readable (200000 -> 200K, 1000000 -> 1M)
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
# 4. Token counts (cumulative input/output, formatted as K with one decimal)
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
#    Pricing (per 1M tokens, approximate as of early 2026):
#      claude-opus-4*       input $15  output $75  cache_write $18.75  cache_read $1.50
#      claude-sonnet-4*     input $3   output $15  cache_write $3.75   cache_read $0.30
#      claude-haiku-3-5*    input $0.80 output $4  cache_write $1.00   cache_read $0.08
#      default fallback     input $3   output $15
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
#    Store session start epoch in a temp file keyed by session_id.
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
# 7. Rate limit segments (5h and 7d), only shown when data is present
# ---------------------------------------------------------------------------
rate_segment=""
if [ -n "$five_h" ] || [ -n "$seven_d" ]; then
  parts=""
  [ -n "$five_h" ] && parts="5h: $(printf '%.0f' "$five_h")%"
  [ -n "$seven_d" ] && parts="${parts:+$parts }7d: $(printf '%.0f' "$seven_d")%"
  rate_segment="$parts"
fi

# ---------------------------------------------------------------------------
# 8. Git diff stats (+additions/-deletions) against HEAD, skip optional locks
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
# 9. Assemble final output
# ---------------------------------------------------------------------------
output="${model_segment} | ${ctx_segment} | ${tokens_segment} | ${cost_segment} | ${elapsed_segment}"
[ -n "$rate_segment" ] && output="${output} | ${rate_segment}"
output="${output} | ${git_segment}"

printf "%s" "$output"
