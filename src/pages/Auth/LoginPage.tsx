import React, { useState, useEffect } from 'react';
import {
  Box,
  Container,
  Paper,
  Tabs,
  Tab,
  Alert,
  Fade,
} from '@mui/material';
import { useLocation } from 'react-router-dom';
import LoginForm from '../../components/Auth/LoginForm';
import RegisterForm from '../../components/Auth/RegisterForm';
import CosmicBackground from '../../components/Effects/CosmicBackground';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index, ...other }) => {
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`auth-tabpanel-${index}`}
      aria-labelledby={`auth-tab-${index}`}
      {...other}
    >
      {value === index && (
        <Fade in={true} timeout={300}>
          <Box sx={{ pt: 3 }}>
            {children}
          </Box>
        </Fade>
      )}
    </div>
  );
};

const LoginPage: React.FC = () => {
  const location = useLocation();
  const [activeTab, setActiveTab] = useState(0);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // 检查是否有来自注册页的成功消息
  useEffect(() => {
    if (location.state?.message && location.state?.type === 'success') {
      setSuccessMessage(location.state.message);
      // 清除location state中的消息
      window.history.replaceState({}, document.title);
    }
  }, [location.state]);

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
    setSuccessMessage(null); // 切换标签时清除成功消息
  };

  const handleSwitchToRegister = () => {
    setActiveTab(1);
    setSuccessMessage(null);
  };

  const handleSwitchToLogin = () => {
    setActiveTab(0);
  };

  return (
    <Box
      className="hd-noise-overlay"
      sx={{
        width: '100vw',
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        position: 'relative',
        left: '50%',
        right: '50%',
        marginLeft: '-50vw',
        marginRight: '-50vw',
      }}
    >
      <CosmicBackground />
      <Container maxWidth="md" sx={{ position: 'relative', zIndex: 1 }}>
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
            minHeight: '80vh',
          }}
        >
          <Paper
            elevation={24}
            sx={{
              width: '100%',
              maxWidth: 500,
              background: 'rgba(26, 11, 46, 0.65)',
              backdropFilter: 'blur(20px)',
              WebkitBackdropFilter: 'blur(20px)',
              border: '1px solid rgba(0, 240, 255, 0.2)',
              borderRadius: 4,
              boxShadow: '0 8px 32px rgba(0, 0, 0, 0.6), inset 0 0 20px rgba(0, 240, 255, 0.05)',
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
                backgroundSize: '200% auto',
                animation: 'pulse 3s infinite',
              },
            }}
            className="slide-up"
          >
            {/* 成功消息提示 */}
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

            {/* 标签栏 */}
            <Box sx={{ borderBottom: 1, borderColor: 'rgba(0, 240, 255, 0.2)' }}>
              <Tabs
                value={activeTab}
                onChange={handleTabChange}
                aria-label="authentication tabs"
                centered
                sx={{
                  '& .MuiTabs-indicator': {
                    background: 'linear-gradient(90deg, #D4AF37, #00F0FF)',
                    height: 3,
                    boxShadow: '0 -2px 10px rgba(0, 240, 255, 0.5)',
                  },
                  '& .MuiTab-root': {
                    color: 'text.secondary',
                    fontFamily: 'Cinzel, serif',
                    fontSize: '1rem',
                    fontWeight: 500,
                    textTransform: 'none',
                    py: 2,
                    '&.Mui-selected': {
                      color: 'secondary.main',
                      textShadow: '0 0 10px rgba(0, 240, 255, 0.4)',
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

            {/* 表单内容 */}
            <Box sx={{ px: 4, pb: 4 }}>
              <TabPanel value={activeTab} index={0}>
                <LoginForm onSwitchToRegister={handleSwitchToRegister} />
              </TabPanel>

              <TabPanel value={activeTab} index={1}>
                <RegisterForm onSwitchToLogin={handleSwitchToLogin} />
              </TabPanel>
            </Box>
          </Paper>
        </Box>

        {/* 装饰性引言 */}
        <Box
          sx={{
            position: 'absolute',
            top: '10%',
            left: '10%',
            transform: 'rotate(-5deg)',
            opacity: 0.7,
            display: { xs: 'none', md: 'block' },
          }}
        >
          <Box
            sx={{
              background: 'rgba(16, 8, 32, 0.5)',
              backdropFilter: 'blur(10px)',
              border: '1px solid rgba(0, 240, 255, 0.2)',
              borderRadius: 2,
              p: 2,
              fontStyle: 'italic',
              color: 'text.secondary',
              fontSize: '0.875rem',
              maxWidth: 200,
              boxShadow: '0 4px 15px rgba(0,0,0,0.3)',
            }}
          >
            "命运不是偶然，而是选择的结果。"
          </Box>
        </Box>

        <Box
          sx={{
            position: 'absolute',
            bottom: '15%',
            right: '10%',
            transform: 'rotate(3deg)',
            opacity: 0.7,
            display: { xs: 'none', md: 'block' },
          }}
        >
          <Box
            sx={{
              background: 'rgba(16, 8, 32, 0.5)',
              backdropFilter: 'blur(10px)',
              border: '1px solid rgba(0, 240, 255, 0.2)',
              borderRadius: 2,
              p: 2,
              fontStyle: 'italic',
              color: 'text.secondary',
              fontSize: '0.875rem',
              maxWidth: 200,
              boxShadow: '0 4px 15px rgba(0,0,0,0.3)',
            }}
          >
            "每一张塔罗牌都诉说着宇宙的秘密。"
          </Box>
        </Box>

        {/* 动态六芒星装饰 (Star of David / Hexagram) */}
        <Box
          sx={{
            position: 'absolute',
            top: '25%',
            left: '10%',
            width: 120,
            height: 120,
            animation: 'rotate-reverse 30s linear infinite',
            opacity: 0.6,
            display: { xs: 'none', md: 'block' },
            zIndex: 0,
          }}
        >
          <svg viewBox="0 0 100 100" className="pulse-glow">
            <polygon points="50,10 85,75 15,75" fill="none" stroke="#D4AF37" strokeWidth="1" filter="url(#neon-glow)" />
            <polygon points="50,90 15,25 85,25" fill="none" stroke="#00F0FF" strokeWidth="1" filter="url(#neon-glow)" />
            <circle cx="50" cy="50" r="10" fill="#D4AF37" filter="url(#neon-glow-strong)" />
          </svg>
        </Box>
        
        {/* 小型塔罗符号碎片漂浮 */}
        {/* Core Dust */}
        <Box
          sx={{
            position: 'absolute',
            top: '20%',
            right: '20%',
            width: 8,
            height: 8,
            background: 'rgba(0, 240, 255, 0.6)',
            boxShadow: '0 0 10px rgba(0, 240, 255, 0.8)',
            borderRadius: '50%',
            animation: 'float 4s ease-in-out infinite',
            zIndex: 0,
          }}
        />
        <Box
          sx={{
            position: 'absolute',
            top: '60%',
            left: '15%',
            width: 6,
            height: 6,
            background: 'rgba(171, 131, 161, 0.6)',
            borderRadius: '50%',
            boxShadow: '0 0 8px rgba(171, 131, 161, 0.8)',
            animation: 'float 5s ease-in-out infinite reverse',
            zIndex: 0,
          }}
        />
        <Box
          sx={{
            position: 'absolute',
            top: '30%',
            left: '70%',
            width: 4,
            height: 4,
            background: 'rgba(212, 175, 55, 0.8)',
            boxShadow: '0 0 8px rgba(212, 175, 55, 1)',
            borderRadius: '50%',
            animation: 'float 6s ease-in-out infinite',
            zIndex: 0,
          }}
        />
        <Box
          sx={{
            position: 'absolute',
            bottom: '25%',
            right: '30%',
            width: 5,
            height: 5,
            background: 'rgba(0, 240, 255, 0.5)',
            boxShadow: '0 0 6px rgba(0, 240, 255, 0.8)',
            borderRadius: '50%',
            animation: 'float 7s ease-in-out infinite reverse',
            zIndex: 0,
          }}
        />
        <Box
          sx={{
            position: 'absolute',
            top: '40%',
            right: '10%',
            width: 7,
            height: 7,
            background: 'rgba(212, 175, 55, 0.4)',
            boxShadow: '0 0 12px rgba(212, 175, 55, 0.6)',
            borderRadius: '50%',
            animation: 'float 3.5s ease-in-out infinite',
            zIndex: 0,
          }}
        />
        <Box
          sx={{
            position: 'absolute',
            bottom: '10%',
            left: '30%',
            width: 3,
            height: 3,
            background: 'rgba(255, 255, 255, 0.7)',
            boxShadow: '0 0 5px rgba(255, 255, 255, 0.9)',
            borderRadius: '50%',
            animation: 'float 5.5s ease-in-out infinite reverse',
            zIndex: 0,
          }}
        />
      </Container>
    </Box>
  );
};

export default LoginPage;