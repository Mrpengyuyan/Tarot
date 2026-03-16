import React, { useState, useEffect, useRef, useCallback } from 'react';
import {
  Card,
  CardContent,
  Typography,
  Box,
  Chip,
  IconButton,
  Tooltip,
  Fade,
  CircularProgress,
  Skeleton,
  Modal,
  Backdrop,
  keyframes,
} from '@mui/material';
import {
  Flip as FlipIcon,
  ZoomIn as ZoomInIcon,
  Hd as HDIcon,
  Error as ErrorIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import { TarotCard as TarotCardType } from '../../types/api';
import { getCardImageUrl, getCardBackUrl, ImageQuality } from '../../utils/cardImageUtils';

interface EnhancedTarotCardProps {
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

  // 新增图片相关属性
  imageQuality?: ImageQuality;
  enableZoom?: boolean;
  enableProgressiveLoading?: boolean;
  enableLazyLoading?: boolean;
  showImageControls?: boolean;
}

// 动画定义
const shimmerEffect = keyframes`
  0% { background-position: -200px 0; }
  100% { background-position: calc(200px + 100%) 0; }
`;

const pulseGlow = keyframes`
  0%, 100% { box-shadow: 0 0 5px rgba(212, 175, 55, 0.3); }
  50% { box-shadow: 0 0 20px rgba(212, 175, 55, 0.6), 0 0 30px rgba(212, 175, 55, 0.4); }
`;

const floatAnimation = keyframes`
  0%, 100% { transform: translateY(0px); }
  50% { transform: translateY(-5px); }
`;

const EnhancedTarotCard: React.FC<EnhancedTarotCardProps> = ({
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
  imageQuality = 'standard',
  enableZoom = true,
  enableProgressiveLoading = true,
  enableLazyLoading = true,
  showImageControls = false,
}) => {
  const [isHovered, setIsHovered] = useState(false);
  const [isRevealing, setIsRevealing] = useState(false);
  const [sparkles, setSparkles] = useState<Array<{ id: number; left: number; top: number }>>([]);
  const [zoomModalOpen, setZoomModalOpen] = useState(false);
  const [currentImageQuality, setCurrentImageQuality] = useState<ImageQuality>(imageQuality);

  const imgRef = useRef<HTMLImageElement>(null);

  // 简化的图片加载状态
  const [imageLoaded, setImageLoaded] = useState(false);
  const [imageError, setImageError] = useState(false);
  const [imageUrl, setImageUrl] = useState<string>('');

  // 图片加载逻辑
  useEffect(() => {
    if (!showBack) {
      const url = getCardImageUrl(card.id, currentImageQuality);
      setImageUrl(url);
      setImageLoaded(false);
      setImageError(false);

      const img = new Image();
      img.onload = () => setImageLoaded(true);
      img.onerror = () => setImageError(true);
      img.src = url;
    }
  }, [card.id, currentImageQuality, showBack]);

  // 启动翻转动画
  useEffect(() => {
    if (revealAnimation) {
      setIsRevealing(true);
      const timer = setTimeout(() => setIsRevealing(false), 800);
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

  // 根据尺寸设置卡片大小
  const getCardSize = () => {
    switch (size) {
      case 'small': return { width: 120, height: 200 };
      case 'large': return { width: 200, height: 333 };
      default: return { width: 150, height: 250 };
    }
  };

  const cardSize = getCardSize();

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
      default:
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

  // 处理放大查看
  const handleZoomClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (enableZoom) {
      setZoomModalOpen(true);
    }
  };

  // 处理图片质量切换
  const handleQualityChange = (quality: ImageQuality) => {
    setCurrentImageQuality(quality);
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

  // 渲染图片内容
  const renderImageContent = () => {
    if (showBack) {
      // 显示牌背
      return (
        <Box
          sx={{
            width: '100%',
            height: '100%',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            background: `radial-gradient(circle at 50% 50%, ${themeStyles.accentColor}40 0%, transparent 50%), ${themeStyles.background}`,
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
            <Typography variant="h4" sx={{ fontWeight: 'bold', textShadow: `0 0 20px ${themeStyles.accentColor}80` }}>
              🔮
            </Typography>
          </Box>

          <Typography variant="caption" sx={{ textAlign: 'center', opacity: 0.8, fontStyle: 'italic' }}>
            塔罗牌
          </Typography>

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
                animation: 'sparkle 2s ease-in-out',
                pointerEvents: 'none',
              }}
            />
          ))}
        </Box>
      );
    }

    // 显示正面图片 - 使用简化的状态
    const isLoading = !imageLoaded && !imageError;
    const hasError = imageError;

    return (
      <Box sx={{ position: 'relative', height: showDetails ? '60%' : '80%' }}>
        {/* 图片加载状态 */}
        {isLoading && (
          <Box
            sx={{
              position: 'absolute',
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              background: 'rgba(0, 0, 0, 0.1)',
              zIndex: 1,
            }}
          >
            <CircularProgress size={24} sx={{ color: themeStyles.accentColor }} />
          </Box>
        )}

        {/* 错误状态 */}
        {hasError && (
          <Box
            sx={{
              position: 'absolute',
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              background: 'rgba(0, 0, 0, 0.1)',
              zIndex: 1,
            }}
          >
            <ErrorIcon sx={{ color: 'error.main', mb: 1 }} />
            <Typography variant="caption" sx={{ color: 'text.secondary', textAlign: 'center', mb: 1 }}>
              图片加载失败
            </Typography>
            <IconButton
              size="small"
              onClick={(e) => {
                e.stopPropagation();
                // 重试加载图片
                setImageError(false);
                setImageLoaded(false);
                const img = new Image();
                img.onload = () => setImageLoaded(true);
                img.onerror = () => setImageError(true);
                img.src = imageUrl;
              }}
              sx={{ color: themeStyles.accentColor }}
            >
              <RefreshIcon fontSize="small" />
            </IconButton>
          </Box>
        )}

        {/* 主图片 */}
        <img
          ref={imgRef}
          src={imageUrl}
          alt={card.name_zh}
          style={{
            width: '100%',
            height: '100%',
            objectFit: 'cover',
            filter: isReversed ? 'brightness(0.8) sepia(20%)' : 'none',
            opacity: isLoading ? 0.5 : 1,
            transition: 'opacity 0.3s ease',
          }}
          onLoad={() => setImageLoaded(true)}
          onError={() => {
            console.warn(`Failed to load image for card ${card.id}`);
          }}
        />

        {/* 图片控制按钮 */}
        <Box
          sx={{
            position: 'absolute',
            top: 4,
            right: 4,
            display: 'flex',
            gap: 0.5,
            opacity: isHovered || showImageControls ? 1 : 0,
            transition: 'opacity 0.3s ease',
          }}
        >
          {enableZoom && (
            <Tooltip title="放大查看">
              <IconButton
                onClick={handleZoomClick}
                size="small"
                sx={{
                  backgroundColor: 'rgba(0, 0, 0, 0.7)',
                  color: 'white',
                  '&:hover': { backgroundColor: 'rgba(0, 0, 0, 0.8)' },
                }}
              >
                <ZoomInIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          )}

          {showImageControls && (
            <Tooltip title="切换高清">
              <IconButton
                onClick={() => handleQualityChange(currentImageQuality === 'high' ? 'standard' : 'high')}
                size="small"
                sx={{
                  backgroundColor: currentImageQuality === 'high' ? 'rgba(212, 175, 55, 0.7)' : 'rgba(0, 0, 0, 0.7)',
                  color: 'white',
                  '&:hover': { backgroundColor: 'rgba(212, 175, 55, 0.8)' },
                }}
              >
                <HDIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          )}
        </Box>

        {/* 简化的加载进度 */}
        {isLoading && (
          <Box
            sx={{
              position: 'absolute',
              bottom: 0,
              left: 0,
              right: 0,
              height: 2,
              background: 'rgba(0, 0, 0, 0.2)',
            }}
          >
            <Box
              sx={{
                height: '100%',
                background: themeStyles.accentColor,
                width: '100%',
                animation: 'pulse 1.5s ease-in-out infinite',
              }}
            />
          </Box>
        )}
      </Box>
    );
  };

  return (
    <>
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
            ? 'revealFlip 0.8s ease-in-out'
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
        {renderImageContent()}

        {/* 卡片详情 */}
        {showDetails && !showBack && (
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

        {/* 翻牌按钮 */}
        {onFlip && (
          <Box sx={{ position: 'absolute', top: 4, left: 4 }}>
            <Tooltip title="翻转卡片">
              <IconButton
                onClick={(e) => {
                  e.stopPropagation();
                  onFlip();
                }}
                size="small"
                sx={{
                  backgroundColor: 'rgba(255, 255, 255, 0.8)',
                  '&:hover': { backgroundColor: 'rgba(255, 255, 255, 0.9)' },
                }}
              >
                <FlipIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          </Box>
        )}
      </Card>

      {/* 放大查看模态框 */}
      <Modal
        open={zoomModalOpen}
        onClose={() => setZoomModalOpen(false)}
        closeAfterTransition
        BackdropComponent={Backdrop}
        BackdropProps={{
          timeout: 500,
          sx: { backgroundColor: 'rgba(0, 0, 0, 0.9)' }
        }}
      >
        <Fade in={zoomModalOpen}>
          <Box
            sx={{
              position: 'absolute',
              top: '50%',
              left: '50%',
              transform: 'translate(-50%, -50%)',
              width: 'auto',
              height: 'auto',
              maxWidth: '90vw',
              maxHeight: '90vh',
              outline: 'none',
            }}
          >
            <img
              src={getCardImageUrl(card.id, 'high')}
              alt={card.name_zh}
              style={{
                width: 'auto',
                height: 'auto',
                maxWidth: '100%',
                maxHeight: '100%',
                objectFit: 'contain',
                borderRadius: '8px',
                filter: isReversed ? 'brightness(0.8) sepia(20%)' : 'none',
                transform: isReversed ? 'rotateZ(180deg)' : 'none',
              }}
            />
          </Box>
        </Fade>
      </Modal>
    </>
  );
};

export default EnhancedTarotCard;