<!--
  Quick Launcher Python project
-->
# Quick Launcher (Python)

This repository contains a minimal proof-of-concept for a **Quick Launcher**
application implemented in Python. It is intended to demonstrate your ability
to build an extensible desktop utility that integrates local search,
machine‑learning inference, and a modern GUI.

## Features

- **Pythonic core** – written in Python 3.11 with type hints for clarity.
- **GUI with PySide6** – a sleek, native desktop interface using Qt.
- **Search with Whoosh** – a lightweight full‑text search engine for indexing
  applications, files, and workflow shortcuts.
- **Local LLM integration** – a stub module ready to load a multilingual model
  (e.g. Qwen2.5-0.5B) from HuggingFace to parse and suggest commands.

## Getting Started

1. **Install dependencies** (preferably in a virtual environment):

   ```bash
   pip install -r requirements.txt
   ```

2. **(Optional) Download a local LLM**  
   To enable natural‑language command interpretation you need a local model. You can
   use the multilingual Qwen 0.5B model as an example:

   ```bash
   python - <<'PY'
   from quicklauncher_py import llm
   llm.load_model("Qwen/Qwen2.5-0.5B")
   print("Model loaded.")
   PY
   ```

   > **Note:** Large language models require significant resources; ensure your
   > machine has enough CPU/GPU and memory to load the model.

3. **Create or load an index**  
   Edit `quicklauncher_py/search.py` to add real documents (e.g. files or
   commands) to the Whoosh index. Then create the index:

   ```bash
   python - <<'PY'
   from quicklauncher_py import search
   eng = search.SearchEngine(index_dir="index")
   eng.create_index()
   # Populate eng.add_documents([...]) here
   PY
   ```

4. **Run the application**:

   ```bash
   python quicklauncher_py/app.py --index-dir index
   ```

## Directory Structure

- `quicklauncher_py/` – application package
  - `app.py` – PySide6 GUI and event handling.
  - `search.py` – Whoosh index management and query functions.
  - `llm.py` – stubs for loading and using a local LLM.
- `requirements.txt` – Python dependencies.
- `README.md` – project overview and setup instructions.

## Next Steps

This starter project provides the bare minimum to get up and running. To
demonstrate more advanced capabilities you can:

- Integrate the LLM to parse complex commands and adapt suggestions based on
  prior usage.
- Expand the search engine to index your filesystem, installed applications, or
  custom workflows.
- Implement learning logic that tracks how frequently you use commands and
  reorders results accordingly.

Contributions and feedback are welcome!
