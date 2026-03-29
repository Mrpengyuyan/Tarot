import React from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Button,
  Stepper,
  Step,
  StepLabel,
  Container,
} from '@mui/material';
import {
  AutoAwesome,
  Psychology,
  TouchApp,
  Visibility,
} from 'icons';
import { useNavigate } from 'react-router-dom';
import CosmicBackground from '../../components/Effects/CosmicBackground';
import { useVisualSettings } from '../../hooks/useVisualSettings';
import { useGameStore, GamePhase, getPhaseTitle } from '../../stores/gameStore';
import { ROUTES } from '../../routes/routeConfig';

// 占卜步骤配置
const steps = [
  {
    label: '选择牌阵',
    icon: <Psychology />,
    description: '根据您的问题选择合适的塔罗牌阵',
  },
  {
    label: '提出问题',
    icon: <AutoAwesome />,
    description: '清晰地表达您想要了解的问题',
  },
  {
    label: '抽取塔罗牌',
    icon: <TouchApp />,
    description: '让宇宙的力量为您选择塔罗牌',
  },
  {
    label: '查看结果',
    icon: <Visibility />,
    description: '获得专业的塔罗解读和建议',
  },
];

const NewReading: React.FC = () => {
  const navigate = useNavigate();
  const { currentPhase, startNewReading } = useGameStore();
  const visual = useVisualSettings();

  const getCurrentStepIndex = (): number => {
    switch (currentPhase) {
      case GamePhase.SELECTING_SPREAD:
        return 0;
      case GamePhase.ASKING_QUESTION:
        return 1;
      case GamePhase.DRAWING_CARDS:
      case GamePhase.CARDS_DRAWN:
        return 2;
      case GamePhase.INTERPRETING:
      case GamePhase.COMPLETED:
        return 3;
      default:
        return -1;
    }
  };

  const handleStartReading = () => {
    startNewReading();
    navigate(ROUTES.DRAW_CARDS);
  };

  return (
    <Box
      sx={{
        width: '100vw',
        minHeight: '100vh',
        position: 'relative',
        left: '50%',
        right: '50%',
        marginLeft: '-50vw',
        marginRight: '-50vw',
        overflow: 'hidden',
        /* 静谧深空背景，不使用会遮挡文字的旋转 SVG */
        background: `
          radial-gradient(ellipse 80% 60% at 50% 20%, rgba(56, 189, 248, 0.06) 0%, transparent 60%),
          radial-gradient(ellipse 70% 50% at 80% 80%, rgba(212, 175, 55, 0.05) 0%, transparent 55%),
          radial-gradient(ellipse 60% 50% at 20% 70%, rgba(120, 80, 200, 0.06) 0%, transparent 55%),
          linear-gradient(180deg, #0A0512 0%, #110820 40%, #1A0B2E 100%)
        `,
      }}
    >
      <CosmicBackground showRings={false} performanceMode={visual.backgroundMode} />
      {/* 装饰层：微弱星尘粒子 + 浮动光球 */}
      <Box
        sx={{
          position: 'absolute',
          inset: 0,
          pointerEvents: 'none',
          background: `
            radial-gradient(1.5px 1.5px at 15% 25%, rgba(212, 175, 55, 0.4), transparent),
            radial-gradient(1px 1px at 85% 15%, rgba(56, 189, 248, 0.35), transparent),
            radial-gradient(1.5px 1.5px at 60% 70%, rgba(212, 175, 55, 0.3), transparent),
            radial-gradient(1px 1px at 30% 85%, rgba(200, 200, 255, 0.35), transparent),
            radial-gradient(1px 1px at 70% 40%, rgba(56, 189, 248, 0.25), transparent),
            radial-gradient(1.5px 1.5px at 45% 55%, rgba(212, 175, 55, 0.25), transparent),
            radial-gradient(1px 1px at 90% 70%, rgba(200, 200, 255, 0.3), transparent),
            radial-gradient(1px 1px at 10% 60%, rgba(56, 189, 248, 0.25), transparent),
            radial-gradient(1px 1px at 50% 10%, rgba(212, 175, 55, 0.3), transparent),
            radial-gradient(1.2px 1.2px at 25% 45%, rgba(200, 160, 255, 0.3), transparent),
            radial-gradient(1px 1px at 75% 90%, rgba(56, 189, 248, 0.2), transparent),
            radial-gradient(1.5px 1.5px at 95% 50%, rgba(212, 175, 55, 0.2), transparent)
          `,
          backgroundSize: '280px 280px',
          opacity: visual.quality === 'minimal' ? 0.3 : visual.quality === 'lite' ? 0.56 : 0.8,
          animation: visual.enableAmbientMotion ? 'float 18s ease-in-out infinite' : 'none',
        }}
      />

      {/* 半透明旋转魔法阵（背景装饰，不遮挡文字） */}
      <Box
        sx={{
          position: 'absolute',
          top: '50%',
          left: '50%',
          width: visual.quality === 'lite' ? { xs: '520px', md: '760px' } : { xs: '600px', md: '900px' },
          height: visual.quality === 'lite' ? { xs: '520px', md: '760px' } : { xs: '600px', md: '900px' },
          transform: 'translate(-50%, -50%)',
          pointerEvents: 'none',
          opacity: visual.quality === 'minimal' ? 0.05 : visual.quality === 'lite' ? 0.1 : 0.16,
          animation: visual.enableAmbientMotion ? 'rotate 60s linear infinite' : 'none',
          '@keyframes rotate': {
            '0%': { transform: 'translate(-50%, -50%) rotate(0deg)' },
            '100%': { transform: 'translate(-50%, -50%) rotate(360deg)' },
          },
        }}
      >
        <svg viewBox="0 0 600 600" xmlns="http://www.w3.org/2000/svg">
          <circle cx="300" cy="300" r="290" fill="none" stroke="#d4af37" strokeWidth="2" strokeDasharray="8 14" />
          <circle cx="300" cy="300" r="260" fill="none" stroke="#d4af37" strokeWidth="3" />
          <circle cx="300" cy="300" r="220" fill="none" stroke="#56bdf8" strokeWidth="1.5" />
          <polygon points="300,30 530,450 70,450" fill="none" stroke="#d4af37" strokeWidth="2" />
          <polygon points="300,570 530,150 70,150" fill="none" stroke="#d4af37" strokeWidth="2" />
          <polygon points="300,80 400,250 500,300 400,350 300,520 200,350 100,300 200,250" fill="none" stroke="#56bdf8" strokeWidth="1.5" />
          <circle cx="300" cy="300" r="160" fill="none" stroke="#d4af37" strokeWidth="1" strokeDasharray="20 8" />
          {(visual.quality === 'lite' ? [0, 90, 180, 270] : [0, 30, 60, 90, 120, 150, 180, 210, 240, 270, 300, 330]).map((a) => {
            const r1 = (a * Math.PI) / 180;
            return <circle key={a} cx={300 + 260 * Math.cos(r1)} cy={300 + 260 * Math.sin(r1)} r="5" fill="#d4af37" />;
          })}
        </svg>
      </Box>

      {/* 浮动环境光球 */}
      <Box sx={{
        position: 'absolute', top: '15%', left: '8%',
        width: visual.quality === 'minimal' ? 180 : 200, height: visual.quality === 'minimal' ? 180 : 200,
        background: visual.quality === 'minimal'
          ? 'radial-gradient(circle, rgba(212, 175, 55, 0.08) 0%, transparent 70%)'
          : 'radial-gradient(circle, rgba(212, 175, 55, 0.12) 0%, transparent 70%)',
        borderRadius: '50%', pointerEvents: 'none',
        animation: visual.enableAmbientMotion ? 'float 8s ease-in-out infinite' : 'none',
      }} />
      <Box sx={{
        position: 'absolute', bottom: '10%', right: '5%',
        width: visual.quality === 'lite' ? 240 : 300, height: visual.quality === 'lite' ? 240 : 300,
        background: 'radial-gradient(circle, rgba(56, 189, 248, 0.08) 0%, transparent 65%)',
        borderRadius: '50%', pointerEvents: 'none',
        opacity: visual.quality === 'minimal' ? 0 : 1,
        animation: visual.enableAmbientMotion && visual.quality !== 'minimal' ? 'float 12s ease-in-out 2s infinite reverse' : 'none',
      }} />
      <Box sx={{
        position: 'absolute', top: '60%', left: '70%',
        width: visual.quality === 'lite' ? 140 : 180, height: visual.quality === 'lite' ? 140 : 180,
        background: 'radial-gradient(circle, rgba(120, 80, 200, 0.1) 0%, transparent 65%)',
        borderRadius: '50%', pointerEvents: 'none',
        opacity: visual.quality === 'minimal' ? 0 : 1,
        animation: visual.enableAmbientMotion && visual.quality !== 'minimal' ? 'float 10s ease-in-out 1s infinite' : 'none',
      }} />
      <Box sx={{
        position: 'absolute', top: '30%', right: '25%',
        width: 120, height: 120,
        background: 'radial-gradient(circle, rgba(212, 175, 55, 0.08) 0%, transparent 65%)',
        borderRadius: '50%', pointerEvents: 'none',
        opacity: visual.quality === 'full' ? 1 : 0,
        animation: visual.enableAmbientMotion && visual.quality === 'full' ? 'float 7s ease-in-out 3s infinite reverse' : 'none',
      }} />

      <Container
        maxWidth="lg"
        sx={{
          py: { xs: 4, md: 6 },
          position: 'relative',
          zIndex: 1,
          display: 'flex',
          flexDirection: 'column',
          minHeight: '90vh',
        }}
      >
        {/* 页面标题区 */}
        <Box sx={{ textAlign: 'center', mb: { xs: 5, md: 7 }, pt: { xs: 2, md: 4 } }} className={visual.enableAnimations ? 'fade-in' : undefined}>
          <AutoAwesome
            sx={{
              fontSize: '4.5rem',
              color: 'primary.main',
              mb: 2,
              filter: 'drop-shadow(0 0 12px rgba(212, 175, 55, 0.5))',
              animation: visual.enableAmbientMotion ? 'float 3s ease-in-out infinite' : 'none',
            }}
          />
          <Typography
            variant="h2"
            component="h1"
            sx={{
              fontFamily: 'Cinzel, serif',
              fontWeight: 700,
              background: 'linear-gradient(45deg, #D4AF37, #FFD700, #D4AF37)',
              backgroundClip: 'text',
              WebkitBackgroundClip: 'text',
              WebkitTextFillColor: 'transparent',
              mb: 3,
              textShadow: 'none',
            }}
          >
            新的占卜
          </Typography>
          <Typography
            variant="h6"
            sx={{
              color: 'rgba(200, 190, 220, 0.9)',
              fontStyle: 'italic',
              maxWidth: 680,
              mx: 'auto',
              lineHeight: 1.8,
              fontWeight: 400,
              letterSpacing: '0.04em',
            }}
          >
            踏入神秘的塔罗世界，让古老的智慧为您揭示人生的奥秘与指引
          </Typography>
        </Box>

        {/* 当前状态显示 */}
        {currentPhase !== GamePhase.IDLE && (
          <Box
            sx={{
              mb: 5,
              p: 4,
              background: 'rgba(16, 10, 32, 0.6)',
              backdropFilter: 'blur(12px)',
              borderRadius: 4,
              border: '1px solid rgba(212, 175, 55, 0.15)',
            }}
            className={visual.enableAnimations ? 'slide-up' : undefined}
          >
            <Typography
              variant="h4"
              sx={{
                fontFamily: 'Cinzel, serif',
                color: 'primary.main',
                textAlign: 'center',
                mb: 4,
              }}
            >
              当前阶段：{getPhaseTitle(currentPhase)}
            </Typography>

            <Stepper
              activeStep={getCurrentStepIndex()}
              alternativeLabel
              sx={{
                '& .MuiStepLabel-root .Mui-completed': { color: 'primary.main' },
                '& .MuiStepLabel-root .Mui-active': { color: 'primary.main' },
                '& .MuiStepConnector-line': { borderColor: 'rgba(212, 175, 55, 0.3)' },
                '& .Mui-completed .MuiStepConnector-line': { borderColor: 'primary.main' },
              }}
            >
              {steps.map((step, index) => (
                <Step key={step.label}>
                  <StepLabel
                    icon={
                      <Box
                        sx={{
                          width: 48,
                          height: 48,
                          borderRadius: '50%',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          background: index <= getCurrentStepIndex()
                            ? 'linear-gradient(45deg, #D4AF37, #FFD700)'
                            : 'rgba(212, 175, 55, 0.15)',
                          color: index <= getCurrentStepIndex() ? 'black' : 'text.secondary',
                          transition: 'all 0.3s ease',
                        }}
                      >
                        {step.icon}
                      </Box>
                    }
                    sx={{
                      '& .MuiStepLabel-label': {
                        fontFamily: 'Cinzel, serif',
                        fontWeight: 500,
                        fontSize: '1.05rem',
                        color: index <= getCurrentStepIndex() ? 'primary.main' : 'text.secondary',
                      },
                    }}
                  >
                    {step.label}
                  </StepLabel>
                </Step>
              ))}
            </Stepper>
          </Box>
        )}

        {/* 占卜步骤介绍（IDLE状态） */}
        {currentPhase === GamePhase.IDLE && (
          <Box className={visual.enableAnimations ? 'scale-in' : undefined} sx={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
            <Typography
              variant="h4"
              sx={{
                fontFamily: 'Cinzel, serif',
                fontWeight: 600,
                color: 'primary.main',
                textAlign: 'center',
                mb: 5,
              }}
            >
              ✦ 占卜流程 ✦
            </Typography>

            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(4, 1fr)' },
                gap: 3,
                mb: 6,
                flex: 1,
              }}
            >
              {steps.map((step, index) => (
                <Card
                  key={step.label}
                  sx={{
                    background: 'rgba(16, 10, 32, 0.65)',
                    backdropFilter: 'blur(16px)',
                    WebkitBackdropFilter: 'blur(16px)',
                    border: '1px solid rgba(212, 175, 55, 0.2)',
                    borderRadius: 4,
                    transition: visual.enableHoverMotion ? 'all 0.4s cubic-bezier(0.4, 0, 0.2, 1)' : 'border-color 0.2s ease',
                    '&:hover': {
                      transform: visual.enableHoverMotion ? 'translateY(-8px)' : 'none',
                      boxShadow: visual.enableHoverMotion ? '0 16px 48px rgba(212, 175, 55, 0.15), inset 0 0 24px rgba(212, 175, 55, 0.05)' : 'none',
                      borderColor: 'rgba(212, 175, 55, 0.5)',
                    },
                  }}
                  className={visual.enableHoverMotion ? 'card-hover' : undefined}
                  style={{ animationDelay: `${index * 0.1}s` }}
                >
                  <CardContent
                    sx={{
                      p: { xs: 3, md: 4 },
                      textAlign: 'center',
                      display: 'flex',
                      flexDirection: 'column',
                      alignItems: 'center',
                      gap: 2,
                      height: '100%',
                    }}
                  >
                    {/* 步骤编号 */}
                    <Box
                      sx={{
                        width: 52,
                        height: 52,
                        borderRadius: '50%',
                        background: 'linear-gradient(135deg, rgba(212, 175, 55, 0.15), rgba(56, 189, 248, 0.1))',
                        border: '1.5px solid rgba(212, 175, 55, 0.4)',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        mb: 1,
                      }}
                    >
                      <Typography
                        sx={{
                          fontFamily: 'Cinzel, serif',
                          fontWeight: 700,
                          fontSize: '1.3rem',
                          color: 'primary.main',
                        }}
                      >
                        {index + 1}
                      </Typography>
                    </Box>

                    <Typography
                      variant="h6"
                      sx={{
                        fontFamily: 'Cinzel, serif',
                        fontWeight: 600,
                        color: '#E8E8E8',
                      }}
                    >
                      {step.label}
                    </Typography>

                    <Typography
                      variant="body1"
                      sx={{
                        color: 'rgba(180, 170, 200, 0.9)',
                        lineHeight: 1.8,
                      }}
                    >
                      {step.description}
                    </Typography>
                  </CardContent>
                </Card>
              ))}
            </Box>

            {/* 开始按钮 */}
            <Box sx={{ textAlign: 'center', mt: 'auto', pb: 4 }}>
              <Button
                variant="contained"
                size="large"
                startIcon={<AutoAwesome />}
                onClick={handleStartReading}
                sx={{
                  py: 2.5,
                  px: 8,
                  fontSize: '1.4rem',
                  fontFamily: 'Cinzel, serif',
                  fontWeight: 600,
                  background: 'linear-gradient(45deg, #D4AF37, #FFD700)',
                  color: 'black',
                  boxShadow: '0 8px 40px rgba(212, 175, 55, 0.35)',
                  borderRadius: 3,
                  '&:hover': {
                    background: 'linear-gradient(45deg, #B8860B, #D4AF37)',
                    boxShadow: visual.enableHoverMotion ? '0 12px 48px rgba(212, 175, 55, 0.5)' : '0 8px 40px rgba(212, 175, 55, 0.35)',
                    transform: visual.enableHoverMotion ? 'translateY(-4px)' : 'none',
                  },
                }}
                className={visual.enableHoverMotion ? 'mystical-glow' : undefined}
              >
                开始占卜之旅
              </Button>

              <Typography
                variant="body1"
                sx={{
                  color: 'rgba(180, 170, 200, 0.7)',
                  mt: 3,
                  fontStyle: 'italic',
                  letterSpacing: '0.04em',
                }}
              >
                "当你准备好聆听宇宙的声音时，塔罗牌就会为你揭示答案"
              </Typography>
            </Box>
          </Box>
        )}

        {/* 非 IDLE 阶段 */}
        {currentPhase !== GamePhase.IDLE && (
          <Box sx={{ textAlign: 'center', py: 6 }}>
            <Typography
              variant="h3"
              sx={{
                fontFamily: 'Cinzel, serif',
                color: 'primary.main',
                mb: 3,
              }}
            >
              {getPhaseTitle(currentPhase)}
            </Typography>
            <Typography
              variant="body1"
              sx={{
                color: 'rgba(180, 170, 200, 0.85)',
                fontStyle: 'italic',
                mb: 5,
                fontSize: '1.15rem',
              }}
            >
              准备开始您的塔罗占卜之旅...
            </Typography>
            <Button
              variant="contained"
              size="large"
              startIcon={<TouchApp />}
              onClick={() => navigate(ROUTES.DRAW_CARDS)}
              sx={{
                py: 2.5,
                px: 6,
                fontSize: '1.3rem',
                fontFamily: 'Cinzel, serif',
                fontWeight: 600,
                background: 'linear-gradient(45deg, #D4AF37, #FFD700)',
                color: 'black',
                boxShadow: '0 8px 32px rgba(212, 175, 55, 0.3)',
                '&:hover': {
                  background: 'linear-gradient(45deg, #B8860B, #D4AF37)',
                  boxShadow: visual.enableHoverMotion ? '0 12px 40px rgba(212, 175, 55, 0.4)' : '0 8px 32px rgba(212, 175, 55, 0.3)',
                  transform: visual.enableHoverMotion ? 'translateY(-4px)' : 'none',
                },
              }}
            >
              进入抽牌界面
            </Button>
          </Box>
        )}
      </Container>
    </Box>
  );
};

export default NewReading;
