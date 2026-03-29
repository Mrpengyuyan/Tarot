import React, { useEffect, useMemo, useState } from 'react';
import { Box, Chip, Paper, Typography } from '@mui/material';
import { Timeline } from 'icons';
import Loading from '../../components/UI/Loading';
import { useNotification } from '../../components/UI/Notification';
import { tarotService } from '../../services/tarotService';
import { SpreadType } from '../../types/api';
import {
  getSpreadDisplayDescription,
  getSpreadDisplayName,
  getSpreadPositionLabels,
} from '../../utils/spreadDisplay';

const questionTypeMeta: Record<string, { label: string; color: string }> = {
  love: { label: '感情', color: '#FF6B9D' },
  career: { label: '事业', color: '#4ECDC4' },
  finance: { label: '财运', color: '#FFD93D' },
  health: { label: '健康', color: '#6BCF7F' },
  general: { label: '综合', color: '#AB83A1' },
};

const difficultyLabel = (difficulty: number): string => {
  if (difficulty <= 2) return '入门友好';
  if (difficulty === 3) return '进阶解读';
  return '深度分析';
};

const getSuitableTypes = (spread: SpreadType): string[] => {
  const matches: string[] = [];
  if (spread.suitable_for_love) matches.push('love');
  if (spread.suitable_for_career) matches.push('career');
  if (spread.suitable_for_finance) matches.push('finance');
  if (spread.suitable_for_health) matches.push('health');
  if (spread.suitable_for_general) matches.push('general');
  return matches;
};

const SpreadCatalogPage: React.FC = () => {
  const { showError } = useNotification();
  const [spreads, setSpreads] = useState<SpreadType[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;

    const loadSpreads = async () => {
      setLoading(true);
      setError(null);
      try {
        const detail = await tarotService.getAllSpreads();
        if (!active) return;
        setSpreads(detail.sort((left, right) => left.card_count - right.card_count || left.id - right.id));
      } catch (err) {
        const message = err instanceof Error ? err.message : '加载牌阵目录失败';
        if (!active) return;
        setError(message);
        showError(message);
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    };

    loadSpreads();

    return () => {
      active = false;
    };
  }, [showError]);

  const stats = useMemo(() => {
    const beginnerCount = spreads.filter((spread) => spread.is_beginner_friendly).length;
    const maxCardCount = spreads.reduce((max, spread) => Math.max(max, spread.card_count), 0);
    return { total: spreads.length, beginnerCount, maxCardCount };
  }, [spreads]);

  if (loading) {
    return <Loading message="正在展开牌阵目录..." />;
  }

  return (
    <Box
      sx={{
        width: 'min(1520px, calc(100vw - 56px))',
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
          background: 'linear-gradient(135deg, rgba(86, 189, 248, 0.12) 0%, rgba(90, 30, 130, 0.72) 45%, rgba(10, 10, 26, 0.94) 100%)',
          border: '1px solid rgba(86, 189, 248, 0.2)',
          boxShadow: '0 24px 72px rgba(14, 6, 35, 0.4)',
        }}
      >
        <Box
          sx={{
            position: 'absolute',
            top: -80,
            right: -80,
            width: 280,
            height: 280,
            borderRadius: '50%',
            background: 'radial-gradient(circle, rgba(86, 189, 248, 0.28) 0%, transparent 70%)',
          }}
        />

        <Box sx={{ position: 'relative', zIndex: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2 }}>
            <Timeline sx={{ color: '#8ED8FF', fontSize: '2.1rem' }} />
            <Typography variant="h3" sx={{ fontFamily: 'Cinzel, serif', color: 'primary.main', fontWeight: 700 }}>
              牌阵目录
            </Typography>
          </Box>

          <Typography variant="h6" sx={{ maxWidth: 860, color: 'text.secondary', lineHeight: 1.8, mb: 3 }}>
            所有牌阵按卡牌数量与复杂度展开展示。你可以快速看到每个牌阵适合的问题类型、牌位结构以及阅读深度。
          </Typography>

          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.2 }}>
            <Chip label={`牌阵总数 ${stats.total}`} sx={{ background: 'rgba(86, 189, 248, 0.16)', color: '#8ED8FF' }} />
            <Chip label={`初学者友好 ${stats.beginnerCount}`} sx={{ background: 'rgba(107, 207, 127, 0.14)', color: '#9DE7A8' }} />
            <Chip label={`最大牌数 ${stats.maxCardCount}`} sx={{ background: 'rgba(212, 175, 55, 0.14)', color: 'primary.main' }} />
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
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', xl: 'repeat(2, minmax(0, 1fr))' },
            gap: 4,
          }}
        >
          {spreads.map((spread) => {
            const labels = getSpreadPositionLabels(spread, spread.card_count);
            const suitableTypes = getSuitableTypes(spread);

            return (
              <Paper
                key={spread.id}
                elevation={0}
                sx={{
                  position: 'relative',
                  overflow: 'hidden',
                  minHeight: 400,
                  p: { xs: 3, md: 5 },
                  borderRadius: 5,
                  border: '1px solid rgba(212, 175, 55, 0.16)',
                  background: 'linear-gradient(145deg, rgba(18, 21, 44, 0.86) 0%, rgba(10, 10, 24, 0.95) 100%)',
                  boxShadow: '0 18px 40px rgba(6, 8, 20, 0.45)',
                }}
              >
                <Box
                  sx={{
                    position: 'absolute',
                    top: -70,
                    right: -60,
                    width: 220,
                    height: 220,
                    borderRadius: '50%',
                    background: 'radial-gradient(circle, rgba(212, 175, 55, 0.16) 0%, transparent 66%)',
                  }}
                />

                <Box sx={{ position: 'relative', zIndex: 1 }}>
                  <Box
                    sx={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      gap: 2,
                      flexWrap: 'wrap',
                      alignItems: 'flex-start',
                      mb: 3,
                    }}
                  >
                    <Box>
                      <Typography
                        variant="h4"
                        sx={{
                          color: 'primary.main',
                          fontFamily: 'Cinzel, serif',
                          fontWeight: 700,
                          mb: 1,
                        }}
                      >
                        {getSpreadDisplayName(spread)}
                      </Typography>
                      <Typography variant="body1" sx={{ color: 'text.secondary', lineHeight: 1.75, maxWidth: 560 }}>
                        {getSpreadDisplayDescription(spread)}
                      </Typography>
                    </Box>

                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5, minWidth: 180 }}>
                      <Chip label={`${spread.card_count} 张牌`} sx={{ background: 'rgba(212, 175, 55, 0.12)', color: 'primary.main' }} />
                      <Chip label={`难度 ${spread.difficulty_level} · ${difficultyLabel(spread.difficulty_level)}`} sx={{ background: 'rgba(171, 131, 161, 0.14)', color: '#E3CFF0' }} />
                      <Chip label={`使用 ${spread.usage_count || 0} 次`} sx={{ background: 'rgba(86, 189, 248, 0.14)', color: '#8ED8FF' }} />
                    </Box>
                  </Box>

                  <Box
                    sx={{
                      display: 'grid',
                      gridTemplateColumns: {
                        xs: 'repeat(2, minmax(0, 1fr))',
                        md: 'repeat(5, minmax(0, 1fr))',
                      },
                      gap: 2,
                      mb: 4,
                    }}
                  >
                    {Array.from({ length: spread.card_count }, (_, index) => (
                      <Box
                        key={`${spread.id}-${index}`}
                        sx={{
                          minHeight: 100,
                          borderRadius: 3,
                          p: 2,
                          border: '1px solid rgba(255,255,255,0.08)',
                          background: index % 2 === 0
                            ? 'linear-gradient(180deg, rgba(212, 175, 55, 0.12) 0%, rgba(26, 28, 54, 0.9) 100%)'
                            : 'linear-gradient(180deg, rgba(86, 189, 248, 0.12) 0%, rgba(26, 28, 54, 0.9) 100%)',
                        }}
                      >
                        <Typography variant="body2" sx={{ color: 'primary.main', letterSpacing: 1, fontWeight: 600 }}>
                          位置 {index + 1}
                        </Typography>
                        <Typography variant="body1" sx={{ color: 'text.primary', mt: 0.5, fontWeight: 500 }}>
                          {labels[index] || `位置 ${index + 1}`}
                        </Typography>
                      </Box>
                    ))}
                  </Box>

                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5, alignItems: 'flex-start' }}>
                    {spread.is_beginner_friendly && (
                      <Chip label="适合初学者" sx={{ background: 'rgba(107, 207, 127, 0.14)', color: '#9DE7A8' }} />
                    )}
                    {suitableTypes.map((type) => (
                      <Chip
                        key={`${spread.id}-${type}`}
                        label={questionTypeMeta[type].label}
                        variant="outlined"
                        sx={{
                          borderColor: `${questionTypeMeta[type].color}66`,
                          color: questionTypeMeta[type].color,
                        }}
                      />
                    ))}
                    {suitableTypes.length === 0 && <Chip label="适用场景待补充" variant="outlined" />}
                  </Box>
                </Box>
              </Paper>
            );
          })}
        </Box>
      )}
    </Box>
  );
};

export default SpreadCatalogPage;
