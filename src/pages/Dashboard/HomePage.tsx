import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  LinearProgress,
  Paper,
  Typography,
} from '@mui/material';
import {
  AccountBalance,
  AutoAwesome,
  History,
  LocalHospital,
  PlayArrow,
  Star,
  TrendingUp,
  WorkOutline,
} from 'icons';
import { useNavigate } from 'react-router-dom';
import ReadingFavoriteButton from '../../components/Reading/ReadingFavoriteButton';
import { useNotification } from '../../components/UI/Notification';
import { ROUTES } from '../../routes/routeConfig';
import { ReadingOverview, ReadingStats, ReadingSummary, tarotService } from '../../services/tarotService';
import { useAuthStore } from '../../stores/authStore';
import { useGameStore } from '../../stores/gameStore';
import { formatSmartDate } from '../../utils/dateUtils';
import { getReadingQuestionDisplay } from '../../utils/readingQuestion';
import { useVisualSettings } from '../../hooks/useVisualSettings';

const QUICK_READING_OPTIONS = [
  {
    id: 'love',
    title: '感情指引',
    description: '聚焦关系状态、双方情绪和接下来最值得留意的互动变化。',
    icon: <Star />,
    color: '#FF6B9D',
    gradient: 'linear-gradient(135deg, #FF6B9D 0%, #C44569 100%)',
  },
  {
    id: 'career',
    title: '事业决策',
    description: '帮助梳理工作重点、机会窗口与当下最需要调整的方向。',
    icon: <WorkOutline />,
    color: '#4ECDC4',
    gradient: 'linear-gradient(135deg, #4ECDC4 0%, #44A08D 100%)',
  },
  {
    id: 'finance',
    title: '财务分析',
    description: '适合支出规划、风险控制与资源配置相关的快速判断。',
    icon: <AccountBalance />,
    color: '#FFD93D',
    gradient: 'linear-gradient(135deg, #FFD93D 0%, #F39C12 100%)',
  },
  {
    id: 'health',
    title: '状态调节',
    description: '关注节奏、压力来源以及恢复与平衡的提醒。',
    icon: <LocalHospital />,
    color: '#6BCF7F',
    gradient: 'linear-gradient(135deg, #6BCF7F 0%, #4D7C0F 100%)',
  },
] as const;

const QUESTION_TYPE_META: Record<string, { label: string; color: string }> = {
  love: { label: '感情', color: '#FF6B9D' },
  career: { label: '事业', color: '#4ECDC4' },
  finance: { label: '财务', color: '#FFD93D' },
  health: { label: '健康', color: '#6BCF7F' },
  general: { label: '综合', color: '#AB83A1' },
};

const STATUS_META: Record<string, { label: string; color: 'default' | 'success' | 'warning' | 'error' }> = {
  completed: { label: '已完成', color: 'success' },
  processing: { label: '解读中', color: 'warning' },
  pending: { label: '待抽牌', color: 'default' },
  failed: { label: '失败', color: 'error' },
};

const calculateStreakDays = (readings: ReadingSummary[]): number => {
  if (readings.length === 0) {
    return 0;
  }

  const uniqueDays = Array.from(
    new Set(readings.map((reading) => new Date(reading.created_at).toISOString().slice(0, 10))),
  ).sort((left, right) => right.localeCompare(left));

  const today = new Date();
  let cursor = new Date(today.getFullYear(), today.getMonth(), today.getDate());
  let streak = 0;

  for (const day of uniqueDays) {
    const current = new Date(`${day}T00:00:00`);
    const diffDays = Math.round((cursor.getTime() - current.getTime()) / (1000 * 60 * 60 * 24));

    if (diffDays === 0) {
      streak += 1;
      cursor.setDate(cursor.getDate() - 1);
      continue;
    }

    if (streak === 0 && diffDays === 1) {
      streak += 1;
      cursor.setDate(cursor.getDate() - 2);
      continue;
    }

    break;
  }

  return streak;
};

const getGreeting = () => {
  const hour = new Date().getHours();
  if (hour < 6) return '凌晨好';
  if (hour < 12) return '上午好';
  if (hour < 18) return '下午好';
  return '晚上好';
};

const getInterpretationSummary = (reading: ReadingOverview): string => {
  const compact = (reading.interpretation_summary || '').replace(/\s+/g, ' ').trim();
  if (compact) {
    return compact.length <= 120 ? compact : `${compact.slice(0, 120)}...`;
  }

  if (reading.status === 'processing') {
    return 'AI 正在整理牌阵关系和重点提示，稍后就会返回完整解读。';
  }
  if (reading.status === 'pending') {
    return '这条记录还没有进入完整解读阶段。';
  }
  if (reading.status === 'failed') {
    return '这次解读未成功生成，你可以进入详情页重新触发 AI。';
  }
  return '进入详情页查看完整抽牌信息和解读内容。';
};

const getRecentSpreadName = (reading: ReadingOverview): string | undefined => {
  const primaryName = (reading.spread_name || '').trim();
  if (primaryName && !/[?�]/.test(primaryName)) {
    return primaryName;
  }

  const fallbackName = (reading.spread_name_en || '').trim();
  return fallbackName || undefined;
};

const updateFavoriteCount = (
  stats: ReadingStats | null,
  previousValue: boolean,
  nextValue: boolean,
): ReadingStats | null => {
  if (!stats || previousValue === nextValue) {
    return stats;
  }

  return {
    ...stats,
    favorite_predictions: Math.max(0, stats.favorite_predictions + (nextValue ? 1 : -1)),
  };
};

const HomePage: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuthStore();
  const { startNewReading } = useGameStore();
  const { showError } = useNotification();
  const showErrorRef = useRef(showError);
  const visual = useVisualSettings();

  const [stats, setStats] = useState<ReadingStats | null>(null);
  const [recentReadings, setRecentReadings] = useState<ReadingSummary[]>([]);
  const [recentOverview, setRecentOverview] = useState<ReadingOverview[]>([]);
  const [loadingRecent, setLoadingRecent] = useState(true);

  useEffect(() => {
    showErrorRef.current = showError;
  }, [showError]);

  const loadDashboardData = useCallback(async () => {
    setLoadingRecent(true);

    try {
      const [statsData, readings, recentOverviewData] = await Promise.all([
        tarotService.getUserStats(),
        tarotService.getUserReadings({ skip: 0, limit: 30 }),
        tarotService.getRecentReadingsOverview(4),
      ]);

      setStats(statsData);
      setRecentReadings(readings);
      setRecentOverview(recentOverviewData);
    } catch (error) {
      const message = error instanceof Error ? error.message : '加载主控台数据失败';
      showErrorRef.current(message);
    } finally {
      setLoadingRecent(false);
    }
  }, []);

  useEffect(() => {
    void loadDashboardData();
  }, [loadDashboardData]);

  const streakDays = useMemo(() => calculateStreakDays(recentReadings), [recentReadings]);

  const statsCards = useMemo(
    () => [
      { label: '总占卜数', value: stats?.total_predictions ?? 0, icon: <AutoAwesome />, color: '#D4AF37' },
      { label: '已完成解读', value: stats?.completed_predictions ?? 0, icon: <Star />, color: '#4ECDC4' },
      { label: '连续记录天数', value: streakDays, icon: <TrendingUp />, color: '#FF6B9D' },
    ],
    [stats, streakDays],
  );

  const progressValue = useMemo(() => {
    if (!stats || stats.total_predictions === 0) {
      return 0;
    }
    return Math.round((stats.completed_predictions / stats.total_predictions) * 100);
  }, [stats]);

  const progressHeadline = useMemo(() => {
    if (!stats || stats.total_predictions === 0) {
      return '还没有完成解读的记录';
    }
    return `已完成解读 ${stats.completed_predictions} / ${stats.total_predictions}`;
  }, [stats]);

  const handleQuickReading = (type: string) => {
    startNewReading();
    navigate(ROUTES.NEW_READING, { state: { quickType: type } });
  };

  const handleStartReading = () => {
    startNewReading();
    navigate(ROUTES.NEW_READING);
  };

  const handleFavoriteChanged = (readingId: number, nextValue: boolean) => {
    let previousValue = false;

    setRecentReadings((previous) => {
      previousValue = Boolean(previous.find((item) => item.id === readingId)?.is_favorite);
      return previous.map((item) => (item.id === readingId ? { ...item, is_favorite: nextValue } : item));
    });

    setRecentOverview((previous) =>
      previous.map((item) => (item.id === readingId ? { ...item, is_favorite: nextValue } : item)),
    );

    setStats((previous) => updateFavoriteCount(previous, previousValue, nextValue));
  };

  return (
    <Box sx={{ py: 2, position: 'relative', minHeight: 'calc(100vh - 80px)' }}>
      <Box
        sx={{
          position: 'fixed',
          inset: 0,
          pointerEvents: 'none',
          zIndex: -1,
        }}
      >
        <Box
          sx={{
            position: 'absolute',
            top: '45%',
            left: '54%',
            width: '800px',
            height: '800px',
            transform: 'translate(-50%, -50%)',
            opacity: visual.quality === 'minimal' ? 0.025 : 0.06,
            animation: visual.enableAmbientMotion ? 'homeSpin 90s linear infinite' : 'none',
            '@keyframes homeSpin': {
              '0%': { transform: 'translate(-50%, -50%) rotate(0deg)' },
              '100%': { transform: 'translate(-50%, -50%) rotate(360deg)' },
            },
          }}
        >
          <svg viewBox="0 0 600 600" xmlns="http://www.w3.org/2000/svg">
            <circle cx="300" cy="300" r="290" fill="none" stroke="#d4af37" strokeWidth="2" strokeDasharray="8 14" />
            <circle cx="300" cy="300" r="250" fill="none" stroke="#d4af37" strokeWidth="2.5" />
            <circle cx="300" cy="300" r="200" fill="none" stroke="#56bdf8" strokeWidth="1.5" />
            <polygon points="300,40 520,430 80,430" fill="none" stroke="#d4af37" strokeWidth="2" />
            <polygon points="300,560 520,170 80,170" fill="none" stroke="#d4af37" strokeWidth="2" />
            <polygon
              points="300,100 380,240 480,300 380,360 300,500 220,360 120,300 220,240"
              fill="none"
              stroke="#56bdf8"
              strokeWidth="1.5"
            />
          </svg>
        </Box>
      </Box>

      <Paper
        sx={{
          mb: 4,
          p: { xs: 3, md: 5 },
          background: 'linear-gradient(135deg, rgba(212, 175, 55, 0.12) 0%, rgba(106, 5, 114, 0.12) 100%)',
          borderRadius: 4,
          border: '1px solid rgba(212, 175, 55, 0.25)',
          position: 'relative',
          overflow: 'hidden',
        }}
      >
        <Box sx={{ position: 'relative', zIndex: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
            <Avatar
              sx={{
                width: 72,
                height: 72,
                background: 'linear-gradient(45deg, #D4AF37, #FFD700)',
                color: 'black',
                fontSize: '1.8rem',
                fontFamily: 'Cinzel, serif',
                mr: 3,
              }}
              src={user?.avatar_url}
            >
              {user?.nickname?.[0] || user?.username?.[0] || 'U'}
            </Avatar>
            <Box>
              <Typography
                variant="h3"
                sx={{
                  fontFamily: 'Cinzel, serif',
                  fontWeight: 600,
                  color: 'primary.main',
                  mb: 0.5,
                }}
              >
                {`${getGreeting()}，${user?.nickname || user?.username}`}
              </Typography>
              <Typography variant="body1" sx={{ color: 'text.secondary', fontStyle: 'italic', fontSize: '1.1rem' }}>
                欢迎回到塔罗之境。今天的直觉，会带你看见什么？
              </Typography>
            </Box>
          </Box>

          <Box sx={{ mt: 3 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
              <Typography variant="body1" sx={{ color: 'text.secondary' }}>
                已完成解读进度
              </Typography>
              <Typography variant="body1" sx={{ color: 'primary.main', fontWeight: 600, fontSize: '1.2rem' }}>
                {`${progressValue}%`}
              </Typography>
            </Box>
            <Typography variant="body2" sx={{ color: 'text.secondary', mb: 1.2 }}>
              {progressHeadline}
            </Typography>
            <LinearProgress
              variant="determinate"
              value={progressValue}
              sx={{
                height: 10,
                borderRadius: 5,
                background: 'rgba(212, 175, 55, 0.2)',
                '& .MuiLinearProgress-bar': {
                  background: 'linear-gradient(90deg, #D4AF37, #FFD700)',
                  borderRadius: 5,
                },
              }}
            />
          </Box>
        </Box>
      </Paper>

      <Box sx={{ mb: 5, textAlign: 'center' }}>
        <Button
          variant="contained"
          size="large"
          startIcon={<AutoAwesome />}
          onClick={handleStartReading}
          sx={{
            py: 2.5,
            px: 6,
            fontSize: '1.35rem',
            fontFamily: 'Cinzel, serif',
            fontWeight: 600,
            background: 'linear-gradient(45deg, #D4AF37, #FFD700)',
            color: 'black',
            boxShadow: '0 8px 36px rgba(212, 175, 55, 0.35)',
            borderRadius: 3,
            '&:hover': {
              background: 'linear-gradient(45deg, #B8860B, #D4AF37)',
              boxShadow: '0 12px 48px rgba(212, 175, 55, 0.5)',
              transform: 'translateY(-4px)',
            },
          }}
        >
          开始新的占卜
        </Button>
      </Box>

      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: '1fr', xl: 'minmax(0, 1.45fr) minmax(360px, 0.9fr)' },
          gap: 4,
          alignItems: 'stretch',
          mb: 4,
        }}
      >
        <Paper
          sx={{
            p: { xs: 3, md: 4 },
            borderRadius: 4,
            background: 'linear-gradient(135deg, rgba(26, 26, 46, 0.82) 0%, rgba(22, 33, 62, 0.82) 100%)',
            border: '1px solid rgba(212, 175, 55, 0.2)',
            display: 'flex',
            flexDirection: 'column',
          }}
        >
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 2, mb: 3 }}>
            <Box>
              <Typography
                variant="h4"
                sx={{
                  fontFamily: 'Cinzel, serif',
                  fontWeight: 600,
                  color: 'primary.main',
                  display: 'flex',
                  alignItems: 'center',
                  mb: 1,
                }}
              >
                <Star sx={{ mr: 1.5, fontSize: '2rem' }} />
                快速开始
              </Typography>
              <Typography variant="body1" sx={{ color: 'text.secondary', maxWidth: 620, lineHeight: 1.75 }}>
                选择一个主题，直接进入适合当前问题的抽牌流程。你也可以点击上方按钮，按完整步骤创建新的占卜。
              </Typography>
            </Box>
            <Chip
              label="4 个快捷入口"
              sx={{
                background: 'rgba(212, 175, 55, 0.12)',
                color: 'primary.main',
                border: '1px solid rgba(212, 175, 55, 0.18)',
              }}
            />
          </Box>

          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' }, gap: 3, flex: 1 }}>
            {QUICK_READING_OPTIONS.map((option) => (
              <Card
                key={option.id}
                onClick={() => handleQuickReading(option.id)}
                sx={{
                  background: 'linear-gradient(135deg, rgba(20, 18, 39, 0.95) 0%, rgba(15, 24, 47, 0.94) 100%)',
                  border: '1px solid rgba(212, 175, 55, 0.16)',
                  borderRadius: 4,
                  cursor: 'pointer',
                  transition: visual.enableHoverMotion ? 'all 0.35s ease' : 'border-color 0.2s ease, box-shadow 0.2s ease',
                  position: 'relative',
                  overflow: 'hidden',
                  '&:hover': {
                    transform: visual.enableHoverMotion ? 'translateY(-8px)' : 'none',
                    boxShadow: visual.enableHoverMotion ? `0 16px 48px ${option.color}30` : 'none',
                    borderColor: `${option.color}80`,
                  },
                }}
              >
                <Box
                  sx={{
                    position: 'absolute',
                    inset: 0,
                    background: `
                      linear-gradient(180deg, ${option.color}24 0%, ${option.color}14 26%, rgba(15, 24, 47, 0.10) 54%, rgba(15, 24, 47, 0) 100%),
                      radial-gradient(circle at top left, ${option.color}22 0%, transparent 48%)
                    `,
                    opacity: 1,
                  }}
                />

                <CardContent sx={{ p: { xs: 3, md: 3.5 }, position: 'relative', minHeight: 220 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2.5 }}>
                    <Box
                      sx={{
                        display: 'inline-flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        background: option.gradient,
                        color: 'white',
                        mr: 2,
                        width: 52,
                        height: 52,
                        borderRadius: '16px',
                        boxShadow: `0 10px 24px ${option.color}30`,
                      }}
                    >
                      {option.icon}
                    </Box>
                    <Typography variant="h5" sx={{ fontFamily: 'Cinzel, serif', fontWeight: 600, color: 'text.primary' }}>
                      {option.title}
                    </Typography>
                  </Box>

                  <Typography
                    variant="body1"
                    sx={{
                      color: 'rgba(236, 233, 245, 0.80)',
                      mb: 3,
                      lineHeight: 1.72,
                      textShadow: '0 1px 2px rgba(6, 8, 18, 0.22)',
                      maxWidth: '92%',
                    }}
                  >
                    {option.description}
                  </Typography>

                  <Button
                    variant="outlined"
                    startIcon={<PlayArrow />}
                    sx={{
                      borderColor: option.color,
                      color: option.color,
                      fontSize: '1rem',
                      py: 1,
                      px: 2.5,
                    '&:hover': {
                      background: `${option.color}15`,
                      borderColor: option.color,
                      boxShadow: visual.enableHoverMotion ? `0 0 16px ${option.color}30` : 'none',
                    },
                  }}
                >
                    立即进入
                  </Button>
                </CardContent>
              </Card>
            ))}
          </Box>
        </Paper>

        <Paper
          sx={{
            p: { xs: 3, md: 4 },
            borderRadius: 4,
            background: 'linear-gradient(135deg, rgba(26, 26, 46, 0.82) 0%, rgba(22, 33, 62, 0.82) 100%)',
            border: '1px solid rgba(212, 175, 55, 0.2)',
            display: 'flex',
            flexDirection: 'column',
            justifyContent: 'space-between',
          }}
        >
          <Box sx={{ mb: 3 }}>
            <Typography
              variant="h4"
              sx={{
                fontFamily: 'Cinzel, serif',
                fontWeight: 600,
                color: 'primary.main',
                mb: 1,
                display: 'flex',
                alignItems: 'center',
              }}
            >
              <TrendingUp sx={{ mr: 1.5, fontSize: '2rem' }} />
              数据概览
            </Typography>
            <Typography variant="body1" sx={{ color: 'text.secondary', lineHeight: 1.75 }}>
              把最近的使用情况、完成度与连续记录状态集中放在这里，便于你快速判断当前占卜节奏。
            </Typography>
          </Box>

          <Box sx={{ display: 'grid', gap: 2.2 }}>
            {statsCards.map((stat) => (
              <Card
                key={stat.label}
                sx={{
                  background: 'linear-gradient(135deg, rgba(18, 21, 44, 0.92) 0%, rgba(10, 13, 32, 0.98) 100%)',
                  border: '1px solid rgba(212, 175, 55, 0.18)',
                  borderRadius: 3,
                  minHeight: 126,
                  display: 'flex',
                  alignItems: 'center',
                }}
              >
                <CardContent sx={{ p: 3, width: '100%' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2 }}>
                    <Box>
                      <Typography variant="body1" sx={{ color: 'text.secondary', mb: 1 }}>
                        {stat.label}
                      </Typography>
                      <Typography variant="h3" sx={{ fontFamily: 'Cinzel, serif', fontWeight: 700, color: stat.color }}>
                        {stat.value}
                      </Typography>
                    </Box>
                    <Box
                      sx={{
                        width: 58,
                        height: 58,
                        borderRadius: '18px',
                        display: 'inline-flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        color: stat.color,
                        background: `${stat.color}18`,
                        boxShadow: `0 10px 30px ${stat.color}18`,
                      }}
                    >
                      {stat.icon}
                    </Box>
                  </Box>
                </CardContent>
              </Card>
            ))}
          </Box>

          <Box sx={{ mt: 3 }}>
            <Chip
              label={`连续记录 ${streakDays} 天`}
              variant="outlined"
              sx={{
                color: 'primary.main',
                borderColor: 'primary.main',
                background: 'rgba(212, 175, 55, 0.1)',
                fontFamily: 'Cinzel, serif',
                fontSize: '1rem',
                py: 2.4,
                '&:hover': {
                  background: 'rgba(212, 175, 55, 0.16)',
                },
              }}
            />
          </Box>
        </Paper>
      </Box>

      <Paper
        sx={{
          p: { xs: 3, md: 4 },
          borderRadius: 4,
          background: 'linear-gradient(135deg, rgba(26, 26, 46, 0.82) 0%, rgba(22, 33, 62, 0.82) 100%)',
          border: '1px solid rgba(212, 175, 55, 0.2)',
        }}
      >
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 2, mb: 3, flexWrap: 'wrap' }}>
          <Box>
            <Typography
              variant="h4"
              sx={{
                fontFamily: 'Cinzel, serif',
                fontWeight: 600,
                color: 'primary.main',
                display: 'flex',
                alignItems: 'center',
                mb: 1,
              }}
            >
              <History sx={{ mr: 1.5, fontSize: '2rem' }} />
              最近记录
            </Typography>
            <Typography variant="body1" sx={{ color: 'text.secondary', lineHeight: 1.75, maxWidth: 760 }}>
              最近四次记录会在这里展示问题标题、当前状态与解读摘要。你可以直接收藏，或进入详情页继续查看完整结果。
            </Typography>
          </Box>
          <Button
            variant="outlined"
            onClick={() => navigate(ROUTES.HISTORY)}
            sx={{ borderColor: 'rgba(212, 175, 55, 0.35)', color: 'primary.main' }}
          >
            查看全部记录
          </Button>
        </Box>

        {loadingRecent ? (
          <Typography variant="body2" sx={{ color: 'text.secondary' }}>
            正在同步最近记录...
          </Typography>
        ) : recentOverview.length === 0 ? (
          <Box sx={{ textAlign: 'center', py: 4 }}>
            <History sx={{ fontSize: '3.5rem', color: 'text.secondary', mb: 2 }} />
            <Typography variant="body1" sx={{ color: 'text.secondary', mb: 3 }}>
              你最近的占卜记录会显示在这里，方便随时回看灵感与结果。
            </Typography>
            <Button
              variant="outlined"
              onClick={() => navigate(ROUTES.NEW_READING)}
              sx={{
                borderColor: 'primary.main',
                color: 'primary.main',
                fontSize: '1rem',
                py: 1.2,
                px: 3,
                '&:hover': {
                  background: 'rgba(212, 175, 55, 0.1)',
                },
              }}
            >
              去创建记录
            </Button>
          </Box>
        ) : (
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: 'repeat(2, minmax(0, 1fr))' }, gap: 2.5 }}>
            {recentOverview.map((reading) => {
              const typeMeta = QUESTION_TYPE_META[reading.question_type] || { label: reading.question_type, color: '#D4AF37' };
              const statusMeta = STATUS_META[reading.status] || { label: reading.status, color: 'default' as const };

              return (
                <Paper
                  key={reading.id}
                  sx={{
                    p: 2.8,
                    borderRadius: 3,
                    background: 'linear-gradient(135deg, rgba(8, 10, 24, 0.72) 0%, rgba(15, 17, 36, 0.84) 100%)',
                    border: '1px solid rgba(212, 175, 55, 0.14)',
                    display: 'flex',
                    flexDirection: 'column',
                    justifyContent: 'space-between',
                    gap: 2.1,
                    position: 'relative',
                    overflow: 'hidden',
                    transition: visual.enableHoverMotion
                      ? 'transform 220ms ease, border-color 220ms ease, box-shadow 220ms ease'
                      : 'border-color 220ms ease, box-shadow 220ms ease',
                    '&::before': {
                      content: '""',
                      position: 'absolute',
                      top: 0,
                      left: 0,
                      right: 0,
                      height: '3px',
                      background: `linear-gradient(90deg, ${typeMeta.color} 0%, rgba(212, 175, 55, 0.9) 100%)`,
                      opacity: 0.92,
                    },
                    '&:hover': {
                      transform: visual.enableHoverMotion ? 'translateY(-3px)' : 'none',
                      borderColor: 'rgba(212, 175, 55, 0.22)',
                      boxShadow: visual.enableHoverMotion ? '0 16px 36px rgba(6, 8, 18, 0.22)' : 'none',
                    },
                  }}
                >
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 1.6, flexWrap: 'wrap', alignItems: 'flex-start' }}>
                    <Box sx={{ flex: 1, minWidth: 220, pt: 0.4 }}>
                      <Typography
                        variant="subtitle1"
                        sx={{
                          color: 'text.primary',
                          mb: 0.7,
                          lineHeight: 1.46,
                          fontSize: { xs: '1.08rem', md: '1.2rem' },
                          fontWeight: 700,
                          letterSpacing: '0.012em',
                          maxWidth: '95%',
                          textWrap: 'balance',
                        }}
                      >
                        {getReadingQuestionDisplay({
                          question: reading.question,
                          questionType: reading.question_type,
                          spreadName: getRecentSpreadName(reading),
                        })}
                      </Typography>
                      <Typography
                        variant="caption"
                        sx={{
                          color: 'rgba(218, 214, 230, 0.70)',
                          fontSize: '0.78rem',
                          letterSpacing: '0.04em',
                          textTransform: 'uppercase',
                        }}
                      >
                        {formatSmartDate(reading.created_at)}
                      </Typography>
                    </Box>

                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.9, flexWrap: 'wrap', justifyContent: 'flex-end' }}>
                      {getRecentSpreadName(reading) && (
                        <Chip
                          label={getRecentSpreadName(reading)}
                          size="small"
                          variant="outlined"
                          sx={{
                            borderColor: 'rgba(255,255,255,0.14)',
                            color: 'rgba(236, 233, 245, 0.76)',
                            background: 'rgba(255,255,255,0.03)',
                          }}
                        />
                      )}
                      <Chip
                        label={typeMeta.label}
                        size="small"
                        variant="outlined"
                        sx={{
                          borderColor: `${typeMeta.color}B5`,
                          color: typeMeta.color,
                          background: `${typeMeta.color}10`,
                        }}
                      />
                      <Chip label={statusMeta.label} size="small" color={statusMeta.color} />
                    </Box>
                  </Box>

                  <Box
                    sx={{
                      p: 2.2,
                      borderRadius: 3,
                      background: 'linear-gradient(135deg, rgba(212, 175, 55, 0.09) 0%, rgba(62, 28, 95, 0.22) 52%, rgba(18, 20, 40, 0.34) 100%)',
                      border: '1px solid rgba(212, 175, 55, 0.13)',
                    }}
                  >
                    <Typography
                      variant="caption"
                      sx={{
                        color: 'primary.main',
                        letterSpacing: '0.14em',
                        display: 'block',
                        mb: 0.9,
                        fontSize: '0.75rem',
                        fontWeight: 700,
                      }}
                    >
                      解读摘要
                    </Typography>
                    <Typography
                      variant="body2"
                      sx={{
                        color: 'rgba(237, 233, 246, 0.82)',
                        lineHeight: 1.78,
                        fontSize: '0.96rem',
                      }}
                    >
                      {getInterpretationSummary(reading)}
                    </Typography>
                  </Box>

                  <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 1.5, flexWrap: 'wrap' }}>
                    <ReadingFavoriteButton
                      readingId={reading.id}
                      isFavorite={reading.is_favorite}
                      onChanged={(nextValue) => handleFavoriteChanged(reading.id, nextValue)}
                    />
                    <Button
                      size="small"
                      variant="outlined"
                      onClick={() => navigate(ROUTES.READING_DETAIL.replace(':id', reading.id.toString()))}
                      sx={{ borderColor: 'rgba(212, 175, 55, 0.35)', color: 'primary.main' }}
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
    </Box>
  );
};

export default HomePage;
