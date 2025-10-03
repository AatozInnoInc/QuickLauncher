"""
Main application module for the Quick Launcher.

This module defines a simple PySide6 application with a search bar and results
list. User input is passed to the search engine and (optionally) the LLM
component to produce suggestions.
"""

from PySide6.QtWidgets import (
    QApplication,
    QWidget,
    QVBoxLayout,
    QLineEdit,
    QListWidget,
    QListWidgetItem,
    QLabel,
)
from PySide6.QtCore import Qt, QUrl
from PySide6.QtGui import QDesktopServices
from pathlib import Path

from . import search
from . import llm


class MainWindow(QWidget):
    """Main window for the Quick Launcher UI."""

    def __init__(self, index_dir: str | None = None, scan_dir: str | None = None) -> None:
        super().__init__()
        self.setWindowTitle("Quick Launcher")
        self.resize(400, 300)

        layout = QVBoxLayout(self)

        self.info_label = QLabel(
            "Type a command or search query. "
            "Matches will appear below as you type.",
            self,
        )
        self.info_label.setWordWrap(True)
        layout.addWidget(self.info_label)

        self.search_bar = QLineEdit(self)
        self.search_bar.setPlaceholderText("Search or type a command…")
        layout.addWidget(self.search_bar)

        self.results_list = QListWidget(self)
        layout.addWidget(self.results_list)

        # Initialize search engine (stub until index is built)
        self.search_engine = search.SearchEngine(index_dir=index_dir)

        # If no index is loaded and a scan directory is provided, create and populate the index
        if self.search_engine._ix is None and scan_dir:
            try:
                self.search_engine.create_index()
                self.search_engine.index_directory(scan_dir)
            except Exception as e:
                print(f"Warning: failed to build index for {scan_dir}: {e}")

        # Connect text change signal to search handler
        self.search_bar.textChanged.connect(self.handle_search)

        # Activate item double-click to open files or folders
        self.results_list.itemActivated.connect(self.open_item)

    def handle_search(self, text: str) -> None:
        """Handle updates to the search bar by querying the search engine.

        This method is called automatically when the text in the search bar changes.
        It clears the current results list and populates it with new entries based on
        the query. In a future version, this method could also call an LLM to
        interpret natural language input.
        """
        # Clear existing results
        self.results_list.clear()

        if not text.strip():
            return

        # Perform search using the Whoosh-backed engine
        for result in self.search_engine.search(text, limit=10):
            # Each result is a dict with 'title' and 'path'
            display_text = result['title']
            if result['path']:
                display_text = f"{result['title']} ({result['path']})"
            item = QListWidgetItem(display_text)
            item.setData(Qt.UserRole, result['path'])
            self.results_list.addItem(item)

        # Optionally call LLM for interpretation/suggestions (stub)
        # llm_suggestion = llm.interpret_command(text)
        # if llm_suggestion:
        #     item = QListWidgetItem(f"LLM: {llm_suggestion}")
        #     item.setForeground(Qt.blue)
        #     item.setData(Qt.UserRole, None)
        #     self.results_list.addItem(item)

    def open_item(self, item: QListWidgetItem) -> None:
        """
        Open the file or folder associated with the given list item. Uses
        QDesktopServices to launch the default application for the file type.
        """
        path = item.data(Qt.UserRole)
        if path:
            url = QUrl.fromLocalFile(path)
            QDesktopServices.openUrl(url)



def main() -> None:
    """Entry point for the quick launcher application."""
    import argparse
    parser = argparse.ArgumentParser(description="Run the Quick Launcher GUI.")
    parser.add_argument(
        "--index-dir",
        dest="index_dir",
        default=None,
        help="Path to the Whoosh index directory (defaults to none).",
    )
    parser.add_argument(
        "--scan-dir",
        dest="scan_dir",
        default=None,
        help=(
            "Directory to scan and index on startup. If omitted, the user's "
            "Downloads folder will be used."
        ),
    )
    args = parser.parse_args()

    # Determine index directory: use provided argument or default to a hidden folder in the user's home directory
    index_dir: str | None = args.index_dir
    if index_dir is None:
        index_dir = str(Path.home() / ".quicklauncher_index")

    # Determine scan directory: use provided argument or default to Downloads
    scan_dir: str | None = args.scan_dir
    if scan_dir is None:
        # Default to the user's Downloads folder if it exists
        dl = Path.home() / "Downloads"
        if dl.exists():
            scan_dir = str(dl)

    # Initialize the local LLM (optional). Catch errors but continue UI startup.
    try:
        llm.load_model("Qwen/Qwen2.5-0.5B")
    except Exception as e:
        print(f"Warning: failed to load LLM: {e}")

    app = QApplication([])
    window = MainWindow(index_dir=index_dir, scan_dir=scan_dir)
    window.show()
    app.exec()


if __name__ == "__main__":
    main()
