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
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useGameStore, GamePhase, getPhaseTitle } from '../../stores/gameStore';
import { ROUTES } from '../../routes/routeConfig';
import CosmicBackground from '../../components/Effects/CosmicBackground';

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

  // 获取当前步骤索引
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
    // 使用React Router跳转到抽牌页面
    navigate(ROUTES.DRAW_CARDS);
  };

  return (
    <Box className="hd-noise-overlay" sx={{
      width: '100vw',
      minHeight: '100vh',
      position: 'relative',
      left: '50%',
      right: '50%',
      marginLeft: '-50vw',
      marginRight: '-50vw',
      overflow: 'hidden',
    }}>
      <CosmicBackground />
      
      <Container maxWidth="lg" sx={{ py: 4, position: 'relative', zIndex: 1 }}>
        {/* 页面标题 */}
        <Box sx={{ textAlign: 'center', mb: 6 }} className="fade-in">
        <AutoAwesome
          sx={{
            fontSize: '4rem',
            color: 'primary.main',
            mb: 2,
            animation: 'float 3s ease-in-out infinite',
          }}
        />
        <Typography
          variant="h3"
          component="h1"
          sx={{
            fontFamily: 'Cinzel, serif',
            fontWeight: 700,
            background: 'linear-gradient(45deg, #D4AF37, #FFD700)',
            backgroundClip: 'text',
            WebkitBackgroundClip: 'text',
            WebkitTextFillColor: 'transparent',
            mb: 2,
          }}
        >
          新的占卜
        </Typography>
        <Typography
          variant="h6"
          sx={{
            color: 'text.secondary',
            fontStyle: 'italic',
            maxWidth: 600,
            mx: 'auto',
            lineHeight: 1.6,
          }}
        >
          踏入神秘的塔罗世界，让古老的智慧为您揭示人生的奥秘与指引
        </Typography>
      </Box>

      {/* 当前状态显示 */}
      {currentPhase !== GamePhase.IDLE && (
        <Box sx={{ mb: 4 }} className="slide-up">
          <Typography
            variant="h5"
            sx={{
              fontFamily: 'Cinzel, serif',
              color: 'primary.main',
              textAlign: 'center',
              mb: 3,
            }}
          >
            当前阶段：{getPhaseTitle(currentPhase)}
          </Typography>

          <Stepper
            activeStep={getCurrentStepIndex()}
            alternativeLabel
            sx={{
              '& .MuiStepLabel-root .Mui-completed': {
                color: 'primary.main',
              },
              '& .MuiStepLabel-root .Mui-active': {
                color: 'primary.main',
              },
              '& .MuiStepConnector-line': {
                borderColor: 'rgba(212, 175, 55, 0.3)',
              },
              '& .Mui-completed .MuiStepConnector-line': {
                borderColor: 'primary.main',
              },
            }}
          >
            {steps.map((step, index) => (
              <Step key={step.label}>
                <StepLabel
                  icon={
                    <Box
                      sx={{
                        width: 40,
                        height: 40,
                        borderRadius: '50%',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        background: index <= getCurrentStepIndex()
                          ? 'linear-gradient(45deg, #D4AF37, #FFD700)'
                          : 'rgba(212, 175, 55, 0.2)',
                        color: index <= getCurrentStepIndex() ? 'black' : 'text.secondary',
                      }}
                    >
                      {step.icon}
                    </Box>
                  }
                  sx={{
                    '& .MuiStepLabel-label': {
                      fontFamily: 'Cinzel, serif',
                      fontWeight: 500,
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

      {/* 占卜步骤介绍 */}
      {currentPhase === GamePhase.IDLE && (
        <Box className="scale-in">
          <Typography
            variant="h5"
            sx={{
              fontFamily: 'Cinzel, serif',
              fontWeight: 600,
              color: 'primary.main',
              textAlign: 'center',
              mb: 4,
            }}
          >
            占卜流程
          </Typography>

          <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: 3, mb: 6 }}>
            {steps.map((step, index) => (
              <Card
                key={step.label}
                sx={{
                  background: 'rgba(26, 11, 46, 0.4)',
                  backdropFilter: 'blur(12px)',
                  WebkitBackdropFilter: 'blur(12px)',
                  border: '1px solid rgba(0, 240, 255, 0.15)',
                  borderRadius: 3,
                  transition: 'all 0.4s cubic-bezier(0.4, 0, 0.2, 1)',
                  '&:hover': {
                    transform: 'translateY(-6px)',
                    boxShadow: '0 12px 40px rgba(0, 240, 255, 0.15), inset 0 0 20px rgba(0, 240, 255, 0.05)',
                    borderColor: 'rgba(0, 240, 255, 0.5)',
                  },
                }}
                className="card-hover"
                style={{ animationDelay: `${index * 0.1}s` }}
              >
                <CardContent sx={{ p: 4, textAlign: 'center' }}>
                  <Typography
                    variant="h6"
                    sx={{
                      fontFamily: 'Cinzel, serif',
                      fontWeight: 600,
                      color: 'text.primary',
                      mb: 2,
                    }}
                  >
                    {index + 1}. {step.label}
                  </Typography>

                  <Typography
                    variant="body2"
                    sx={{
                      color: 'text.secondary',
                      lineHeight: 1.6,
                    }}
                  >
                    {step.description}
                  </Typography>
                </CardContent>
              </Card>
            ))}
          </Box>

          {/* 开始按钮 */}
          <Box sx={{ textAlign: 'center' }}>
            <Button
              variant="contained"
              size="large"
              startIcon={<AutoAwesome />}
              onClick={handleStartReading}
              sx={{
                py: 2,
                px: 6,
                fontSize: '1.3rem',
                fontFamily: 'Cinzel, serif',
                fontWeight: 600,
                background: 'linear-gradient(45deg, #D4AF37, #FFD700)',
                color: 'black',
                boxShadow: '0 8px 32px rgba(212, 175, 55, 0.3)',
                '&:hover': {
                  background: 'linear-gradient(45deg, #B8860B, #D4AF37)',
                  boxShadow: '0 12px 40px rgba(212, 175, 55, 0.4)',
                  transform: 'translateY(-4px)',
                },
              }}
              className="mystical-glow"
            >
              开始占卜之旅
            </Button>

            <Typography
              variant="body2"
              sx={{
                color: 'text.secondary',
                mt: 2,
                fontStyle: 'italic',
              }}
            >
              "当你准备好聆听宇宙的声音时，塔罗牌就会为你揭示答案"
            </Typography>
          </Box>
        </Box>
      )}

      {/* 根据不同阶段显示不同内容 */}
      {currentPhase !== GamePhase.IDLE && (
        <Box sx={{ textAlign: 'center', py: 6 }}>
          <Typography
            variant="h4"
            sx={{
              fontFamily: 'Cinzel, serif',
              color: 'primary.main',
              mb: 2,
            }}
          >
            {getPhaseTitle(currentPhase)}
          </Typography>
          <Typography
            variant="body1"
            sx={{
              color: 'text.secondary',
              fontStyle: 'italic',
              mb: 4,
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
              py: 2,
              px: 4,
              fontSize: '1.2rem',
              fontFamily: 'Cinzel, serif',
              fontWeight: 600,
              background: 'linear-gradient(45deg, #D4AF37, #FFD700)',
              color: 'black',
              boxShadow: '0 8px 32px rgba(212, 175, 55, 0.3)',
              '&:hover': {
                background: 'linear-gradient(45deg, #B8860B, #D4AF37)',
                boxShadow: '0 12px 40px rgba(212, 175, 55, 0.4)',
                transform: 'translateY(-4px)',
              },
            }}
          >
            进入抽牌界面
          </Button>
        </Box>
      )}

      <Box
        sx={{
          position: 'fixed',
          top: '20%',
          left: '10%',
          width: 300,
          height: 300,
          background: 'radial-gradient(circle, rgba(0, 240, 255, 0.08) 0%, transparent 70%)',
          borderRadius: '50%',
          pointerEvents: 'none',
          zIndex: -1,
          animation: 'float 8s ease-in-out infinite',
        }}
      />
      <Box
        sx={{
          position: 'fixed',
          bottom: '10%',
          right: '15%',
          width: 400,
          height: 400,
          background: 'radial-gradient(circle, rgba(212, 175, 55, 0.08) 0%, transparent 70%)',
          borderRadius: '50%',
          pointerEvents: 'none',
          zIndex: -1,
          animation: 'float 10s ease-in-out infinite reverse',
        }}
      />
    </Container>
    </Box>
  );
};

export default NewReading;