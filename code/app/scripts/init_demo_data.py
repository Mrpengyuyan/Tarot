"""Initialize demo users for local development."""

import os

from sqlalchemy.orm import Session

from app.crud.user import create_user, get_user_by_username
from app.db.session import SessionLocal
from app.schemas.user import UserCreate


def _demo_credentials():
    return {
      "admin": {
          "email": "admin@tarotgame.com",
          "password": os.getenv("DEMO_ADMIN_PASSWORD", "admin123"),
          "is_superuser": True,
      },
      "demo_user": {
          "email": "demo@tarotgame.com",
          "password": os.getenv("DEMO_USER_PASSWORD", "demo123"),
          "is_superuser": False,
      },
      "alice": {
          "email": "alice@example.com",
          "password": os.getenv("DEMO_ALICE_PASSWORD", "alice123"),
          "is_superuser": False,
      },
      "bob": {
          "email": "bob@example.com",
          "password": os.getenv("DEMO_BOB_PASSWORD", "bob123"),
          "is_superuser": False,
      },
    }


def init_demo_users(db: Session) -> None:
    creds = _demo_credentials()

    for username, info in creds.items():
      existing_user = get_user_by_username(db, username=username)
      if existing_user:
          print(f"[skip] user exists: {username}")
          continue

      created = create_user(
          db=db,
          user_create=UserCreate(
              username=username,
              email=info["email"],
              password=info["password"],
          ),
      )
      if info["is_superuser"]:
          created.is_superuser = True
          db.commit()

      print(f"[ok] created user: {username}")


def main():
    print("Initializing demo data...")
    db = SessionLocal()
    try:
      init_demo_users(db)
      print("Demo data initialization complete.")
      print("Demo usernames: admin, demo_user, alice, bob")
      print("Passwords are configured via DEMO_*_PASSWORD env vars.")
    except Exception as exc:
      print(f"Demo data initialization failed: {exc}")
      db.rollback()
    finally:
      db.close()


if __name__ == "__main__":
    main()
