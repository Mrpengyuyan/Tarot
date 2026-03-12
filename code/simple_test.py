#!/usr/bin/env python3
"""Basic smoke tests."""

import os
import sys

if sys.platform.startswith('win'):
    os.environ['PYTHONIOENCODING'] = 'utf-8'

sys.path.insert(0, os.path.dirname(__file__))


def test_basic_config():
    from app.core.config import settings

    assert settings.PROJECT_NAME
    assert settings.API_V1_STR.startswith('/')
    assert settings.DATABASE_URL


def test_database():
    from app.db.base_class import Base
    from app.db.session import engine

    Base.metadata.create_all(bind=engine)
    assert engine is not None


def test_imports():
    from app.services.enhanced_tarot_interpretation import enhanced_tarot_interpretation_service

    assert enhanced_tarot_interpretation_service is not None


if __name__ == '__main__':
    # Optional manual smoke execution.
    test_basic_config()
    test_database()
    test_imports()
    print('All checks passed.')
