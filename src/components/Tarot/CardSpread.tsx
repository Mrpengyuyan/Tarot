import React, { useMemo, useState } from 'react';
import {
  Box,
  Typography,
  Paper,
  Fade,
  Zoom,
} from '@mui/material';
import Card3DFlip from './Card3DFlip';
import { FlipSparklesContainer } from '../Effects/FlipSparkles';
import { TarotCard as TarotCardType, SpreadType } from '../../types/api';
import { getTarotCardImagePath } from '../../utils/tarotImageMapper';

interface DrawnCard {
  card: TarotCardType;
  isReversed: boolean;
  position: number;
}

interface CardSpreadProps {
  spread: SpreadType;
  drawnCards: DrawnCard[];
  onCardClick?: (card: TarotCardType, position: number) => void;
  onCardFlip?: (card: TarotCardType, position: number) => void;
  isRevealing?: boolean;
  revealedPositions?: number[];
  flippedPositions?: number[];
  allowManualFlip?: boolean;
  cardSizeOverride?: 'small' | 'medium' | 'large';
  showSpreadMeta?: boolean;
  showSpreadDescription?: boolean;
}

const CardSpread: React.FC<CardSpreadProps> = ({
  spread,
  drawnCards,
  onCardClick,
  onCardFlip,
  isRevealing = false,
  revealedPositions = [],
  flippedPositions,
  allowManualFlip = false,
  cardSizeOverride,
  showSpreadMeta = true,
  showSpreadDescription = true,
}) => {
  const [imageErrors, setImageErrors] = useState<Set<number>>(new Set());
  const [activeSparklePosition, setActiveSparklePosition] = useState<number | null>(null);
  const revealedSet = useMemo(() => new Set(revealedPositions), [revealedPositions]);
  const explicitFlippedSet = useMemo(() => new Set(flippedPositions || []), [flippedPositions]);

  const getCardSizeByCount = (count: number): 'small' | 'medium' | 'large' => {
    if (count >= 6) {
      return 'small';
    }
    if (count >= 4) {
      return 'medium';
    }
    return 'large';
  };

  const cardSize = cardSizeOverride || getCardSizeByCount(spread.card_count);
  const cardDimensions = {
    small: { width: 150, height: 250 },
    medium: { width: 200, height: 330 },
    large: { width: 280, height: 460 },
  }[cardSize];

  // 获取牌位布局配置
  const getSpreadLayout = (spreadName: string, cardCount: number) => {
    switch (spreadName) {
      case '单牌抽取':
        return {
          positions: [{ row: 0, col: 0, label: '指导' }],
          gridCols: 1,
          spacing: 2,
        };

      case '过去现在未来':
        return {
          positions: [
            { row: 0, col: 0, label: '过去' },
            { row: 0, col: 1, label: '现在' },
            { row: 0, col: 2, label: '未来' },
          ],
          gridCols: 3,
          spacing: 2,
        };

      case '爱情牌阵':
        return {
          positions: [
            { row: 0, col: 1, label: '你的感受' },
            { row: 1, col: 0, label: '对方感受' },
            { row: 1, col: 2, label: '关系现状' },
            { row: 2, col: 0, label: '阻碍因素' },
            { row: 2, col: 2, label: '发展方向' },
          ],
          gridCols: 3,
          spacing: 1.5,
        };

      case '财运牌阵':
        return {
          positions: [
            { row: 0, col: 0, label: '当前财务' },
            { row: 0, col: 1, label: '收入来源' },
            { row: 1, col: 0, label: '支出压力' },
            { row: 1, col: 1, label: '理财建议' },
          ],
          gridCols: 2,
          spacing: 2,
        };

      case '事业发展':
        return {
          positions: [
            { row: 0, col: 1, label: '当前状况' },
            { row: 1, col: 0, label: '优势' },
            { row: 1, col: 1, label: '核心' },
            { row: 1, col: 2, label: '挑战' },
            { row: 2, col: 0, label: '行动建议' },
            { row: 2, col: 2, label: '最终结果' },
          ],
          gridCols: 3,
          spacing: 1.5,
        };

      case '凯尔特十字':
        return {
          positions: [
            { row: 1, col: 2, label: '现状' },
            { row: 1, col: 2, label: '影响', offset: true },
            { row: 0, col: 2, label: '未来' },
            { row: 2, col: 2, label: '过去' },
            { row: 1, col: 3, label: '可能性' },
            { row: 1, col: 1, label: '内心' },
            { row: 3, col: 4, label: '环境' },
            { row: 2, col: 4, label: '希望恐惧' },
            { row: 1, col: 4, label: '他人看法' },
            { row: 0, col: 4, label: '最终结果' },
          ],
          gridCols: 5,
          spacing: 1,
        };

      default:
        // 默认网格布局
        const cols = Math.ceil(Math.sqrt(cardCount));
        const positions = Array.from({ length: cardCount }, (_, i) => ({
          row: Math.floor(i / cols),
          col: i % cols,
          label: `位置 ${i + 1}`,
        }));
        return { positions, gridCols: cols, spacing: 2 };
    }
  };

  const layout = getSpreadLayout(spread.name, spread.card_count);

  // 创建网格系统
  const createGrid = () => {
    const maxRow = Math.max(...layout.positions.map(p => p.row)) + 1;
    const maxCol = layout.gridCols;

    const grid = Array.from({ length: maxRow }, () =>
      Array.from({ length: maxCol }, () => null as React.ReactNode)
    );

    // 放置卡片到对应位置
    layout.positions.forEach((pos, index) => {
      const drawnCard = drawnCards[index];

      if (drawnCard) {
        const isFlippedByExplicitState = explicitFlippedSet.has(index);
        const isFlippedByLegacyReveal = (isRevealing || revealedPositions.length > 0)
          ? revealedSet.has(index)
          : !allowManualFlip;
        const isCardFlipped = isFlippedByExplicitState || isFlippedByLegacyReveal;
        const canManualFlip = allowManualFlip && Boolean(onCardFlip) && !isCardFlipped;

        grid[pos.row][pos.col] = (
          <Box
            key={`card-${index}`}
            sx={{
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              gap: 1,
              position: 'relative',
            }}
          >
            {/* 牌位标签 */}
            <Typography
              variant="caption"
              sx={{
                color: 'text.secondary',
                fontWeight: 'medium',
                textAlign: 'center',
                minHeight: 20,
                fontSize: '0.75rem',
              }}
            >
              {pos.label}
            </Typography>

            {/* 3D 翻转塔罗牌 */}
            <Zoom
              in={true}
              timeout={500 + index * 200}
              style={{ transitionDelay: `${index * 200}ms` }}
            >
              <Box>
                <Card3DFlip
                  card={drawnCard.card}
                  isFlipped={isCardFlipped}
                  isReversed={drawnCard.isReversed}
                  isRevealing={isRevealing && revealedSet.has(index)}
                  size={cardSize}
                  imageSrc={getTarotCardImagePath(drawnCard.card)}
                  imageError={imageErrors.has(index)}
                  onImageError={() => {
                    const newErrors = new Set(imageErrors);
                    newErrors.add(index);
                    setImageErrors(newErrors);
                  }}
                  onCardClick={() => {
                    if (canManualFlip) {
                      setActiveSparklePosition(index);
                      onCardFlip?.(drawnCard.card, index);
                      // Turn off sparkles after animation duration approx
                      setTimeout(() => setActiveSparklePosition(null), 2500);
                      return;
                    }

                    onCardClick?.(drawnCard.card, index);
                  }}
                  disableClick={!canManualFlip && !onCardClick}
                  onFlipComplete={() => {
                    if (!allowManualFlip && onCardClick) {
                      onCardClick(drawnCard.card, index);
                    }
                  }}
                />

                {/* 3D 星尘涌动爆发特效 */}
                {activeSparklePosition === index && (
                  <FlipSparklesContainer isActive={true} />
                )}
              </Box>
            </Zoom>

            {/* 位置编号 */}
            <Box
              sx={{
                position: 'absolute',
                top: -8,
                left: -8,
                width: 24,
                height: 24,
                borderRadius: '50%',
                backgroundColor: 'primary.main',
                color: 'white',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontSize: '0.75rem',
                fontWeight: 'bold',
                zIndex: 1,
              }}
            >
              {index + 1}
            </Box>
          </Box>
        );
      } else {
        // 空位占位符
        grid[pos.row][pos.col] = (
          <Box
            key={`placeholder-${index}`}
            sx={{
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              gap: 1,
            }}
          >
            <Typography
              variant="caption"
              sx={{
                color: 'text.secondary',
                fontWeight: 'medium',
                textAlign: 'center',
                minHeight: 20,
              }}
            >
              {pos.label}
            </Typography>

            <Paper
              elevation={1}
              sx={{
                width: cardDimensions.width,
                height: cardDimensions.height,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                backgroundColor: 'rgba(0, 0, 0, 0.05)',
                border: '2px dashed rgba(0, 0, 0, 0.2)',
                borderRadius: 2,
              }}
            >
              <Typography
                variant="body2"
                sx={{
                  color: 'text.secondary',
                  textAlign: 'center',
                }}
              >
                位置 {index + 1}
              </Typography>
            </Paper>
          </Box>
        );
      }
    });

    return grid;
  };

  const grid = createGrid();

  return (
    <Box sx={{ width: '100%', maxWidth: 1280, mx: 'auto', p: 2, overflowX: 'auto' }}>
      {/* 牌阵标题 */}
      {showSpreadMeta && (
        <Box sx={{ textAlign: 'center', mb: 4 }}>
          <Typography
            variant="h4"
            component="h2"
            sx={{
              fontWeight: 'bold',
              color: 'primary.main',
              mb: 1,
            }}
          >
            {spread.name}
          </Typography>
          <Typography
            variant="body1"
            sx={{
              color: 'text.secondary',
              maxWidth: 600,
              mx: 'auto',
            }}
          >
            {spread.description}
          </Typography>
        </Box>
      )}

      {/* 牌阵布局 */}
      <Fade in={true} timeout={300}>
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: layout.spacing,
          }}
        >
          {grid.map((row, rowIndex) => (
            <Box
              key={`row-${rowIndex}`}
              sx={{
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center',
                gap: layout.spacing,
                flexWrap: 'wrap',
              }}
            >
              {row.map((cell, colIndex) => (
                <Box key={`cell-${rowIndex}-${colIndex}`}>
                  {cell}
                </Box>
              ))}
            </Box>
          ))}
        </Box>
      </Fade>

      {/* 牌阵说明 */}
      {showSpreadDescription && drawnCards.length > 0 && (
        <Box
          sx={{
            mt: 4,
            p: 2,
            backgroundColor: 'rgba(0, 0, 0, 0.02)',
            borderRadius: 2,
            border: '1px solid rgba(0, 0, 0, 0.1)',
          }}
        >
          <Typography
            variant="subtitle2"
            sx={{
              fontWeight: 'bold',
              mb: 1,
              color: 'primary.main',
            }}
          >
            牌阵说明：
          </Typography>
          <Typography
            variant="body2"
            sx={{
              color: 'text.secondary',
              lineHeight: 1.6,
            }}
          >
            {spread.description}
          </Typography>
        </Box>
      )}
    </Box>
  );
};

export default CardSpread;
