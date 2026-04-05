# Context Management

Context is the most critical resource in a Claude Code session. Manage it actively.

## File-Backed State (Primary Strategy)

**The file is the memory, not the conversation.** Conversations are ephemeral and
will be compacted or lost. Files on disk persist across compactions and session crashes.

### Session State File

Maintain `production/session-state/active.md` as a living checkpoint. Update it
after each significant milestone.

The state file should contain: current task, progress checklist, key decisions
made, files being worked on, and open questions.

#### Structured Status Block

For production-stage work, include a parsed status section showing the breadcrumb
for quick context recovery:

```markdown
## Current Status
- **Epic**: [e.g., MVP Combat Loop]
- **Feature**: [e.g., Damage & Health System]
- **Task**: [e.g., Implement DamageCalculator.Calculate()]
- **Blocked by**: [any dependencies or open questions]
- **Files in progress**: [list of partially-complete files]
- **Last milestone**: [what was completed and when]
```

### Incremental File Writing

When creating multi-section documents:

1. Create the file immediately with a skeleton (all section headers, empty bodies)
2. Discuss and draft one section at a time in conversation
3. Write each section to the file as soon as it's approved
4. Update the session state file after each section

## Proactive Compaction

- **Compact proactively** at ~60-70% context usage
- **Use `/clear`** between unrelated tasks
- **Natural compaction points:** after writing a section to file, after committing

## Context Budgets by Task Type

- Light (read/review): ~3k tokens startup
- Medium (implement feature): ~8k tokens
- Heavy (multi-system refactor): ~15k tokens

## Subagent Delegation

Use subagents for research and exploration to keep the main session clean.

## Recovery After Session Crash

1. Read `production/session-state/active.md` for context
2. Read partially-completed files listed in the state
3. Continue from the next incomplete section or task
