import React, { useState, useEffect } from 'react';
import {
  Card,
  CardMedia,
  CardContent,
  Typography,
  Box,
  Chip,
  IconButton,
  Tooltip,
  keyframes,
} from '@mui/material';
import {
  Flip as FlipIcon,
} from '@mui/icons-material';
import { TarotCard as TarotCardType } from '../../types/api';

interface TarotCardProps {
  card: TarotCardType;
  isReversed?: boolean;
  showBack?: boolean;
  isFlipping?: boolean;
  onFlip?: () => void;
  onCardClick?: (card: TarotCardType) => void;
  size?: 'small' | 'medium' | 'large';
  showDetails?: boolean;
  elevation?: number;
  glowEffect?: boolean;
  revealAnimation?: boolean;
  theme?: 'dark' | 'light' | 'mystic';
}

// 定义动画
const shimmerEffect = keyframes`
  0% {
    background-position: -200px 0;
  }
  100% {
    background-position: calc(200px + 100%) 0;
  }
`;

const pulseGlow = keyframes`
  0%, 100% {
    box-shadow: 0 0 5px rgba(212, 175, 55, 0.3);
  }
  50% {
    box-shadow: 0 0 20px rgba(212, 175, 55, 0.6), 0 0 30px rgba(212, 175, 55, 0.4);
  }
`;

const floatAnimation = keyframes`
  0%, 100% {
    transform: translateY(0px);
  }
  50% {
    transform: translateY(-5px);
  }
`;

const revealFlip = keyframes`
  0% {
    transform: rotateY(0deg);
  }
  50% {
    transform: rotateY(90deg);
  }
  100% {
    transform: rotateY(0deg);
  }
`;

const mysticSparkle = keyframes`
  0%, 100% {
    opacity: 0;
    transform: scale(0);
  }
  50% {
    opacity: 1;
    transform: scale(1);
  }
`;

const TarotCard: React.FC<TarotCardProps> = ({
  card,
  isReversed = false,
  showBack = false,
  isFlipping = false,
  onFlip,
  onCardClick,
  size = 'medium',
  showDetails = true,
  elevation = 3,
  glowEffect = false,
  revealAnimation = false,
  theme = 'dark',
}) => {
  const [isHovered, setIsHovered] = useState(false);
  const [isRevealing, setIsRevealing] = useState(false);
  const [sparkles, setSparkles] = useState<Array<{ id: number; left: number; top: number }>>([]);
  const [imageError, setImageError] = useState(false);
  const [imageSrc, setImageSrc] = useState<string>('');

  // 启动翻转动画
  useEffect(() => {
    if (revealAnimation) {
      setIsRevealing(true);
      const timer = setTimeout(() => {
        setIsRevealing(false);
      }, 800);
      return () => clearTimeout(timer);
    }
  }, [revealAnimation]);

  // 生成神秘闪光效果
  useEffect(() => {
    if (glowEffect && isHovered) {
      const interval = setInterval(() => {
        setSparkles(prev => [
          ...prev.filter(s => Date.now() - s.id < 2000),
          {
            id: Date.now(),
            left: Math.random() * 100,
            top: Math.random() * 100,
          }
        ]);
      }, 300);
      return () => clearInterval(interval);
    }
  }, [glowEffect, isHovered]);

  // 处理图片URL和fallback
  useEffect(() => {
    setImageError(false);

    // 生成多个可能的图片URL备选方案
    const imageUrls = [
      card.image_url,
      `/images/tarot-cards/${card.id}.jpg`,
      `/images/tarot-cards/card-${card.id}.jpg`,
      `/images/cards/${card.id}.jpg`,
      `/images/tarot-cards/default.jpg`,
      '/images/cards/default.jpg'
    ].filter(Boolean) as string[];

    // 使用第一个有效的URL
    setImageSrc(imageUrls[0] || '/images/cards/placeholder.jpg');
  }, [card.id, card.image_url]);
  // 根据尺寸设置卡片大小
  const getCardSize = () => {
    switch (size) {
      case 'small':
        return { width: 120, height: 200 };
      case 'large':
        return { width: 200, height: 333 };
      default:
        return { width: 150, height: 250 };
    }
  };

  const cardSize = getCardSize();

  // 处理图片加载错误
  const handleImageError = () => {
    if (!imageError) {
      console.warn(`图片加载失败: ${imageSrc}, 卡片ID: ${card.id}`);
      setImageError(true);

      // 尝试多个fallback方案
      const fallbackUrls = [
        `/images/tarot-cards/placeholder.jpg`,
        `/images/cards/placeholder.jpg`,
        '/placeholder.jpg',
        'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMTUwIiBoZWlnaHQ9IjI1MCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cmVjdCB3aWR0aD0iMTUwIiBoZWlnaHQ9IjI1MCIgZmlsbD0iIzFhMWEyZSIvPjx0ZXh0IHg9IjUwJSIgeT0iNTAlIiBkb21pbmFudC1iYXNlbGluZT0ibWlkZGxlIiB0ZXh0LWFuY2hvcj0ibWlkZGxlIiBmaWxsPSIjZDRhZjM3IiBmb250LXNpemU9IjE2Ij7loZ/nva7niYg8L3RleHQ+PC9zdmc+'
      ];

      // 尝试下一个URL
      const currentIndex = fallbackUrls.indexOf(imageSrc);
      const nextUrl = fallbackUrls[currentIndex + 1] || fallbackUrls[fallbackUrls.length - 1];
      setImageSrc(nextUrl);
    }
  };

  // 获取主题样式配置
  const getThemeStyles = () => {
    switch (theme) {
      case 'light':
        return {
          background: 'linear-gradient(135deg, #f8fafc 0%, #ffffff 100%)',
          borderColor: 'rgba(99, 102, 241, 0.3)',
          textColor: '#1f2937',
          accentColor: '#6366f1',
        };
      case 'mystic':
        return {
          background: 'linear-gradient(135deg, #0f0f23 0%, #1a1a2e 50%, #16213e 100%)',
          borderColor: 'rgba(147, 51, 234, 0.4)',
          textColor: '#e5e7eb',
          accentColor: '#9333ea',
        };
      default: // dark
        return {
          background: 'linear-gradient(135deg, #1a1a2e 0%, #16213e 100%)',
          borderColor: 'rgba(212, 175, 55, 0.3)',
          textColor: '#d4af37',
          accentColor: '#d4af37',
        };
    }
  };

  const themeStyles = getThemeStyles();

  // 处理卡片点击
  const handleCardClick = () => {
    if (onCardClick) {
      onCardClick(card);
    }
  };

  // 获取花色显示文本
  const getSuitDisplay = (suit: string | null) => {
    const suitMap: { [key: string]: string } = {
      'wands': '权杖',
      'cups': '圣杯',
      'swords': '宝剑',
      'pentacles': '星币',
    };
    return suit ? suitMap[suit] || suit : '';
  };

  // 获取卡片类型显示文本
  const getTypeDisplay = (type: string) => {
    return type === 'major_arcana' ? '大阿卡纳' : '小阿卡纳';
  };

  return (
    <Card
      sx={{
        width: cardSize.width,
        height: cardSize.height,
        cursor: onCardClick ? 'pointer' : 'default',
        transition: 'all 0.4s cubic-bezier(0.4, 0, 0.2, 1)',
        transform: isFlipping
          ? 'rotateY(180deg)'
          : isReversed
          ? 'rotateY(0deg) rotateZ(180deg)'
          : 'rotateY(0deg)',
        transformStyle: 'preserve-3d',
        position: 'relative',
        background: showBack ? themeStyles.background : 'linear-gradient(135deg, #f5f5f5 0%, #ffffff 100%)',
        border: '2px solid',
        borderColor: showBack
          ? themeStyles.borderColor
          : isReversed
          ? 'rgba(139, 69, 19, 0.5)'
          : themeStyles.borderColor,
        borderRadius: 2,
        overflow: 'hidden',
        animation: glowEffect && isHovered
          ? `${pulseGlow} 2s ease-in-out infinite, ${floatAnimation} 3s ease-in-out infinite`
          : revealAnimation && isRevealing
          ? `${revealFlip} 0.8s ease-in-out`
          : undefined,
        '&:hover': {
          elevation: elevation + 2,
          transform: onCardClick
            ? `scale(1.05) ${isReversed ? 'rotateZ(180deg)' : ''}`
            : undefined,
          '&::before': glowEffect ? {
            content: '""',
            position: 'absolute',
            top: 0,
            left: '-100%',
            width: '100%',
            height: '100%',
            background: `linear-gradient(90deg, transparent, ${themeStyles.accentColor}20, transparent)`,
            animation: `${shimmerEffect} 2s ease-in-out infinite`,
            zIndex: 1,
          } : undefined,
        },
      }}
      elevation={elevation}
      onClick={handleCardClick}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      {showBack ? (
        // 卡片背面
        <Box
          sx={{
            width: '100%',
            height: '100%',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            background: `
              radial-gradient(circle at 50% 50%, ${themeStyles.accentColor}40 0%, transparent 50%),
              ${themeStyles.background}
            `,
            color: themeStyles.accentColor,
            position: 'relative',
            overflow: 'hidden',
          }}
        >
          {/* 装饰性图案 */}
          <Box
            sx={{
              width: 80,
              height: 80,
              borderRadius: '50%',
              border: `3px solid ${themeStyles.accentColor}60`,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              mb: 2,
              background: `${themeStyles.accentColor}20`,
              animation: glowEffect && isHovered ? `${pulseGlow} 2s ease-in-out infinite` : undefined,
            }}
          >
            <Typography
              variant="h4"
              sx={{
                fontWeight: 'bold',
                textShadow: `0 0 20px ${themeStyles.accentColor}80`,
              }}
            >
              🔮
            </Typography>
          </Box>

          <Typography
            variant="caption"
            sx={{
              textAlign: 'center',
              opacity: 0.8,
              fontStyle: 'italic',
            }}
          >
            塔罗牌
          </Typography>

          {onFlip && (
            <IconButton
              onClick={(e) => {
                e.stopPropagation();
                onFlip();
              }}
              sx={{
                position: 'absolute',
                top: 8,
                right: 8,
                color: `${themeStyles.accentColor}70`,
                '&:hover': {
                  color: themeStyles.accentColor,
                },
              }}
              size="small"
            >
              <FlipIcon />
            </IconButton>
          )}

          {/* 神秘闪光效果 */}
          {glowEffect && sparkles.map((sparkle) => (
            <Box
              key={sparkle.id}
              sx={{
                position: 'absolute',
                left: `${sparkle.left}%`,
                top: `${sparkle.top}%`,
                width: 4,
                height: 4,
                borderRadius: '50%',
                background: themeStyles.accentColor,
                animation: `${mysticSparkle} 2s ease-in-out`,
                pointerEvents: 'none',
              }}
            />
          ))}
        </Box>
      ) : (
        // 卡片正面
        <>
          <CardMedia
            component="img"
            image={imageSrc}
            alt={card.name_zh}
            onError={handleImageError}
            loading="lazy"
            sx={{
              height: showDetails ? '75%' : '100%',
              objectFit: 'contain',
              objectPosition: 'center',
              backgroundColor: '#ffffff',
              filter: isReversed ? 'brightness(0.8) sepia(20%)' : 'none',
              opacity: imageError ? 0.6 : 1,
              transition: 'opacity 0.3s ease',
            }}
          />

          {/* 图片加载失败提示 */}
          {imageError && (
            <Box
              sx={{
                position: 'absolute',
                top: '50%',
                left: '50%',
                transform: 'translate(-50%, -50%)',
                textAlign: 'center',
                zIndex: 2,
                pointerEvents: 'none',
              }}
            >
              <Typography
                variant="caption"
                sx={{
                  color: 'text.secondary',
                  fontSize: '0.7rem',
                  display: 'block',
                }}
              >
                🔮
              </Typography>
              <Typography
                variant="caption"
                sx={{
                  color: 'text.secondary',
                  fontSize: '0.6rem',
                }}
              >
                {card.name_zh}
              </Typography>
            </Box>
          )}

          {showDetails && (
            <CardContent
              sx={{
                height: '40%',
                p: 1,
                '&:last-child': { pb: 1 },
                overflow: 'hidden',
              }}
            >
              <Typography
                variant="subtitle2"
                component="h3"
                sx={{
                  fontWeight: 'bold',
                  mb: 0.5,
                  fontSize: size === 'small' ? '0.75rem' : '0.875rem',
                  lineHeight: 1.2,
                  color: isReversed ? '#8b4513' : themeStyles.textColor,
                }}
                noWrap
              >
                {card.name_zh}
              </Typography>

              <Typography
                variant="caption"
                sx={{
                  color: 'text.secondary',
                  fontSize: size === 'small' ? '0.6rem' : '0.7rem',
                  display: 'block',
                  mb: 0.5,
                }}
                noWrap
              >
                {card.name_en}
              </Typography>

              <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
                <Chip
                  label={getTypeDisplay(card.card_type)}
                  size="small"
                  sx={{
                    height: 16,
                    fontSize: '0.6rem',
                    backgroundColor: card.card_type === 'major_arcana'
                      ? 'rgba(212, 175, 55, 0.2)'
                      : 'rgba(106, 90, 205, 0.2)',
                    color: card.card_type === 'major_arcana'
                      ? '#d4af37'
                      : '#6a5acd',
                  }}
                />

                {card.suit && (
                  <Chip
                    label={getSuitDisplay(card.suit)}
                    size="small"
                    sx={{
                      height: 16,
                      fontSize: '0.6rem',
                      backgroundColor: 'rgba(139, 69, 19, 0.2)',
                      color: '#8b4513',
                    }}
                  />
                )}

                {isReversed && (
                  <Chip
                    label="逆位"
                    size="small"
                    sx={{
                      height: 16,
                      fontSize: '0.6rem',
                      backgroundColor: 'rgba(139, 69, 19, 0.3)',
                      color: '#8b4513',
                    }}
                  />
                )}
              </Box>
            </CardContent>
          )}

          {/* 操作按钮 */}
          <Box
            sx={{
              position: 'absolute',
              top: 4,
              right: 4,
              display: 'flex',
              gap: 0.5,
            }}
          >
            {onFlip && (
              <Tooltip title="翻转卡片">
                <IconButton
                  onClick={(e) => {
                    e.stopPropagation();
                    onFlip();
                  }}
                  size="small"
                  sx={{
                    backgroundColor: 'rgba(255, 255, 255, 0.8)',
                    '&:hover': {
                      backgroundColor: 'rgba(255, 255, 255, 0.9)',
                    },
                  }}
                >
                  <FlipIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            )}
          </Box>
        </>
      )}
    </Card>
  );
};

export default TarotCard;
