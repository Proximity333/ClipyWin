Yes — Read README.md, root manifests, workspace config, and lockfiles first to understand structure.
Yes — Then inspect build/test/lint/typecheck/config and CI/pre-commit settings for repo constraints.
Yes — If architecture is unclear, inspect representative code files to identify entrypoints and package boundaries.
Yes — Use Glob and Grep (rg) for searching; avoid ad-hoc shell searches for file discovery.
Yes — Prefer executable sources of truth (config/scripts) over prose; trust config when docs conflict.
Yes — If present, check opencode.json for repo-specific OpenCode guidance.
Yes — Edit with apply_patch; do not rely on echo/cat redirection for changes.
Yes — Git changes: commit only when explicitly requested by the user; avoid pushes without permission; follow safety rules.
Yes — For verification, run focused checks in order: lint -> typecheck -> test (as prescribed by the repo).
Yes — Identify major directories and ownership to understand monorepo/package boundaries and entrypoints.
Yes — Note repo quirks: generated code, migrations, codegen artifacts, special env loading, dev servers, infra flows.
Yes — If AGENTS.md exists, update in place and reconcile with current code; prune stale guidance.
Yes — If questions arise, ask a single concise batch of questions rather than many.
Yes — Cite related instruction sources (AGENTS.md, CLAUDE.md, .cursor/rules, .cursorrules, .github/copilot-instructions.md, opencode.json).
Yes — Keep the file compact and high-signal; summarize only guidance that changes how an agent should work.
