import React from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Button,
  IconButton,
  Chip,
  Avatar,
  LinearProgress,
} from '@mui/material';
import {
  AutoAwesome,
  History,
  TrendingUp,
  Star,
  PlayArrow,
  Favorite,
  WorkOutline,
  AccountBalance,
  LocalHospital,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../stores/authStore';
import { useGameStore } from '../../stores/gameStore';
import { ROUTES } from '../../routes/routeConfig';

// 快速占卜选项
const quickReadingOptions = [
  {
    id: 'love',
    title: '感情运势',
    description: '探索您的爱情与人际关系',
    icon: <Favorite />,
    color: '#FF6B9D',
    gradient: 'linear-gradient(135deg, #FF6B9D 0%, #C44569 100%)',
  },
  {
    id: 'career',
    title: '事业发展',
    description: '了解您的职业道路与机遇',
    icon: <WorkOutline />,
    color: '#4ECDC4',
    gradient: 'linear-gradient(135deg, #4ECDC4 0%, #44A08D 100%)',
  },
  {
    id: 'finance',
    title: '财运分析',
    description: '洞察您的财务状况与投资',
    icon: <AccountBalance />,
    color: '#FFD93D',
    gradient: 'linear-gradient(135deg, #FFD93D 0%, #F39C12 100%)',
  },
  {
    id: 'health',
    title: '健康指引',
    description: '关注您的身心健康状态',
    icon: <LocalHospital />,
    color: '#6BCF7F',
    gradient: 'linear-gradient(135deg, #6BCF7F 0%, #4D7C0F 100%)',
  },
];

// 统计数据
const statsData = [
  { label: '总占卜次数', value: 23, icon: <AutoAwesome />, color: '#D4AF37' },
  { label: '准确预测', value: 18, icon: <Star />, color: '#4ECDC4' },
  { label: '连续签到', value: 7, icon: <TrendingUp />, color: '#FF6B9D' },
];

const HomePage: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuthStore();
  const { startNewReading } = useGameStore();

  const handleQuickReading = (type: string) => {
    startNewReading();
    navigate(ROUTES.NEW_READING, { state: { quickType: type } });
  };

  const handleStartReading = () => {
    startNewReading();
    navigate(ROUTES.NEW_READING);
  };

  // 获取当前时间的问候语
  const getGreeting = () => {
    const hour = new Date().getHours();
    if (hour < 6) return '夜深了';
    if (hour < 12) return '早上好';
    if (hour < 18) return '下午好';
    return '晚上好';
  };

  return (
    <Box sx={{ py: 3 }}>
      {/* 欢迎区域 */}
      <Box
        sx={{
          mb: 4,
          p: 4,
          background: 'linear-gradient(135deg, rgba(212, 175, 55, 0.1) 0%, rgba(106, 5, 114, 0.1) 100%)',
          borderRadius: 3,
          border: '1px solid rgba(212, 175, 55, 0.2)',
          position: 'relative',
          overflow: 'hidden',
        }}
        className="fade-in"
      >
        <Box sx={{ position: 'relative', zIndex: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
            <Avatar
              sx={{
                width: 64,
                height: 64,
                background: 'linear-gradient(45deg, #D4AF37, #FFD700)',
                color: 'black',
                fontSize: '1.5rem',
                fontFamily: 'Cinzel, serif',
                mr: 3,
              }}
              src={user?.avatar_url}
            >
              {user?.nickname?.[0] || user?.username?.[0] || 'U'}
            </Avatar>
            <Box>
              <Typography
                variant="h4"
                sx={{
                  fontFamily: 'Cinzel, serif',
                  fontWeight: 600,
                  color: 'primary.main',
                  mb: 0.5,
                }}
              >
                {getGreeting()}，{user?.nickname || user?.username}
              </Typography>
              <Typography
                variant="body1"
                sx={{
                  color: 'text.secondary',
                  fontStyle: 'italic',
                }}
              >
                欢迎回到神秘的塔罗世界，星辰正在为您指引方向
              </Typography>
            </Box>
          </Box>

          {/* 今日运势进度 */}
          <Box sx={{ mt: 3 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                今日灵感值
              </Typography>
              <Typography variant="body2" sx={{ color: 'primary.main', fontWeight: 600 }}>
                78%
              </Typography>
            </Box>
            <LinearProgress
              variant="determinate"
              value={78}
              sx={{
                height: 8,
                borderRadius: 4,
                background: 'rgba(212, 175, 55, 0.2)',
                '& .MuiLinearProgress-bar': {
                  background: 'linear-gradient(90deg, #D4AF37, #FFD700)',
                  borderRadius: 4,
                },
              }}
            />
          </Box>
        </Box>

        {/* 背景装饰 */}
        <Box
          sx={{
            position: 'absolute',
            top: -20,
            right: -20,
            width: 100,
            height: 100,
            background: 'radial-gradient(circle, rgba(212, 175, 55, 0.2) 0%, transparent 70%)',
            borderRadius: '50%',
          }}
        />
      </Box>

      {/* 快速开始按钮 */}
      <Box sx={{ mb: 4, textAlign: 'center' }} className="slide-up">
        <Button
          variant="contained"
          size="large"
          startIcon={<AutoAwesome />}
          onClick={handleStartReading}
          sx={{
            py: 2,
            px: 4,
            fontSize: '1.2rem',
            fontFamily: 'Cinzel, serif',
            fontWeight: 600,
            background: 'linear-gradient(45deg, #D4AF37, #FFD700)',
            color: 'black',
            boxShadow: '0 8px 32px rgba(212, 175, 55, 0.3)',
            '&:hover': {
              background: 'linear-gradient(45deg, #B8860B, #D4AF37)',
              boxShadow: '0 12px 40px rgba(212, 175, 55, 0.4)',
              transform: 'translateY(-4px)',
            },
          }}
        >
          开始新的占卜
        </Button>
      </Box>

      <Box sx={{ display: 'flex', flexDirection: { xs: 'column', md: 'row' }, gap: 3 }}>
        {/* 左侧：快速占卜选项 */}
        <Box sx={{ flex: 2 }}>
          <Typography
            variant="h5"
            sx={{
              fontFamily: 'Cinzel, serif',
              fontWeight: 600,
              color: 'primary.main',
              mb: 3,
              display: 'flex',
              alignItems: 'center',
            }}
          >
            <Star sx={{ mr: 1 }} />
            快速占卜
          </Typography>

          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' }, gap: 2 }}>
            {quickReadingOptions.map((option, index) => (
              <Box key={option.id}>
                <Card
                  sx={{
                    background: 'linear-gradient(135deg, rgba(26, 26, 46, 0.8) 0%, rgba(22, 33, 62, 0.8) 100%)',
                    border: '1px solid rgba(212, 175, 55, 0.2)',
                    borderRadius: 3,
                    cursor: 'pointer',
                    transition: 'all 0.3s ease',
                    position: 'relative',
                    overflow: 'hidden',
                    '&:hover': {
                      transform: 'translateY(-8px)',
                      boxShadow: '0 12px 40px rgba(212, 175, 55, 0.2)',
                      borderColor: 'rgba(212, 175, 55, 0.5)',
                    },
                  }}
                  className="scale-in"
                  style={{ animationDelay: `${index * 0.1}s` }}
                  onClick={() => handleQuickReading(option.id)}
                >
                  {/* 背景渐变 */}
                  <Box
                    sx={{
                      position: 'absolute',
                      top: 0,
                      left: 0,
                      right: 0,
                      height: '50%',
                      background: option.gradient,
                      opacity: 0.1,
                    }}
                  />

                  <CardContent sx={{ p: 3, position: 'relative' }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                      <IconButton
                        sx={{
                          background: option.gradient,
                          color: 'white',
                          mr: 2,
                          '&:hover': {
                            background: option.gradient,
                            transform: 'scale(1.1)',
                          },
                        }}
                      >
                        {option.icon}
                      </IconButton>
                      <Typography
                        variant="h6"
                        sx={{
                          fontFamily: 'Cinzel, serif',
                          fontWeight: 600,
                          color: 'text.primary',
                        }}
                      >
                        {option.title}
                      </Typography>
                    </Box>

                    <Typography
                      variant="body2"
                      sx={{
                        color: 'text.secondary',
                        mb: 2,
                        lineHeight: 1.6,
                      }}
                    >
                      {option.description}
                    </Typography>

                    <Button
                      variant="outlined"
                      size="small"
                      startIcon={<PlayArrow />}
                      sx={{
                        borderColor: 'primary.main',
                        color: 'primary.main',
                        '&:hover': {
                          background: 'rgba(212, 175, 55, 0.1)',
                          borderColor: 'primary.light',
                        },
                      }}
                    >
                      立即占卜
                    </Button>
                  </CardContent>
                </Card>
              </Box>
            ))}
          </Box>
        </Box>

        {/* 右侧：统计信息和历史 */}
        <Box sx={{ flex: 1 }}>
          {/* 统计数据 */}
          <Typography
            variant="h5"
            sx={{
              fontFamily: 'Cinzel, serif',
              fontWeight: 600,
              color: 'primary.main',
              mb: 3,
              display: 'flex',
              alignItems: 'center',
            }}
          >
            <TrendingUp sx={{ mr: 1 }} />
            我的数据
          </Typography>

          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mb: 4 }}>
            {statsData.map((stat, index) => (
              <Box key={stat.label}>
                <Card
                  sx={{
                    background: 'linear-gradient(135deg, rgba(26, 26, 46, 0.8) 0%, rgba(22, 33, 62, 0.8) 100%)',
                    border: '1px solid rgba(212, 175, 55, 0.2)',
                    borderRadius: 2,
                  }}
                  className="slide-up"
                  style={{ animationDelay: `${index * 0.1}s` }}
                >
                  <CardContent sx={{ p: 2 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                      <Box>
                        <Typography
                          variant="h4"
                          sx={{
                            fontFamily: 'Cinzel, serif',
                            fontWeight: 700,
                            color: stat.color,
                          }}
                        >
                          {stat.value}
                        </Typography>
                        <Typography
                          variant="body2"
                          sx={{ color: 'text.secondary' }}
                        >
                          {stat.label}
                        </Typography>
                      </Box>
                      <IconButton
                        sx={{
                          color: stat.color,
                          background: `${stat.color}20`,
                        }}
                      >
                        {stat.icon}
                      </IconButton>
                    </Box>
                  </CardContent>
                </Card>
              </Box>
            ))}
          </Box>

          {/* 最近占卜 */}
          <Typography
            variant="h6"
            sx={{
              fontFamily: 'Cinzel, serif',
              fontWeight: 600,
              color: 'primary.main',
              mb: 2,
              display: 'flex',
              alignItems: 'center',
            }}
          >
            <History sx={{ mr: 1 }} />
            最近占卜
          </Typography>

          <Card
            sx={{
              background: 'linear-gradient(135deg, rgba(26, 26, 46, 0.8) 0%, rgba(22, 33, 62, 0.8) 100%)',
              border: '1px solid rgba(212, 175, 55, 0.2)',
              borderRadius: 2,
            }}
          >
            <CardContent sx={{ p: 3 }}>
              <Box sx={{ textAlign: 'center', py: 2 }}>
                <History sx={{ fontSize: '3rem', color: 'text.secondary', mb: 2 }} />
                <Typography
                  variant="body2"
                  sx={{ color: 'text.secondary', mb: 2 }}
                >
                  您还没有占卜记录
                </Typography>
                <Button
                  variant="outlined"
                  size="small"
                  onClick={() => navigate(ROUTES.NEW_READING)}
                  sx={{
                    borderColor: 'primary.main',
                    color: 'primary.main',
                    '&:hover': {
                      background: 'rgba(212, 175, 55, 0.1)',
                    },
                  }}
                >
                  开始第一次占卜
                </Button>
              </Box>
            </CardContent>
          </Card>

          {/* 每日签到 */}
          <Box sx={{ mt: 3 }}>
            <Chip
              label="已连续签到 7 天"
              variant="outlined"
              sx={{
                color: 'primary.main',
                borderColor: 'primary.main',
                background: 'rgba(212, 175, 55, 0.1)',
                fontFamily: 'Cinzel, serif',
                '&:hover': {
                  background: 'rgba(212, 175, 55, 0.2)',
                },
              }}
            />
          </Box>
        </Box>
      </Box>
    </Box>
  );
};

export default HomePage;