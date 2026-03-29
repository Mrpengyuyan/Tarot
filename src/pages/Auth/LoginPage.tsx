import React, { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Container,
  Paper,
  Tab,
  Tabs,
} from '@mui/material';
import { useLocation } from 'react-router-dom';
import LoginForm from '../../components/Auth/LoginForm';
import RegisterForm from '../../components/Auth/RegisterForm';
import CosmicBackground from '../../components/Effects/CosmicBackground';
import { useVisualSettings } from '../../hooks/useVisualSettings';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index }) => {
  if (value !== index) {
    return null;
  }

  return <Box sx={{ pt: 3 }}>{children}</Box>;
};

const LoginPage: React.FC = () => {
  const location = useLocation();
  const [activeTab, setActiveTab] = useState(0);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const visual = useVisualSettings();

  useEffect(() => {
    if (location.state?.message && location.state?.type === 'success') {
      setSuccessMessage(location.state.message);
      window.history.replaceState({}, document.title);
    }
  }, [location.state]);

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
    setSuccessMessage(null);
  };

  return (
    <Box
      className="hd-noise-overlay"
      sx={{
        width: '100%',
        height: '100dvh',
        display: 'flex',
        alignItems: 'center',
        position: 'relative',
        overflow: 'hidden',
      }}
    >
      <CosmicBackground performanceMode={visual.backgroundMode} />

      <Container
        maxWidth="md"
        sx={{
          position: 'relative',
          zIndex: 1,
          height: '100%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
            minHeight: '100%',
            width: '100%',
            position: 'relative',
            zIndex: 2,
          }}
        >
          <Paper
            elevation={24}
            sx={{
              width: '100%',
              maxWidth: 520,
              maxHeight: 'min(720px, calc(100dvh - 48px))',
              background: 'linear-gradient(155deg, rgba(31, 14, 54, 0.94) 0%, rgba(16, 8, 36, 0.92) 45%, rgba(7, 20, 40, 0.93) 100%)',
              border: '1px solid rgba(212, 175, 55, 0.2)',
              borderRadius: 4,
              boxShadow: `
                0 24px 56px rgba(0, 0, 0, 0.5),
                0 8px 24px rgba(0, 0, 0, 0.22),
                inset 0 1px 0 rgba(255, 255, 255, 0.08),
                inset 0 0 26px rgba(0, 240, 255, 0.05),
                inset 0 -26px 50px rgba(212, 175, 55, 0.04)
              `,
              overflow: 'hidden',
              position: 'relative',
              '&::before': {
                content: '""',
                position: 'absolute',
                top: 0,
                left: 0,
                right: 0,
                height: '4px',
                background: 'linear-gradient(90deg, #D4AF37, #FFD700, #00F0FF, #D4AF37)',
                backgroundSize: '180% auto',
                animation: visual.enableAmbientMotion ? 'authPanelPulse 6s linear infinite' : 'none',
              },
              '&::after': {
                content: '""',
                position: 'absolute',
                inset: 0,
                pointerEvents: 'none',
                background: `
                  radial-gradient(circle at 14% 18%, rgba(212, 175, 55, 0.12), transparent 28%),
                  radial-gradient(circle at 85% 12%, rgba(0, 240, 255, 0.13), transparent 26%),
                  radial-gradient(circle at 80% 82%, rgba(149, 101, 255, 0.1), transparent 30%)
                `,
                mixBlendMode: 'screen',
                opacity: 0.92,
              },
              '@keyframes authPanelPulse': {
                '0%': { backgroundPosition: '0% 50%' },
                '100%': { backgroundPosition: '180% 50%' },
              },
            }}
            className="slide-up"
          >
            {successMessage && (
              <Alert
                severity="success"
                sx={{
                  m: 2,
                  background: 'rgba(78, 205, 196, 0.1)',
                  border: '1px solid rgba(78, 205, 196, 0.3)',
                  color: 'success.main',
                  '& .MuiAlert-icon': {
                    color: 'success.main',
                  },
                }}
              >
                {successMessage}
              </Alert>
            )}

            <Box sx={{ borderBottom: 1, borderColor: 'rgba(0, 240, 255, 0.18)' }}>
              <Tabs
                value={activeTab}
                onChange={handleTabChange}
                aria-label="authentication tabs"
                centered
                sx={{
                  '& .MuiTabs-indicator': {
                    background: 'linear-gradient(90deg, #D4AF37, #00F0FF)',
                    height: 3,
                    boxShadow: '0 -2px 10px rgba(0, 240, 255, 0.34)',
                  },
                  '& .MuiTab-root': {
                    color: 'text.secondary',
                    fontFamily: 'Cinzel, serif',
                    fontSize: '1.2rem',
                    fontWeight: 600,
                    textTransform: 'none',
                    py: 2.1,
                    minHeight: 64,
                    transition: 'color 160ms ease, text-shadow 160ms ease',
                    '&.Mui-selected': {
                      color: 'secondary.main',
                      textShadow: '0 0 10px rgba(0, 240, 255, 0.24)',
                    },
                    '&:hover': {
                      color: 'primary.light',
                    },
                  },
                }}
              >
                <Tab label="登录" id="auth-tab-0" aria-controls="auth-tabpanel-0" />
                <Tab label="注册" id="auth-tab-1" aria-controls="auth-tabpanel-1" />
              </Tabs>
            </Box>

            <Box sx={{ px: 4, pb: 4 }}>
              <TabPanel value={activeTab} index={0}>
                <LoginForm onSwitchToRegister={() => setActiveTab(1)} />
              </TabPanel>

              <TabPanel value={activeTab} index={1}>
                <RegisterForm onSwitchToLogin={() => setActiveTab(0)} />
              </TabPanel>
            </Box>
          </Paper>
        </Box>

        <Box
          sx={{
            position: 'absolute',
            top: '6%',
            left: '2.5%',
            transform: 'rotate(-5deg)',
            opacity: 0.82,
            display: { xs: 'none', md: 'block' },
            zIndex: 3,
          }}
        >
          <Box
            sx={{
              background: 'linear-gradient(135deg, rgba(14, 8, 28, 0.86) 0%, rgba(26, 11, 46, 0.76) 100%)',
              backdropFilter: 'blur(10px)',
              WebkitBackdropFilter: 'blur(10px)',
              border: '1.2px solid rgba(212, 175, 55, 0.38)',
              borderRadius: '16px',
              p: 2.5,
              fontStyle: 'italic',
              color: '#F0DEAA',
              fontSize: '1rem',
              fontWeight: 600,
              letterSpacing: '0.04em',
              lineHeight: 1.7,
              maxWidth: 236,
              textShadow: '0 0 12px rgba(212, 175, 55, 0.18)',
              boxShadow: '0 12px 34px rgba(0, 0, 0, 0.36), inset 0 0 22px rgba(212, 175, 55, 0.04)',
              transition: 'transform 260ms ease, box-shadow 260ms ease, border-color 260ms ease, background 260ms ease',
              cursor: 'default',
              '&:hover': {
                transform: visual.enableHoverMotion ? 'rotate(-4deg) translateY(-4px)' : 'rotate(-5deg)',
                borderColor: 'rgba(255, 218, 128, 0.56)',
                background: 'linear-gradient(135deg, rgba(26, 12, 46, 0.88) 0%, rgba(38, 18, 56, 0.76) 100%)',
                boxShadow: '0 18px 40px rgba(0, 0, 0, 0.42), 0 0 24px rgba(212, 175, 55, 0.14), inset 0 0 28px rgba(212, 175, 55, 0.08)',
              },
            }}
          >
            “命运不会替你作答，它只会让你更清楚自己真正想要什么。”
          </Box>
        </Box>

        <Box
          sx={{
            position: 'absolute',
            bottom: '4.5%',
            right: '2.8%',
            transform: 'rotate(3deg)',
            opacity: 0.8,
            display: { xs: 'none', md: 'block' },
            zIndex: 3,
          }}
        >
          <Box
            sx={{
              background: 'linear-gradient(135deg, rgba(8, 16, 36, 0.8) 0%, rgba(16, 8, 32, 0.7) 100%)',
              backdropFilter: 'blur(10px)',
              WebkitBackdropFilter: 'blur(10px)',
              border: '1.2px solid rgba(0, 240, 255, 0.34)',
              borderRadius: '16px',
              p: 2.5,
              fontStyle: 'italic',
              color: '#CDEBFA',
              fontSize: '1rem',
              fontWeight: 600,
              letterSpacing: '0.04em',
              lineHeight: 1.7,
              maxWidth: 236,
              textShadow: '0 0 10px rgba(0, 240, 255, 0.14)',
              boxShadow: '0 12px 34px rgba(0, 0, 0, 0.36), inset 0 0 22px rgba(0, 240, 255, 0.03)',
              transition: 'transform 260ms ease, box-shadow 260ms ease, border-color 260ms ease, background 260ms ease',
              cursor: 'default',
              '&:hover': {
                transform: visual.enableHoverMotion ? 'rotate(2deg) translateY(-4px)' : 'rotate(3deg)',
                borderColor: 'rgba(114, 242, 255, 0.54)',
                background: 'linear-gradient(135deg, rgba(9, 20, 44, 0.84) 0%, rgba(16, 10, 36, 0.74) 100%)',
                boxShadow: '0 18px 40px rgba(0, 0, 0, 0.42), 0 0 26px rgba(0, 240, 255, 0.12), inset 0 0 26px rgba(0, 240, 255, 0.06)',
              },
            }}
          >
            “每一次翻牌，都是一次重新整理情绪、问题与方向的机会。”
          </Box>
        </Box>

        <Box
          sx={{
            position: 'absolute',
            top: '28%',
            left: '5.5%',
            width: 102,
            height: 102,
            opacity: 0.34,
            display: { xs: 'none', md: 'block' },
            zIndex: 1,
          }}
        >
          <svg
            viewBox="0 0 100 100"
            className={visual.enableAmbientMotion ? 'rotate-slow-reverse' : undefined}
            style={{ width: '100%', height: '100%' }}
          >
            <polygon points="50,12 83,69 17,69" fill="none" stroke="#D4AF37" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round" />
            <polygon points="50,88 17,31 83,31" fill="none" stroke="#00F0FF" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round" />
            <circle cx="50" cy="50" r="8" fill="#D4AF37" opacity="0.68" />
          </svg>
        </Box>

        {visual.enableAnimations && ([
          { top: '18%', right: '20%', size: 7, color: 'rgba(0, 240, 255, 0.62)', duration: '4.8s' },
          { top: '62%', left: '14%', size: 6, color: 'rgba(171, 131, 161, 0.56)', duration: '5.6s' },
          { top: '32%', left: '72%', size: 4, color: 'rgba(212, 175, 55, 0.74)', duration: '6.2s' },
        ] as Array<{ top?: string; bottom?: string; left?: string; right?: string; size: number; color: string; duration: string }>).map((spark, index) => (
          <Box
            key={index}
            sx={{
              position: 'absolute',
              top: spark.top,
              bottom: spark.bottom,
              left: spark.left,
              right: spark.right,
              width: spark.size,
              height: spark.size,
              borderRadius: '50%',
              background: spark.color,
              boxShadow: `0 0 12px ${spark.color}`,
              opacity: 0.82,
              animation: `authFloat ${spark.duration} ease-in-out infinite`,
              '@keyframes authFloat': {
                '0%, 100%': { transform: 'translate3d(0, 0, 0)' },
                '50%': { transform: 'translate3d(0, -10px, 0)' },
              },
            }}
          />
        ))}
      </Container>
    </Box>
  );
};

export default LoginPage;
