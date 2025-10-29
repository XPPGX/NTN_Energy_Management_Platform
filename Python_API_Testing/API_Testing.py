from __future__ import annotations

import sys

from app import AppState, bootstrap_state, create_app, create_state

state: AppState = create_state()
app = create_app(state)


def initialize_state() -> None:
    """Load default JSON fixtures into the in-memory stores."""
    bootstrap_state(state)


# Initialize immediately so the Flask app works when imported by a WSGI server.
initialize_state()


if __name__ == "__main__":
    print(sys.executable)
    app.run(host="0.0.0.0", port=5050, debug=False)
