import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Container,
  Paper,
  Typography,
} from '@mui/material';
import {
  AutoAwesome,
  History,
  Star,
  Favorite,
  ArrowForward,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../stores/authStore';
import { tarotService, ReadingStats, ReadingSummary } from '../../services/tarotService';
import { formatSmartDate } from '../../utils/dateUtils';
import { ROUTES } from '../../routes/routeConfig';
import Loading from '../../components/UI/Loading';
import { useNotification } from '../../components/UI/Notification';

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

const ProfilePage: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuthStore();
  const { showError } = useNotification();
  const [stats, setStats] = useState<ReadingStats | null>(null);
  const [recentReadings, setRecentReadings] = useState<ReadingSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchProfileData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [statsData, recentData] = await Promise.all([
        tarotService.getUserStats(),
        tarotService.getRecentReadings(14, 6),
      ]);
      setStats(statsData);
      setRecentReadings(recentData);
    } catch (err) {
      const message = err instanceof Error ? err.message : '加载个人数据失败';
      setError(message);
      showError(message);
    } finally {
      setLoading(false);
    }
  }, [showError]);

  useEffect(() => {
    fetchProfileData();
  }, [fetchProfileData]);

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
        <Loading message="正在加载个人中心..." />
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Paper sx={{ p: 4, mb: 4 }}>
        <Box sx={{ display: 'flex', flexDirection: { xs: 'column', md: 'row' }, gap: 3, alignItems: 'center' }}>
          <Avatar
            sx={{
              width: 96,
              height: 96,
              background: 'linear-gradient(45deg, #D4AF37, #FFD700)',
              color: 'black',
              fontSize: '2rem',
              fontFamily: 'Cinzel, serif',
            }}
            src={user?.avatar_url}
          >
            {user?.nickname?.[0] || user?.username?.[0] || 'U'}
          </Avatar>

          <Box sx={{ flex: 1 }}>
            <Typography variant="h4" sx={{ fontFamily: 'Cinzel, serif', mb: 1 }}>
              {user?.nickname || user?.username || '塔罗旅人'}
            </Typography>
            <Typography variant="body2" sx={{ color: 'text.secondary', mb: 2 }}>
              {user?.email || '未绑定邮箱'}
            </Typography>

            {stats?.most_used_question_type && (
              <Chip
                label={`常用问题：${questionTypeLabels[stats.most_used_question_type]?.label || stats.most_used_question_type}`}
                variant="outlined"
                size="small"
                sx={{ borderColor: 'primary.main', color: 'primary.main' }}
              />
            )}
          </Box>

          <Box>
            <Button variant="outlined" onClick={fetchProfileData}>
              刷新数据
            </Button>
          </Box>
        </Box>

        {error && (
          <Typography color="error" sx={{ mt: 2 }}>
            {error}
          </Typography>
        )}
      </Paper>

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

      <Paper sx={{ p: 4 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <History sx={{ color: 'primary.main' }} />
            <Typography variant="h6">最近占卜</Typography>
          </Box>
          <Button
            size="small"
            variant="outlined"
            endIcon={<ArrowForward />}
            onClick={() => navigate(ROUTES.HISTORY)}
          >
            查看全部
          </Button>
        </Box>

        {recentReadings.length === 0 ? (
          <Box sx={{ textAlign: 'center', py: 4 }}>
            <Typography variant="body2" sx={{ color: 'text.secondary', mb: 2 }}>
              暂无最近记录
            </Typography>
            <Button variant="contained" onClick={() => navigate(ROUTES.NEW_READING)}>
              开始占卜
            </Button>
          </Box>
        ) : (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {recentReadings.map((reading) => {
              const typeMeta = questionTypeLabels[reading.question_type] || { label: reading.question_type, color: '#D4AF37' };
              const statusMeta = statusLabels[reading.status] || { label: reading.status, color: 'default' as const };
              return (
                <Paper key={reading.id} sx={{ p: 2 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', flexWrap: 'wrap', gap: 1 }}>
                    <Box>
                      <Typography variant="subtitle1" sx={{ mb: 0.5 }}>
                        {reading.question}
                      </Typography>
                      <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                        {formatSmartDate(reading.created_at)}
                      </Typography>
                    </Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Chip
                        label={typeMeta.label}
                        size="small"
                        variant="outlined"
                        sx={{ borderColor: typeMeta.color, color: typeMeta.color }}
                      />
                      <Chip label={statusMeta.label} size="small" color={statusMeta.color} />
                    </Box>
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'flex-end', mt: 1 }}>
                    <Button
                      size="small"
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
      </Paper>
    </Container>
  );
};

export default ProfilePage;
