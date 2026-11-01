# Project map

For codebase work, consult `graphify-out/GRAPH_TREE.html`, `graphify-out/GRAPH_REPORT.md` and query `graphify-out/graph.json` first. Use the map to locate relevant modules, then read the actual source before editing; the map is not runtime/test evidence.

After changing source or dependency/config code, run the interpreter in `graphify-out/.graphify_python` with `Tools/update_project_map.py`. It compares content hashes and does nothing when code/config is unchanged. Do not rebuild for questions, documentation-only edits, or merely opening a session. Do not start a background watcher or paid semantic API automatically.

If `Docs/Network_Architecture.vi.md` changes, re-extract its semantic fragment on the next code-map update; do not treat stale document nodes as verified implementation. Review parser/graph health warnings in the report. Keep existing browser/delegation instructions from the user's session.
