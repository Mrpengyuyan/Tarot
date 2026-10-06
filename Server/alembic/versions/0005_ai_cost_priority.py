"""Track possibly sent AI requests across workers and restarts.

Revision ID: 0005_ai_cost_priority
Revises: 0004_persistent_ai_budget
"""

from alembic import op
import sqlalchemy as sa


revision = "0005_ai_cost_priority"
down_revision = "0004_persistent_ai_budget"
branch_labels = None
depends_on = None


def upgrade() -> None:
    inspector = sa.inspect(op.get_bind())
    columns = {column["name"] for column in inspector.get_columns("predictions")}
    with op.batch_alter_table("predictions") as batch:
        if "ai_run_state" not in columns:
            batch.add_column(sa.Column("ai_run_state", sa.String(32), nullable=False, server_default="not_started"))
        if "ai_requests_started" not in columns:
            batch.add_column(sa.Column("ai_requests_started", sa.Integer(), nullable=False, server_default="0"))
        if "ai_request_started_at" not in columns:
            batch.add_column(sa.Column("ai_request_started_at", sa.DateTime(timezone=True), nullable=True))
        if "ai_budget_retry_at" not in columns:
            batch.add_column(sa.Column("ai_budget_retry_at", sa.DateTime(timezone=True), nullable=True))
        if "ai_last_error" not in columns:
            batch.add_column(sa.Column("ai_last_error", sa.String(32), nullable=True))

    predictions = sa.table(
        "predictions",
        sa.column("interpretation_attempts", sa.Integer()),
        sa.column("interpretation_started_at", sa.DateTime(timezone=True)),
        sa.column("ai_run_state", sa.String(32)),
        sa.column("ai_requests_started", sa.Integer()),
        sa.column("ai_request_started_at", sa.DateTime(timezone=True)),
        sa.column("status", sa.String(32)),
    )
    bind = op.get_bind()
    # This migration runs with old workers stopped. Existing attempts are conservatively
    # treated as possibly sent, even if the old process never reached the provider.
    bind.execute(
        sa.update(predictions)
        .where(predictions.c.interpretation_attempts > 0)
        .values(
            ai_requests_started=sa.case((predictions.c.interpretation_attempts >= 2, 2), else_=1),
            ai_request_started_at=predictions.c.interpretation_started_at,
            ai_run_state=sa.case(
                (predictions.c.interpretation_attempts >= 2, "exhausted"), else_="retry_required"
            ),
        )
    )
    bind.execute(
        sa.text(
            "UPDATE predictions SET ai_run_state = 'completed' "
            "WHERE EXISTS (SELECT 1 FROM interpretations WHERE interpretations.prediction_id = predictions.id)"
        )
    )


def downgrade() -> None:
    columns = {column["name"] for column in sa.inspect(op.get_bind()).get_columns("predictions")}
    with op.batch_alter_table("predictions") as batch:
        for name in (
            "ai_last_error", "ai_budget_retry_at", "ai_request_started_at",
            "ai_requests_started", "ai_run_state",
        ):
            if name in columns:
                batch.drop_column(name)
