from setuptools import setup, find_packages

setup(
    name="tarot-game",
    version="1.0.0",
    description="塔罗牌游戏后端API",
    author="Tarot Game Developer",
    author_email="developer@tarotgame.com",
    packages=find_packages(),
    install_requires=[
        # FastAPI
        "fastapi==0.111.0",
        "uvicorn[standard]==0.29.0",
        "python-decouple==3.8",
        
        # Database
        "sqlalchemy==2.0.30",
        "psycopg2-binary==2.9.9",
        "alembic==1.13.1",
        
        # Pydantic
        "pydantic==2.7.1",
        "pydantic-settings==2.2.1",
        
        # Security
        "python-jose[cryptography]==3.3.0",
        "passlib[bcrypt]==1.7.4",
        
        # API Client
        "httpx==0.27.0",
        
        # Email validation
        "email-validator==2.1.0"
    ],
    python_requires=">=3.8",
    classifiers=[
        "Development Status :: 3 - Alpha",
        "Intended Audience :: Developers",
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3.8",
        "Programming Language :: Python :: 3.9",
        "Programming Language :: Python :: 3.10",
        "Programming Language :: Python :: 3.11",
    ],
) 