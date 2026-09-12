"""Track interpretation generation attempts

Revision ID: 0002_interpretation_tracking
Revises: 0001_initial_schema
Create Date: 2026-09-12 00:00:00
"""

from alembic import op
import sqlalchemy as sa

# revision identifiers, used by Alembic.
revision = "0002_interpretation_tracking"
down_revision = "0001_initial_schema"
branch_labels = None
depends_on = None


def _prediction_columns() -> set[str]:
    return {column["name"] for column in sa.inspect(op.get_bind()).get_columns("predictions")}


def upgrade() -> None:
    # 0001 creates tables from the current models, so fresh databases already have these columns.
    existing = _prediction_columns()
    if "interpretation_started_at" not in existing:
        op.add_column(
            "predictions",
            sa.Column("interpretation_started_at", sa.DateTime(timezone=True), nullable=True),
        )
    if "interpretation_attempts" not in existing:
        op.add_column(
            "predictions",
            sa.Column("interpretation_attempts", sa.Integer(), nullable=False, server_default="0"),
        )


def downgrade() -> None:
    existing = _prediction_columns()
    with op.batch_alter_table("predictions") as batch_op:
        if "interpretation_attempts" in existing:
            batch_op.drop_column("interpretation_attempts")
        if "interpretation_started_at" in existing:
            batch_op.drop_column("interpretation_started_at")
