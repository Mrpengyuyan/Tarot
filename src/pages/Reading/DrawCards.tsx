import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  Chip,
  Container,
  Paper,
  Step,
  StepLabel,
  Stepper,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material';
import {
  ArrowBack,
  AutoAwesome,
  Autorenew,
  Refresh,
  Visibility,
} from 'icons';
import { useNavigate } from 'react-router-dom';
import CardSpread from '../../components/Tarot/CardSpread';
import Loading from '../../components/UI/Loading';
import { useNotification } from '../../components/UI/Notification';
import { ROUTES } from '../../routes/routeConfig';
import CosmicBackground from '../../components/Effects/CosmicBackground';
import { useVisualSettings } from '../../hooks/useVisualSettings';
import {
  CardDrawWithMeaning,
  CreateReadingParams,
  DrawnCard,
  Interpretation,
  localDrawingUtils,
  tarotService,
} from '../../services/tarotService';
import { GamePhase, useGameStore } from '../../stores/gameStore';
import { SpreadType } from '../../types/api';
import {
  getSpreadDisplayDescription,
  getSpreadDisplayName,
} from '../../utils/spreadDisplay';

type QuestionType = CreateReadingParams['questionType'];

const steps = ['选择牌阵', '输入问题', '抽牌与解读'];

const questionTypeOptions: Array<{ value: QuestionType; label: string }> = [
  { value: 'general', label: '综合' },
  { value: 'love', label: '感情' },
  { value: 'career', label: '事业' },
  { value: 'finance', label: '财运' },
  { value: 'health', label: '健康' },
];

const DrawCards: React.FC = () => {
  const navigate = useNavigate();
  const { showError } = useNotification();
  const { setPhase } = useGameStore();
  const showErrorRef = useRef(showError);
  const visual = useVisualSettings();

  const [activeStep, setActiveStep] = useState(0);
  const [spreads, setSpreads] = useState<SpreadType[]>([]);
  const [isLoadingSpreads, setIsLoadingSpreads] = useState(false);
  const [selectedSpreadId, setSelectedSpreadId] = useState<number | null>(null);

  const [question, setQuestion] = useState('');
  const [questionType, setQuestionType] = useState<QuestionType>('general');

  const [isDrawing, setIsDrawing] = useState(false);
  const [drawError, setDrawError] = useState<string | null>(null);
  const [drawnCards, setDrawnCards] = useState<DrawnCard[]>([]);
  const [flippedPositions, setFlippedPositions] = useState<number[]>([]);

  const [readingId, setReadingId] = useState<number | null>(null);
  const readingIdRef = useRef<number | null>(null);
  const aiRequestStartedRef = useRef(false);
  const spreadsLoadRequestRef = useRef(0);

  const [aiRequested, setAiRequested] = useState(false);
  const [aiLoading, setAiLoading] = useState(false);
  const [aiError, setAiError] = useState<string | null>(null);
  const [interpretation, setInterpretation] = useState<Interpretation | null>(null);

  const selectedSpread = useMemo(
    () => spreads.find((spread) => spread.id === selectedSpreadId) || null,
    [selectedSpreadId, spreads],
  );

  const interpretationThemes = useMemo(() => {
    if (!interpretation?.key_themes) return [];
    return interpretation.key_themes
      .split(',')
      .map((item) => item.trim())
      .filter(Boolean);
  }, [interpretation?.key_themes]);

  const allCardsFlipped = drawnCards.length > 0 && flippedPositions.length === drawnCards.length;

  useEffect(() => {
    showErrorRef.current = showError;
  }, [showError]);

  useEffect(() => {
    let cancelled = false;
    const requestId = spreadsLoadRequestRef.current + 1;
    spreadsLoadRequestRef.current = requestId;

    const loadSpreads = async () => {
      setIsLoadingSpreads(true);
      try {
        const spreadList = await tarotService.getAllSpreads();
        if (cancelled || requestId !== spreadsLoadRequestRef.current) {
          return;
        }
        setSpreads(spreadList);
      } catch (error) {
        if (cancelled || requestId !== spreadsLoadRequestRef.current) {
          return;
        }
        const message = error instanceof Error ? error.message : '加载牌阵失败';
        showErrorRef.current(message);
      } finally {
        if (!cancelled && requestId === spreadsLoadRequestRef.current) {
          setIsLoadingSpreads(false);
        }
      }
    };

    void loadSpreads();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (activeStep === 0) {
      setPhase(GamePhase.SELECTING_SPREAD);
      return;
    }
    if (activeStep === 1) {
      setPhase(GamePhase.ASKING_QUESTION);
      return;
    }
    if (activeStep === 2) {
      setPhase(drawnCards.length > 0 ? GamePhase.CARDS_DRAWN : GamePhase.DRAWING_CARDS);
    }
  }, [activeStep, drawnCards.length, setPhase]);

  const mapCardsForDisplay = (cardDraws: CardDrawWithMeaning[]): DrawnCard[] => {
    return cardDraws
      .slice()
      .sort((a, b) => a.position - b.position)
      .reduce<DrawnCard[]>((acc, draw, index) => {
        if (!draw.tarot_card) return acc;
        acc.push({
          card: {
            ...draw.tarot_card,
            image_url: localDrawingUtils.generateCardImageUrl(draw.tarot_card),
          },
          isReversed: draw.is_reversed,
          position: index,
        });
        return acc;
      }, []);
  };

  const getAIInterpretation = useCallback(
    async (targetReadingId: number) => {
      setAiLoading(true);
      setAiError(null);
      setPhase(GamePhase.INTERPRETING);

      try {
        const result = await tarotService.createInterpretation(targetReadingId, {
          forceAI: true,
          timeoutMs: 65000,
        });
        setInterpretation(result);
        setPhase(GamePhase.COMPLETED);
      } catch (error) {
        const message = error instanceof Error ? error.message : 'AI 解读失败';

        // 兼容“已存在解读”的场景，回查详情直接展示结果
        if (message.includes('已有解读') || message.toLowerCase().includes('already')) {
          try {
            const detail = await tarotService.getReadingDetailInfo(targetReadingId);
            if (detail.interpretation) {
              setInterpretation(detail.interpretation);
              setAiError(null);
              setPhase(GamePhase.COMPLETED);
              return;
            }
          } catch {
            // 回查失败时继续走原错误提示
          }
        }

        setAiError(message);
        showError(message);
        setPhase(GamePhase.CARDS_DRAWN);
      } finally {
        setAiLoading(false);
      }
    },
    [setPhase, showError],
  );

  useEffect(() => {
    if (!allCardsFlipped || aiRequested || aiRequestStartedRef.current || !readingIdRef.current) return;
    aiRequestStartedRef.current = true;
    setAiRequested(true);
    void getAIInterpretation(readingIdRef.current);
  }, [aiRequested, allCardsFlipped, getAIInterpretation]);

  const resetDrawStage = useCallback(() => {
    setDrawError(null);
    setDrawnCards([]);
    setFlippedPositions([]);
    setAiRequested(false);
    setAiError(null);
    setAiLoading(false);
    setInterpretation(null);
    setReadingId(null);
    readingIdRef.current = null;
    aiRequestStartedRef.current = false;
  }, []);

  const handleStartDraw = async () => {
    if (!selectedSpread) return;

    const trimmedQuestion = question.trim();
    if (!trimmedQuestion) {
      setActiveStep(1);
      return;
    }

    resetDrawStage();
    setIsDrawing(true);

    try {
      const reading = await tarotService.createReading({
        spreadId: selectedSpread.id,
        question: trimmedQuestion,
        questionType,
      });

      setReadingId(reading.id);
      readingIdRef.current = reading.id;

      await tarotService.drawCardsForReading(reading.id);
      const cardsWithMeaning = await tarotService.getReadingCards(reading.id);
      const mappedCards = mapCardsForDisplay(cardsWithMeaning);

      if (!mappedCards.length) {
        throw new Error('未获取到牌面数据，请重试');
      }

      setDrawnCards(mappedCards);
      setPhase(GamePhase.CARDS_DRAWN);
    } catch (error) {
      const message = error instanceof Error ? error.message : '抽牌失败，请重试';
      setDrawError(message);
      showError(message);
    } finally {
      setIsDrawing(false);
    }
  };

  const handleCardFlip = useCallback((_: any, position: number) => {
    setFlippedPositions((prev) => {
      if (prev.includes(position)) return prev;
      return [...prev, position];
    });
  }, []);

  const handleRetryInterpretation = () => {
    if (!readingIdRef.current) return;
    void getAIInterpretation(readingIdRef.current);
  };

  const handleRestartAll = () => {
    setActiveStep(0);
    setSelectedSpreadId(null);
    setQuestion('');
    setQuestionType('general');
    resetDrawStage();
    setPhase(GamePhase.SELECTING_SPREAD);
  };

  const handleContinueToQuestion = () => {
    if (!selectedSpread) return;
    setActiveStep(1);
  };

  const handleContinueToDraw = () => {
    if (!question.trim()) return;
    setActiveStep(2);
  };

  const isQuestionValid = question.trim().length >= 5;

  const drawTipText = useMemo(() => {
    if (!drawnCards.length) return '请先开始抽牌';
    if (!allCardsFlipped) return `点击牌面翻开（${flippedPositions.length}/${drawnCards.length}）`;
    if (aiLoading) return '✦ 星辰正在为您揭示命运...';
    if (interpretation) return '✦ 命运已揭示';
    if (aiError) return '星辰暂时沉默，请再次感应';
    return '正在等待命运的回响...';
  }, [aiError, aiLoading, allCardsFlipped, drawnCards.length, flippedPositions.length, interpretation]);

  return (
    <Box className="hd-noise-overlay" sx={{
      width: '100vw',
      minHeight: '100vh',
      position: 'relative',
      left: '50%',
      right: '50%',
      marginLeft: '-50vw',
      marginRight: '-50vw',
      pt: { xs: 2, md: 4 },
      pb: { xs: 8, md: 4 }
    }}>
      <CosmicBackground showRings={false} performanceMode={visual.backgroundMode} />
      
      {/* 魔法阵装饰背景 */}
      <Container maxWidth="xl" sx={{ position: 'relative', zIndex: 1, '& .MuiPaper-root': { backdropFilter: 'blur(16px)', background: 'rgba(16, 8, 32, 0.7)' } }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Button
          variant="outlined"
          startIcon={<ArrowBack />}
          onClick={() => navigate(ROUTES.NEW_READING)}
        >
          返回
        </Button>
        <Button
          variant="text"
          startIcon={<Refresh />}
          onClick={handleRestartAll}
        >
          重新开始
        </Button>
      </Box>

      <Paper sx={{ p: 3, mb: 4 }}>
        <Stepper activeStep={activeStep} alternativeLabel>
          {steps.map((label) => (
            <Step key={label}>
              <StepLabel>{label}</StepLabel>
            </Step>
          ))}
        </Stepper>
      </Paper>

      {activeStep === 0 && (
        <Paper sx={{ p: { xs: 3, md: 4 } }}>
          <Typography variant="h4" sx={{ mb: 4, fontFamily: 'Cinzel, serif', fontWeight: 700 }}>
            选择牌阵
          </Typography>

          {isLoadingSpreads ? (
            <Loading message="正在加载牌阵..." />
          ) : (
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', md: 'repeat(2, minmax(0, 1fr))' },
                gap: 3,
              }}
            >
              {spreads.map((spread) => {
                const selected = spread.id === selectedSpreadId;
                return (
                  <Card
                    key={spread.id}
                    sx={{
                      border: selected ? '2px solid' : '1px solid',
                      borderColor: selected ? 'primary.main' : 'divider',
                    }}
                  >
                    <CardActionArea onClick={() => setSelectedSpreadId(spread.id)}>
                      <CardContent sx={{ p: 3 }}>
                        <Typography variant="h5" sx={{ mb: 1.5 }}>
                          {getSpreadDisplayName(spread)}
                        </Typography>
                        <Typography variant="body1" sx={{ color: 'text.secondary', mb: 2 }}>
                          {getSpreadDisplayDescription(spread)}
                        </Typography>
                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5 }}>
                          <Chip label={`${spread.card_count} 张牌`} />
                          <Chip variant="outlined" label={`难度 ${spread.difficulty_level}`} />
                          {spread.is_beginner_friendly && (
                            <Chip color="success" variant="outlined" label="新手友好" />
                          )}
                        </Box>
                      </CardContent>
                    </CardActionArea>
                  </Card>
                );
              })}
            </Box>
          )}

          <Box sx={{ mt: 3, display: 'flex', justifyContent: 'flex-end' }}>
            <Button
              variant="contained"
              onClick={handleContinueToQuestion}
              disabled={!selectedSpread}
              startIcon={<AutoAwesome />}
            >
              下一步
            </Button>
          </Box>
        </Paper>
      )}

      {activeStep === 1 && (
        <Paper sx={{ p: { xs: 3, md: 4 } }}>
          <Typography variant="h4" sx={{ mb: 2, fontFamily: 'Cinzel, serif', fontWeight: 700 }}>
            输入问题
          </Typography>
          <Typography variant="body1" sx={{ color: 'text.secondary', mb: 3 }}>
            当前牌阵：{getSpreadDisplayName(selectedSpread)}
          </Typography>

          <ToggleButtonGroup
            color="primary"
            value={questionType}
            exclusive
            onChange={(_, value: QuestionType | null) => {
              if (value) setQuestionType(value);
            }}
            sx={{ mb: 3, flexWrap: 'wrap', gap: 1.5 }}
          >
            {questionTypeOptions.map((option) => (
              <ToggleButton key={option.value} value={option.value}>
                {option.label}
              </ToggleButton>
            ))}
          </ToggleButtonGroup>

          <TextField
            fullWidth
            multiline
            minRows={5}
            label="你的问题"
            value={question}
            onChange={(event) => setQuestion(event.target.value)}
            helperText={`${question.trim().length} 字（至少 5 字）`}
          />

          <Box sx={{ mt: 3, display: 'flex', justifyContent: 'space-between' }}>
            <Button variant="outlined" onClick={() => setActiveStep(0)}>
              上一步
            </Button>
            <Button
              variant="contained"
              onClick={handleContinueToDraw}
              disabled={!isQuestionValid}
              startIcon={<AutoAwesome />}
            >
              进入抽牌
            </Button>
          </Box>
        </Paper>
      )}

      {activeStep === 2 && (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
          <Paper sx={{ p: 2.5 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 1 }}>
              <Typography variant="h6" sx={{ fontFamily: 'Cinzel, serif', fontWeight: 700 }}>
                抽牌与解读
              </Typography>
              <Box sx={{ display: 'flex', gap: 1 }}>
                {drawnCards.length > 0 && (
                  <Button variant="outlined" startIcon={<Autorenew />} onClick={resetDrawStage}>
                    重新抽牌
                  </Button>
                )}
                {readingId && (
                  <Button
                    variant="outlined"
                    startIcon={<Visibility />}
                    onClick={() => navigate(ROUTES.READING_DETAIL.replace(':id', String(readingId)))}
                  >
                    查看记录
                  </Button>
                )}
              </Box>
            </Box>
          </Paper>

          {!drawnCards.length ? (
            <Paper
              sx={{
                minHeight: { xs: 460, md: 620 },
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                justifyContent: 'center',
                gap: 3,
                px: 2,
              }}
            >
              <Box
                sx={{
                  position: 'relative',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  mb: 4,
                  mt: 2,
                  width: { xs: 300, sm: 420, md: 520 },
                  height: { xs: 300, sm: 420, md: 520 },
                  '&::before': {
                    content: '""',
                    position: 'absolute',
                    inset: { xs: 12, sm: 18, md: 24 },
                    borderRadius: '50%',
                    border: '1px solid rgba(212, 175, 55, 0.22)',
                    boxShadow: '0 0 40px rgba(212, 175, 55, 0.16), inset 0 0 24px rgba(212, 175, 55, 0.08)',
                    background: `
                      radial-gradient(circle, rgba(212, 175, 55, 0.12) 0%, rgba(212, 175, 55, 0.03) 45%, transparent 72%),
                      repeating-conic-gradient(
                        from 0deg,
                        rgba(212, 175, 55, 0.16) 0deg 10deg,
                        transparent 10deg 28deg
                      )
                    `,
                    animation: visual.enableAmbientMotion ? 'draw-spin 24s linear infinite' : 'none',
                  },
                  '&::after': {
                    content: '""',
                    position: 'absolute',
                    inset: { xs: 42, sm: 58, md: 76 },
                    borderRadius: '50%',
                    border: '1px dashed rgba(120, 220, 255, 0.28)',
                    opacity: visual.quality === 'minimal' ? 0.34 : 0.7,
                    animation: visual.enableAmbientMotion ? 'draw-spin 18s linear infinite reverse' : 'none',
                  },
                  '@keyframes draw-spin': {
                    '0%': { transform: 'rotate(0deg)' },
                    '100%': { transform: 'rotate(360deg)' },
                  },
                }}
              >
                <Box
                  sx={{
                    width: { xs: 220, sm: 280, md: 320 },
                    aspectRatio: '2 / 3',
                    borderRadius: 4,
                    border: '2.5px solid rgba(212, 175, 55, 0.85)',
                    background: 'linear-gradient(155deg, #0c0920 0%, #1a1040 35%, #0f1c38 65%, #080614 100%)',
                    boxShadow: '0 24px 60px rgba(0, 0, 0, 0.8), 0 0 30px rgba(0, 240, 255, 0.2)',
                    position: 'relative',
                    zIndex: 2,
                    transition: visual.enableHoverMotion ? 'all 0.3s ease' : 'box-shadow 0.2s ease',
                    overflow: 'hidden',
                    '&:hover': {
                      boxShadow: visual.enableHoverMotion
                        ? '0 30px 70px rgba(0, 0, 0, 0.82), 0 0 50px rgba(0, 240, 255, 0.32)'
                        : '0 24px 60px rgba(0, 0, 0, 0.8), 0 0 30px rgba(0, 240, 255, 0.2)',
                      transform: visual.enableHoverMotion ? 'translateY(-5px)' : 'none',
                    },
                  }}
                >
                  <svg viewBox="0 0 400 660" style={{ position: 'absolute', inset: 0, width: '100%', height: '100%' }} xmlns="http://www.w3.org/2000/svg">
                    <defs>
                      <radialGradient id="pg" cx="50%" cy="50%" r="42%"><stop offset="0%" stopColor="#f5d97b" stopOpacity="0.4" /><stop offset="60%" stopColor="#c9a84c" stopOpacity="0.1" /><stop offset="100%" stopColor="#f5d97b" stopOpacity="0" /></radialGradient>
                      <radialGradient id="pgc" cx="50%" cy="50%" r="45%"><stop offset="0%" stopColor="#56bdf8" stopOpacity="0.1" /><stop offset="100%" stopColor="#56bdf8" stopOpacity="0" /></radialGradient>
                      <filter id="pn" x="-40%" y="-40%" width="180%" height="180%"><feGaussianBlur stdDeviation="2.5" result="b" /><feMerge><feMergeNode in="b" /><feMergeNode in="SourceGraphic" /></feMerge></filter>
                    </defs>
                    {[[32,45,1.4,'#f5d97b',0.5],[370,28,1.1,'#8cd3ff',0.4],[55,120,1,'#fff',0.35],[345,95,1.3,'#c99bff',0.4],[28,250,1.2,'#f5d97b',0.3],[372,200,0.9,'#8cd3ff',0.35],[40,400,1.1,'#fff',0.3],[360,440,1.3,'#f5d97b',0.4],[50,550,1,'#8cd3ff',0.35],[350,580,1.2,'#c99bff',0.3],[200,25,1.1,'#fff',0.3],[200,635,1,'#fff',0.3]].map(([cx,cy,r,f,o],i)=>(<circle key={i} cx={cx as number} cy={cy as number} r={r as number} fill={f as string} opacity={o as number} />))}
                    <rect x="12" y="12" width="376" height="636" rx="18" fill="none" stroke="#d4af37" strokeWidth="2.5" opacity="0.8" />
                    <rect x="22" y="22" width="356" height="616" rx="14" fill="none" stroke="#d4af37" strokeWidth="1" opacity="0.35" />
                    <rect x="30" y="30" width="340" height="600" rx="11" fill="none" stroke="#56bdf8" strokeWidth="0.6" opacity="0.2" strokeDasharray="5 6" />
                    <g opacity="0.65" filter="url(#pn)">
                      <path d="M32 40 L32 72 L64 40 Z" fill="none" stroke="#d4af37" strokeWidth="1.5" /><circle cx="38" cy="46" r="2.5" fill="#d4af37" opacity="0.7" />
                      <path d="M368 40 L368 72 L336 40 Z" fill="none" stroke="#d4af37" strokeWidth="1.5" /><circle cx="362" cy="46" r="2.5" fill="#d4af37" opacity="0.7" />
                      <path d="M32 620 L32 588 L64 620 Z" fill="none" stroke="#d4af37" strokeWidth="1.5" /><circle cx="38" cy="614" r="2.5" fill="#d4af37" opacity="0.7" />
                      <path d="M368 620 L368 588 L336 620 Z" fill="none" stroke="#d4af37" strokeWidth="1.5" /><circle cx="362" cy="614" r="2.5" fill="#d4af37" opacity="0.7" />
                    </g>
                    <circle cx="200" cy="330" r="200" fill="url(#pgc)" /><circle cx="200" cy="330" r="160" fill="url(#pg)" />
                    <circle cx="200" cy="330" r="140" fill="none" stroke="#d4af37" strokeWidth="2.5" opacity="0.7" filter="url(#pn)" />
                    <circle cx="200" cy="330" r="128" fill="none" stroke="#d4af37" strokeWidth="0.8" opacity="0.3" strokeDasharray="10 5" />
                    <circle cx="200" cy="330" r="100" fill="none" stroke="#56bdf8" strokeWidth="1.5" opacity="0.4" filter="url(#pn)" />
                    <circle cx="200" cy="330" r="70" fill="none" stroke="#d4af37" strokeWidth="1.2" opacity="0.5" />
                    <polygon points="200,185 222,300 340,330 222,360 200,475 178,360 60,330 178,300" fill="none" stroke="#f5d97b" strokeWidth="3" opacity="0.75" filter="url(#pn)" />
                    <g transform="rotate(22.5, 200, 330)"><polygon points="200,225 212,305 292,330 212,355 200,435 188,355 108,330 188,305" fill="none" stroke="#56bdf8" strokeWidth="1.5" opacity="0.35" /></g>
                    {[0,45,90,135,180,225,270,315].map((a,i)=>{const r=(a*Math.PI)/180;return <circle key={`p${i}`} cx={200+140*Math.cos(r)} cy={330+140*Math.sin(r)} r={i%2===0?4:3} fill={i%2===0?'#f5d97b':'#56bdf8'} opacity={i%2===0?0.85:0.6} filter="url(#pn)" />;})}
                    <circle cx="200" cy="330" r="20" fill="#f5d97b" opacity="0.8" filter="url(#pn)" /><circle cx="200" cy="330" r="10" fill="#0d0820" /><circle cx="200" cy="330" r="4.5" fill="#56bdf8" opacity="0.9" />
                    <path d="M172 330 Q200 310 228 330 Q200 350 172 330" fill="none" stroke="#f5d97b" strokeWidth="1.8" opacity="0.6" />
                    <text x="200" y="88" textAnchor="middle" fontSize="26" fontFamily="Georgia, serif" fill="#f5d97b" letterSpacing="8" opacity="0.85" filter="url(#pn)">TAROT</text>
                    <text x="200" y="115" textAnchor="middle" fontSize="11" fill="#d4af37" opacity="0.5" letterSpacing="3">✦   ✦   ✦</text>
                    <text x="200" y="575" textAnchor="middle" fontSize="15" fontFamily="Georgia, serif" fill="#8cd3ff" letterSpacing="7" opacity="0.55">ARCANA</text>
                    <text x="200" y="553" textAnchor="middle" fontSize="11" fill="#d4af37" opacity="0.5" letterSpacing="3">✦   ✦   ✦</text>
                  </svg>
                </Box>
              </Box>

              <Typography variant="body1" sx={{ color: 'text.secondary', textAlign: 'center', zIndex: 2 }}>
                牌堆已就绪，开始抽牌后请逐张点击牌面翻开
              </Typography>

              <Button
                variant="contained"
                size="large"
                startIcon={<AutoAwesome />}
                onClick={handleStartDraw}
                disabled={isDrawing}
              >
                {isDrawing ? '抽牌中...' : '开始抽牌'}
              </Button>

              {drawError && (
                <Alert severity="error" sx={{ width: '100%', maxWidth: 760 }}>
                  {drawError}
                </Alert>
              )}
            </Paper>
          ) : (
            <>
              <Paper sx={{ p: { xs: 1, md: 2 } }}>
                <Typography
                  variant="h6"
                  sx={{
                    textAlign: 'center',
                    mb: 2,
                    color: allCardsFlipped ? 'primary.main' : 'text.secondary',
                  }}
                >
                  {drawTipText}
                </Typography>

                <CardSpread
                  spread={selectedSpread as SpreadType}
                  drawnCards={drawnCards}
                  allowManualFlip={true}
                  flippedPositions={flippedPositions}
                  onCardFlip={handleCardFlip}
                  cardSizeOverride={drawnCards.length <= 5 ? 'large' : 'medium'}
                  showSpreadMeta={false}
                  showSpreadDescription={false}
                  visualQuality={visual.quality}
                  motionPreset={visual.cardMotionPreset}
                />
              </Paper>

              <Paper sx={{ p: 3 }}>
                <Typography variant="h5" sx={{ mb: 2, fontFamily: 'Cinzel, serif', color: 'primary.main' }}>
                  ✦ 命运的启示
                </Typography>

                {!allCardsFlipped && (
                  <Alert severity="info" sx={{ fontSize: '1rem' }}>请先翻开全部牌面，命运将自动为您揭晓。</Alert>
                )}

                {aiLoading && (
                  <Box sx={{ py: 2 }}>
                    <Loading variant="cosmic" message="古老的智慧正在聆听牌面的低语..." />
                  </Box>
                )}

                {aiError && (
                  <Alert
                    severity="error"
                    sx={{ mb: 2 }}
                    action={
                      <Button color="inherit" size="small" onClick={handleRetryInterpretation} disabled={aiLoading}>
                        重试
                      </Button>
                    }
                  >
                    {aiError}
                  </Alert>
                )}

                {interpretation && (
                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                    <Box>
                      <Typography variant="h6" sx={{ fontWeight: 700, mb: 1, color: 'primary.main' }}>
                        总览
                      </Typography>
                      <Typography variant="body2" sx={{ lineHeight: 1.8, whiteSpace: 'pre-wrap' }}>
                        {interpretation.overall_interpretation}
                      </Typography>
                    </Box>

                    {interpretationThemes.length > 0 && (
                      <Box>
                        <Typography variant="h6" sx={{ fontWeight: 700, mb: 1, color: 'primary.main' }}>
                          关键主题
                        </Typography>
                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                          {interpretationThemes.map((theme) => (
                            <Chip key={theme} label={theme} size="small" variant="outlined" />
                          ))}
                        </Box>
                      </Box>
                    )}

                    {interpretation.card_analysis && (
                      <Box>
                        <Typography variant="h6" sx={{ fontWeight: 700, mb: 1, color: 'primary.main' }}>
                          逐牌分析
                        </Typography>
                        <Typography variant="body2" sx={{ lineHeight: 1.8, whiteSpace: 'pre-wrap' }}>
                          {interpretation.card_analysis}
                        </Typography>
                      </Box>
                    )}

                    {interpretation.relationship_analysis && (
                      <Box>
                        <Typography variant="h6" sx={{ fontWeight: 700, mb: 1, color: 'primary.main' }}>
                          牌面关系
                        </Typography>
                        <Typography variant="body2" sx={{ lineHeight: 1.8, whiteSpace: 'pre-wrap' }}>
                          {interpretation.relationship_analysis}
                        </Typography>
                      </Box>
                    )}

                    {interpretation.advice && (
                      <Box>
                        <Typography variant="h6" sx={{ fontWeight: 700, mb: 1, color: 'primary.main' }}>
                          建议
                        </Typography>
                        <Typography variant="body2" sx={{ lineHeight: 1.8, whiteSpace: 'pre-wrap' }}>
                          {interpretation.advice}
                        </Typography>
                      </Box>
                    )}
                  </Box>
                )}
              </Paper>
            </>
          )}
        </Box>
      )}
      </Container>
    </Box>
  );
};

export default DrawCards;
