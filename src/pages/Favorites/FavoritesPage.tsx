import React, { useCallback, useDeferredValue, useEffect, useState } from 'react';
import { Box, Button, Chip, MenuItem, Paper, TextField, Typography } from '@mui/material';
import { ArrowForward, Favorite, History } from 'icons';
import { useNavigate } from 'react-router-dom';
import ReadingFavoriteButton from '../../components/Reading/ReadingFavoriteButton';
import Loading from '../../components/UI/Loading';
import { useNotification } from '../../components/UI/Notification';
import { ROUTES } from '../../routes/routeConfig';
import { ReadingSummary, tarotService } from '../../services/tarotService';
import { formatSmartDate } from '../../utils/dateUtils';
import { getReadingQuestionDisplay } from '../../utils/readingQuestion';

const questionTypeLabels: Record<string, { label: string; color: string }> = {
  love: { label: '感情', color: '#FF6B9D' },
  career: { label: '事业', color: '#4ECDC4' },
  finance: { label: '财务', color: '#FFD93D' },
  health: { label: '健康', color: '#6BCF7F' },
  general: { label: '综合', color: '#AB83A1' },
};

const FavoritesPage: React.FC = () => {
  const navigate = useNavigate();
  const { showError } = useNotification();
  const [favorites, setFavorites] = useState<ReadingSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | ReadingSummary['status']>('all');
  const [questionTypeFilter, setQuestionTypeFilter] = useState<'all' | ReadingSummary['question_type']>('all');
  const [sortBy, setSortBy] = useState<'created_at' | 'completed_at' | 'status' | 'question_type'>('created_at');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');
  const deferredSearch = useDeferredValue(search.trim());

  const loadFavorites = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await tarotService.getFavoriteReadings({
        skip: 0,
        limit: 24,
        status: statusFilter === 'all' ? undefined : statusFilter,
        questionType: questionTypeFilter === 'all' ? undefined : questionTypeFilter,
        search: deferredSearch || undefined,
        sortBy,
        sortOrder,
      });
      setFavorites(data);
    } catch (err) {
      const message = err instanceof Error ? err.message : '加载收藏失败';
      setError(message);
      showError(message);
    } finally {
      setLoading(false);
    }
  }, [deferredSearch, questionTypeFilter, showError, sortBy, sortOrder, statusFilter]);

  useEffect(() => {
    void loadFavorites();
  }, [loadFavorites]);

  const handleFavoriteChanged = (readingId: number, nextValue: boolean) => {
    if (!nextValue) {
      setFavorites((previous) => previous.filter((item) => item.id !== readingId));
      return;
    }

    setFavorites((previous) =>
      previous.map((item) => (
        item.id === readingId
          ? { ...item, is_favorite: true }
          : item
      )),
    );
  };

  const resetFilters = () => {
    setSearch('');
    setStatusFilter('all');
    setQuestionTypeFilter('all');
    setSortBy('created_at');
    setSortOrder('desc');
  };

  if (loading) {
    return <Loading message="正在载入你的收藏..." />;
  }

  return (
    <Box sx={{ py: 2 }}>
      <Paper
        sx={{
          p: { xs: 3, md: 4 },
          borderRadius: 4,
          mb: 3,
          background: 'linear-gradient(135deg, rgba(212, 175, 55, 0.12) 0%, rgba(80, 20, 120, 0.28) 100%)',
          border: '1px solid rgba(212, 175, 55, 0.18)',
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 1.5 }}>
          <Favorite sx={{ color: 'primary.main' }} />
          <Typography variant="h4" sx={{ fontFamily: 'Cinzel, serif', color: 'primary.main' }}>
            我的收藏
          </Typography>
        </Box>
        <Typography variant="body1" sx={{ color: 'text.secondary', lineHeight: 1.8 }}>
          这里会集中展示你标记过的占卜记录，方便回看、对比和继续追踪同一个问题。
        </Typography>
      </Paper>

      <Paper sx={{ p: 3, borderRadius: 4, mb: 3 }}>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', md: 'minmax(240px, 2fr) repeat(4, minmax(0, 1fr)) auto' },
            gap: 2,
            alignItems: 'center',
          }}
        >
          <TextField
            label="搜索收藏记录"
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

      {error ? (
        <Paper sx={{ p: 4, borderRadius: 4 }}>
          <Typography color="error" sx={{ mb: 2 }}>{error}</Typography>
          <Button variant="outlined" onClick={loadFavorites}>
            重新加载
          </Button>
        </Paper>
      ) : favorites.length === 0 ? (
        <Paper sx={{ p: 6, borderRadius: 4, textAlign: 'center' }}>
          <History sx={{ fontSize: '3rem', color: 'text.secondary', mb: 2 }} />
          <Typography variant="h6" sx={{ mb: 1.5 }}>
            还没有收藏记录
          </Typography>
          <Typography variant="body2" sx={{ color: 'text.secondary', mb: 3 }}>
            先完成一次占卜，再把想保留的结果收藏起来。
          </Typography>
          <Button variant="contained" onClick={() => navigate(ROUTES.HISTORY)}>
            去查看历史
          </Button>
        </Paper>
      ) : (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          {favorites.map((reading) => {
            const typeMeta = questionTypeLabels[reading.question_type] || { label: reading.question_type, color: '#D4AF37' };
            return (
              <Paper key={reading.id} sx={{ p: 3, borderRadius: 3 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
                  <Box sx={{ flex: 1, minWidth: 240 }}>
                    <Typography variant="h6" sx={{ mb: 0.75 }}>
                      {getReadingQuestionDisplay({
                        question: reading.question,
                        questionType: reading.question_type,
                      })}
                    </Typography>
                    <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                      {formatSmartDate(reading.created_at)}
                    </Typography>
                  </Box>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Chip
                      label={typeMeta.label}
                      variant="outlined"
                      size="small"
                      sx={{ borderColor: typeMeta.color, color: typeMeta.color }}
                    />
                    <Chip label="已收藏" size="small" sx={{ background: 'rgba(212, 175, 55, 0.14)', color: 'primary.main' }} />
                  </Box>
                </Box>

                <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 1.5, mt: 2, flexWrap: 'wrap' }}>
                  <ReadingFavoriteButton
                    readingId={reading.id}
                    isFavorite={true}
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
        </Box>
      )}
    </Box>
  );
};

export default FavoritesPage;
