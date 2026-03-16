import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Box,
  Button,
  Chip,
  Container,
  Divider,
  Paper,
  Typography,
} from '@mui/material';
import {
  ArrowBack,
  AutoAwesome,
  Favorite,
  Star,
} from '@mui/icons-material';
import { useNavigate, useParams } from 'react-router-dom';
import CardSpread from '../../components/Tarot/CardSpread';
import { ROUTES } from '../../routes/routeConfig';
import { tarotService, CardDrawWithMeaning, DrawnCard, ReadingDetail, localDrawingUtils } from '../../services/tarotService';
import { SpreadType } from '../../types/api';
import { formatDateTime } from '../../utils/dateUtils';
import Loading from '../../components/UI/Loading';
import { useNotification } from '../../components/UI/Notification';
import CosmicBackground from '../../components/Effects/CosmicBackground';

const questionTypeLabels: Record<string, { label: string; color: string }> = {
  love: { label: '感情', color: '#FF6B9D' },
  career: { label: '事业', color: '#4ECDC4' },
  finance: { label: '财运', color: '#FFD93D' },
  health: { label: '健康', color: '#6BCF7F' },
  general: { label: '综合', color: '#AB83A1' },
};

const statusLabels: Record<string, { label: string; color: 'default' | 'success' | 'warning' | 'error' }> = {
  completed: { label: '已完成', color: 'success' },
  processing: { label: '解读中', color: 'warning' },
  pending: { label: '待抽牌', color: 'default' },
  failed: { label: '失败', color: 'error' },
};

const ReadingDetailPage: React.FC = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { showError } = useNotification();
  const [reading, setReading] = useState<ReadingDetail | null>(null);
  const [cards, setCards] = useState<CardDrawWithMeaning[]>([]);
  const [spread, setSpread] = useState<SpreadType | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [aiGenerating, setAiGenerating] = useState(false);
  const [aiPolling, setAiPolling] = useState(false);

  const readingId = useMemo(() => {
    const parsed = Number(id);
    return Number.isNaN(parsed) ? null : parsed;
  }, [id]);

  const fetchReadingDetail = useCallback(
    async (targetReadingId: number) => {
      const [detail, cardDraws] = await Promise.all([
        tarotService.getReadingDetailInfo(targetReadingId),
        tarotService.getReadingCards(targetReadingId),
      ]);

      setReading(detail);
      setCards(cardDraws);

      if (detail.spread_type) {
        setSpread(detail.spread_type as SpreadType);
      } else if (detail.spread_type_id) {
        const spreadDetail = await tarotService.getSpreadDetail(detail.spread_type_id);
        setSpread(spreadDetail);
      }
    },
    [],
  );

  useEffect(() => {
    if (!id || readingId === null) {
      setError('无效的占卜记录');
      setLoading(false);
      return;
    }

    const fetchDetail = async () => {
      setLoading(true);
      setError(null);
      try {
        await fetchReadingDetail(readingId);
      } catch (err) {
        const message = err instanceof Error ? err.message : '获取占卜详情失败';
        setError(message);
        showError(message);
      } finally {
        setLoading(false);
      }
    };

    fetchDetail();
  }, [fetchReadingDetail, id, readingId, showError]);

  useEffect(() => {
    if (!readingId || !reading || reading.interpretation || !['pending', 'processing'].includes(reading.status)) {
      setAiPolling(false);
      return;
    }

    let cancelled = false;
    let attempts = 0;
    const maxAttempts = 15;
    setAiPolling(true);

    const interval = window.setInterval(async () => {
      if (cancelled) return;
      attempts += 1;
      try {
        const detail = await tarotService.getReadingDetailInfo(readingId);
        if (detail.interpretation) {
          setReading(detail);
          window.clearInterval(interval);
          setAiPolling(false);
          return;
        }

        if (!['pending', 'processing'].includes(detail.status) || attempts >= maxAttempts) {
          window.clearInterval(interval);
          setAiPolling(false);
        }
      } catch {
        if (attempts >= maxAttempts) {
          window.clearInterval(interval);
          setAiPolling(false);
        }
      }
    }, 4000);

    return () => {
      cancelled = true;
      window.clearInterval(interval);
      setAiPolling(false);
    };
  }, [reading, readingId]);

  const handleGenerateInterpretation = useCallback(async () => {
    if (!readingId) return;

    setAiGenerating(true);
    try {
      await tarotService.createInterpretation(readingId, {
        forceAI: true,
        timeoutMs: 65000,
      });
      await fetchReadingDetail(readingId);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'AI 解读生成失败';
      showError(message);
    } finally {
      setAiGenerating(false);
    }
  }, [fetchReadingDetail, readingId, showError]);

  const drawnCards = useMemo<DrawnCard[]>(() => {
    if (!cards.length) return [];
    const ordered = cards.slice().sort((a, b) => a.position - b.position);
    return ordered.reduce<DrawnCard[]>((acc, draw, index) => {
      if (!draw.tarot_card) {
        return acc;
      }

      acc.push({
        card: {
          ...draw.tarot_card,
          image_url: localDrawingUtils.generateCardImageUrl(draw.tarot_card),
        },
        isReversed: draw.is_reversed,
        position: index,
      });

      return acc;
    }, []);
  }, [cards]);

  const interpretationThemes = useMemo(() => {
    if (!reading?.interpretation) return [];
    const themeList = (reading.interpretation as any).key_themes_list;
    if (Array.isArray(themeList)) {
      return themeList.filter(Boolean);
    }
    if (reading.interpretation.key_themes) {
      return reading.interpretation.key_themes
        .split(',')
        .map((item) => item.trim())
        .filter(Boolean);
    }
    return [];
  }, [reading?.interpretation]);

  if (loading) {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Loading message="正在加载占卜详情..." />
      </Container>
    );
  }

  if (error || !reading) {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <Typography variant="h6" sx={{ mb: 2, color: 'error.main' }}>
            {error || '暂无占卜详情'}
          </Typography>
          <Button variant="outlined" onClick={() => navigate(ROUTES.HISTORY)}>
            返回历史
          </Button>
        </Paper>
      </Container>
    );
  }

  const typeMeta = questionTypeLabels[reading.question_type] || { label: reading.question_type, color: '#D4AF37' };
  const statusMeta = statusLabels[reading.status] || { label: reading.status, color: 'default' as const };

  return (
    <Box className="hd-noise-overlay" sx={{
      width: '100vw',
      minHeight: '100vh',
      position: 'relative',
      left: '50%',
      right: '50%',
      marginLeft: '-50vw',
      marginRight: '-50vw',
      pt: { xs: 2, md: 4 },
      pb: { xs: 8, md: 4 }
    }}>
      <CosmicBackground showRings={false} />

      <Container maxWidth="lg" sx={{ py: 4, position: 'relative', zIndex: 1, '& .MuiPaper-root': { backdropFilter: 'blur(16px)', background: 'rgba(16, 8, 32, 0.7)', border: '1px solid rgba(0, 240, 255, 0.15)' } }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 3 }}>
          <Button
            variant="outlined"
            startIcon={<ArrowBack />}
            onClick={() => navigate(ROUTES.HISTORY)}
          >
            返回历史
          </Button>
          <Chip
            label={statusMeta.label}
            color={statusMeta.color}
            size="small"
          />
        </Box>

        <Paper sx={{ p: 4, mb: 4 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
            <AutoAwesome sx={{ color: 'primary.main' }} />
            <Typography variant="h5" sx={{ fontFamily: 'Cinzel, serif', fontWeight: 700 }}>
              占卜详情
            </Typography>
            {reading.is_favorite && <Favorite sx={{ color: 'primary.main' }} fontSize="small" />}
          </Box>

          <Typography variant="h6" sx={{ mb: 1 }}>
            {reading.question}
          </Typography>
          <Typography variant="body2" sx={{ color: 'text.secondary', mb: 2 }}>
            创建于 {formatDateTime(reading.created_at)}
          </Typography>

          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 2 }}>
            <Chip
              label={typeMeta.label}
              size="small"
              variant="outlined"
              sx={{ borderColor: typeMeta.color, color: typeMeta.color }}
            />
            {reading.user_rating !== undefined && reading.user_rating !== null && (
              <Chip
                label={`评分 ${reading.user_rating}`}
                size="small"
                icon={<Star />}
                variant="outlined"
              />
            )}
          </Box>

          {reading.user_notes && (
            <Box sx={{ mt: 2 }}>
              <Typography variant="subtitle2" sx={{ mb: 1 }}>
                我的备注
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                {reading.user_notes}
              </Typography>
            </Box>
          )}
        </Paper>

        {spread && drawnCards.length > 0 && (
          <Paper sx={{ p: 3, mb: 4 }}>
            <CardSpread
              spread={spread}
              drawnCards={drawnCards}
              isRevealing={false}
              revealedPositions={drawnCards.map((_, index) => index)}
            />
          </Paper>
        )}

        <Paper sx={{ p: 4, mb: 4 }}>
          <Typography variant="h6" sx={{ mb: 2 }}>
            抽牌详情
          </Typography>
          {cards.length === 0 ? (
            <Typography variant="body2" sx={{ color: 'text.secondary' }}>
              尚未抽牌
            </Typography>
          ) : (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              {cards
                .slice()
                .sort((a, b) => a.position - b.position)
                .map((draw) => (
                  <Paper key={draw.id} sx={{ p: 2 }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', flexWrap: 'wrap', gap: 1 }}>
                      <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                        {draw.card_meaning?.name_zh || draw.tarot_card?.name_zh || '未知牌面'}
                      </Typography>
                      <Chip
                        size="small"
                        label={draw.is_reversed ? '逆位' : '正位'}
                        color={draw.is_reversed ? 'warning' : 'success'}
                      />
                    </Box>
                    <Typography variant="body2" sx={{ color: 'text.secondary', mt: 1 }}>
                      {draw.position_name ? `${draw.position_name}（位置 ${draw.position}）` : `位置 ${draw.position}`}
                    </Typography>
                    {draw.position_meaning && (
                      <Typography variant="body2" sx={{ color: 'text.secondary', mt: 1 }}>
                        {draw.position_meaning}
                      </Typography>
                    )}
                    <Divider sx={{ my: 1.5 }} />
                    <Typography variant="body2" sx={{ lineHeight: 1.7 }}>
                      {draw.card_meaning?.meaning || '暂无牌意'}
                    </Typography>
                  </Paper>
                ))}
            </Box>
          )}
        </Paper>

        <Paper sx={{ p: 4 }}>
          <Typography variant="h6" sx={{ mb: 2 }}>
            AI 解读
          </Typography>
          {!reading.interpretation ? (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                {aiPolling ? 'AI 解读生成中，正在自动刷新...' : '暂无 AI 解读'}
              </Typography>
              <Box>
                <Button
                  variant="outlined"
                  onClick={handleGenerateInterpretation}
                  disabled={aiGenerating}
                >
                  {aiGenerating ? '正在生成...' : '生成/重试 AI 解读'}
                </Button>
              </Box>
            </Box>
          ) : (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Box>
                <Typography variant="subtitle1" sx={{ mb: 1, fontWeight: 600 }}>
                  总览
                </Typography>
                <Typography variant="body2" sx={{ lineHeight: 1.7 }}>
                  {reading.interpretation.overall_interpretation}
                </Typography>
              </Box>

              {interpretationThemes.length > 0 && (
                <Box>
                  <Typography variant="subtitle1" sx={{ mb: 1, fontWeight: 600 }}>
                    主题关键词
                  </Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                    {interpretationThemes.map((theme) => (
                      <Chip key={theme} label={theme} size="small" variant="outlined" />
                    ))}
                  </Box>
                </Box>
              )}

              {reading.interpretation.card_analysis && (
                <Box>
                  <Typography variant="subtitle1" sx={{ mb: 1, fontWeight: 600 }}>
                    逐牌分析
                  </Typography>
                  <Typography variant="body2" sx={{ lineHeight: 1.7 }}>
                    {reading.interpretation.card_analysis}
                  </Typography>
                </Box>
              )}

              {reading.interpretation.relationship_analysis && (
                <Box>
                  <Typography variant="subtitle1" sx={{ mb: 1, fontWeight: 600 }}>
                    牌面关系
                  </Typography>
                  <Typography variant="body2" sx={{ lineHeight: 1.7 }}>
                    {reading.interpretation.relationship_analysis}
                  </Typography>
                </Box>
              )}

              {reading.interpretation.advice && (
                <Box>
                  <Typography variant="subtitle1" sx={{ mb: 1, fontWeight: 600 }}>
                    建议
                  </Typography>
                  <Typography variant="body2" sx={{ lineHeight: 1.7 }}>
                    {reading.interpretation.advice}
                  </Typography>
                </Box>
              )}

              {reading.interpretation.warning && (
                <Box>
                  <Typography variant="subtitle1" sx={{ mb: 1, fontWeight: 600 }}>
                    提醒
                  </Typography>
                  <Typography variant="body2" sx={{ lineHeight: 1.7 }}>
                    {reading.interpretation.warning}
                  </Typography>
                </Box>
              )}

              {reading.interpretation.summary && (
                <Box>
                  <Typography variant="subtitle1" sx={{ mb: 1, fontWeight: 600 }}>
                    总结
                  </Typography>
                  <Typography variant="body2" sx={{ lineHeight: 1.7 }}>
                    {reading.interpretation.summary}
                  </Typography>
                </Box>
              )}
            </Box>
          )}
        </Paper>
      </Container>
    </Box>
  );
};

export default ReadingDetailPage;
