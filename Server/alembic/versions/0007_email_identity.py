"""Backfill binary email identities without rewriting original addresses.

Revision ID: 0007_email_identity
Revises: 0006_guest_session_rate_limit
"""

from alembic import op
import sqlalchemy as sa

from app.core.email_identity import email_identity_key


revision = "0007_email_identity"
down_revision = "0006_guest_session_rate_limit"
branch_labels = None
depends_on = None


def _require_safe_connection(bind) -> None:
    if bind.dialect.name == "sqlite" and bind.exec_driver_sql("PRAGMA foreign_keys").scalar():
        raise RuntimeError(
            "Email identity migration requires a dedicated SQLite connection with foreign keys disabled. "
            "No records were changed. Use the standard Alembic connection, not an application connection."
        )


def upgrade() -> None:
    bind = op.get_bind()
    _require_safe_connection(bind)
    users = sa.table(
        "users",
        sa.column("id", sa.Integer()),
        sa.column("email", sa.String()),
        sa.column("email_key", sa.LargeBinary()),
    )
    keys = {}
    owners = {}
    invalid_ids = []
    duplicate_ids = set()
    for user_id, email in bind.execute(sa.select(users.c.id, users.c.email).order_by(users.c.id)):
        try:
            key = email_identity_key(email)
        except (TypeError, ValueError):
            invalid_ids.append(user_id)
            continue
        keys[user_id] = key
        owner = owners.setdefault(key, user_id)
        if owner != user_id:
            duplicate_ids.update((owner, user_id))

    # Raise outside validation handlers so no private exception is chained.
    problems = []
    if invalid_ids:
        problems.append("Invalid email user IDs: " + ", ".join(map(str, invalid_ids)))
    if duplicate_ids:
        problems.append(
            "Colliding email identity user IDs: " + ", ".join(map(str, sorted(duplicate_ids)))
        )
    if problems:
        raise RuntimeError(
            "Email identity migration blocked. " + "; ".join(problems)
            + ". Resolve these records before retrying; no records were changed."
        )

    inspector = sa.inspect(bind)
    columns = {column["name"]: column for column in inspector.get_columns("users")}
    column = columns.get("email_key")
    needs_backfill = column is None or column["nullable"]
    if column is None:
        op.add_column("users", sa.Column("email_key", sa.LargeBinary(), nullable=True))
    if needs_backfill:
        if keys:
            bind.execute(
                sa.update(users)
                .where(users.c.id == sa.bindparam("user_id"))
                .values(email_key=sa.bindparam("key_value")),
                [{"user_id": user_id, "key_value": key} for user_id, key in keys.items()],
            )
        with op.batch_alter_table("users") as batch_op:
            batch_op.alter_column("email_key", existing_type=sa.LargeBinary(), nullable=False)

    # 0001 creates current metadata, including this column and index on fresh DBs.
    if not any(index["name"] == "ix_users_email_key" for index in inspector.get_indexes("users")):
        op.create_index("ix_users_email_key", "users", ["email_key"], unique=True)


def downgrade() -> None:
    _require_safe_connection(op.get_bind())
    op.drop_index("ix_users_email_key", table_name="users")
    with op.batch_alter_table("users") as batch_op:
        batch_op.drop_column("email_key")
