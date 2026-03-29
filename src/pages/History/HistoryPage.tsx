import React, { useCallback, useDeferredValue, useEffect, useMemo, useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Divider,
  MenuItem,
  Paper,
  TextField,
  Typography,
} from '@mui/material';
import {
  History,
  Star,
  AutoAwesome,
  Favorite,
  Refresh,
  ArrowForward,
} from 'icons';
import { useNavigate } from 'react-router-dom';
import ReadingFavoriteButton from '../../components/Reading/ReadingFavoriteButton';
import Loading from '../../components/UI/Loading';
import { useNotification } from '../../components/UI/Notification';
import { ROUTES } from '../../routes/routeConfig';
import { ReadingStats, ReadingSummary, tarotService } from '../../services/tarotService';
import { formatSmartDate } from '../../utils/dateUtils';
import { getReadingQuestionDisplay } from '../../utils/readingQuestion';
import { useVisualSettings } from '../../hooks/useVisualSettings';

const PAGE_SIZE = 12;

const questionTypeLabels: Record<string, { label: string; color: string }> = {
  love: { label: '感情', color: '#FF6B9D' },
  career: { label: '事业', color: '#4ECDC4' },
  finance: { label: '财务', color: '#FFD93D' },
  health: { label: '健康', color: '#6BCF7F' },
  general: { label: '综合', color: '#AB83A1' },
};

const statusLabels: Record<string, { label: string; color: 'default' | 'success' | 'warning' | 'error' }> = {
  completed: { label: '已完成', color: 'success' },
  processing: { label: '解读中', color: 'warning' },
  pending: { label: '待抽牌', color: 'default' },
  failed: { label: '失败', color: 'error' },
};

const updateFavoriteCount = (stats: ReadingStats | null, previousValue: boolean, nextValue: boolean) => {
  if (!stats || previousValue === nextValue) {
    return stats;
  }

  return {
    ...stats,
    favorite_predictions: Math.max(0, stats.favorite_predictions + (nextValue ? 1 : -1)),
  };
};

const HistoryPage: React.FC = () => {
  const navigate = useNavigate();
  const { showError } = useNotification();
  const visual = useVisualSettings();
  const [stats, setStats] = useState<ReadingStats | null>(null);
  const [readings, setReadings] = useState<ReadingSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(true);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | ReadingSummary['status']>('all');
  const [questionTypeFilter, setQuestionTypeFilter] = useState<'all' | ReadingSummary['question_type']>('all');
  const [sortBy, setSortBy] = useState<'created_at' | 'completed_at' | 'status' | 'question_type'>('created_at');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');
  const deferredSearch = useDeferredValue(search.trim());

  const fetchInitialData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [statsData, readingData] = await Promise.all([
        tarotService.getUserStats(),
        tarotService.getUserReadings({
          skip: 0,
          limit: PAGE_SIZE,
          status: statusFilter === 'all' ? undefined : statusFilter,
          questionType: questionTypeFilter === 'all' ? undefined : questionTypeFilter,
          search: deferredSearch || undefined,
          sortBy,
          sortOrder,
        }),
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
  }, [deferredSearch, questionTypeFilter, showError, sortBy, sortOrder, statusFilter]);

  useEffect(() => {
    void fetchInitialData();
  }, [fetchInitialData]);

  const handleLoadMore = async () => {
    if (loadingMore || !hasMore) {
      return;
    }
    setLoadingMore(true);
    try {
      const nextBatch = await tarotService.getUserReadings({
        skip: readings.length,
        limit: PAGE_SIZE,
        status: statusFilter === 'all' ? undefined : statusFilter,
        questionType: questionTypeFilter === 'all' ? undefined : questionTypeFilter,
        search: deferredSearch || undefined,
        sortBy,
        sortOrder,
      });
      setReadings((prev) => [...prev, ...nextBatch]);
      setHasMore(nextBatch.length === PAGE_SIZE);
    } catch (err) {
      const message = err instanceof Error ? err.message : '加载更多记录失败';
      showError(message);
    } finally {
      setLoadingMore(false);
    }
  };

  const resetFilters = () => {
    setSearch('');
    setStatusFilter('all');
    setQuestionTypeFilter('all');
    setSortBy('created_at');
    setSortOrder('desc');
  };

  const handleFavoriteChanged = (readingId: number, nextValue: boolean) => {
    let previousValue = false;
    setReadings((previous) => {
      previousValue = Boolean(previous.find((item) => item.id === readingId)?.is_favorite);
      return previous.map((item) => (
        item.id === readingId
          ? { ...item, is_favorite: nextValue }
          : item
      ));
    });
    setStats((previous) => updateFavoriteCount(previous, previousValue, nextValue));
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
      <Box sx={{ py: 4, px: { xs: 2, md: 3 } }}>
        <Loading message="正在加载占卜历史..." />
      </Box>
    );
  }

  return (
    <Box sx={{ py: 4, px: { xs: 2, md: 3 } }}>
      <Box sx={{ textAlign: 'center', mb: 4 }}>
        <History
          sx={{
            fontSize: '3rem',
            color: 'primary.main',
            mb: 2,
            animation: visual.enableAmbientMotion ? 'float 3s ease-in-out infinite' : 'none',
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
        <Typography variant="body1" sx={{ color: 'text.secondary' }}>
          回看每一次抽牌、解读与收藏记录
        </Typography>
      </Box>

      {error && (
        <Paper sx={{ p: 3, mb: 3 }}>
          <Typography color="error" sx={{ mb: 2 }}>
            {error}
          </Typography>
          <Button variant="outlined" startIcon={<Refresh />} onClick={fetchInitialData}>
            重新加载
          </Button>
        </Paper>
      )}

      {summaryCards.length > 0 && (
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', lg: 'repeat(4, 1fr)' }, gap: 3, mb: 4 }}>
          {summaryCards.map((item) => (
            <Card
              key={item.title}
              sx={{
                position: 'relative',
                overflow: 'hidden',
                transition: visual.enableHoverMotion
                  ? 'transform 220ms ease, box-shadow 220ms ease, border-color 220ms ease'
                  : 'border-color 220ms ease',
                border: '1px solid rgba(212, 175, 55, 0.08)',
                '&::before': {
                  content: '""',
                  position: 'absolute',
                  top: 0,
                  left: 0,
                  right: 0,
                  height: '3px',
                  background: 'linear-gradient(90deg, rgba(212, 175, 55, 0.95), rgba(0, 240, 255, 0.65))',
                  opacity: 0.85,
                },
                '&:hover': {
                  transform: visual.enableHoverMotion ? 'translateY(-4px)' : 'none',
                  boxShadow: visual.enableHoverMotion ? '0 14px 30px rgba(6, 8, 18, 0.18)' : 'none',
                  borderColor: 'rgba(212, 175, 55, 0.18)',
                },
              }}
            >
              <CardContent sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', p: 3 }}>
                <Box>
                  <Typography variant="body1" sx={{ color: 'text.secondary', mb: 1 }}>
                    {item.title}
                  </Typography>
                  <Typography variant="h4" sx={{ fontWeight: 700, color: 'primary.main' }}>
                    {item.value}
                  </Typography>
                </Box>
                <Box sx={{ color: 'primary.main', fontSize: '2rem' }}>{item.icon}</Box>
              </CardContent>
            </Card>
          ))}
        </Box>
      )}

      <Paper sx={{ p: 3, mb: 4 }}>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', md: 'minmax(240px, 2fr) repeat(4, minmax(0, 1fr)) auto' },
            gap: 2,
            alignItems: 'center',
          }}
        >
          <TextField
            label="搜索问题或备注"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            fullWidth
          />
          <TextField
            select
            label="状态"
            value={statusFilter}
            onChange={(event) => setStatusFilter(event.target.value as 'all' | ReadingSummary['status'])}
          >
            <MenuItem value="all">全部状态</MenuItem>
            <MenuItem value="completed">已完成</MenuItem>
            <MenuItem value="processing">解读中</MenuItem>
            <MenuItem value="pending">待抽牌</MenuItem>
            <MenuItem value="failed">失败</MenuItem>
          </TextField>
          <TextField
            select
            label="问题类型"
            value={questionTypeFilter}
            onChange={(event) => setQuestionTypeFilter(event.target.value as 'all' | ReadingSummary['question_type'])}
          >
            <MenuItem value="all">全部类型</MenuItem>
            <MenuItem value="love">感情</MenuItem>
            <MenuItem value="career">事业</MenuItem>
            <MenuItem value="finance">财务</MenuItem>
            <MenuItem value="health">健康</MenuItem>
            <MenuItem value="general">综合</MenuItem>
          </TextField>
          <TextField
            select
            label="排序字段"
            value={sortBy}
            onChange={(event) => setSortBy(event.target.value as 'created_at' | 'completed_at' | 'status' | 'question_type')}
          >
            <MenuItem value="created_at">创建时间</MenuItem>
            <MenuItem value="completed_at">完成时间</MenuItem>
            <MenuItem value="status">状态</MenuItem>
            <MenuItem value="question_type">问题类型</MenuItem>
          </TextField>
          <TextField
            select
            label="排序方向"
            value={sortOrder}
            onChange={(event) => setSortOrder(event.target.value as 'asc' | 'desc')}
          >
            <MenuItem value="desc">降序</MenuItem>
            <MenuItem value="asc">升序</MenuItem>
          </TextField>
          <Button variant="outlined" onClick={resetFilters}>
            重置
          </Button>
        </Box>
      </Paper>

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
            开始一次新的占卜，记录你的每次旅程。
          </Typography>
          <Button variant="contained" onClick={() => navigate(ROUTES.NEW_READING)}>
            开始占卜
          </Button>
        </Paper>
      ) : (
        <Box>
          {readings.map((reading) => {
            const typeMeta = questionTypeLabels[reading.question_type] || { label: reading.question_type, color: '#D4AF37' };
            const statusMeta = statusLabels[reading.status] || { label: reading.status, color: 'default' as const };
            return (
              <Paper
                key={reading.id}
                sx={{
                  p: 4,
                  mb: 3,
                  position: 'relative',
                  overflow: 'hidden',
                  transition: visual.enableHoverMotion
                    ? 'transform 220ms ease, box-shadow 220ms ease, border-color 220ms ease'
                    : 'border-color 220ms ease',
                  border: '1px solid rgba(212, 175, 55, 0.08)',
                  '&::before': {
                    content: '""',
                    position: 'absolute',
                    top: 0,
                    left: 0,
                    right: 0,
                    height: '3px',
                    background: `linear-gradient(90deg, ${typeMeta.color}, rgba(212, 175, 55, 0.75))`,
                    opacity: 0.9,
                  },
                  '&:hover': {
                    transform: visual.enableHoverMotion ? 'translateY(-3px)' : 'none',
                    boxShadow: visual.enableHoverMotion ? '0 16px 34px rgba(6, 8, 18, 0.18)' : 'none',
                    borderColor: 'rgba(212, 175, 55, 0.16)',
                  },
                }}
              >
                <Box sx={{ display: 'flex', justifyContent: 'space-between', flexWrap: 'wrap', gap: 2 }}>
                  <Box sx={{ flex: 1, minWidth: 240 }}>
                    <Typography variant="h5" sx={{ mb: 1 }}>
                      {getReadingQuestionDisplay({
                        question: reading.question,
                        questionType: reading.question_type,
                      })}
                    </Typography>
                    <Typography variant="body1" sx={{ color: 'text.secondary' }}>
                      {formatSmartDate(reading.created_at)}
                    </Typography>
                  </Box>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
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
                  </Box>
                </Box>

                {reading.user_rating !== undefined && reading.user_rating !== null && (
                  <Box sx={{ mt: 2, display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Star fontSize="small" sx={{ color: 'primary.main' }} />
                    <Typography variant="body1" sx={{ color: 'text.secondary' }}>
                      评分 {reading.user_rating}
                    </Typography>
                  </Box>
                )}

                <Divider sx={{ my: 2 }} />

                <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 1.5, flexWrap: 'wrap' }}>
                  <ReadingFavoriteButton
                    readingId={reading.id}
                    isFavorite={reading.is_favorite}
                    onChanged={(nextValue) => handleFavoriteChanged(reading.id, nextValue)}
                  />
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
    </Box>
  );
};

export default HistoryPage;
