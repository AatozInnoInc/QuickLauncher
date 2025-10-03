"""
Search engine integration for the Quick Launcher.

This module provides a thin wrapper around a Whoosh index. It is responsible for
creating the index if it does not exist, adding documents to the index, and
performing search queries against the index.

For now the engine is a minimal stub that simply returns placeholder values. You
should expand the ``create_index`` and ``add_documents`` functions to crawl your
filesystem or other sources and add real documents to the index.
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
        if not (self.index_dir / "MAIN_WRITELOCK").exists():
            # Try to open the index if it exists
            try:
                self._ix = index.open_dir(self.index_dir)
            except Exception:
                self._ix = None
        else:
            # A lock file exists; do not try to open
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

    def search(self, query: str, limit: int = 10) -> List[str]:
        """
        Search the index for the given query string and return a list of formatted
        result strings. If the index is not available, return a placeholder result.

        :param query: Search query text.
        :param limit: Maximum number of results to return.
        :return: A list of result strings.
        """
        if self._ix is None:
            # No index available; return a stub response
            return [f"(no index) You searched for: {query}"]

        with self._ix.searcher() as searcher:
            parser = MultifieldParser(["title", "content"], schema=self._ix.schema)
            q = parser.parse(query)
            results = searcher.search(q, limit=limit)
            return [f"{r['title']} ({r['path']})" for r in results]

