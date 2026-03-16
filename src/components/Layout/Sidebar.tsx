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
  Person,
  MenuBook,
  Timeline,
  Star,
} from '@mui/icons-material';
import { useNavigate, useLocation } from 'react-router-dom';
import { useUiStore } from '../../stores/uiStore';
import { useAuthStore } from '../../stores/authStore';
import { ROUTES } from '../../routes/routeConfig';

// 导航菜单项配置
interface NavItem {
  id: string;
  label: string;
  path: string;
  icon: React.ReactNode;
  badge?: string;
  description?: string;
}

const navItems: NavItem[] = [
  {
    id: 'dashboard',
    label: '主控台',
    path: ROUTES.DASHBOARD,
    icon: <Dashboard />,
    description: '总览您的塔罗之旅',
  },
  {
    id: 'new-reading',
    label: '新占卜',
    path: ROUTES.NEW_READING,
    icon: <AutoAwesome />,
    description: '开始一次神秘的占卜',
  },
  {
    id: 'history',
    label: '占卜历史',
    path: ROUTES.HISTORY,
    icon: <History />,
    description: '查看过往的预言',
  },
  {
    id: 'profile',
    label: '个人中心',
    path: ROUTES.PROFILE,
    icon: <Person />,
    description: '管理账户设置',
  },
];

// 快捷功能菜单
const quickActions: NavItem[] = [
  {
    id: 'cards',
    label: '塔罗牌库',
    path: '/cards',
    icon: <MenuBook />,
    description: '探索78张塔罗牌',
  },
  {
    id: 'spreads',
    label: '牌阵大全',
    path: '/spreads',
    icon: <Timeline />,
    description: '学习各种牌阵',
  },
  {
    id: 'favorites',
    label: '收藏夹',
    path: '/favorites',
    icon: <Star />,
    badge: '3',
    description: '您收藏的内容',
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

  // 检查当前路径是否激活
  const isActive = (path: string) => {
    if (path === ROUTES.DASHBOARD) {
      return location.pathname === path;
    }
    return location.pathname.startsWith(path);
  };

  const drawerContent = (
    <Box
      sx={{
        width: 280,
        height: '100%',
        background: 'linear-gradient(180deg, #1A1A2E 0%, #16213E 100%)',
        borderRight: '1px solid rgba(212, 175, 55, 0.2)',
        overflow: 'hidden',
      }}
    >
      {/* 侧边栏头部 */}
      <Box
        sx={{
          p: 3,
          borderBottom: '1px solid rgba(212, 175, 55, 0.2)',
          background: 'linear-gradient(135deg, rgba(212, 175, 55, 0.1) 0%, transparent 100%)',
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
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
            神秘导航
          </Typography>
        </Box>

        {/* 用户信息卡片 */}
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
            欢迎回来
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

      {/* 主导航菜单 */}
      <Box sx={{ py: 1 }}>
        <Typography
          variant="overline"
          sx={{
            px: 3,
            py: 1,
            color: 'text.secondary',
            fontSize: '0.75rem',
            fontFamily: 'Cinzel, serif',
            letterSpacing: 1,
          }}
        >
          主要功能
        </Typography>

        <List sx={{ px: 1 }}>
          {navItems.map((item) => (
            <ListItem key={item.id} disablePadding sx={{ mb: 0.5 }}>
              <ListItemButton
                onClick={() => handleNavigate(item.path)}
                selected={isActive(item.path)}
                sx={{
                  borderRadius: 2,
                  mx: 1,
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
                    minWidth: 40,
                  }}
                >
                  {item.icon}
                </ListItemIcon>
                <ListItemText
                  primary={item.label}
                  secondary={item.description}
                  primaryTypographyProps={{
                    fontSize: '0.9rem',
                    fontWeight: isActive(item.path) ? 600 : 400,
                    color: isActive(item.path) ? 'primary.main' : 'text.primary',
                  }}
                  secondaryTypographyProps={{
                    fontSize: '0.75rem',
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

      <Divider sx={{ mx: 2, borderColor: 'rgba(212, 175, 55, 0.2)' }} />

      {/* 快捷功能菜单 */}
      <Box sx={{ py: 1 }}>
        <Typography
          variant="overline"
          sx={{
            px: 3,
            py: 1,
            color: 'text.secondary',
            fontSize: '0.75rem',
            fontFamily: 'Cinzel, serif',
            letterSpacing: 1,
          }}
        >
          探索更多
        </Typography>

        <List sx={{ px: 1 }}>
          {quickActions.map((item) => (
            <ListItem key={item.id} disablePadding sx={{ mb: 0.5 }}>
              <ListItemButton
                onClick={() => handleNavigate(item.path)}
                selected={isActive(item.path)}
                sx={{
                  borderRadius: 2,
                  mx: 1,
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
                    minWidth: 40,
                  }}
                >
                  {item.icon}
                </ListItemIcon>
                <ListItemText
                  primary={item.label}
                  secondary={item.description}
                  primaryTypographyProps={{
                    fontSize: '0.85rem',
                    fontWeight: isActive(item.path) ? 600 : 400,
                    color: isActive(item.path) ? 'primary.main' : 'text.primary',
                  }}
                  secondaryTypographyProps={{
                    fontSize: '0.7rem',
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

      {/* 底部装饰 */}
      <Box
        sx={{
          position: 'absolute',
          bottom: 0,
          left: 0,
          right: 0,
          p: 2,
          background: 'linear-gradient(135deg, rgba(212, 175, 55, 0.05) 0%, transparent 100%)',
          borderTop: '1px solid rgba(212, 175, 55, 0.1)',
        }}
      >
        <Typography
          variant="caption"
          sx={{
            color: 'text.secondary',
            textAlign: 'center',
            display: 'block',
            fontStyle: 'italic',
          }}
        >
          "星辰指引着我们的道路"
        </Typography>
      </Box>
    </Box>
  );

  return (
    <>
      {/* 桌面端持久侧边栏 */}
      {!isMobile && (
        <Drawer
          variant="persistent"
          anchor="left"
          open={sidebarOpen}
          sx={{
            width: sidebarOpen ? 280 : 0,
            flexShrink: 0,
            '& .MuiDrawer-paper': {
              width: 280,
              boxSizing: 'border-box',
              border: 'none',
            },
          }}
        >
          {drawerContent}
        </Drawer>
      )}

      {/* 移动端临时侧边栏 */}
      {isMobile && (
        <Drawer
          variant="temporary"
          anchor="left"
          open={sidebarOpen}
          onClose={handleClose}
          ModalProps={{
            keepMounted: true, // 更好的移动端性能
          }}
          sx={{
            '& .MuiDrawer-paper': {
              width: 280,
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