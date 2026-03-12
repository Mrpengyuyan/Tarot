import React, { useState, useEffect } from 'react';
import {
  Container,
  Typography,
  Box,
  Button,
  Paper,
  Card,
  CardContent,
  Chip,
  Alert,
  CircularProgress,
} from '@mui/material';
import {
  PlayArrow as StartIcon,
  Refresh as RefreshIcon,
  Visibility as ViewIcon,
  Psychology as AiIcon,
} from '@mui/icons-material';
import TarotCard from '../../components/Tarot/TarotCard';
import CardSpread from '../../components/Tarot/CardSpread';
import { tarotService, localDrawingUtils, DrawnCard } from '../../services/tarotService';
import { aiService } from '../../services/aiService';
import { useAuthStore } from '../../stores/authStore';
// import { cardService } from '../../services/cardService';
// import { spreadService } from '../../services/spreadService';
// import { readingService } from '../../services/readingService';
import { TarotCard as TarotCardType, SpreadType } from '../../types/api';

const DemoPage: React.FC = () => {
  const [cards, setCards] = useState<TarotCardType[]>([]);
  const [spreads, setSpreads] = useState<SpreadType[]>([]);
  const [selectedSpread, setSelectedSpread] = useState<SpreadType | null>(null);
  const [drawnCards, setDrawnCards] = useState<DrawnCard[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>('');
  const [revealedCards, setRevealedCards] = useState<number[]>([]);
  const [interpretation, setInterpretation] = useState<string>('');
  const [interpretingLoading] = useState(false);
  const [aiHealthy, setAiHealthy] = useState(false);
  const { isLoggedIn } = useAuthStore();

  // 加载数据
  useEffect(() => {
    loadData();
    checkAIHealth();
  }, []);

  const loadData = async () => {
    setLoading(true);
    setError('');
    try {
      const [cardsData, spreadsData] = await Promise.all([
        tarotService.getAllCards(),
        tarotService.getAllSpreads(),
      ]);

      setCards(cardsData);
      setSpreads(spreadsData);

      // 默认选择第一个牌阵
      if (spreadsData.length > 0) {
        setSelectedSpread(spreadsData[0]);
      }
    } catch (err) {
      setError('加载数据失败，请检查后端服务是否正常运行');
      console.error('加载数据失败:', err);
    } finally {
      setLoading(false);
    }
  };

  // 演示抽牌功能
  const handleDrawCards = () => {
    if (!selectedSpread || cards.length === 0) {
      setError('请选择牌阵或等待数据加载完成');
      return;
    }

    try {
      const drawn = localDrawingUtils.drawFromDeck(cards, selectedSpread.card_count);
      setDrawnCards(drawn);
      setRevealedCards([]);
      setError('');

      // 逐渐翻开卡片
      setTimeout(() => {
        drawn.forEach((_, index) => {
          setTimeout(() => {
            setRevealedCards(prev => [...prev, index]);
          }, index * 800);
        });
      }, 500);
    } catch (err) {
      setError('抽牌失败: ' + (err instanceof Error ? err.message : '未知错误'));
    }
  };

  // 检查AI健康状态
  const checkAIHealth = async () => {
    try {
      const isHealthy = await aiService.isAIAvailable();
      setAiHealthy(isHealthy);
    } catch (error) {
      setAiHealthy(false);
      console.error('AI健康检查失败:', error);
    }
  };

  // 获取AI解读
  const handleGetInterpretation = async () => {
    if (!selectedSpread || drawnCards.length === 0) {
      setError('请先抽取塔罗牌');
      return;
    }

    if (!isLoggedIn) {
      setError('AI解读需要登录，请在“新占卜”流程中体验');
      return;
    }

    setError('演示页暂不支持AI解读，请在占卜页面体验完整流程');
  };

  // 重置演示
  const handleReset = () => {
    setDrawnCards([]);
    setRevealedCards([]);
    setInterpretation('');
    setError('');
  };

  if (loading) {
    return (
      <Container maxWidth="lg" sx={{ py: 8, textAlign: 'center' }}>
        <CircularProgress size={60} />
        <Typography variant="h6" sx={{ mt: 2 }}>
          正在加载塔罗牌数据...
        </Typography>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      {/* 页面标题 */}
      <Box sx={{ textAlign: 'center', mb: 4 }}>
        <Typography
          variant="h3"
          component="h1"
          sx={{
            fontWeight: 'bold',
            background: 'linear-gradient(45deg, #d4af37 30%, #f4e4bc 90%)',
            backgroundClip: 'text',
            WebkitBackgroundClip: 'text',
            WebkitTextFillColor: 'transparent',
            mb: 2,
          }}
        >
          🔮 塔罗牌演示系统
        </Typography>
        <Typography variant="h6" sx={{ color: 'text.secondary', mb: 3 }}>
          展示塔罗牌抽牌功能和牌阵布局
        </Typography>
      </Box>

      {/* 错误提示 */}
      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      {/* 数据统计 */}
      <Paper elevation={2} sx={{ p: 3, mb: 4 }}>
        <Typography variant="h6" sx={{ mb: 2 }}>
          系统状态
        </Typography>
        <Box sx={{ display: 'flex', justifyContent: 'space-around', flexWrap: 'wrap', gap: 2 }}>
          <Box sx={{ textAlign: 'center', flex: 1, minWidth: 150 }}>
            <Typography variant="h4" color="primary.main">
              {cards.length}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              塔罗牌总数
            </Typography>
          </Box>
          <Box sx={{ textAlign: 'center', flex: 1, minWidth: 150 }}>
            <Typography variant="h4" color="secondary.main">
              {spreads.length}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              可用牌阵
            </Typography>
          </Box>
          <Box sx={{ textAlign: 'center', flex: 1, minWidth: 150 }}>
            <Typography variant="h4" color="success.main">
              {drawnCards.length}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              已抽牌数
            </Typography>
          </Box>
          <Box sx={{ textAlign: 'center', flex: 1, minWidth: 150 }}>
            <Typography variant="h4" color={aiHealthy ? "success.main" : "error.main"}>
              {aiHealthy ? '✓' : '✗'}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              AI服务状态
            </Typography>
          </Box>
        </Box>
      </Paper>

      {/* 牌阵选择 */}
      <Paper elevation={2} sx={{ p: 3, mb: 4 }}>
        <Typography variant="h6" sx={{ mb: 2 }}>
          选择牌阵
        </Typography>
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2 }}>
          {spreads.map((spread) => (
            <Box key={spread.id} sx={{ flex: '1 1 300px', minWidth: 300 }}>
              <Card
                sx={{
                  cursor: 'pointer',
                  border: selectedSpread?.id === spread.id ? '2px solid' : '1px solid',
                  borderColor: selectedSpread?.id === spread.id ? 'primary.main' : 'divider',
                  '&:hover': {
                    boxShadow: 3,
                  },
                }}
                onClick={() => setSelectedSpread(spread)}
              >
                <CardContent>
                  <Typography variant="h6" sx={{ mb: 1 }}>
                    {spread.name}
                  </Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                    {spread.description}
                  </Typography>
                  <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                    <Chip label={`${spread.card_count}张牌`} size="small" color="primary" />
                    <Chip label={`难度${spread.difficulty_level}`} size="small" variant="outlined" />
                    {spread.is_beginner_friendly && (
                      <Chip label="新手友好" size="small" color="success" variant="outlined" />
                    )}
                  </Box>
                </CardContent>
              </Card>
            </Box>
          ))}
        </Box>
      </Paper>

      {/* 操作按钮 */}
      <Box sx={{ textAlign: 'center', mb: 4 }}>
        <Button
          variant="contained"
          size="large"
          onClick={handleDrawCards}
          disabled={!selectedSpread || cards.length === 0}
          startIcon={<StartIcon />}
          sx={{ mr: 2 }}
        >
          抽取塔罗牌
        </Button>
        <Button
          variant="contained"
          size="large"
          color="secondary"
          onClick={handleGetInterpretation}
          disabled={!drawnCards.length || !aiHealthy || interpretingLoading || !isLoggedIn}
          startIcon={interpretingLoading ? <CircularProgress size={20} color="inherit" /> : <AiIcon />}
          sx={{ mr: 2 }}
        >
          {interpretingLoading ? 'AI解读中...' : 'AI解读'}
        </Button>
        <Button
          variant="outlined"
          size="large"
          onClick={handleReset}
          startIcon={<RefreshIcon />}
          sx={{ mr: 2 }}
        >
          重置
        </Button>
        <Button
          variant="text"
          size="large"
          onClick={loadData}
          startIcon={<ViewIcon />}
        >
          刷新数据
        </Button>
      </Box>

      {/* 抽牌结果展示 */}
      {selectedSpread && drawnCards.length > 0 && (
        <Paper elevation={2} sx={{ p: 3, mb: 4 }}>
          <CardSpread
            spread={selectedSpread}
            drawnCards={drawnCards}
            isRevealing={revealedCards.length < drawnCards.length}
            revealedPositions={revealedCards}
            onCardClick={(card, position) => {
              console.log('点击卡片:', card.name_zh, '位置:', position);
            }}
          />
        </Paper>
      )}

      {/* AI解读结果 */}
      {interpretation && (
        <Paper elevation={2} sx={{ p: 3, mb: 4 }}>
          <Typography variant="h6" sx={{ mb: 2, display: 'flex', alignItems: 'center' }}>
            <AiIcon sx={{ mr: 1 }} />
            AI塔罗解读
          </Typography>
          <Box sx={{
            whiteSpace: 'pre-wrap',
            lineHeight: 1.8,
            fontSize: '1rem',
            color: 'text.primary',
            backgroundColor: 'grey.50',
            p: 3,
            borderRadius: 2,
            border: '1px solid',
            borderColor: 'divider'
          }}>
            {interpretation}
          </Box>
        </Paper>
      )}

      {/* 样例卡片展示 */}
      {cards.length > 0 && (
        <Paper elevation={2} sx={{ p: 3, mt: 4 }}>
          <Typography variant="h6" sx={{ mb: 3 }}>
            塔罗牌样例展示
          </Typography>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 3, justifyContent: 'center' }}>
            {cards.slice(0, 6).map((card, index) => (
              <Box key={card.id}>
                <TarotCard
                  card={card}
                  size="small"
                  showDetails={true}
                  isReversed={index % 3 === 0}
                  onCardClick={(card) => {
                    console.log('点击卡片:', card.name_zh);
                  }}
                />
              </Box>
            ))}
          </Box>
        </Paper>
      )}
    </Container>
  );
};

export default DemoPage;
