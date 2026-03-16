"""Manual AI integration script (not collected as pytest tests)."""

import asyncio
import os
import sys
from pathlib import Path

PROJECT_ROOT = Path(__file__).parent.parent.parent
sys.path.insert(0, str(PROJECT_ROOT))

from app.crud.card import draw_random_cards, get_card_by_id
from app.crud.prediction import batch_create_card_draws, create_prediction
from app.crud.spread import get_spread_by_name
from app.crud.user import get_user_by_username
from app.db.session import SessionLocal
from app.schemas.prediction import CardDrawCreate, PredictionCreate
from app.services.tarot_service import tarot_interpretation_service


async def run_complete_tarot_flow() -> None:
    print('Running full tarot flow integration check...')

    db = SessionLocal()
    try:
        user = get_user_by_username(db, 'demo_user')
        if not user:
            print('demo_user not found. Run init_demo_data first.')
            return

        spread = get_spread_by_name(db, '三牌占卜')
        if not spread:
            print("Spread '三牌占卜' not found.")
            return

        prediction_data = PredictionCreate(
            question='我的事业发展前景如何？',
            question_type='career',
            spread_type_id=spread.id,
        )
        prediction = create_prediction(db, user_id=user.id, prediction_create=prediction_data)

        cards = draw_random_cards(db, count=spread.card_count)
        card_draws_data = []
        for i, card in enumerate(cards):
            card_draws_data.append(
                CardDrawCreate(
                    tarot_card_id=card.id,
                    position=i,
                    is_reversed=(i % 3 == 0),
                )
            )

        card_draws = batch_create_card_draws(db, prediction_id=prediction.id, card_draws_data=card_draws_data)

        cards_data = []
        for i, draw in enumerate(card_draws):
            card = get_card_by_id(db, draw.tarot_card_id)
            cards_data.append(
                {
                    'card': card,
                    'position': f'位置{i + 1}',
                    'is_reversed': draw.is_reversed,
                }
            )

        interpretation = await tarot_interpretation_service.create_interpretation(
            db=db,
            prediction=prediction,
            cards_data=cards_data,
            user_context='我是一名软件工程师，正在考虑职业方向。',
        )
        print('Interpretation generated successfully:')
        print(interpretation)
    finally:
        db.close()


async def run_ai_service_health() -> None:
    print('Checking AI service health...')
    status = await tarot_interpretation_service.health_check()
    for key, value in status.items():
        print(f'{key}: {value}')


async def main() -> None:
    await run_ai_service_health()
    await run_complete_tarot_flow()


if __name__ == '__main__':
    asyncio.run(main())
