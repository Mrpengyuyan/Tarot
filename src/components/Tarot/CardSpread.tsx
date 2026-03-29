import React, { useMemo, useState } from 'react';
import {
  Box,
  Fade,
  Paper,
  Typography,
  Zoom,
} from '@mui/material';
import Card3DFlip from './Card3DFlip';
import { FlipSparklesContainer } from '../Effects/FlipSparkles';
import { TarotCard as TarotCardType, SpreadType } from '../../types/api';
import { getTarotCardImagePath } from '../../utils/tarotImageMapper';
import type { VisualQuality } from '../../hooks/useVisualSettings';
import {
  getSpreadDisplayDescription,
  getSpreadDisplayName,
  getSpreadPositionLabels,
} from '../../utils/spreadDisplay';

interface DrawnCard {
  card: TarotCardType;
  isReversed: boolean;
  position: number;
}

interface SpreadLayoutCell {
  row: number;
  col: number;
  label: string;
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
  visualQuality?: VisualQuality;
  motionPreset?: 'full' | 'lite' | 'flat';
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
  visualQuality = 'full',
  motionPreset = 'full',
}) => {
  const [imageErrors, setImageErrors] = useState<Set<number>>(new Set());
  const [activeSparklePosition, setActiveSparklePosition] = useState<number | null>(null);
  const revealedSet = useMemo(() => new Set(revealedPositions), [revealedPositions]);
  const explicitFlippedSet = useMemo(() => new Set(flippedPositions || []), [flippedPositions]);
  const sparkleQuality = visualQuality === 'full' ? 'full' : visualQuality === 'lite' ? 'lite' : 'off';
  const zoomTimeout = motionPreset === 'flat' ? 0 : motionPreset === 'lite' ? 240 : 500;
  const zoomDelay = motionPreset === 'flat' ? 0 : motionPreset === 'lite' ? 60 : 120;

  const cardSize = cardSizeOverride || (spread.card_count >= 6 ? 'small' : spread.card_count >= 4 ? 'medium' : 'large');
  const cardDimensions = {
    small: { width: 150, height: 250 },
    medium: { width: 200, height: 330 },
    large: { width: 280, height: 460 },
  }[cardSize];

  const layout = useMemo(() => {
    const labels = getSpreadPositionLabels(spread, spread.card_count);

    const withLabels = (cells: Omit<SpreadLayoutCell, 'label'>[]): SpreadLayoutCell[] =>
      cells.map((cell, index) => ({
        ...cell,
        label: labels[index] || `位置 ${index + 1}`,
      }));

    switch (spread.id) {
      case 1:
        return { gridCols: 1, spacing: 2, positions: withLabels([{ row: 0, col: 0 }]) };
      case 2:
        return {
          gridCols: 3,
          spacing: 2,
          positions: withLabels([
            { row: 0, col: 0 },
            { row: 0, col: 1 },
            { row: 0, col: 2 },
          ]),
        };
      case 3:
        return {
          gridCols: 3,
          spacing: 1.5,
          positions: withLabels([
            { row: 0, col: 1 },
            { row: 1, col: 0 },
            { row: 1, col: 1 },
            { row: 1, col: 2 },
            { row: 2, col: 1 },
          ]),
        };
      case 4:
        return {
          gridCols: 3,
          spacing: 1.5,
          positions: withLabels([
            { row: 0, col: 1 },
            { row: 1, col: 0 },
            { row: 1, col: 1 },
            { row: 1, col: 2 },
            { row: 2, col: 0 },
            { row: 2, col: 2 },
          ]),
        };
      case 5:
        return {
          gridCols: 5,
          spacing: 1.2,
          positions: withLabels([
            { row: 1, col: 1 },
            { row: 1, col: 2 },
            { row: 0, col: 1 },
            { row: 2, col: 1 },
            { row: 1, col: 3 },
            { row: 1, col: 0 },
            { row: 0, col: 4 },
            { row: 1, col: 4 },
            { row: 2, col: 4 },
            { row: 3, col: 4 },
          ]),
        };
      case 6:
        return {
          gridCols: 2,
          spacing: 2,
          positions: withLabels([
            { row: 0, col: 0 },
            { row: 0, col: 1 },
            { row: 1, col: 0 },
            { row: 1, col: 1 },
          ]),
        };
      default: {
        const cols = Math.ceil(Math.sqrt(spread.card_count));
        const positions = Array.from({ length: spread.card_count }, (_, index) => ({
          row: Math.floor(index / cols),
          col: index % cols,
        }));
        return {
          gridCols: cols,
          spacing: 2,
          positions: withLabels(positions),
        };
      }
    }
  }, [spread]);

  const grid = useMemo(() => {
    const maxRow = Math.max(...layout.positions.map((position) => position.row)) + 1;
    const gridCells = Array.from({ length: maxRow }, () =>
      Array.from({ length: layout.gridCols }, () => null as React.ReactNode),
    );

    layout.positions.forEach((position, index) => {
      const drawnCard = drawnCards[index];

      if (!drawnCard) {
        gridCells[position.row][position.col] = (
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
                fontWeight: 500,
                textAlign: 'center',
                minHeight: 20,
              }}
            >
              {position.label}
            </Typography>

            <Paper
              elevation={1}
              sx={{
                width: cardDimensions.width,
                height: cardDimensions.height,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                backgroundColor: 'rgba(255, 255, 255, 0.03)',
                border: '2px dashed rgba(212, 175, 55, 0.18)',
                borderRadius: 2,
              }}
            >
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                位置 {index + 1}
              </Typography>
            </Paper>
          </Box>
        );
        return;
      }

      const isFlippedByExplicitState = explicitFlippedSet.has(index);
      const isFlippedByLegacyReveal = (isRevealing || revealedPositions.length > 0)
        ? revealedSet.has(index)
        : !allowManualFlip;
      const isCardFlipped = isFlippedByExplicitState || isFlippedByLegacyReveal;
      const canManualFlip = allowManualFlip && Boolean(onCardFlip) && !isCardFlipped;
      const isCelticCrossCenterPair = spread.id === 5 && (index === 0 || index === 1);

      gridCells[position.row][position.col] = (
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
          <Typography
            variant="caption"
            sx={{
              color: 'text.secondary',
              fontWeight: 500,
              textAlign: 'center',
              minHeight: 20,
              fontSize: '0.75rem',
            }}
          >
            {position.label}
          </Typography>

          <Zoom
            in={true}
            timeout={zoomTimeout + index * (motionPreset === 'flat' ? 0 : motionPreset === 'lite' ? 90 : 160)}
            style={{ transitionDelay: `${index * zoomDelay}ms` }}
          >
            <Box sx={{ transform: isCelticCrossCenterPair && index === 1 ? 'rotate(90deg)' : 'none' }}>
              <Card3DFlip
                card={drawnCard.card}
                isFlipped={isCardFlipped}
                isReversed={drawnCard.isReversed}
                isRevealing={isRevealing && revealedSet.has(index)}
                size={cardSize}
                imageSrc={getTarotCardImagePath(drawnCard.card)}
                imageError={imageErrors.has(index)}
                onImageError={() => {
                  setImageErrors((prev) => {
                    const next = new Set(prev);
                    next.add(index);
                    return next;
                  });
                }}
                motionPreset={motionPreset}
                onCardClick={() => {
                  if (canManualFlip) {
                    setActiveSparklePosition(index);
                    onCardFlip?.(drawnCard.card, index);
                    window.setTimeout(() => setActiveSparklePosition(null), 2500);
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

              {activeSparklePosition === index && (
                <FlipSparklesContainer isActive={true} quality={sparkleQuality} />
              )}
            </Box>
          </Zoom>

          <Box
            sx={{
              position: 'absolute',
              top: -8,
              left: -8,
              width: 24,
              height: 24,
              borderRadius: '50%',
              backgroundColor: 'primary.main',
              color: '#0b0816',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: '0.75rem',
              fontWeight: 700,
              zIndex: 1,
              boxShadow: '0 4px 14px rgba(0, 0, 0, 0.35)',
            }}
          >
            {index + 1}
          </Box>
        </Box>
      );
    });

    return gridCells;
  }, [
    allowManualFlip,
    cardDimensions.height,
    cardDimensions.width,
    cardSize,
    drawnCards,
    explicitFlippedSet,
    imageErrors,
    isRevealing,
    layout.gridCols,
    layout.positions,
    onCardClick,
    onCardFlip,
    revealedPositions.length,
    revealedSet,
    spread.id,
    activeSparklePosition,
    motionPreset,
    sparkleQuality,
    zoomDelay,
    zoomTimeout,
  ]);

  return (
    <Box sx={{ width: '100%', maxWidth: 1280, mx: 'auto', p: 2, overflowX: 'auto' }}>
      {showSpreadMeta && (
        <Box sx={{ textAlign: 'center', mb: 4 }}>
          <Typography
            variant="h4"
            component="h2"
            sx={{
              fontWeight: 700,
              color: 'primary.main',
              mb: 1,
            }}
          >
            {getSpreadDisplayName(spread)}
          </Typography>
          <Typography
            variant="body1"
            sx={{
              color: 'text.secondary',
              maxWidth: 680,
              mx: 'auto',
              lineHeight: 1.7,
            }}
          >
            {getSpreadDisplayDescription(spread)}
          </Typography>
        </Box>
      )}

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
                <Box key={`cell-${rowIndex}-${colIndex}`}>{cell}</Box>
              ))}
            </Box>
          ))}
        </Box>
      </Fade>

      {showSpreadDescription && drawnCards.length > 0 && (
        <Box
          sx={{
            mt: 4,
            p: 2,
            backgroundColor: 'rgba(255, 255, 255, 0.03)',
            borderRadius: 2,
            border: '1px solid rgba(212, 175, 55, 0.12)',
          }}
        >
          <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1, color: 'primary.main' }}>
            牌阵说明
          </Typography>
          <Typography variant="body2" sx={{ color: 'text.secondary', lineHeight: 1.7 }}>
            {getSpreadDisplayDescription(spread)}
          </Typography>
        </Box>
      )}
    </Box>
  );
};

export default CardSpread;
