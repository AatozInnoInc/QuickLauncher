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
from PySide6.QtCore import Qt

from . import search
from . import llm


class MainWindow(QWidget):
    """Main window for the Quick Launcher UI."""

    def __init__(self, index_dir: str | None = None) -> None:
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

        # Connect text change signal to search handler
        self.search_bar.textChanged.connect(self.handle_search)

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
            item = QListWidgetItem(result)
            self.results_list.addItem(item)

        # Optionally call LLM for interpretation/suggestions (stub)
        # llm_suggestion = llm.interpret_command(text)
        # if llm_suggestion:
        #     item = QListWidgetItem(f"LLM: {llm_suggestion}")
        #     item.setForeground(Qt.blue)
        #     self.results_list.addItem(item)


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
    args = parser.parse_args()

    app = QApplication([])
    window = MainWindow(index_dir=args.index_dir)
    window.show()
    app.exec()


if __name__ == "__main__":
    main()
