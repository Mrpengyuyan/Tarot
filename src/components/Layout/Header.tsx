import React, { useState } from 'react';
import {
  AppBar,
  Toolbar,
  Typography,
  Box,
  IconButton,
  Avatar,
  Menu,
  MenuItem,
  Chip,
  useTheme,
  useMediaQuery,
} from '@mui/material';
import {
  Menu as MenuIcon,
  AccountCircle,
  Logout,
  Settings,
  History,
  AutoAwesome,
} from '@mui/icons-material';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '../../stores/authStore';
import { useUiStore } from '../../stores/uiStore';
import { ROUTES, getRouteTitle } from '../../routes/routeConfig';

const Header: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  const { user, isLoggedIn, logout } = useAuthStore();
  const { toggleSidebar } = useUiStore();

  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const open = Boolean(anchorEl);

  const handleProfileMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleProfileMenuClose = () => {
    setAnchorEl(null);
  };

  const handleLogout = () => {
    logout();
    handleProfileMenuClose();
    navigate(ROUTES.LOGIN);
  };

  const handleNavigate = (path: string) => {
    navigate(path);
    handleProfileMenuClose();
  };

  // 检查是否是认证页面
  const isAuthPage = location.pathname.startsWith('/auth');
  const currentPageTitle = getRouteTitle(location.pathname);

  return (
    <AppBar
      position="sticky"
      elevation={0}
      sx={{
        background: 'linear-gradient(135deg, rgba(26, 26, 46, 0.95) 0%, rgba(22, 33, 62, 0.95) 100%)',
        backdropFilter: 'blur(10px)',
        borderBottom: '1px solid rgba(212, 175, 55, 0.2)',
        zIndex: theme.zIndex.drawer + 1,
      }}
    >
      <Toolbar sx={{ px: { xs: 1, sm: 3 } }}>
        {/* 左侧：菜单按钮和Logo */}
        <Box sx={{ display: 'flex', alignItems: 'center', flexGrow: 1 }}>
          {/* 侧边栏切换按钮 - 仅在已登录且非认证页面时显示 */}
          {isLoggedIn && !isAuthPage && (
            <IconButton
              edge="start"
              color="inherit"
              aria-label="toggle menu"
              onClick={toggleSidebar}
              sx={{
                mr: 2,
                color: 'primary.main',
                '&:hover': {
                  background: 'rgba(212, 175, 55, 0.1)',
                },
              }}
            >
              <MenuIcon />
            </IconButton>
          )}

          {/* Logo和标题 */}
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              cursor: 'pointer',
              '&:hover': {
                '& .logo-text': {
                  textShadow: '0 0 15px rgba(212, 175, 55, 0.5)',
                },
              },
            }}
            onClick={() => navigate(isLoggedIn ? ROUTES.DASHBOARD : ROUTES.HOME)}
          >
            <AutoAwesome
              sx={{
                fontSize: '2rem',
                color: 'primary.main',
                mr: 1,
                animation: 'float 3s ease-in-out infinite',
              }}
            />
            <Typography
              variant="h6"
              component="div"
              className="logo-text"
              sx={{
                fontFamily: 'Cinzel, serif',
                fontWeight: 600,
                background: 'linear-gradient(45deg, #D4AF37, #FFD700)',
                backgroundClip: 'text',
                WebkitBackgroundClip: 'text',
                WebkitTextFillColor: 'transparent',
                transition: 'text-shadow 0.3s ease',
                fontSize: { xs: '1.1rem', sm: '1.25rem' },
              }}
            >
              塔罗之境
            </Typography>
          </Box>

          {/* 当前页面标题 - 桌面端显示 */}
          {!isMobile && !isAuthPage && isLoggedIn && (
            <Box sx={{ ml: 4, display: 'flex', alignItems: 'center' }}>
              <Typography
                variant="body1"
                sx={{
                  color: 'text.secondary',
                  fontSize: '0.875rem',
                  mr: 1,
                }}
              >
                /
              </Typography>
              <Typography
                variant="body1"
                sx={{
                  color: 'text.primary',
                  fontFamily: 'Cinzel, serif',
                }}
              >
                {currentPageTitle}
              </Typography>
            </Box>
          )}
        </Box>

        {/* 右侧：用户信息和操作 */}
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          {isLoggedIn ? (
            <>
              {/* 在线状态指示 */}
              <Chip
                label="在线"
                size="small"
                sx={{
                  background: 'linear-gradient(45deg, #4ECDC4, #44A08D)',
                  color: 'white',
                  fontSize: '0.75rem',
                  display: { xs: 'none', sm: 'flex' },
                }}
              />

              {/* 用户头像和菜单 */}
              <IconButton
                size="large"
                edge="end"
                aria-label="account menu"
                aria-controls={open ? 'account-menu' : undefined}
                aria-haspopup="true"
                aria-expanded={open ? 'true' : undefined}
                onClick={handleProfileMenuOpen}
                sx={{
                  ml: 1,
                  '&:hover': {
                    background: 'rgba(212, 175, 55, 0.1)',
                  },
                }}
              >
                <Avatar
                  sx={{
                    width: 36,
                    height: 36,
                    background: 'linear-gradient(45deg, #D4AF37, #FFD700)',
                    color: 'black',
                    fontSize: '1rem',
                    fontFamily: 'Cinzel, serif',
                    border: '2px solid rgba(212, 175, 55, 0.3)',
                  }}
                  src={user?.avatar_url}
                >
                  {user?.nickname?.[0] || user?.username?.[0] || 'U'}
                </Avatar>
              </IconButton>

              {/* 用户菜单 */}
              <Menu
                id="account-menu"
                anchorEl={anchorEl}
                open={open}
                onClose={handleProfileMenuClose}
                onClick={handleProfileMenuClose}
                PaperProps={{
                  elevation: 8,
                  sx: {
                    background: 'linear-gradient(135deg, #1A1A2E 0%, #16213E 100%)',
                    border: '1px solid rgba(212, 175, 55, 0.2)',
                    borderRadius: 2,
                    mt: 1.5,
                    minWidth: 200,
                    '& .MuiMenuItem-root': {
                      px: 2,
                      py: 1,
                      color: 'text.primary',
                      '&:hover': {
                        background: 'rgba(212, 175, 55, 0.1)',
                      },
                    },
                  },
                }}
                transformOrigin={{ horizontal: 'right', vertical: 'top' }}
                anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
              >
                {/* 用户信息 */}
                <Box sx={{ px: 2, py: 1, borderBottom: '1px solid rgba(212, 175, 55, 0.2)' }}>
                  <Typography variant="subtitle2" sx={{ color: 'primary.main' }}>
                    {user?.nickname || user?.username}
                  </Typography>
                  <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                    {user?.email}
                  </Typography>
                </Box>

                {/* 菜单项 */}
                <MenuItem onClick={() => handleNavigate(ROUTES.PROFILE)}>
                  <AccountCircle sx={{ mr: 1, fontSize: '1.25rem' }} />
                  个人中心
                </MenuItem>
                <MenuItem onClick={() => handleNavigate(ROUTES.HISTORY)}>
                  <History sx={{ mr: 1, fontSize: '1.25rem' }} />
                  占卜历史
                </MenuItem>
                <MenuItem onClick={() => handleNavigate(ROUTES.PROFILE)}>
                  <Settings sx={{ mr: 1, fontSize: '1.25rem' }} />
                  设置
                </MenuItem>
                <MenuItem onClick={handleLogout}>
                  <Logout sx={{ mr: 1, fontSize: '1.25rem' }} />
                  退出登录
                </MenuItem>
              </Menu>
            </>
          ) : (
            // 未登录状态：显示登录按钮
            !isAuthPage && (
              <Box sx={{ display: 'flex', gap: 1 }}>
                <IconButton
                  color="primary"
                  onClick={() => navigate(ROUTES.LOGIN)}
                  sx={{
                    background: 'rgba(212, 175, 55, 0.1)',
                    '&:hover': {
                      background: 'rgba(212, 175, 55, 0.2)',
                    },
                  }}
                >
                  <AccountCircle />
                </IconButton>
              </Box>
            )
          )}
        </Box>
      </Toolbar>
    </AppBar>
  );
};

export default Header;