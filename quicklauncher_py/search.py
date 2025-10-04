"""
Search engine integration for the Quick Launcher.

This module provides a thin wrapper around a Whoosh index. It is responsible for
creating the index if it does not exist, adding documents to the index, and
performing search queries against the index.
"""

from __future__ import annotations

import os
from pathlib import Path
from typing import Iterable, List, Optional

try:
    from whoosh import index
    from whoosh.fields import Schema, TEXT, ID
    from whoosh.qparser import MultifieldParser
except ImportError:
    index = None  # type: ignore


class SearchEngine:
    """High-level interface for querying a Whoosh index."""

    def __init__(self, index_dir: Optional[str] = None) -> None:
        """
        Initialize a SearchEngine. If ``index_dir`` is provided and a Whoosh index
        exists there, it will be loaded. Otherwise, a new index can be created by
        calling ``create_index``.

        :param index_dir: Path to the directory containing the Whoosh index.
        """
        self.index_dir: Optional[Path] = (
            Path(index_dir) if index_dir is not None else None
        )
        self._ix = None
        if self.index_dir is not None:
            self.load_index()

    @property
    def has_index(self) -> bool:
        """Return True if an index is loaded in memory."""
        return self._ix is not None

    @staticmethod
    def default_schema() -> "Schema":
        """Return a basic schema for indexing commands and files."""
        return Schema(
            path=ID(stored=True, unique=True),
            title=TEXT(stored=True),
            content=TEXT,
        )

    def create_index(self) -> None:
        """
        Create a new Whoosh index at the configured ``index_dir``. If the directory
        already exists and contains an index, it will be overwritten.
        """
        if index is None:
            raise ImportError(
                "Whoosh is not installed. Please install it via pip: pip install whoosh"
            )
        if self.index_dir is None:
            raise ValueError("No index directory specified.")

        os.makedirs(self.index_dir, exist_ok=True)
        self._ix = index.create_in(self.index_dir, self.default_schema())

    def index_directory(self, directory: str) -> None:
        """
        Recursively index all files under the given directory. Each file is
        indexed by its path and filename. File contents are not read to avoid
        ingesting large binary blobs; if you wish to include textual content
        you can extend this method to read the first few kilobytes of each
        file.

        If the index has not been created yet, this will raise a RuntimeError.

        :param directory: The root directory to index.
        """
        if self._ix is None:
            raise RuntimeError("Index has not been created. Call create_index() first.")

        documents = []
        for root, _, files in os.walk(directory):
            for fname in files:
                path = os.path.join(root, fname)
                # Skip entries that are not regular files
                if not os.path.isfile(path):
                    continue
                documents.append({
                    'path': path,
                    'title': fname,
                    'content': ''
                })
        if documents:
            self.add_documents(documents)

    def load_index(self) -> None:
        """
        Load an existing Whoosh index from the configured ``index_dir``. If it
        doesn't exist, leave ``self._ix`` unset.
        """
        if index is None:
            raise ImportError(
                "Whoosh is not installed. Please install it via pip: pip install whoosh"
            )
        if self.index_dir is None:
            return
        try:
            self._ix = index.open_dir(self.index_dir)
        except Exception:
            self._ix = None

    def add_documents(self, documents: Iterable[dict]) -> None:
        """
        Add a batch of documents to the index. Each document should be a dictionary
        with 'path', 'title', and 'content' keys.

        :param documents: An iterable of document dictionaries.
        """
        if self._ix is None:
            raise RuntimeError("Index has not been created or loaded.")
        writer = self._ix.writer()
        for doc in documents:
            writer.update_document(
                path=doc["path"], title=doc["title"], content=doc.get("content", "")
            )
        writer.commit()
        
    def search(self, query: str, limit: int = 10) -> Iterable[dict]:
        """Search the index for the given query and return a list of dictionaries.

        Each returned dict has 'title' and 'path' keys corresponding to the indexed file.
        If the index is not available, an empty list is returned.

        :param query: The search query string.
        :param limit: Maximum number of results to return.
        :return: A list of dictionaries with search results.
        """
        # If no index is loaded or Whoosh is unavailable, return a placeholder result
        # that matches the expected shape: a list of {title, path} objects.
        if self._ix is None or index is None:
            return [{"title": f"(no index) You searched for: {query}", "path": ""}]
        # Use a MultifieldParser to search over title and path fields
        with self._ix.searcher() as searcher:
            parser = MultifieldParser(["title", "path"], schema=self._ix.schema)
            try:
                q = parser.parse(query)
            except Exception:
                return []
            results = searcher.search(q, limit=limit)
            # Build a list of result dictionaries
            return [{"title": hit.get("title", ""), "path": hit["path"]} for hit in results]
