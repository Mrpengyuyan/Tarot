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
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import CardSpread from '../../components/Tarot/CardSpread';
import Loading from '../../components/UI/Loading';
import { useNotification } from '../../components/UI/Notification';
import { ROUTES } from '../../routes/routeConfig';
import CosmicBackground from '../../components/Effects/CosmicBackground';
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
    const loadSpreads = async () => {
      setIsLoadingSpreads(true);
      try {
        const spreadList = await tarotService.getAllSpreads();
        setSpreads(spreadList);
      } catch (error) {
        const message = error instanceof Error ? error.message : '加载牌阵失败';
        showError(message);
      } finally {
        setIsLoadingSpreads(false);
      }
    };

    loadSpreads();
  }, [showError]);

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
    if (aiLoading) return 'AI 解读中...';
    if (interpretation) return '解读已完成';
    if (aiError) return '解读失败，请重试';
    return '正在等待解读结果...';
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
      <CosmicBackground showRings={false} />
      
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
        <Paper sx={{ p: 3 }}>
          <Typography variant="h5" sx={{ mb: 3, fontFamily: 'Cinzel, serif', fontWeight: 700 }}>
            选择牌阵
          </Typography>

          {isLoadingSpreads ? (
            <Loading message="正在加载牌阵..." />
          ) : (
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', md: 'repeat(2, minmax(0, 1fr))' },
                gap: 2,
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
                      <CardContent>
                        <Typography variant="h6" sx={{ mb: 1 }}>
                          {spread.name}
                        </Typography>
                        <Typography variant="body2" sx={{ color: 'text.secondary', mb: 1.5 }}>
                          {spread.description}
                        </Typography>
                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                          <Chip size="small" label={`${spread.card_count} 张牌`} />
                          <Chip size="small" variant="outlined" label={`难度 ${spread.difficulty_level}`} />
                          {spread.is_beginner_friendly && (
                            <Chip size="small" color="success" variant="outlined" label="新手友好" />
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
        <Paper sx={{ p: 3 }}>
          <Typography variant="h5" sx={{ mb: 1, fontFamily: 'Cinzel, serif', fontWeight: 700 }}>
            输入问题
          </Typography>
          <Typography variant="body2" sx={{ color: 'text.secondary', mb: 3 }}>
            当前牌阵：{selectedSpread?.name}
          </Typography>

          <ToggleButtonGroup
            color="primary"
            value={questionType}
            exclusive
            onChange={(_, value: QuestionType | null) => {
              if (value) setQuestionType(value);
            }}
            sx={{ mb: 2, flexWrap: 'wrap', gap: 1 }}
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
            minRows={4}
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
                  mt: 2
                }}
              >
                <Box
                  component="img"
                  src="/images/theme/magic_circle_accent.png"
                  sx={{
                    position: 'absolute',
                    width: { xs: 350, sm: 450, md: 550 },
                    height: { xs: 350, sm: 450, md: 550 },
                    objectFit: 'contain',
                    opacity: 0.6,
                    animation: 'draw-spin 20s linear infinite',
                    pointerEvents: 'none',
                    mixBlendMode: 'screen',
                    '@keyframes draw-spin': {
                      '0%': { transform: 'rotate(0deg)' },
                      '100%': { transform: 'rotate(360deg)' },
                    },
                  }}
                />
                <Box
                  sx={{
                    width: { xs: 220, sm: 260, md: 300 },
                    aspectRatio: '2 / 3',
                    borderRadius: 3,
                    border: '2px solid rgba(212, 175, 55, 0.4)',
                    background: 'url(/images/theme/card_back_nebula.png)',
                    backgroundSize: 'cover',
                    backgroundPosition: 'center',
                    boxShadow: '0 24px 60px rgba(0, 0, 0, 0.8), 0 0 30px rgba(0, 240, 255, 0.2)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    position: 'relative',
                    zIndex: 2,
                    transition: 'all 0.3s ease',
                    '&:hover': {
                      boxShadow: '0 30px 70px rgba(0, 0, 0, 0.8), 0 0 50px rgba(0, 240, 255, 0.4)',
                      transform: 'translateY(-5px)',
                    }
                  }}
                />
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
                />
              </Paper>

              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" sx={{ mb: 2 }}>
                  AI 解读结果
                </Typography>

                {!allCardsFlipped && (
                  <Alert severity="info">请先翻开全部牌面，系统将自动开始 AI 解读。</Alert>
                )}

                {aiLoading && (
                  <Box sx={{ py: 2 }}>
                    <Loading variant="simple" message="AI 正在解读，请稍候..." />
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
                      <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
                        总览
                      </Typography>
                      <Typography variant="body2" sx={{ lineHeight: 1.8, whiteSpace: 'pre-wrap' }}>
                        {interpretation.overall_interpretation}
                      </Typography>
                    </Box>

                    {interpretationThemes.length > 0 && (
                      <Box>
                        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
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
                        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
                          逐牌分析
                        </Typography>
                        <Typography variant="body2" sx={{ lineHeight: 1.8, whiteSpace: 'pre-wrap' }}>
                          {interpretation.card_analysis}
                        </Typography>
                      </Box>
                    )}

                    {interpretation.relationship_analysis && (
                      <Box>
                        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
                          牌面关系
                        </Typography>
                        <Typography variant="body2" sx={{ lineHeight: 1.8, whiteSpace: 'pre-wrap' }}>
                          {interpretation.relationship_analysis}
                        </Typography>
                      </Box>
                    )}

                    {interpretation.advice && (
                      <Box>
                        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
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
