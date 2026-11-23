#!/usr/bin/env bash
# PostToolUse(Write|Edit) hook.
# When a CODE or WORKFLOW file changes, remind Claude to update the status trackers
# (README.md "Status tracker" + ASSIGNMENTS.md). This only injects a context note —
# it never blocks the edit. Fires only when Claude is the one editing files.
#
# Payload arrives as JSON on stdin: { "tool_input": { "file_path": "..." }, ... }
# All parsing/matching is done in node so Windows backslash paths are handled correctly.

exec node -e '
let d="";
process.stdin.on("data", c => d += c).on("end", () => {
  try {
    const j = JSON.parse(d);
    const f = (j.tool_input && j.tool_input.file_path) ||
              (j.tool_response && j.tool_response.filePath) || "";
    const n = f.replace(/\\/g, "/");
    // Backend code, frontend code, or CI workflows — NOT the docs themselves.
    if (/\/(API|UI-Angular)\//.test(n) || /\/\.github\/workflows\//.test(n)) {
      const msg = "A code or workflow file changed (" + f + "). Before ending this turn, " +
        "update the \"Status tracker\" table in README.md (and its \"Last updated\" date) " +
        "and the matching row(s) in ASSIGNMENTS.md so both stay in sync with this change.";
      process.stdout.write(JSON.stringify({
        hookSpecificOutput: { hookEventName: "PostToolUse", additionalContext: msg }
      }));
    }
  } catch (e) { /* malformed payload: stay silent, never block */ }
});
'
