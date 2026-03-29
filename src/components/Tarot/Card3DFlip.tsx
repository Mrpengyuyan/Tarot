import React, { useEffect, useRef, useState } from 'react';
import { Box, Typography } from '@mui/material';
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
  motionPreset?: 'full' | 'lite' | 'flat';
}

const getCardLabel = (card: TarotCardType): string => {
  return card.name_en || card.name_zh || 'Tarot Card';
};

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
  motionPreset = 'full',
}) => {
  const [internalFlipped, setInternalFlipped] = useState(Boolean(isFlipped));
  const previousFlippedRef = useRef(Boolean(isFlipped));
  const isControlled = typeof isFlipped === 'boolean';
  const displayFlipped = isControlled ? Boolean(isFlipped) : internalFlipped;

  useEffect(() => {
    if (!isControlled && isRevealing && !internalFlipped) {
      const timer = window.setTimeout(() => {
        setInternalFlipped(true);
      }, 100);
      return () => window.clearTimeout(timer);
    }
  }, [internalFlipped, isControlled, isRevealing]);

  useEffect(() => {
    const wasFlipped = previousFlippedRef.current;
    if (!wasFlipped && displayFlipped && onFlipComplete) {
      const timer = window.setTimeout(onFlipComplete, 1000);
      previousFlippedRef.current = displayFlipped;
      return () => window.clearTimeout(timer);
    }
    previousFlippedRef.current = displayFlipped;
  }, [displayFlipped, onFlipComplete]);

  const cardSize = {
    small: { width: 150, height: 250 },
    medium: { width: 200, height: 330 },
    large: { width: 280, height: 460 },
  }[size];

  const isFlatMotion = motionPreset === 'flat';
  const isLiteMotion = motionPreset === 'lite';
  const perspective = isFlatMotion ? '900px' : isLiteMotion ? '1120px' : '1400px';
  const flipTransition = isFlatMotion
    ? 'transform 0.55s ease-out'
    : isLiteMotion
      ? 'transform 0.72s cubic-bezier(0.22, 1, 0.36, 1)'
      : 'transform 0.95s cubic-bezier(0.22, 1, 0.36, 1)';
  const shellShadow = isFlatMotion
    ? '0 16px 34px rgba(0, 0, 0, 0.48)'
    : isLiteMotion
      ? '0 18px 44px rgba(0, 0, 0, 0.58), inset 0 0 28px rgba(212, 175, 55, 0.05)'
      : '0 22px 60px rgba(0, 0, 0, 0.8), inset 0 0 50px rgba(212, 175, 55, 0.08)';

  return (
    <Box
      onClick={() => {
        if (!disableClick) {
          onCardClick?.();
        }
      }}
      sx={{
        perspective,
        width: cardSize.width,
        height: cardSize.height,
        position: 'relative',
        cursor: !disableClick && onCardClick ? 'pointer' : 'default',
      }}
    >
      <Box
        sx={{
          width: '100%',
          height: '100%',
          position: 'relative',
          transformStyle: 'preserve-3d',
          transform: displayFlipped ? 'rotateY(180deg)' : 'rotateY(0deg)',
          transition: flipTransition,
          willChange: isFlatMotion ? 'auto' : 'transform',
        }}
      >
        <Box
          sx={{
            position: 'absolute',
            inset: 0,
            backfaceVisibility: 'hidden',
            border: '2.5px solid rgba(212, 175, 55, 0.85)',
            borderRadius: 3,
            overflow: 'hidden',
            background: 'linear-gradient(155deg, #0c0920 0%, #1a1040 35%, #0f1c38 65%, #080614 100%)',
            boxShadow: shellShadow,
          }}
        >
          {/* 内嵌 SVG 牌背图案 */}
          <svg
            viewBox="0 0 400 660"
            style={{
              position: 'absolute',
              inset: 0,
              width: '100%',
              height: '100%',
            }}
            xmlns="http://www.w3.org/2000/svg"
          >
            <defs>
              <radialGradient id={`glow-${card.id}`} cx="50%" cy="50%" r="42%">
                <stop offset="0%" stopColor="#f5d97b" stopOpacity="0.4" />
                <stop offset="60%" stopColor="#c9a84c" stopOpacity="0.1" />
                <stop offset="100%" stopColor="#f5d97b" stopOpacity="0" />
              </radialGradient>
              <radialGradient id={`glow-c-${card.id}`} cx="50%" cy="50%" r="45%">
                <stop offset="0%" stopColor="#56bdf8" stopOpacity="0.1" />
                <stop offset="100%" stopColor="#56bdf8" stopOpacity="0" />
              </radialGradient>
              <filter id={`neon-${card.id}`} x="-40%" y="-40%" width="180%" height="180%">
                <feGaussianBlur stdDeviation="2.5" result="blur" />
                <feMerge>
                  <feMergeNode in="blur" />
                  <feMergeNode in="SourceGraphic" />
                </feMerge>
              </filter>
            </defs>

            {/* 星辰散点 */}
            {[
              [32, 45, 1.4, '#f5d97b', 0.5],
              [370, 28, 1.1, '#8cd3ff', 0.4],
              [55, 120, 1.0, '#fff', 0.35],
              [345, 95, 1.3, '#c99bff', 0.4],
              [28, 250, 1.2, '#f5d97b', 0.3],
              [372, 200, 0.9, '#8cd3ff', 0.35],
              [40, 400, 1.1, '#fff', 0.3],
              [360, 440, 1.3, '#f5d97b', 0.4],
              [50, 550, 1.0, '#8cd3ff', 0.35],
              [350, 580, 1.2, '#c99bff', 0.3],
              [120, 50, 0.8, '#f5d97b', 0.45],
              [280, 50, 0.9, '#8cd3ff', 0.35],
              [100, 610, 1.0, '#f5d97b', 0.4],
              [300, 610, 0.8, '#c99bff', 0.35],
              [200, 25, 1.1, '#fff', 0.3],
              [200, 635, 1.0, '#fff', 0.3],
            ].map(([cx, cy, r, fill, op], i) => (
              <circle key={i} cx={cx as number} cy={cy as number} r={r as number} fill={fill as string} opacity={op as number} />
            ))}

            {/* 外框双金边 */}
            <rect x="12" y="12" width="376" height="636" rx="18" fill="none" stroke="#d4af37" strokeWidth="2.5" opacity="0.8" />
            <rect x="22" y="22" width="356" height="616" rx="14" fill="none" stroke="#d4af37" strokeWidth="1" opacity="0.35" />
            <rect x="30" y="30" width="340" height="600" rx="11" fill="none" stroke="#56bdf8" strokeWidth="0.6" opacity="0.2" strokeDasharray="5 6" />

            {/* 四角装饰 */}
            <g opacity="0.65" filter={`url(#neon-${card.id})`}>
              <path d="M32 40 L32 72 L64 40 Z" fill="none" stroke="#d4af37" strokeWidth="1.5" />
              <circle cx="38" cy="46" r="2.5" fill="#d4af37" opacity="0.7" />
              <path d="M368 40 L368 72 L336 40 Z" fill="none" stroke="#d4af37" strokeWidth="1.5" />
              <circle cx="362" cy="46" r="2.5" fill="#d4af37" opacity="0.7" />
              <path d="M32 620 L32 588 L64 620 Z" fill="none" stroke="#d4af37" strokeWidth="1.5" />
              <circle cx="38" cy="614" r="2.5" fill="#d4af37" opacity="0.7" />
              <path d="M368 620 L368 588 L336 620 Z" fill="none" stroke="#d4af37" strokeWidth="1.5" />
              <circle cx="362" cy="614" r="2.5" fill="#d4af37" opacity="0.7" />
            </g>

            {/* 中央光晕 */}
            <circle cx="200" cy="330" r="200" fill={`url(#glow-c-${card.id})`} />
            <circle cx="200" cy="330" r="160" fill={`url(#glow-${card.id})`} />

            {/* 外圈 */}
            <circle cx="200" cy="330" r="140" fill="none" stroke="#d4af37" strokeWidth="2.5" opacity="0.7" filter={`url(#neon-${card.id})`} />
            <circle cx="200" cy="330" r="128" fill="none" stroke="#d4af37" strokeWidth="0.8" opacity="0.3" strokeDasharray="10 5" />

            {/* 中圈 - 青色 */}
            <circle cx="200" cy="330" r="100" fill="none" stroke="#56bdf8" strokeWidth="1.5" opacity="0.4" filter={`url(#neon-${card.id})`} />

            {/* 内圈 */}
            <circle cx="200" cy="330" r="70" fill="none" stroke="#d4af37" strokeWidth="1.2" opacity="0.5" />

            {/* 八角星 - 主 */}
            <polygon
              points="200,185 222,300 340,330 222,360 200,475 178,360 60,330 178,300"
              fill="none" stroke="#f5d97b" strokeWidth="3" opacity="0.75" filter={`url(#neon-${card.id})`}
            />

            {/* 八角星 - 旋转副 */}
            <g transform="rotate(22.5, 200, 330)">
              <polygon
                points="200,225 212,305 292,330 212,355 200,435 188,355 108,330 188,305"
                fill="none" stroke="#56bdf8" strokeWidth="1.5" opacity="0.35"
              />
            </g>

            {/* 8个方向定位点 */}
            <g filter={`url(#neon-${card.id})`}>
              {[0, 45, 90, 135, 180, 225, 270, 315].map((angle, i) => {
                const rad = (angle * Math.PI) / 180;
                const r = 140;
                const cx = 200 + r * Math.cos(rad);
                const cy = 330 + r * Math.sin(rad);
                return (
                  <circle
                    key={`d-${i}`}
                    cx={cx}
                    cy={cy}
                    r={i % 2 === 0 ? 4 : 3}
                    fill={i % 2 === 0 ? '#f5d97b' : '#56bdf8'}
                    opacity={i % 2 === 0 ? 0.85 : 0.6}
                  />
                );
              })}
            </g>

            {/* 全视之眼中心 */}
            <circle cx="200" cy="330" r="20" fill="#f5d97b" opacity="0.8" filter={`url(#neon-${card.id})`} />
            <circle cx="200" cy="330" r="10" fill="#0d0820" />
            <circle cx="200" cy="330" r="4.5" fill="#56bdf8" opacity="0.9" />
            <path d="M172 330 Q200 310 228 330 Q200 350 172 330" fill="none" stroke="#f5d97b" strokeWidth="1.8" opacity="0.6" />

            {/* 上方标题 */}
            <text x="200" y="88" textAnchor="middle" fontSize="26" fontFamily="Georgia, serif" fill="#f5d97b" letterSpacing="8" opacity="0.85" filter={`url(#neon-${card.id})`}>TAROT</text>
            <text x="200" y="115" textAnchor="middle" fontSize="11" fill="#d4af37" opacity="0.5" letterSpacing="3">✦   ✦   ✦</text>

            {/* 下方标题 */}
            <text x="200" y="575" textAnchor="middle" fontSize="15" fontFamily="Georgia, serif" fill="#8cd3ff" letterSpacing="7" opacity="0.55">ARCANA</text>
            <text x="200" y="553" textAnchor="middle" fontSize="11" fill="#d4af37" opacity="0.5" letterSpacing="3">✦   ✦   ✦</text>
          </svg>
        </Box>

        <Box
          sx={{
            position: 'absolute',
            inset: 0,
            backfaceVisibility: 'hidden',
            transform: 'rotateY(180deg)',
            background: '#ffffff',
            border: '2px solid rgba(224, 224, 224, 0.95)',
            borderRadius: 3,
            overflow: 'hidden',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: isFlatMotion ? '0 16px 34px rgba(0, 0, 0, 0.44)' : '0 22px 60px rgba(0, 0, 0, 0.72)',
          }}
        >
          {imageSrc && !imageError ? (
            <Box
              component="img"
              src={imageSrc}
              onError={onImageError}
              alt={getCardLabel(card)}
              sx={{
                width: '100%',
                height: '100%',
                objectFit: 'cover',
                objectPosition: 'center',
                filter: isReversed ? 'brightness(0.85) sepia(12%)' : 'none',
                transform: isReversed ? 'rotate(180deg)' : 'none',
              }}
            />
          ) : (
            <Box
              sx={{
                width: '100%',
                height: '100%',
                px: 2.5,
                py: 3,
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                justifyContent: 'center',
                gap: 1.5,
                textAlign: 'center',
                background: 'linear-gradient(180deg, #f4efe4 0%, #e8ddc8 100%)',
              }}
            >
              <Typography variant="h6" sx={{ fontWeight: 700, color: '#4b3b27' }}>
                {getCardLabel(card)}
              </Typography>
              <Typography variant="body2" sx={{ color: '#6b5a40', lineHeight: 1.6 }}>
                图片暂未加载，仍可继续进行解读流程。
              </Typography>
              {isReversed && (
                <Typography variant="caption" sx={{ fontWeight: 700, color: '#8b4513', letterSpacing: '0.08em' }}>
                  逆位
                </Typography>
              )}
            </Box>
          )}
        </Box>
      </Box>
    </Box>
  );
};

export default Card3DFlip;
