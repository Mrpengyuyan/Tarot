# 小范围公测部署指南（低预算 + 自动 HTTPS）

本目录提供一套极简的单机 Docker Compose 部署方案（FastAPI + SQLite 持久卷 + Caddy 自动 Let's Encrypt HTTPS），专为**自费 DeepSeek API、面向朋友/小圈子试玩**设计。

## 1. 准备生产环境变量

在服务器上的 `Server/` 目录执行：

```bash
cp .env.production.example .env
cp deploy/Caddyfile.example deploy/Caddyfile
```

编辑 `Server/.env`，只改 4 处必填项：

1. `SECRET_KEY`：执行 `python3 -c "import secrets; print(secrets.token_urlsafe(48))"` 生成随机密钥。
2. `DEEPSEEK_API_KEY`：填入你的 DeepSeek API Key（建议同时在 DeepSeek 控制台设月度消费上限）。
3. `CORS_ORIGINS`：改为你的正式域名，例如 `https://tarot.yourdomain.com`。
4. `AI_DAILY_BUDGET_USD` / `AI_MONTHLY_BUDGET_USD` / `GUEST_DAILY_READING_LIMIT`：默认已设保守值（每日 \$1、每月 \$10、每人每日 2 次、关闭 reasoner 回退），可按需再调低。

编辑 `Server/deploy/Caddyfile`，将 `tarot.example.com` 替换为你的真实域名（并确保域名 A 记录已解析到服务器公网 IP，防火墙放行 80/443 端口）。

## 2. 上线前配置自检

```bash
python3 check_release_hygiene.py
python3 check_config.py --production-check
```

## 3. 初始化数据库并启动服务

```bash
docker compose -f deploy/docker-compose.prod.yml build
docker compose -f deploy/docker-compose.prod.yml run --rm tarot-api sh -c "alembic upgrade head && python -m app.scripts.init_tarot_data"
docker compose -f deploy/docker-compose.prod.yml up -d
```

验证服务已通过 HTTPS 正常响应：

```bash
curl https://tarot.yourdomain.com/api/v1/health/
```

## 4. 切换桌面发行包后端地址

打包发给朋友试玩时，将构建产物中的 `tarot_desktop_config.json`（位于 `StreamingAssets/`）改为：

```json
{
  "backendBaseUrl": "https://tarot.yourdomain.com/api/v1",
  "requestTimeoutSeconds": 15
}
```

> 若只希望分享**零费用纯离线试玩包**，无需部署服务器：玩家启动后在右上角“设置 → 解读模式”切换为“离线牌义”，或默认连接不到后端时也会自动回退到本地 78 张牌义解读。
