# Temp Files

- Save all throwaway files (Python node scripts, graph JSON payloads, scratch artifacts) to a `temp/` folder at the repo root.
- Create `temp/` if it does not already exist.
- Do not write generated `.py`, `.json`, or other scratch files directly to the repo root, `src/`, or other source folders.
- Use absolute paths when referencing temp files from `graph.apply` payloads — resolve `<repo_root>/temp/<file>` at runtime instead of hardcoding any user-specific path.
- Treat `temp/` as disposable — anything inside may be deleted or overwritten between runs.
