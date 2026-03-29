import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { ArrowBack, AutoAwesome, Star } from 'icons';
import {
  Box,
  Button,
  Chip,
  Container,
  Divider,
  Paper,
  Typography,
} from '@mui/material';
import { useNavigate, useParams } from 'react-router-dom';
import CardSpread from '../../components/Tarot/CardSpread';
import CosmicBackground from '../../components/Effects/CosmicBackground';
import ReadingFavoriteButton from '../../components/Reading/ReadingFavoriteButton';
import Loading from '../../components/UI/Loading';
import { useNotification } from '../../components/UI/Notification';
import { ROUTES } from '../../routes/routeConfig';
import {
  CardDrawWithMeaning,
  DrawnCard,
  localDrawingUtils,
  ReadingDetail,
  tarotService,
} from '../../services/tarotService';
import { getQuestionTypeLabel } from '../../stores/gameStore';
import { SpreadType } from '../../types/api';
import { formatDateTime } from '../../utils/dateUtils';
import { getReadingQuestionDisplay } from '../../utils/readingQuestion';
import {
  getSpreadDisplayDescription,
  getSpreadDisplayName,
} from '../../utils/spreadDisplay';
import { useVisualSettings } from '../../hooks/useVisualSettings';

const questionTypeColors: Record<string, string> = {
  love: '#FF6B9D',
  career: '#4ECDC4',
  finance: '#FFD93D',
  health: '#6BCF7F',
  general: '#AB83A1',
};

const statusLabels: Record<
  string,
  { label: string; color: 'default' | 'success' | 'warning' | 'error' }
> = {
  completed: { label: '已完成', color: 'success' },
  processing: { label: '处理中', color: 'warning' },
  pending: { label: '等待中', color: 'default' },
  failed: { label: '失败', color: 'error' },
};

const ReadingDetailPage: React.FC = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { showError } = useNotification();
  const showErrorRef = useRef(showError);
  const detailLoadRequestRef = useRef(0);
  const visual = useVisualSettings();

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

  useEffect(() => {
    showErrorRef.current = showError;
  }, [showError]);

  const fetchReadingDetail = useCallback(async (targetReadingId: number) => {
    const [detail, cardDraws] = await Promise.all([
      tarotService.getReadingDetailInfo(targetReadingId),
      tarotService.getReadingCards(targetReadingId),
    ]);

    setReading(detail);
    setCards(cardDraws);

    if (detail.spread_type) {
      setSpread(detail.spread_type as SpreadType);
      return;
    }

    if (detail.spread_type_id) {
      const spreadDetail = await tarotService.getSpreadDetail(detail.spread_type_id);
      setSpread(spreadDetail);
    }
  }, []);

  useEffect(() => {
    if (!id || readingId === null) {
      setError('无效的占卜记录 ID');
      setLoading(false);
      return;
    }

    let cancelled = false;
    const requestId = detailLoadRequestRef.current + 1;
    detailLoadRequestRef.current = requestId;

    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        await fetchReadingDetail(readingId);
        if (cancelled || requestId !== detailLoadRequestRef.current) {
          return;
        }
      } catch (err) {
        if (cancelled || requestId !== detailLoadRequestRef.current) {
          return;
        }
        const message = err instanceof Error ? err.message : '获取占卜详情失败';
        setError(message);
        showErrorRef.current(message);
      } finally {
        if (!cancelled && requestId === detailLoadRequestRef.current) {
          setLoading(false);
        }
      }
    };

    void load();
    return () => {
      cancelled = true;
    };
  }, [fetchReadingDetail, id, readingId]);

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
      if (cancelled) {
        return;
      }

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
    if (!readingId) {
      return;
    }

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

    return cards
      .slice()
      .sort((a, b) => a.position - b.position)
      .reduce<DrawnCard[]>((acc, draw, index) => {
        if (!draw.tarot_card) return acc;

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

    const themeList = (reading.interpretation as ReadingDetail['interpretation'] & { key_themes_list?: string[] })?.key_themes_list;
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
            返回历史记录
          </Button>
        </Paper>
      </Container>
    );
  }

  const questionTypeLabel = getQuestionTypeLabel(reading.question_type as any);
  const questionTypeColor = questionTypeColors[reading.question_type] || '#D4AF37';
  const statusMeta = statusLabels[reading.status] || {
    label: reading.status,
    color: 'default' as const,
  };

  return (
    <Box
      className="hd-noise-overlay"
      sx={{
        width: '100vw',
        minHeight: '100vh',
        position: 'relative',
        left: '50%',
        right: '50%',
        marginLeft: '-50vw',
        marginRight: '-50vw',
        pt: { xs: 2, md: 4 },
        pb: { xs: 8, md: 4 },
      }}
    >
      <CosmicBackground showRings={false} performanceMode={visual.backgroundMode} />

      <Container
        maxWidth="lg"
        sx={{
          py: 4,
          position: 'relative',
          zIndex: 1,
          '& .MuiPaper-root': {
            backdropFilter: 'blur(16px)',
            background: 'rgba(16, 8, 32, 0.7)',
            border: '1px solid rgba(0, 240, 255, 0.15)',
          },
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 3, gap: 2, flexWrap: 'wrap' }}>
          <Button variant="outlined" startIcon={<ArrowBack />} onClick={() => navigate(ROUTES.HISTORY)}>
            返回历史记录
          </Button>
          <Box sx={{ display: 'flex', gap: 1.2, alignItems: 'center', flexWrap: 'wrap' }}>
            <Chip label={statusMeta.label} color={statusMeta.color} size="small" />
            <ReadingFavoriteButton
              readingId={reading.id}
              isFavorite={reading.is_favorite}
              onChanged={(nextValue) => setReading((previous) => (previous ? { ...previous, is_favorite: nextValue } : previous))}
            />
          </Box>
        </Box>

        <Paper sx={{ p: 4, mb: 4 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
            <AutoAwesome sx={{ color: 'primary.main' }} />
            <Typography variant="h5" sx={{ fontFamily: 'Cinzel, serif', fontWeight: 700 }}>
              占卜详情
            </Typography>
          </Box>

          <Typography variant="h6" sx={{ mb: 1 }}>
            {getReadingQuestionDisplay({
              question: reading.question,
              questionType: reading.question_type,
              spreadName: spread ? getSpreadDisplayName(spread) : undefined,
            })}
          </Typography>
          <Typography variant="body2" sx={{ color: 'text.secondary', mb: 2 }}>
            创建时间：{formatDateTime(reading.created_at)}
          </Typography>

          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 2 }}>
            <Chip
              label={questionTypeLabel}
              size="small"
              variant="outlined"
              sx={{ borderColor: questionTypeColor, color: questionTypeColor }}
            />
            {reading.user_rating !== undefined && reading.user_rating !== null && (
              <Chip label={`评分 ${reading.user_rating}`} size="small" icon={<Star />} variant="outlined" />
            )}
            {spread && <Chip label={getSpreadDisplayName(spread)} size="small" variant="outlined" />}
          </Box>

          {spread && (
            <Typography variant="body2" sx={{ color: 'text.secondary', lineHeight: 1.7 }}>
              {getSpreadDisplayDescription(spread)}
            </Typography>
          )}

          {reading.user_notes && (
            <Box sx={{ mt: 2 }}>
              <Typography variant="subtitle2" sx={{ mb: 1 }}>
                用户备注
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
              allowManualFlip={false}
              flippedPositions={drawnCards.map((_, index) => index)}
              visualQuality={visual.quality}
              motionPreset={visual.cardMotionPreset}
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
                      {draw.position_name ? `${draw.position_name} · 位置 ${draw.position}` : `位置 ${draw.position}`}
                    </Typography>
                    {draw.position_meaning && (
                      <Typography variant="body2" sx={{ color: 'text.secondary', mt: 1, lineHeight: 1.7 }}>
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
          <Typography variant="h5" sx={{ mb: 2, fontFamily: 'Cinzel, serif', color: 'primary.main' }}>
            AI 解读
          </Typography>
          {!reading.interpretation ? (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Typography variant="body1" sx={{ color: 'text.secondary' }}>
                {aiPolling ? 'AI 正在整理牌阵关系与关键提示，请稍候...' : '当前还没有生成完整的 AI 解读。'}
              </Typography>
              <Box>
                <Button variant="outlined" onClick={handleGenerateInterpretation} disabled={aiGenerating}>
                  {aiGenerating ? '生成中...' : '请求 AI 解读'}
                </Button>
              </Box>
            </Box>
          ) : (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Box>
                <Typography variant="h6" sx={{ mb: 1, fontWeight: 600, color: 'primary.main' }}>
                  总览
                </Typography>
                <Typography variant="body2" sx={{ lineHeight: 1.8, whiteSpace: 'pre-wrap' }}>
                  {reading.interpretation.overall_interpretation}
                </Typography>
              </Box>

              {interpretationThemes.length > 0 && (
                <Box>
                  <Typography variant="h6" sx={{ mb: 1, fontWeight: 600, color: 'primary.main' }}>
                    关键主题
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
                  <Typography variant="h6" sx={{ mb: 1, fontWeight: 600, color: 'primary.main' }}>
                    逐牌分析
                  </Typography>
                  <Typography variant="body2" sx={{ lineHeight: 1.8, whiteSpace: 'pre-wrap' }}>
                    {reading.interpretation.card_analysis}
                  </Typography>
                </Box>
              )}

              {reading.interpretation.relationship_analysis && (
                <Box>
                  <Typography variant="h6" sx={{ mb: 1, fontWeight: 600, color: 'primary.main' }}>
                    牌面关系
                  </Typography>
                  <Typography variant="body2" sx={{ lineHeight: 1.8, whiteSpace: 'pre-wrap' }}>
                    {reading.interpretation.relationship_analysis}
                  </Typography>
                </Box>
              )}

              {reading.interpretation.advice && (
                <Box>
                  <Typography variant="h6" sx={{ mb: 1, fontWeight: 600, color: 'primary.main' }}>
                    建议
                  </Typography>
                  <Typography variant="body2" sx={{ lineHeight: 1.8, whiteSpace: 'pre-wrap' }}>
                    {reading.interpretation.advice}
                  </Typography>
                </Box>
              )}

              {reading.interpretation.warning && (
                <Box>
                  <Typography variant="h6" sx={{ mb: 1, fontWeight: 600, color: 'primary.main' }}>
                    提醒
                  </Typography>
                  <Typography variant="body2" sx={{ lineHeight: 1.8, whiteSpace: 'pre-wrap' }}>
                    {reading.interpretation.warning}
                  </Typography>
                </Box>
              )}

              {reading.interpretation.summary && (
                <Box>
                  <Typography variant="h6" sx={{ mb: 1, fontWeight: 600, color: 'primary.main' }}>
                    总结
                  </Typography>
                  <Typography variant="body2" sx={{ lineHeight: 1.8, whiteSpace: 'pre-wrap' }}>
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
