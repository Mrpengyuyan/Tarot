import React, { useEffect, useMemo, useState } from 'react';
import { Box, Chip, Divider, Paper, Typography } from '@mui/material';
import { AutoAwesome, MenuBook } from 'icons';
import Loading from '../../components/UI/Loading';
import { useNotification } from '../../components/UI/Notification';
import { tarotService } from '../../services/tarotService';
import { TarotCard } from '../../types/api';
import { getTarotCardImagePath } from '../../utils/tarotImageMapper';

const suitMeta: Record<string, { label: string; accent: string; glow: string }> = {
  wands: { label: '权杖', accent: '#FF8C42', glow: 'rgba(255, 140, 66, 0.28)' },
  cups: { label: '圣杯', accent: '#56BDF8', glow: 'rgba(86, 189, 248, 0.24)' },
  swords: { label: '宝剑', accent: '#C9D6FF', glow: 'rgba(201, 214, 255, 0.22)' },
  pentacles: { label: '星币', accent: '#6BCF7F', glow: 'rgba(107, 207, 127, 0.22)' },
};

const minorNumberLabel = (value: number): string => {
  if (value === 1) return 'A';
  if (value >= 2 && value <= 10) return `${value}`;
  if (value === 11) return '侍从';
  if (value === 12) return '骑士';
  if (value === 13) return '皇后';
  return '国王';
};

const getDisplaySequence = (card: TarotCard): string => {
  if (card.card_type === 'major_arcana') {
    return `大阿卡纳 · ${card.card_number.toString().padStart(2, '0')}`;
  }

  const suit = card.suit ? suitMeta[card.suit]?.label || card.suit : '小阿卡纳';
  return `${suit} · ${minorNumberLabel(card.card_number)}`;
};

const sortCards = (cards: TarotCard[]): TarotCard[] => {
  const suitOrder = ['wands', 'cups', 'swords', 'pentacles'];
  return [...cards].sort((left, right) => {
    if (left.card_type !== right.card_type) {
      return left.card_type === 'major_arcana' ? -1 : 1;
    }

    if (left.card_type === 'major_arcana') {
      return left.card_number - right.card_number;
    }

    const leftSuit = suitOrder.indexOf(left.suit || '');
    const rightSuit = suitOrder.indexOf(right.suit || '');
    if (leftSuit !== rightSuit) {
      return leftSuit - rightSuit;
    }

    return left.card_number - right.card_number;
  });
};

const cardGridSx = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fill, minmax(160px, 1fr))',
  gap: { xs: 1.5, md: 2.2 },
};

const TarotLibraryPage: React.FC = () => {
  const { showError } = useNotification();
  const [cards, setCards] = useState<TarotCard[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;

    const loadCards = async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await tarotService.getAllCards();
        if (!active) return;
        setCards(sortCards(data));
      } catch (err) {
        const message = err instanceof Error ? err.message : '加载塔罗牌库失败';
        if (!active) return;
        setError(message);
        showError(message);
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    };

    loadCards();

    return () => {
      active = false;
    };
  }, [showError]);

  const majorArcana = useMemo(
    () => cards.filter((card) => card.card_type === 'major_arcana'),
    [cards],
  );
  const minorArcanaBySuit = useMemo(() => {
    return ['wands', 'cups', 'swords', 'pentacles'].map((suit) => ({
      suit,
      meta: suitMeta[suit],
      cards: cards.filter((card) => card.card_type === 'minor_arcana' && card.suit === suit),
    }));
  }, [cards]);

  if (loading) {
    return <Loading message="正在整理塔罗牌库..." />;
  }

  return (
    <Box
      sx={{
        width: 'min(1560px, calc(100vw - 56px))',
        marginLeft: '50%',
        transform: 'translateX(-50%)',
        py: 2,
      }}
    >
      <Box
        sx={{
          position: 'relative',
          overflow: 'hidden',
          borderRadius: 5,
          px: { xs: 3, md: 5 },
          py: { xs: 4, md: 5 },
          mb: 4,
          background: 'linear-gradient(135deg, rgba(212, 175, 55, 0.15) 0%, rgba(41, 10, 75, 0.72) 42%, rgba(10, 10, 28, 0.92) 100%)',
          border: '1px solid rgba(212, 175, 55, 0.22)',
          boxShadow: '0 24px 80px rgba(14, 6, 35, 0.45)',
        }}
      >
        <Box
          sx={{
            position: 'absolute',
            top: -80,
            right: -60,
            width: 260,
            height: 260,
            borderRadius: '50%',
            background: 'radial-gradient(circle, rgba(212, 175, 55, 0.34) 0%, transparent 68%)',
          }}
        />
        <Box
          sx={{
            position: 'absolute',
            bottom: -120,
            left: -40,
            width: 320,
            height: 320,
            borderRadius: '50%',
            background: 'radial-gradient(circle, rgba(86, 189, 248, 0.18) 0%, transparent 66%)',
          }}
        />

        <Box sx={{ position: 'relative', zIndex: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2 }}>
            <MenuBook sx={{ color: 'primary.main', fontSize: '2.1rem' }} />
            <Typography variant="h3" sx={{ fontFamily: 'Cinzel, serif', color: 'primary.main', fontWeight: 700 }}>
              塔罗牌库
            </Typography>
          </Box>

          <Typography variant="h6" sx={{ maxWidth: 860, color: 'text.secondary', lineHeight: 1.8, mb: 3 }}>
            78 张牌按大阿卡纳与四组小阿卡纳展开，保持种子数据的结构顺序，方便你从整体象征到花色脉络完整浏览。
          </Typography>

          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.2 }}>
            <Chip label={`总牌数 ${cards.length}`} sx={{ background: 'rgba(212, 175, 55, 0.16)', color: 'primary.main' }} />
            <Chip label={`大阿卡纳 ${majorArcana.length}`} sx={{ background: 'rgba(255,255,255,0.08)', color: 'text.primary' }} />
            <Chip label={`小阿卡纳 ${cards.length - majorArcana.length}`} sx={{ background: 'rgba(86, 189, 248, 0.14)', color: '#8ED8FF' }} />
            <Chip label="四大花色完整展开" sx={{ background: 'rgba(122, 90, 255, 0.16)', color: '#D6C2FF' }} />
          </Box>
        </Box>
      </Box>

      {error ? (
        <Paper sx={{ p: 4, borderRadius: 4 }}>
          <Typography color="error" variant="h6">
            {error}
          </Typography>
        </Paper>
      ) : (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
          <Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2 }}>
              <AutoAwesome sx={{ color: 'primary.main' }} />
              <Typography variant="h4" sx={{ fontFamily: 'Cinzel, serif', color: 'primary.main' }}>
                大阿卡纳
              </Typography>
            </Box>
            <Typography variant="body1" sx={{ color: 'text.secondary', mb: 3 }}>
              从愚者到世界，完整呈现 22 张主牌的成长路径与象征主线。
            </Typography>

            <Box sx={cardGridSx}>
              {majorArcana.map((card) => (
                <Paper
                  key={card.id}
                  elevation={0}
                  sx={{
                    position: 'relative',
                    overflow: 'hidden',
                    minHeight: 320,
                    borderRadius: 4,
                    border: '1px solid rgba(212, 175, 55, 0.18)',
                    background: 'linear-gradient(180deg, rgba(14, 18, 41, 0.65) 0%, rgba(8, 8, 20, 0.98) 100%)',
                    boxShadow: '0 14px 34px rgba(0, 0, 0, 0.28)',
                    transition: 'transform 0.28s ease, box-shadow 0.28s ease',
                    '&:hover': {
                      transform: 'translateY(-8px) scale(1.02)',
                      boxShadow: '0 24px 46px rgba(212, 175, 55, 0.2)',
                    },
                  }}
                >
                  <Box
                    component="img"
                    src={getTarotCardImagePath(card)}
                    alt={card.name_zh}
                    loading="lazy"
                    onError={(event: React.SyntheticEvent<HTMLImageElement>) => {
                      event.currentTarget.src = '/images/tarot-cards/card-backs/classic.svg';
                    }}
                    sx={{
                      width: '100%',
                      height: 240,
                      objectFit: 'cover',
                      display: 'block',
                      background: '#090B18',
                    }}
                  />
                  <Box
                    sx={{
                      position: 'absolute',
                      inset: 'auto 0 0 0',
                      px: 2,
                      py: 1.8,
                      background: 'linear-gradient(180deg, rgba(10, 10, 22, 0.02) 0%, rgba(10, 10, 22, 0.92) 38%, rgba(7, 7, 18, 0.98) 100%)',
                    }}
                  >
                    <Typography variant="caption" sx={{ color: 'primary.main', letterSpacing: 1 }}>
                      {getDisplaySequence(card)}
                    </Typography>
                    <Typography variant="h6" sx={{ mt: 0.5, color: 'text.primary', fontFamily: 'Cinzel, serif' }}>
                      {card.name_zh}
                    </Typography>
                    <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                      {card.name_en}
                    </Typography>
                  </Box>
                </Paper>
              ))}
            </Box>
          </Box>

          {minorArcanaBySuit.map(({ suit, meta, cards: suitCards }) => (
            <Box key={suit}>
              <Box
                sx={{
                  display: 'flex',
                  flexDirection: { xs: 'column', md: 'row' },
                  justifyContent: 'space-between',
                  gap: 2,
                  alignItems: { xs: 'flex-start', md: 'center' },
                  mb: 2.5,
                }}
              >
                <Box>
                  <Typography variant="h4" sx={{ fontFamily: 'Cinzel, serif', color: meta.accent, fontWeight: 700 }}>
                    {meta.label}
                  </Typography>
                  <Typography variant="body1" sx={{ color: 'text.secondary', mt: 0.5 }}>
                    14 张 {meta.label} 依序铺开，覆盖数字牌与宫廷牌的完整结构。
                  </Typography>
                </Box>
                <Chip
                  label={`${meta.label} · ${suitCards.length} 张`}
                  sx={{
                    color: meta.accent,
                    border: `1px solid ${meta.accent}66`,
                    background: meta.glow,
                  }}
                />
              </Box>

              <Box
                sx={{
                  ...cardGridSx,
                  p: { xs: 1, md: 1.5 },
                  borderRadius: 5,
                  background: `linear-gradient(180deg, ${meta.glow} 0%, rgba(7, 9, 18, 0.1) 100%)`,
                }}
              >
                {suitCards.map((card) => (
                  <Paper
                    key={card.id}
                    elevation={0}
                    sx={{
                      position: 'relative',
                      overflow: 'hidden',
                      minHeight: 300,
                      borderRadius: 4,
                      border: '1px solid rgba(255,255,255,0.08)',
                      background: 'linear-gradient(180deg, rgba(18, 21, 44, 0.7) 0%, rgba(9, 10, 22, 0.98) 100%)',
                      boxShadow: `0 14px 28px ${meta.glow}`,
                      transition: 'transform 0.28s ease, box-shadow 0.28s ease',
                      '&:hover': {
                        transform: 'translateY(-6px)',
                        boxShadow: `0 18px 40px ${meta.glow}`,
                      },
                    }}
                  >
                    <Box
                      component="img"
                      src={getTarotCardImagePath(card)}
                      alt={card.name_zh}
                      loading="lazy"
                      onError={(event: React.SyntheticEvent<HTMLImageElement>) => {
                        event.currentTarget.src = '/images/tarot-cards/card-backs/classic.svg';
                      }}
                      sx={{
                        width: '100%',
                        height: 220,
                        objectFit: 'cover',
                        display: 'block',
                        background: '#090B18',
                      }}
                    />
                    <Box sx={{ p: 2 }}>
                      <Typography variant="caption" sx={{ color: meta.accent, letterSpacing: 0.8 }}>
                        {getDisplaySequence(card)}
                      </Typography>
                      <Typography variant="h6" sx={{ mt: 0.5, color: 'text.primary', fontFamily: 'Cinzel, serif' }}>
                        {card.name_zh}
                      </Typography>
                      <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                        {card.name_en}
                      </Typography>
                    </Box>
                  </Paper>
                ))}
              </Box>
              <Divider sx={{ mt: 4, borderColor: 'rgba(255,255,255,0.08)' }} />
            </Box>
          ))}
        </Box>
      )}
    </Box>
  );
};

export default TarotLibraryPage;
