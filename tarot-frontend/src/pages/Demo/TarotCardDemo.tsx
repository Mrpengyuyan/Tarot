import React, { useState } from 'react';
import {
  Box,
  Typography,
  Button,
  ToggleButtonGroup,
  ToggleButton,
  Card,
  FormControlLabel,
  Switch,
  Slider
} from '@mui/material';
import TarotCard from '../../components/Tarot/TarotCard';
import { TarotCard as TarotCardType } from '../../types/api';

const TarotCardDemo: React.FC = () => {
  const [showBack, setShowBack] = useState(false);
  const [isReversed, setIsReversed] = useState(false);
  const [isFlipping, setIsFlipping] = useState(false);
  const [size, setSize] = useState<'small' | 'medium' | 'large'>('medium');
  const [showDetails, setShowDetails] = useState(true);
  const [elevation, setElevation] = useState(3);
  const [glowEffect, setGlowEffect] = useState(false);
  const [revealAnimation, setRevealAnimation] = useState(false);
  const [theme, setTheme] = useState<'dark' | 'light' | 'mystic'>('dark');

  // 示例塔罗牌数据
  const sampleCard: TarotCardType = {
    id: 1,
    name_zh: '愚者',
    name_en: 'The Fool',
    card_number: 0,
    card_type: 'major_arcana',
    suit: null,
    image_url: '/images/cards/fool.jpg',
  };

  const handleFlip = () => {
    setIsFlipping(true);
    setTimeout(() => {
      setShowBack(!showBack);
      setIsFlipping(false);
    }, 400);
  };

  const handleRevealAnimation = () => {
    setRevealAnimation(true);
    setTimeout(() => setRevealAnimation(false), 1000);
  };

  return (
    <Box sx={{ p: 3, maxWidth: 1200, mx: 'auto' }}>
      <Typography variant="h4" gutterBottom align="center">
        🔮 增强塔罗牌组件演示
      </Typography>

      <Box sx={{ display: 'flex', flexDirection: { xs: 'column', md: 'row' }, gap: 4 }}>
        {/* 控制面板 */}
        <Box sx={{ flex: { md: '0 0 33%' } }}>
          <Card sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              ⚙️ 控制面板
            </Typography>

            {/* 基本控制 */}
            <Box sx={{ mb: 3 }}>
              <Typography variant="subtitle2" gutterBottom>
                基本控制
              </Typography>
              <FormControlLabel
                control={<Switch checked={showBack} onChange={(e) => setShowBack(e.target.checked)} />}
                label="显示背面"
              />
              <FormControlLabel
                control={<Switch checked={isReversed} onChange={(e) => setIsReversed(e.target.checked)} />}
                label="逆位"
              />
              <FormControlLabel
                control={<Switch checked={showDetails} onChange={(e) => setShowDetails(e.target.checked)} />}
                label="显示详情"
              />
            </Box>

            {/* 尺寸选择 */}
            <Box sx={{ mb: 3 }}>
              <Typography variant="subtitle2" gutterBottom>
                卡片尺寸
              </Typography>
              <ToggleButtonGroup
                value={size}
                exclusive
                onChange={(_, newSize) => newSize && setSize(newSize)}
                size="small"
              >
                <ToggleButton value="small">小</ToggleButton>
                <ToggleButton value="medium">中</ToggleButton>
                <ToggleButton value="large">大</ToggleButton>
              </ToggleButtonGroup>
            </Box>

            {/* 主题选择 */}
            <Box sx={{ mb: 3 }}>
              <Typography variant="subtitle2" gutterBottom>
                主题样式
              </Typography>
              <ToggleButtonGroup
                value={theme}
                exclusive
                onChange={(_, newTheme) => newTheme && setTheme(newTheme)}
                size="small"
              >
                <ToggleButton value="dark">暗色</ToggleButton>
                <ToggleButton value="light">亮色</ToggleButton>
                <ToggleButton value="mystic">神秘</ToggleButton>
              </ToggleButtonGroup>
            </Box>

            {/* 高级效果 */}
            <Box sx={{ mb: 3 }}>
              <Typography variant="subtitle2" gutterBottom>
                特殊效果
              </Typography>
              <FormControlLabel
                control={<Switch checked={glowEffect} onChange={(e) => setGlowEffect(e.target.checked)} />}
                label="发光效果"
              />
            </Box>

            {/* 阴影强度 */}
            <Box sx={{ mb: 3 }}>
              <Typography variant="subtitle2" gutterBottom>
                阴影强度: {elevation}
              </Typography>
              <Slider
                value={elevation}
                onChange={(_, newValue) => setElevation(newValue as number)}
                min={0}
                max={24}
                step={1}
                marks
                valueLabelDisplay="auto"
              />
            </Box>

            {/* 动画控制 */}
            <Box sx={{ mb: 3 }}>
              <Typography variant="subtitle2" gutterBottom>
                动画演示
              </Typography>
              <Button
                variant="outlined"
                onClick={handleFlip}
                sx={{ mr: 1, mb: 1 }}
                disabled={isFlipping}
              >
                翻转卡片
              </Button>
              <Button
                variant="outlined"
                onClick={handleRevealAnimation}
                sx={{ mb: 1 }}
                disabled={revealAnimation}
              >
                揭示动画
              </Button>
            </Box>
          </Card>
        </Box>

        {/* 卡片展示区域 */}
        <Box sx={{ flex: 1 }}>
          <Box sx={{
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
            minHeight: 400,
            background: theme === 'light'
              ? 'linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%)'
              : theme === 'mystic'
              ? 'linear-gradient(135deg, #0f0f23 0%, #1a1a2e 50%, #16213e 100%)'
              : 'linear-gradient(135deg, #1a1a2e 0%, #16213e 100%)',
            borderRadius: 2,
            p: 4
          }}>
            <TarotCard
              card={sampleCard}
              isReversed={isReversed}
              showBack={showBack}
              isFlipping={isFlipping}
              onFlip={handleFlip}
              onCardClick={(card) => console.log('卡片点击:', card)}
              size={size}
              showDetails={showDetails}
              elevation={elevation}
              glowEffect={glowEffect}
              revealAnimation={revealAnimation}
              theme={theme}
            />
          </Box>

          {/* 效果说明 */}
          <Card sx={{ mt: 3, p: 3 }}>
            <Typography variant="h6" gutterBottom>
              ✨ 新增特性说明
            </Typography>
            <Box component="ul" sx={{ pl: 2 }}>
              <Typography component="li" variant="body2" sx={{ mb: 1 }}>
                <strong>主题系统：</strong> 支持暗色、亮色、神秘三种主题风格
              </Typography>
              <Typography component="li" variant="body2" sx={{ mb: 1 }}>
                <strong>发光效果：</strong> 鼠标悬停时的脉冲发光和闪光粒子效果
              </Typography>
              <Typography component="li" variant="body2" sx={{ mb: 1 }}>
                <strong>流畅动画：</strong> 卡片翻转、揭示、浮动等多种动画效果
              </Typography>
              <Typography component="li" variant="body2" sx={{ mb: 1 }}>
                <strong>响应式设计：</strong> 三种尺寸适配不同设备和场景
              </Typography>
              <Typography component="li" variant="body2">
                <strong>交互反馈：</strong> 悬停、点击等交互状态的视觉反馈
              </Typography>
            </Box>
          </Card>
        </Box>
      </Box>
    </Box>
  );
};

export default TarotCardDemo;