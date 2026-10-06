"""Persist per-source guest session admission windows.

Revision ID: 0006_guest_session_rate_limit
Revises: 0005_ai_cost_priority
"""

from alembic import op
import sqlalchemy as sa


revision = "0006_guest_session_rate_limit"
down_revision = "0005_ai_cost_priority"
branch_labels = None
depends_on = None


def upgrade() -> None:
    if sa.inspect(op.get_bind()).has_table("guest_session_windows"):
        return
    op.create_table(
        "guest_session_windows",
        sa.Column("window_key", sa.String(length=13), nullable=False),
        sa.Column("source_hash", sa.String(length=64), nullable=False),
        sa.Column("issued", sa.Integer(), nullable=False, server_default="0"),
        sa.PrimaryKeyConstraint("window_key", "source_hash"),
    )


def downgrade() -> None:
    op.drop_table("guest_session_windows")
