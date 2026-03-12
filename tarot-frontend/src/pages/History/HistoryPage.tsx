import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Container,
  Divider,
  Paper,
  Typography,
} from '@mui/material';
import {
  History,
  Star,
  AutoAwesome,
  Favorite,
  Refresh,
  ArrowForward,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { tarotService, ReadingStats, ReadingSummary } from '../../services/tarotService';
import { formatSmartDate } from '../../utils/dateUtils';
import { ROUTES } from '../../routes/routeConfig';
import Loading from '../../components/UI/Loading';
import { useNotification } from '../../components/UI/Notification';

const PAGE_SIZE = 12;

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

const HistoryPage: React.FC = () => {
  const navigate = useNavigate();
  const { showError } = useNotification();
  const [stats, setStats] = useState<ReadingStats | null>(null);
  const [readings, setReadings] = useState<ReadingSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(true);

  const fetchInitialData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [statsData, readingData] = await Promise.all([
        tarotService.getUserStats(),
        tarotService.getUserReadings(0, PAGE_SIZE),
      ]);
      setStats(statsData);
      setReadings(readingData);
      setHasMore(readingData.length === PAGE_SIZE);
    } catch (err) {
      const message = err instanceof Error ? err.message : '加载历史记录失败';
      setError(message);
      showError(message);
    } finally {
      setLoading(false);
    }
  }, [showError]);

  useEffect(() => {
    fetchInitialData();
  }, [fetchInitialData]);

  const handleLoadMore = async () => {
    if (loadingMore || !hasMore) return;
    setLoadingMore(true);
    try {
      const nextBatch = await tarotService.getUserReadings(readings.length, PAGE_SIZE);
      setReadings((prev) => [...prev, ...nextBatch]);
      setHasMore(nextBatch.length === PAGE_SIZE);
    } catch (err) {
      const message = err instanceof Error ? err.message : '加载更多记录失败';
      showError(message);
    } finally {
      setLoadingMore(false);
    }
  };

  const summaryCards = useMemo(() => {
    if (!stats) return [];
    return [
      {
        title: '总占卜数',
        value: stats.total_predictions,
        icon: <AutoAwesome />,
      },
      {
        title: '已完成',
        value: stats.completed_predictions,
        icon: <Star />,
      },
      {
        title: '收藏',
        value: stats.favorite_predictions,
        icon: <Favorite />,
      },
      {
        title: '平均评分',
        value: stats.average_rating ? stats.average_rating.toFixed(1) : '暂无',
        icon: <Star />,
      },
    ];
  }, [stats]);

  if (loading) {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Loading message="正在加载占卜历史..." />
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Box sx={{ textAlign: 'center', mb: 4 }}>
        <History
          sx={{
            fontSize: '3rem',
            color: 'primary.main',
            mb: 2,
            animation: 'float 3s ease-in-out infinite',
          }}
        />
        <Typography
          variant="h4"
          sx={{
            fontFamily: 'Cinzel, serif',
            color: 'primary.main',
            mb: 1,
          }}
        >
          占卜历史
        </Typography>
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          回顾每一次塔罗指引
        </Typography>
      </Box>

      {error && (
        <Paper sx={{ p: 3, mb: 3 }}>
          <Typography color="error" sx={{ mb: 2 }}>
            {error}
          </Typography>
          <Button
            variant="outlined"
            startIcon={<Refresh />}
            onClick={fetchInitialData}
          >
            重新加载
          </Button>
        </Paper>
      )}

      {summaryCards.length > 0 && (
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', lg: 'repeat(4, 1fr)' }, gap: 2, mb: 4 }}>
          {summaryCards.map((item) => (
            <Card key={item.title}>
              <CardContent sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Box>
                  <Typography variant="body2" sx={{ color: 'text.secondary', mb: 1 }}>
                    {item.title}
                  </Typography>
                  <Typography variant="h5" sx={{ fontWeight: 700, color: 'primary.main' }}>
                    {item.value}
                  </Typography>
                </Box>
                <Box sx={{ color: 'primary.main', fontSize: '2rem' }}>{item.icon}</Box>
              </CardContent>
            </Card>
          ))}
        </Box>
      )}

      {readings.length === 0 && !error ? (
        <Paper
          sx={{
            p: 6,
            textAlign: 'center',
            background: 'linear-gradient(135deg, rgba(26, 26, 46, 0.8) 0%, rgba(22, 33, 62, 0.8) 100%)',
            border: '1px solid rgba(212, 175, 55, 0.2)',
          }}
        >
          <History sx={{ fontSize: '3rem', color: 'text.secondary', mb: 2 }} />
          <Typography variant="h6" sx={{ mb: 2 }}>
            暂无历史记录
          </Typography>
          <Typography variant="body2" sx={{ color: 'text.secondary', mb: 3 }}>
            开始一次新的占卜，记录你的每次旅程
          </Typography>
          <Button
            variant="contained"
            onClick={() => navigate(ROUTES.NEW_READING)}
          >
            开始占卜
          </Button>
        </Paper>
      ) : (
        <Box>
          {readings.map((reading) => {
            const typeMeta = questionTypeLabels[reading.question_type] || { label: reading.question_type, color: '#D4AF37' };
            const statusMeta = statusLabels[reading.status] || { label: reading.status, color: 'default' as const };
            return (
              <Paper key={reading.id} sx={{ p: 3, mb: 2 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', flexWrap: 'wrap', gap: 2 }}>
                  <Box sx={{ flex: 1, minWidth: 240 }}>
                    <Typography variant="h6" sx={{ mb: 1 }}>
                      {reading.question}
                    </Typography>
                    <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                      {formatSmartDate(reading.created_at)}
                    </Typography>
                  </Box>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Chip
                      label={typeMeta.label}
                      size="small"
                      variant="outlined"
                      sx={{
                        borderColor: typeMeta.color,
                        color: typeMeta.color,
                      }}
                    />
                    <Chip
                      label={statusMeta.label}
                      size="small"
                      color={statusMeta.color}
                    />
                    {reading.is_favorite && (
                      <Favorite sx={{ color: 'primary.main' }} fontSize="small" />
                    )}
                  </Box>
                </Box>

                {reading.user_rating !== undefined && reading.user_rating !== null && (
                  <Box sx={{ mt: 2, display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Star fontSize="small" sx={{ color: 'primary.main' }} />
                    <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                      评分 {reading.user_rating}
                    </Typography>
                  </Box>
                )}

                <Divider sx={{ my: 2 }} />

                <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
                  <Button
                    variant="outlined"
                    endIcon={<ArrowForward />}
                    onClick={() => navigate(ROUTES.READING_DETAIL.replace(':id', reading.id.toString()))}
                  >
                    查看详情
                  </Button>
                </Box>
              </Paper>
            );
          })}

          {hasMore && (
            <Box sx={{ textAlign: 'center', mt: 3 }}>
              <Button
                variant="outlined"
                onClick={handleLoadMore}
                disabled={loadingMore}
              >
                {loadingMore ? '加载中...' : '加载更多'}
              </Button>
            </Box>
          )}
        </Box>
      )}
    </Container>
  );
};

export default HistoryPage;
