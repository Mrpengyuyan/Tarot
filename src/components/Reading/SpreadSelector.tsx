import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Button,
  Chip,
  Paper,
  Fade,
  Zoom,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Alert,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  AutoAwesome,
  Psychology,
  Favorite,
  TrendingUp,
  LocalHospital,
  Balance,
  Info,
  Star,
  Speed,
  Explore,
} from '@mui/icons-material';
import { SpreadType } from '../../types/api';
import { useGameStore, QuestionType, getQuestionTypeLabel } from '../../stores/gameStore';
import { spreadService } from '../../services/spreadService';

interface SpreadSelectorProps {
  onSpreadSelected: (spread: SpreadType) => void;
  preselectedQuestionType?: QuestionType;
}

const getQuestionTypeIcon = (type: QuestionType) => {
  switch (type) {
    case 'love': return <Favorite sx={{ color: '#e91e63' }} />;
    case 'career': return <TrendingUp sx={{ color: '#2196f3' }} />;
    case 'finance': return <Balance sx={{ color: '#4caf50' }} />;
    case 'health': return <LocalHospital sx={{ color: '#ff9800' }} />;
    default: return <AutoAwesome sx={{ color: '#9c27b0' }} />;
  }
};

const getDifficultyLabel = (level: number): { label: string; color: string } => {
  if (level <= 2) return { label: '初级', color: '#4caf50' };
  if (level <= 3) return { label: '中级', color: '#ff9800' };
  return { label: '高级', color: '#f44336' };
};

const SpreadSelector: React.FC<SpreadSelectorProps> = ({
  onSpreadSelected,
  preselectedQuestionType = 'general'
}) => {
  const { availableSpreads, setSpreads, setLoading, setError } = useGameStore();
  const [selectedQuestionType, setSelectedQuestionType] = useState<QuestionType>(preselectedQuestionType);
  const [filteredSpreads, setFilteredSpreads] = useState<SpreadType[]>([]);
  const [selectedSpread, setSelectedSpread] = useState<SpreadType | null>(null);
  const [filterBy, setFilterBy] = useState<'all' | 'beginner' | 'quick' | 'detailed'>('all');
  const [showSpreadDetails, setShowSpreadDetails] = useState<number | null>(null);

  // 加载牌阵数据
  useEffect(() => {
    const loadSpreads = async () => {
      try {
        setLoading(true, '正在加载牌阵...');
        const spreads = await spreadService.getAllSpreads();
        setSpreads(spreads);
      } catch (error) {
        console.error('加载牌阵失败:', error);
        setError('加载牌阵失败，请稍后重试');
      } finally {
        setLoading(false);
      }
    };

    if (availableSpreads.length === 0) {
      loadSpreads();
    }
  }, [availableSpreads.length, setSpreads, setLoading, setError]);

  // 根据选择的问题类型和过滤条件筛选牌阵
  useEffect(() => {
    let filtered = [...availableSpreads];

    // 按问题类型筛选
    if (selectedQuestionType !== 'general') {
      filtered = filtered.filter(spread =>
        spreadService.isSpreadSuitableFor(spread, selectedQuestionType)
      );
    }

    // 按其他条件筛选
    switch (filterBy) {
      case 'beginner':
        filtered = filtered.filter(spread => spread.is_beginner_friendly);
        break;
      case 'quick':
        filtered = filtered.filter(spread => spread.card_count <= 3);
        break;
      case 'detailed':
        filtered = filtered.filter(spread => spread.card_count >= 7);
        break;
    }

    // 按难度和使用次数排序
    filtered.sort((a, b) => {
      // 优先显示初学者友好的牌阵
      if (a.is_beginner_friendly && !b.is_beginner_friendly) return -1;
      if (!a.is_beginner_friendly && b.is_beginner_friendly) return 1;

      // 然后按使用次数排序
      return (b.usage_count || 0) - (a.usage_count || 0);
    });

    setFilteredSpreads(filtered);
  }, [availableSpreads, selectedQuestionType, filterBy]);

  const handleSpreadSelect = (spread: SpreadType) => {
    setSelectedSpread(spread);
  };

  const handleConfirmSelection = () => {
    if (selectedSpread) {
      onSpreadSelected(selectedSpread);
    }
  };

  const getSpreadComplexityColor = (spread: SpreadType): string => {
    const complexity = spreadService.calculateComplexityScore(spread);
    if (complexity <= 50) return '#4caf50';
    if (complexity <= 100) return '#ff9800';
    return '#f44336';
  };

  return (
    <Box sx={{ p: 3 }}>
      {/* 标题 */}
      <Box sx={{ textAlign: 'center', mb: 4 }}>
        <Psychology sx={{ fontSize: '3rem', color: 'primary.main', mb: 2 }} />
        <Typography
          variant="h4"
          sx={{
            fontFamily: 'Cinzel, serif',
            fontWeight: 700,
            color: 'primary.main',
            mb: 2,
          }}
        >
          选择您的塔罗牌阵
        </Typography>
        <Typography
          variant="body1"
          sx={{
            color: 'text.secondary',
            fontStyle: 'italic',
            maxWidth: 600,
            mx: 'auto',
          }}
        >
          不同的牌阵有着不同的侧重点，选择最适合您问题的牌阵，将为您带来更精准的指引
        </Typography>
      </Box>

      {/* 筛选控件 */}
      <Paper elevation={1} sx={{ p: 3, mb: 4, background: 'rgba(26, 26, 46, 0.3)' }}>
        <Box sx={{ display: 'flex', flexDirection: { xs: 'column', md: 'row' }, gap: 3, alignItems: 'center' }}>
          <Box sx={{ flex: 1 }}>
            <FormControl fullWidth>
              <InputLabel>问题类型</InputLabel>
              <Select
                value={selectedQuestionType}
                label="问题类型"
                onChange={(e) => setSelectedQuestionType(e.target.value as QuestionType)}
              >
                <MenuItem value="general">
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    {getQuestionTypeIcon('general')}
                    {getQuestionTypeLabel('general')}
                  </Box>
                </MenuItem>
                <MenuItem value="love">
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    {getQuestionTypeIcon('love')}
                    {getQuestionTypeLabel('love')}
                  </Box>
                </MenuItem>
                <MenuItem value="career">
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    {getQuestionTypeIcon('career')}
                    {getQuestionTypeLabel('career')}
                  </Box>
                </MenuItem>
                <MenuItem value="finance">
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    {getQuestionTypeIcon('finance')}
                    {getQuestionTypeLabel('finance')}
                  </Box>
                </MenuItem>
                <MenuItem value="health">
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    {getQuestionTypeIcon('health')}
                    {getQuestionTypeLabel('health')}
                  </Box>
                </MenuItem>
              </Select>
            </FormControl>
          </Box>
          <Box sx={{ flex: 1 }}>
            <FormControl fullWidth>
              <InputLabel>筛选条件</InputLabel>
              <Select
                value={filterBy}
                label="筛选条件"
                onChange={(e) => setFilterBy(e.target.value as any)}
              >
                <MenuItem value="all">
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Explore />
                    全部牌阵
                  </Box>
                </MenuItem>
                <MenuItem value="beginner">
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Star />
                    初学者友好
                  </Box>
                </MenuItem>
                <MenuItem value="quick">
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Speed />
                    快速占卜 (≤3张牌)
                  </Box>
                </MenuItem>
                <MenuItem value="detailed">
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Psychology />
                    深度解读 (≥7张牌)
                  </Box>
                </MenuItem>
              </Select>
            </FormControl>
          </Box>
        </Box>
      </Paper>

      {/* 牌阵列表 */}
      {filteredSpreads.length === 0 ? (
        <Alert severity="info" sx={{ textAlign: 'center' }}>
          没有找到符合条件的牌阵，请尝试调整筛选条件
        </Alert>
      ) : (
        <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: 3 }}>
          {filteredSpreads.map((spread, index) => {
            const difficulty = getDifficultyLabel(spread.difficulty_level);
            const isSelected = selectedSpread?.id === spread.id;

            return (
              <Box key={spread.id}>
                <Fade in timeout={300 + index * 100}>
                  <Card
                    sx={{
                      height: '100%',
                      background: isSelected
                        ? 'linear-gradient(135deg, rgba(212, 175, 55, 0.2) 0%, rgba(212, 175, 55, 0.1) 100%)'
                        : 'linear-gradient(135deg, rgba(26, 26, 46, 0.8) 0%, rgba(22, 33, 62, 0.8) 100%)',
                      border: isSelected
                        ? '2px solid #d4af37'
                        : '1px solid rgba(212, 175, 55, 0.2)',
                      borderRadius: 3,
                      transition: 'all 0.3s ease',
                      cursor: 'pointer',
                      transform: isSelected ? 'scale(1.02)' : 'scale(1)',
                      '&:hover': {
                        transform: isSelected ? 'scale(1.02)' : 'scale(1.05)',
                        boxShadow: '0 12px 40px rgba(212, 175, 55, 0.3)',
                        borderColor: 'rgba(212, 175, 55, 0.5)',
                      },
                    }}
                    onClick={() => handleSpreadSelect(spread)}
                  >
                    <CardContent sx={{ p: 3, height: '100%', display: 'flex', flexDirection: 'column' }}>
                      {/* 牌阵名称和标签 */}
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
                        <Typography
                          variant="h6"
                          sx={{
                            fontFamily: 'Cinzel, serif',
                            fontWeight: 600,
                            color: 'text.primary',
                            flex: 1,
                          }}
                        >
                          {spread.name}
                        </Typography>
                        <Tooltip title="查看详情">
                          <IconButton
                            size="small"
                            onClick={(e) => {
                              e.stopPropagation();
                              setShowSpreadDetails(showSpreadDetails === spread.id ? null : spread.id);
                            }}
                          >
                            <Info />
                          </IconButton>
                        </Tooltip>
                      </Box>

                      {/* 牌阵描述 */}
                      <Typography
                        variant="body2"
                        sx={{
                          color: 'text.secondary',
                          mb: 2,
                          lineHeight: 1.6,
                          flex: 1,
                        }}
                      >
                        {spread.description}
                      </Typography>

                      {/* 牌阵属性 */}
                      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 2 }}>
                        <Chip
                          size="small"
                          label={`${spread.card_count}张牌`}
                          sx={{
                            backgroundColor: getSpreadComplexityColor(spread) + '20',
                            color: getSpreadComplexityColor(spread),
                          }}
                        />
                        <Chip
                          size="small"
                          label={difficulty.label}
                          sx={{
                            backgroundColor: difficulty.color + '20',
                            color: difficulty.color,
                          }}
                        />
                        {spread.is_beginner_friendly && (
                          <Chip
                            size="small"
                            label="新手友好"
                            icon={<Star sx={{ fontSize: '0.8rem' }} />}
                            sx={{
                              backgroundColor: 'rgba(76, 175, 80, 0.2)',
                              color: '#4caf50',
                            }}
                          />
                        )}
                      </Box>

                      {/* 详细信息展开 */}
                      {showSpreadDetails === spread.id && (
                        <Zoom in>
                          <Box sx={{ mt: 2, p: 2, background: 'rgba(0, 0, 0, 0.3)', borderRadius: 2 }}>
                            <Typography variant="subtitle2" sx={{ mb: 1, color: 'primary.main' }}>
                              牌位说明：
                            </Typography>
                            {spreadService.getSpreadPositionGuide(spread).slice(0, 3).map((position, idx) => (
                              <Typography
                                key={idx}
                                variant="caption"
                                display="block"
                                sx={{ color: 'text.secondary', mb: 0.5 }}
                              >
                                {position.position}. {position.name}: {position.meaning}
                              </Typography>
                            ))}
                            {spread.card_count > 3 && (
                              <Typography
                                variant="caption"
                                sx={{ color: 'text.secondary', fontStyle: 'italic' }}
                              >
                                ... 还有 {spread.card_count - 3} 个牌位
                              </Typography>
                            )}
                          </Box>
                        </Zoom>
                      )}

                      {/* 选择指示器 */}
                      {isSelected && (
                        <Box sx={{ textAlign: 'center', mt: 2 }}>
                          <Typography
                            variant="caption"
                            sx={{
                              color: 'primary.main',
                              fontWeight: 600,
                              display: 'flex',
                              alignItems: 'center',
                              justifyContent: 'center',
                              gap: 0.5,
                            }}
                          >
                            <AutoAwesome sx={{ fontSize: '1rem' }} />
                            已选择
                          </Typography>
                        </Box>
                      )}
                    </CardContent>
                  </Card>
                </Fade>
              </Box>
            );
          })}
        </Box>
      )}

      {/* 确认按钮 */}
      {selectedSpread && (
        <Fade in>
          <Box sx={{ textAlign: 'center', mt: 4 }}>
            <Button
              variant="contained"
              size="large"
              onClick={handleConfirmSelection}
              sx={{
                py: 2,
                px: 6,
                fontSize: '1.2rem',
                fontFamily: 'Cinzel, serif',
                fontWeight: 600,
                background: 'linear-gradient(45deg, #D4AF37, #FFD700)',
                color: 'black',
                boxShadow: '0 8px 32px rgba(212, 175, 55, 0.3)',
                '&:hover': {
                  background: 'linear-gradient(45deg, #B8860B, #D4AF37)',
                  boxShadow: '0 12px 40px rgba(212, 175, 55, 0.4)',
                  transform: 'translateY(-2px)',
                },
              }}
            >
              确认选择：{selectedSpread.name}
            </Button>
            <Typography
              variant="body2"
              sx={{
                color: 'text.secondary',
                mt: 2,
                fontStyle: 'italic',
              }}
            >
              您选择了一个包含 {selectedSpread.card_count} 张牌的{getDifficultyLabel(selectedSpread.difficulty_level).label}牌阵
            </Typography>
          </Box>
        </Fade>
      )}
    </Box>
  );
};

export default SpreadSelector;