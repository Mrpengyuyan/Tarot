import React, { useState, useEffect, useRef } from 'react';
import { Box } from '@mui/material';
import { TarotCard as TarotCardType } from '../../types/api';

interface Card3DFlipProps {
  card: TarotCardType;
  isFlipped?: boolean;
  isReversed?: boolean;
  isRevealing?: boolean;
  onFlipComplete?: () => void;
  onCardClick?: () => void;
  disableClick?: boolean;
  size?: 'small' | 'medium' | 'large';
  imageError?: boolean;
  imageSrc?: string;
  onImageError?: () => void;
}

const Card3DFlip: React.FC<Card3DFlipProps> = ({
  card,
  isFlipped,
  isReversed = false,
  isRevealing = false,
  onFlipComplete,
  onCardClick,
  disableClick = false,
  size = 'medium',
  imageError = false,
  imageSrc = '',
  onImageError,
}) => {
  const [internalFlipped, setInternalFlipped] = useState(Boolean(isFlipped));
  const previousFlippedRef = useRef(Boolean(isFlipped));
  const isControlled = typeof isFlipped === 'boolean';
  const displayFlipped = isControlled ? Boolean(isFlipped) : internalFlipped;

  // 向后兼容旧逻辑：仅在非受控模式下根据 isRevealing 自动翻牌
  useEffect(() => {
    if (!isControlled && isRevealing && !internalFlipped) {
      const timer = setTimeout(() => {
        setInternalFlipped(true);
      }, 100);
      return () => clearTimeout(timer);
    }
  }, [isRevealing, internalFlipped, isControlled]);

  // 在翻牌完成后统一回调（受控/非受控都适用）
  useEffect(() => {
    const wasFlipped = previousFlippedRef.current;
    if (!wasFlipped && displayFlipped && onFlipComplete) {
      const timer = setTimeout(onFlipComplete, 1000);
      previousFlippedRef.current = displayFlipped;
      return () => clearTimeout(timer);
    }
    previousFlippedRef.current = displayFlipped;
  }, [displayFlipped, onFlipComplete]);

  const getCardSize = () => {
    switch (size) {
      case 'small':
        return { width: 150, height: 250 };
      case 'large':
        return { width: 280, height: 460 };
      default:
        return { width: 200, height: 330 };
    }
  };

  const cardSize = getCardSize();

  // 处理图片加载失败
  const handleImageError = () => {
    console.log('图片加载失败:', imageSrc, card.name_zh);
    if (onImageError) {
      onImageError();
    }
  };

  return (
    <Box
      onClick={() => {
        if (!disableClick) {
          onCardClick?.();
        }
      }}
      sx={{
        perspective: '1200px',
        width: cardSize.width,
        height: cardSize.height,
        position: 'relative',
        cursor: !disableClick && onCardClick ? 'pointer' : 'default',
      }}
    >
      {/* 3D 翻转容器 */}
      <Box
        sx={{
          width: '100%',
          height: '100%',
          position: 'relative',
          transformStyle: 'preserve-3d',
          transform: displayFlipped ? 'rotateY(180deg)' : 'rotateY(0deg)',
          transition: 'transform 1s ease-in-out',
        }}
      >
        {/* 牌背（初始面） */}
        <Box
          sx={{
            position: 'absolute',
            width: '100%',
            height: '100%',
            backfaceVisibility: 'hidden',
            background: 'linear-gradient(135deg, #1a1a2e 0%, #16213e 100%)',
            border: '2px solid #d4af37',
            borderRadius: 2,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            flexDirection: 'column',
            fontSize: '60px',
            color: '#d4af37',
            boxShadow: '0 20px 60px rgba(0, 0, 0, 0.8), inset 0 0 40px rgba(212, 175, 55, 0.2)',
          }}
        >
          <Box sx={{ fontSize: '60px', mb: 1 }}>🔮</Box>
          <Box
            sx={{
              width: 60,
              height: 60,
              border: '2px solid #d4af37',
              borderRadius: '50%',
              animation: 'spin 3s linear infinite',
              '@keyframes spin': {
                '0%': { transform: 'rotate(0deg)' },
                '100%': { transform: 'rotate(360deg)' }
              }
            }}
          />
        </Box>

        {/* 牌面（翻转后） */}
        <Box
          sx={{
            position: 'absolute',
            width: '100%',
            height: '100%',
            backfaceVisibility: 'hidden',
            transform: 'rotateY(180deg)',
            background: '#ffffff',
            border: '2px solid #e0e0e0',
            borderRadius: 2,
            overflow: 'hidden',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: '0 20px 60px rgba(0, 0, 0, 0.8)',
          }}
        >
          {/* 卡片图片 - 完全填充 */}
          {imageSrc && !imageError ? (
            <Box
              component="img"
              src={imageSrc}
              onError={handleImageError}
              alt={card.name_zh}
              sx={{
                width: '100%',
                height: '100%',
                objectFit: 'cover',
                objectPosition: 'center',
                filter: isReversed ? 'brightness(0.8) sepia(20%)' : 'none',
                transform: isReversed ? 'rotate(180deg)' : 'none',
              }}
            />
          ) : (
            <Box sx={{
              width: '100%',
              height: '100%',
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              gap: 2,
              background: 'linear-gradient(135deg, #f5f5f5 0%, #e0e0e0 100%)',
            }}>
              <Box sx={{ fontSize: '4rem' }}>🎴</Box>
              <Box sx={{
                fontSize: '0.9rem',
                fontWeight: 'bold',
                textAlign: 'center',
                px: 2,
                color: '#666',
              }}>
                {card.name_zh}
              </Box>
              {isReversed && (
                <Box sx={{ fontSize: '0.8rem', color: '#8b4513', fontWeight: 'bold' }}>
                  ⚡ 逆位
                </Box>
              )}
            </Box>
          )}
        </Box>
      </Box>
    </Box>
  );
};

export default Card3DFlip;
