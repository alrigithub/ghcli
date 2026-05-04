# Samples

Runnable end-to-end GhCLI sample definitions.

Each sample folder follows:

```text
samples/<sample-name>/
  script.py
  graph.json
```

Rules:

- folder names use kebab-case
- Python file is always `script.py`
- graph payload is always `graph.json`
- `graph.json` references `script.py` by absolute `file_path`
- each Python node exposes `dbg`

Run a sample:

```powershell
src\GhCLI\bin\Debug\net8.0\GhCLI.exe graph.apply --file samples\<sample-name>\graph.json --timeout-ms 15000
```
