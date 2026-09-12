"""Initial schema

Revision ID: 0001_initial_schema
Revises:
Create Date: 2026-03-24 00:00:00
"""

from alembic import op
from sqlalchemy import text

from app.db import base  # noqa: F401
from app.db.base_class import Base

# revision identifiers, used by Alembic.
revision = "0001_initial_schema"
down_revision = None
branch_labels = None
depends_on = None


def upgrade() -> None:
    bind = op.get_bind()
    Base.metadata.create_all(bind=bind)
    bind.execute(
        text(
            "CREATE UNIQUE INDEX IF NOT EXISTS uq_card_draw_prediction_position "
            "ON card_draws (prediction_id, position)"
        )
    )
    bind.execute(
        text(
            "CREATE UNIQUE INDEX IF NOT EXISTS uq_interpretation_prediction "
            "ON interpretations (prediction_id)"
        )
    )


def downgrade() -> None:
    bind = op.get_bind()
    bind.execute(text("DROP INDEX IF EXISTS uq_card_draw_prediction_position"))
    bind.execute(text("DROP INDEX IF EXISTS uq_interpretation_prediction"))
    Base.metadata.drop_all(bind=bind)
