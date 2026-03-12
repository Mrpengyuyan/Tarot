import React from 'react';
import {
  Box,
  CssBaseline,
  ThemeProvider,
} from '@mui/material';
import { useLocation } from 'react-router-dom';
import { darkTheme } from '../../styles/theme';
import Header from './Header';
import Sidebar from './Sidebar';
import { useUiStore } from '../../stores/uiStore';
import { useAuthStore } from '../../stores/authStore';
import '../../styles/globals.css';

interface AppLayoutProps {
  children: React.ReactNode;
}

const AppLayout: React.FC<AppLayoutProps> = ({ children }) => {
  const location = useLocation();
  const { sidebarOpen } = useUiStore();
  const { isLoggedIn } = useAuthStore();

  // 检查是否是认证页面（不显示侧边栏）
  const isAuthPage = location.pathname.startsWith('/auth');

  return (
    <ThemeProvider theme={darkTheme}>
      <CssBaseline />
      <Box sx={{ display: 'flex', minHeight: '100vh' }}>
        {/* 侧边栏 - 仅在已登录且非认证页面时显示 */}
        {isLoggedIn && !isAuthPage && <Sidebar />}

        {/* 主内容区域 */}
        <Box
          component="main"
          sx={{
            flexGrow: 1,
            display: 'flex',
            flexDirection: 'column',
            minHeight: '100vh',
            transition: 'margin-left 0.3s ease',
            marginLeft: isLoggedIn && !isAuthPage && sidebarOpen ? '280px' : '0',
            position: 'relative',
            overflow: 'hidden',
          }}
        >
          {/* 顶部导航栏 */}
          <Header />

          {/* 页面内容 */}
          <Box
            sx={{
              flexGrow: 1,
              p: isAuthPage ? 0 : 3,
              pt: isAuthPage ? 0 : 4,
              background: isAuthPage
                ? 'linear-gradient(135deg, rgba(10, 10, 15, 0.95) 0%, rgba(46, 0, 58, 0.95) 100%)'
                : 'transparent',
              position: 'relative',
              minHeight: 'calc(100vh - 64px)',
            }}
          >
            {/* 背景装饰效果 */}
            <Box
              sx={{
                position: 'absolute',
                top: 0,
                left: 0,
                right: 0,
                bottom: 0,
                background: `
                  radial-gradient(circle at 20% 80%, rgba(212, 175, 55, 0.1) 0%, transparent 50%),
                  radial-gradient(circle at 80% 20%, rgba(106, 5, 114, 0.1) 0%, transparent 50%),
                  radial-gradient(circle at 40% 40%, rgba(171, 131, 161, 0.05) 0%, transparent 50%)
                `,
                pointerEvents: 'none',
                zIndex: 0,
              }}
            />

            {/* 页面内容容器 */}
            <Box
              sx={{
                position: 'relative',
                zIndex: 1,
                maxWidth: '1200px',
                margin: '0 auto',
                width: '100%',
              }}
              className="fade-in"
            >
              {children}
            </Box>
          </Box>
        </Box>

        {/* 全局背景粒子效果 */}
        <Box
          sx={{
            position: 'fixed',
            top: 0,
            left: 0,
            width: '100%',
            height: '100%',
            pointerEvents: 'none',
            zIndex: -1,
            background: `
              linear-gradient(135deg, #0A0A0F 0%, #2E003A 100%)
            `,
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
              animation: 'float 20s ease-in-out infinite',
              opacity: 0.5,
            },
          }}
        />
      </Box>
    </ThemeProvider>
  );
};

export default AppLayout;