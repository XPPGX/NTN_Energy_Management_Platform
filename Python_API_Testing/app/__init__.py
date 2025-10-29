from __future__ import annotations

from flask import Flask

from .config import DEFAULT_MEMORY_FILE, DEFAULT_REAL_WRITE_FILE
from .state import AppState
from .routes import create_routes


def create_state() -> AppState:
    return AppState()


def bootstrap_state(state: AppState) -> None:
    state.memory_init(DEFAULT_MEMORY_FILE)
    state.real_write_memory_init(DEFAULT_REAL_WRITE_FILE)


def create_app(state: AppState | None = None) -> Flask:
    state = state or create_state()
    app = Flask(__name__)
    app.config["JSONIFY_PRETTYPRINT_REGULAR"] = False
    app.register_blueprint(create_routes(state))
    app.state = state  # type: ignore[attr-defined]
    return app


__all__ = ["AppState", "create_app", "create_state", "bootstrap_state"]
