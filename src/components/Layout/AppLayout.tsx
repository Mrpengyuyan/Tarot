import React from 'react';
import {
  Box,
  CssBaseline,
  ThemeProvider,
  useMediaQuery,
} from '@mui/material';
import { useLocation } from 'react-router-dom';
import { darkTheme } from '../../styles/theme';
import Header from './Header';
import Sidebar from './Sidebar';
import NotificationContainer from '../UI/Notification';
import { useAuthStore } from '../../stores/authStore';
import { useUiStore } from '../../stores/uiStore';
import { useVisualSettings } from '../../hooks/useVisualSettings';
import '../../styles/globals.css';

interface AppLayoutProps {
  children: React.ReactNode;
}

const AppLayout: React.FC<AppLayoutProps> = ({ children }) => {
  const location = useLocation();
  const isDesktop = useMediaQuery(darkTheme.breakpoints.up('lg'));
  const { sidebarOpen } = useUiStore();
  const { isLoggedIn } = useAuthStore();
  const visual = useVisualSettings();
  const isAuthPage = location.pathname.startsWith('/auth');
  const animateDesktopShell = isLoggedIn && !isAuthPage && isDesktop && visual.enableAnimations;
  const desktopShellTransition = darkTheme.transitions.create('transform', {
    duration: sidebarOpen ? 380 : 260,
    easing: sidebarOpen
      ? 'cubic-bezier(0.22, 1, 0.36, 1)'
      : 'cubic-bezier(0.4, 0, 0.2, 1)',
  });

  return (
    <ThemeProvider theme={darkTheme}>
      <CssBaseline />
      <Box
        sx={{ display: 'flex', minHeight: '100vh' }}
        data-visual-quality={visual.quality}
      >
        {isLoggedIn && !isAuthPage && <Sidebar />}

        <Box
          component="main"
          sx={{
            flexGrow: 1,
            minWidth: 0,
            display: 'flex',
            flexDirection: 'column',
            minHeight: '100vh',
            transform: animateDesktopShell && sidebarOpen ? 'translateX(20px) scale(0.992)' : 'translateX(0) scale(1)',
            transformOrigin: 'left center',
            transition: desktopShellTransition,
            position: 'relative',
            overflow: 'hidden',
            willChange: animateDesktopShell ? 'transform' : 'auto',
          }}
        >
          {!isAuthPage && <Header />}

          <Box
            sx={{
              flexGrow: 1,
              p: isAuthPage ? 0 : 2,
              pt: isAuthPage ? 0 : 2,
              background: isAuthPage
                ? 'linear-gradient(135deg, rgba(10, 10, 15, 0.95) 0%, rgba(46, 0, 58, 0.95) 100%)'
                : 'transparent',
              position: 'relative',
              minHeight: isAuthPage ? '100vh' : 'calc(100vh - 64px)',
              overflow: isAuthPage ? 'hidden' : 'visible',
            }}
          >
            <Box
              sx={{
                position: 'absolute',
                inset: 0,
                background: `
                  radial-gradient(circle at 20% 80%, rgba(212, 175, 55, 0.1) 0%, transparent 50%),
                  radial-gradient(circle at 80% 20%, rgba(106, 5, 114, 0.1) 0%, transparent 50%),
                  radial-gradient(circle at 40% 40%, rgba(171, 131, 161, 0.05) 0%, transparent 50%)
                `,
                pointerEvents: 'none',
                zIndex: 0,
              }}
            />

            <Box
              sx={{
                position: 'relative',
                zIndex: 1,
                maxWidth: 'none',
                margin: '0',
                width: '100%',
              }}
              className={visual.enableAnimations ? 'fade-in' : undefined}
            >
              {children}
            </Box>
          </Box>
        </Box>

        <NotificationContainer />

        <Box
          sx={{
            position: 'fixed',
            top: 0,
            left: 0,
            width: '100%',
            height: '100%',
            pointerEvents: 'none',
            zIndex: -1,
            background: 'linear-gradient(135deg, #0A0A0F 0%, #2E003A 100%)',
            '&::before': {
              content: '""',
              position: 'absolute',
              top: 0,
              left: 0,
              width: '100%',
              height: '100%',
              background: `
                radial-gradient(2px 2px at 20px 30px, rgba(212, 175, 55, 0.3), transparent),
                radial-gradient(2px 2px at 40px 70px, rgba(171, 131, 161, 0.2), transparent),
                radial-gradient(1px 1px at 90px 40px, rgba(212, 175, 55, 0.2), transparent),
                radial-gradient(1px 1px at 130px 80px, rgba(106, 5, 114, 0.1), transparent),
                radial-gradient(2px 2px at 160px 30px, rgba(212, 175, 55, 0.1), transparent)
              `,
              backgroundRepeat: 'repeat',
              backgroundSize: '200px 100px',
              animation: visual.enableAmbientMotion ? 'float 20s ease-in-out infinite' : 'none',
              opacity: visual.quality === 'minimal' ? 0.22 : visual.quality === 'lite' ? 0.34 : 0.5,
            },
          }}
        />
      </Box>
    </ThemeProvider>
  );
};

export default AppLayout;
