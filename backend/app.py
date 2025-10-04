from fastapi import FastAPI, HTTPException
from typing import Optional
from fastapi.middleware.cors import CORSMiddleware
import os
import platform
import subprocess
from quicklauncher_py.search import SearchEngine
from quicklauncher_py import llm

app = FastAPI()
# allow any origin for local dev; adjust as needed
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# Initialize search engine and index directory
INDEX_DIR = os.path.expanduser("~/.quicklauncher_index")
search_engine = SearchEngine(index_dir=INDEX_DIR)
# If index does not exist, create and index Downloads directory
if not search_engine.has_index:
    home_downloads = os.path.join(os.path.expanduser("~"), "Downloads")
    search_engine.create_index()
    if os.path.isdir(home_downloads):
        search_engine.index_directory(home_downloads)

# Load LLM (safe to wrap in try/except)
try:
    llm.load_model("Qwen/Qwen2.5-0.5B")
except Exception as e:
    print("LLM load failed:", e)

@app.get("/search")
async def search(query: Optional[str] = None, q: Optional[str] = None):
    effective_query = query or q
    if not effective_query:
        raise HTTPException(status_code=400, detail="Missing 'query' or 'q' parameter")
    results = search_engine.search(effective_query)
    return {"results": results}

@app.get("/interpret")
async def interpret(text: str):
    # Use the LLM to interpret a natural language command
    result = llm.interpret_command(text)
    return {"result": result}

@app.post("/launch")
async def launch(path: str):
    # Launch a file or directory using the host OS
    if not os.path.exists(path):
        raise HTTPException(status_code=404, detail="Path not found")
    system = platform.system()
    if system == "Windows":
        os.startfile(path)  # type: ignore
    elif system == "Darwin":
        subprocess.Popen(["open", path])
    else:
        subprocess.Popen(["xdg-open", path])
    return {"status": "ok"}
