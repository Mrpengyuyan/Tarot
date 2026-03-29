import React from 'react';
import {
  Drawer,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Divider,
  Box,
  Typography,
  Chip,
  useTheme,
  useMediaQuery,
} from '@mui/material';
import {
  Dashboard,
  AutoAwesome,
  History,
  Menu as MenuIcon,
  Person,
  MenuBook,
  Timeline,
  Star,
} from 'icons';
import { useNavigate, useLocation } from 'react-router-dom';
import { useUiStore } from '../../stores/uiStore';
import { useAuthStore } from '../../stores/authStore';
import { ROUTES } from '../../routes/routeConfig';

interface NavItem {
  id: string;
  label: string;
  path: string;
  icon: React.ReactNode;
  badge?: string;
  description?: string;
}

const DESKTOP_DRAWER_WIDTH = 340;
const MOBILE_DRAWER_WIDTH = 320;

const navItems: NavItem[] = [
  {
    id: 'dashboard',
    label: '仪表盘',
    path: ROUTES.DASHBOARD,
    icon: <Dashboard />,
    description: '查看今日概览与最近状态',
  },
  {
    id: 'new-reading',
    label: '新占卜',
    path: ROUTES.NEW_READING,
    icon: <AutoAwesome />,
    description: '开始一次新的塔罗解读',
  },
  {
    id: 'history',
    label: '历史记录',
    path: ROUTES.HISTORY,
    icon: <History />,
    description: '回看过往抽牌与 AI 解读',
  },
  {
    id: 'profile',
    label: '个人中心',
    path: ROUTES.PROFILE,
    icon: <Person />,
    description: '管理账号资料与偏好',
  },
];

const quickActions: NavItem[] = [
  {
    id: 'cards',
    label: '塔罗牌库',
    path: ROUTES.CARDS,
    icon: <MenuBook />,
    description: '浏览 78 张牌的基础信息',
  },
  {
    id: 'spreads',
    label: '牌阵目录',
    path: ROUTES.SPREADS,
    icon: <Timeline />,
    description: '快速了解不同牌阵用途',
  },
  {
    id: 'favorites',
    label: '我的收藏',
    path: ROUTES.FAVORITES,
    icon: <Star />,
    description: '查看你标记的常用内容',
  },
];

const Sidebar: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  const { sidebarOpen, setSidebarOpen } = useUiStore();
  const { user } = useAuthStore();

  const handleNavigate = (path: string) => {
    navigate(path);
    if (isMobile) {
      setSidebarOpen(false);
    }
  };

  const handleClose = () => {
    setSidebarOpen(false);
  };

  const isActive = (path: string) => {
    if (path === ROUTES.DASHBOARD) {
      return location.pathname === path;
    }
    return location.pathname.startsWith(path);
  };

  const drawerContent = (
    <Box
      sx={{
        width: isMobile ? MOBILE_DRAWER_WIDTH : DESKTOP_DRAWER_WIDTH,
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        background: 'linear-gradient(180deg, #1A1A2E 0%, #16213E 100%)',
        borderRight: '1px solid rgba(212, 175, 55, 0.2)',
        overflow: 'auto',
        opacity: sidebarOpen ? 1 : 0,
        transform: sidebarOpen ? 'translateX(0)' : 'translateX(-18px)',
        transition: theme.transitions.create(['opacity', 'transform'], {
          duration: sidebarOpen ? 360 : 220,
          easing: sidebarOpen
            ? 'cubic-bezier(0.22, 1, 0.36, 1)'
            : 'cubic-bezier(0.4, 0, 0.2, 1)',
        }),
        willChange: 'transform, opacity',
      }}
    >
      <Box
        sx={{
          p: 3,
          borderBottom: '1px solid rgba(212, 175, 55, 0.2)',
          background: 'linear-gradient(135deg, rgba(212, 175, 55, 0.1) 0%, transparent 100%)',
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2, mb: 2 }}>
          <AutoAwesome
            sx={{
              fontSize: '1.5rem',
              color: 'primary.main',
              mr: 1,
            }}
          />
          <Typography
            variant="h6"
            sx={{
              fontFamily: 'Cinzel, serif',
              fontWeight: 600,
              background: 'linear-gradient(45deg, #D4AF37, #FFD700)',
              backgroundClip: 'text',
              WebkitBackgroundClip: 'text',
              WebkitTextFillColor: 'transparent',
            }}
          >
            塔罗导航
          </Typography>
          {!isMobile && (
            <Box
              component="button"
              type="button"
              aria-label="close sidebar"
              onClick={handleClose}
              sx={{
                width: 40,
                height: 40,
                borderRadius: '14px',
                border: '1px solid rgba(212, 175, 55, 0.18)',
                background: 'rgba(10, 12, 28, 0.36)',
                color: 'primary.main',
                display: 'inline-flex',
                alignItems: 'center',
                justifyContent: 'center',
                cursor: 'pointer',
                flexShrink: 0,
                transition: theme.transitions.create(['background-color', 'transform', 'border-color'], {
                  duration: 180,
                }),
                '&:hover': {
                  background: 'rgba(212, 175, 55, 0.10)',
                  borderColor: 'rgba(212, 175, 55, 0.34)',
                  transform: 'translateX(1px)',
                },
              }}
            >
              <MenuIcon fontSize="small" />
            </Box>
          )}
        </Box>

        <Box
          sx={{
            p: 2,
            background: 'rgba(212, 175, 55, 0.1)',
            borderRadius: 2,
            border: '1px solid rgba(212, 175, 55, 0.2)',
          }}
        >
          <Typography
            variant="subtitle2"
            sx={{
              color: 'primary.main',
              fontFamily: 'Cinzel, serif',
              mb: 0.5,
            }}
          >
            当前旅者
          </Typography>
          <Typography
            variant="body2"
            sx={{
              color: 'text.primary',
              fontWeight: 500,
            }}
          >
            {user?.nickname || user?.username}
          </Typography>
        </Box>
      </Box>

      <Box sx={{ py: 2.5 }}>
        <Typography
          variant="overline"
          sx={{
            px: 3,
            py: 1,
            color: 'text.secondary',
            fontSize: '0.85rem',
            fontFamily: 'Cinzel, serif',
            letterSpacing: 1,
          }}
        >
          主导航
        </Typography>

        <List sx={{ px: 1 }}>
          {navItems.map((item) => (
            <ListItem key={item.id} disablePadding sx={{ mb: 1.5 }}>
              <ListItemButton
                onClick={() => handleNavigate(item.path)}
                selected={isActive(item.path)}
                sx={{
                  borderRadius: 2,
                  mx: 1,
                  py: 1.5,
                  transition: 'all 0.3s ease',
                  '&.Mui-selected': {
                    background: 'linear-gradient(135deg, rgba(212, 175, 55, 0.2) 0%, rgba(212, 175, 55, 0.1) 100%)',
                    borderLeft: '3px solid',
                    borderLeftColor: 'primary.main',
                    '&:hover': {
                      background: 'linear-gradient(135deg, rgba(212, 175, 55, 0.25) 0%, rgba(212, 175, 55, 0.15) 100%)',
                    },
                  },
                  '&:hover': {
                    background: 'rgba(212, 175, 55, 0.1)',
                    transform: 'translateX(4px)',
                  },
                }}
              >
                <ListItemIcon
                  sx={{
                    color: isActive(item.path) ? 'primary.main' : 'text.secondary',
                    minWidth: 44,
                  }}
                >
                  {item.icon}
                </ListItemIcon>
                <ListItemText
                  primary={item.label}
                  secondary={item.description}
                  primaryTypographyProps={{
                    fontSize: '1.1rem',
                    fontWeight: isActive(item.path) ? 600 : 400,
                    color: isActive(item.path) ? 'primary.main' : 'text.primary',
                  }}
                  secondaryTypographyProps={{
                    fontSize: '0.85rem',
                    color: 'text.secondary',
                  }}
                />
                {item.badge && (
                  <Chip
                    label={item.badge}
                    size="small"
                    sx={{
                      background: 'primary.main',
                      color: 'black',
                      fontSize: '0.7rem',
                      height: '20px',
                    }}
                  />
                )}
              </ListItemButton>
            </ListItem>
          ))}
        </List>
      </Box>

      <Divider sx={{ mx: 2, my: 1, borderColor: 'rgba(212, 175, 55, 0.2)' }} />

      <Box sx={{ py: 2.5 }}>
        <Typography
          variant="overline"
          sx={{
            px: 3,
            py: 1,
            color: 'text.secondary',
            fontSize: '0.85rem',
            fontFamily: 'Cinzel, serif',
            letterSpacing: 1,
          }}
        >
          快捷入口
        </Typography>

        <List sx={{ px: 1 }}>
          {quickActions.map((item) => (
            <ListItem key={item.id} disablePadding sx={{ mb: 1.5 }}>
              <ListItemButton
                onClick={() => handleNavigate(item.path)}
                selected={isActive(item.path)}
                sx={{
                  borderRadius: 2,
                  mx: 1,
                  py: 1.5,
                  transition: 'all 0.3s ease',
                  '&.Mui-selected': {
                    background: 'linear-gradient(135deg, rgba(212, 175, 55, 0.2) 0%, rgba(212, 175, 55, 0.1) 100%)',
                    borderLeft: '3px solid',
                    borderLeftColor: 'primary.main',
                  },
                  '&:hover': {
                    background: 'rgba(212, 175, 55, 0.1)',
                    transform: 'translateX(4px)',
                  },
                }}
              >
                <ListItemIcon
                  sx={{
                    color: isActive(item.path) ? 'primary.main' : 'text.secondary',
                    minWidth: 44,
                  }}
                >
                  {item.icon}
                </ListItemIcon>
                <ListItemText
                  primary={item.label}
                  secondary={item.description}
                  primaryTypographyProps={{
                    fontSize: '1.05rem',
                    fontWeight: isActive(item.path) ? 600 : 400,
                    color: isActive(item.path) ? 'primary.main' : 'text.primary',
                  }}
                  secondaryTypographyProps={{
                    fontSize: '0.8rem',
                    color: 'text.secondary',
                  }}
                />
                {item.badge && (
                  <Chip
                    label={item.badge}
                    size="small"
                    sx={{
                      background: 'primary.main',
                      color: 'black',
                      fontSize: '0.7rem',
                      height: '20px',
                    }}
                  />
                )}
              </ListItemButton>
            </ListItem>
          ))}
        </List>
      </Box>

      <Box
        sx={{
          mt: 'auto',
          p: 2.5,
          background: 'linear-gradient(135deg, rgba(212, 175, 55, 0.05) 0%, transparent 100%)',
          borderTop: '1px solid rgba(212, 175, 55, 0.1)',
        }}
      >
        <Typography
          variant="body2"
          sx={{
            color: 'text.secondary',
            textAlign: 'center',
            display: 'block',
            fontStyle: 'italic',
          }}
        >
          “命运不是偶然，而是你每次选择的回响。”
        </Typography>
      </Box>
    </Box>
  );

  return (
    <>
      {!isMobile && (
        <Drawer
          variant="temporary"
          anchor="left"
          open={sidebarOpen}
          onClose={handleClose}
          transitionDuration={{ enter: 360, exit: 220 }}
          ModalProps={{
            keepMounted: true,
            hideBackdrop: true,
          }}
          sx={{
            '& .MuiDrawer-paper': {
              width: DESKTOP_DRAWER_WIDTH,
              boxSizing: 'border-box',
              border: 'none',
              boxShadow: '22px 0 56px rgba(0, 0, 0, 0.26)',
              backgroundImage: 'linear-gradient(180deg, rgba(26, 26, 46, 0.98) 0%, rgba(22, 33, 62, 0.98) 100%)',
              overflow: 'hidden',
            },
          }}
        >
          {drawerContent}
        </Drawer>
      )}

      {isMobile && (
        <Drawer
          variant="temporary"
          anchor="left"
          open={sidebarOpen}
          onClose={handleClose}
          ModalProps={{
            keepMounted: true,
          }}
          sx={{
            '& .MuiDrawer-paper': {
              width: MOBILE_DRAWER_WIDTH,
              boxSizing: 'border-box',
              border: 'none',
            },
          }}
        >
          {drawerContent}
        </Drawer>
      )}
    </>
  );
};

export default Sidebar;
