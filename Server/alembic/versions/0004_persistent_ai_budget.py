"""Persist paid AI request reservations and budget windows.

Revision ID: 0004_persistent_ai_budget
Revises: 0003_reading_quota
"""

from alembic import op
import sqlalchemy as sa


revision = "0004_persistent_ai_budget"
down_revision = "0003_reading_quota"
branch_labels = None
depends_on = None


def upgrade() -> None:
    tables = set(sa.inspect(op.get_bind()).get_table_names())
    if "ai_budget_windows" not in tables:
        op.create_table(
            "ai_budget_windows",
            sa.Column("period_key", sa.String(32), primary_key=True),
            sa.Column("spent_micros", sa.BigInteger(), nullable=False, server_default="0"),
            sa.Column("reserved_micros", sa.BigInteger(), nullable=False, server_default="0"),
        )
    if "ai_budget_reservations" not in tables:
        op.create_table(
            "ai_budget_reservations",
            sa.Column("id", sa.String(36), primary_key=True),
            sa.Column("day_key", sa.String(32), nullable=False),
            sa.Column("month_key", sa.String(32), nullable=False),
            sa.Column("model", sa.String(100), nullable=False),
            sa.Column("estimated_micros", sa.BigInteger(), nullable=False),
            sa.Column("actual_micros", sa.BigInteger(), nullable=True),
            sa.Column("created_at", sa.DateTime(timezone=True), nullable=False),
        )


def downgrade() -> None:
    tables = set(sa.inspect(op.get_bind()).get_table_names())
    if "ai_budget_reservations" in tables:
        op.drop_table("ai_budget_reservations")
    if "ai_budget_windows" in tables:
        op.drop_table("ai_budget_windows")
