import React, { useEffect } from 'react';
import {
  Snackbar,
  Alert,
  AlertTitle,
  Slide,
  Box,
  IconButton,
  Typography,
  styled,
  keyframes,
} from '@mui/material';
import {
  CheckCircle,
  Error,
  Warning,
  Info,
  Close,
  AutoAwesome,
} from '@mui/icons-material';
import { create } from 'zustand';

const shimmer = keyframes`
  0% {
    background-position: -200px 0;
  }
  100% {
    background-position: calc(200px + 100%) 0;
  }
`;

const StyledAlert = styled(Alert)(({ theme, severity }) => ({
  borderRadius: 12,
  backdropFilter: 'blur(10px)',
  border: `1px solid ${
    severity === 'success' ? theme.palette.success.main :
    severity === 'error' ? theme.palette.error.main :
    severity === 'warning' ? theme.palette.warning.main :
    severity === 'info' ? theme.palette.primary.main :
    theme.palette.primary.main
  }40`,

  '& .MuiAlert-icon': {
    fontSize: '1.5rem',
  },

  '& .MuiAlert-message': {
    flexGrow: 1,
  },

  // 神秘风格特效
  '&.mystical': {
    background: `linear-gradient(135deg,
      rgba(26, 26, 46, 0.95) 0%,
      rgba(16, 33, 62, 0.95) 100%
    )`,
    position: 'relative',
    overflow: 'hidden',

    '&::before': {
      content: '""',
      position: 'absolute',
      top: 0,
      left: '-200px',
      width: '200px',
      height: '100%',
      background: `linear-gradient(
        90deg,
        transparent,
        rgba(212, 175, 55, 0.2),
        transparent
      )`,
      animation: `${shimmer} 2s infinite`,
    },
  },
}));

export type NotificationType = 'success' | 'error' | 'warning' | 'info' | 'mystical';

export interface NotificationData {
  id: string;
  type: NotificationType;
  title?: string;
  message: string;
  duration?: number;
  persistent?: boolean;
  action?: {
    label: string;
    onClick: () => void;
  };
}

interface NotificationStore {
  notifications: NotificationData[];
  addNotification: (notification: Omit<NotificationData, 'id'>) => string;
  removeNotification: (id: string) => void;
  clearAll: () => void;
}

// Zustand store for notifications
export const useNotificationStore = create<NotificationStore>((set, get) => ({
  notifications: [],

  addNotification: (notification) => {
    const id = `notification_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    const newNotification: NotificationData = {
      ...notification,
      id,
      duration: notification.duration ?? (notification.persistent ? undefined : 5000),
    };

    set(state => ({
      notifications: [...state.notifications, newNotification],
    }));

    return id;
  },

  removeNotification: (id) => {
    set(state => ({
      notifications: state.notifications.filter(n => n.id !== id),
    }));
  },

  clearAll: () => {
    set({ notifications: [] });
  },
}));

// Notification hook for easy usage
export const useNotification = () => {
  const { addNotification, removeNotification, clearAll } = useNotificationStore();

  const showSuccess = (message: string, options?: Partial<NotificationData>) =>
    addNotification({ type: 'success', message, ...options });

  const showError = (message: string, options?: Partial<NotificationData>) =>
    addNotification({ type: 'error', message, ...options });

  const showWarning = (message: string, options?: Partial<NotificationData>) =>
    addNotification({ type: 'warning', message, ...options });

  const showInfo = (message: string, options?: Partial<NotificationData>) =>
    addNotification({ type: 'info', message, ...options });

  const showMystical = (message: string, options?: Partial<NotificationData>) =>
    addNotification({ type: 'mystical', message, ...options });

  return {
    showSuccess,
    showError,
    showWarning,
    showInfo,
    showMystical,
    remove: removeNotification,
    clearAll,
  };
};

interface NotificationItemProps {
  notification: NotificationData;
  onClose: (id: string) => void;
}

const NotificationItem: React.FC<NotificationItemProps> = ({ notification, onClose }) => {
  const { id, type, title, message, duration, action } = notification;

  useEffect(() => {
    if (duration) {
      const timer = setTimeout(() => {
        onClose(id);
      }, duration);
      return () => clearTimeout(timer);
    }
  }, [id, duration, onClose]);

  const getIcon = () => {
    switch (type) {
      case 'success':
        return <CheckCircle />;
      case 'error':
        return <Error />;
      case 'warning':
        return <Warning />;
      case 'info':
        return <Info />;
      case 'mystical':
        return <AutoAwesome />;
      default:
        return <Info />;
    }
  };

  const getSeverity = () => {
    if (type === 'mystical') return 'info';
    return type;
  };

  return (
    <Snackbar
      key={id}
      open={true}
      anchorOrigin={{ vertical: 'top', horizontal: 'right' }}
      TransitionComponent={Slide}
      TransitionProps={{ direction: 'left' } as any}
      sx={{
        position: 'relative',
        '& .MuiSnackbar-root': {
          position: 'relative',
        },
      }}
    >
      <StyledAlert
        severity={getSeverity()}
        icon={getIcon()}
        className={type === 'mystical' ? 'mystical' : ''}
        action={
          <Box display="flex" alignItems="center" gap={1}>
            {action && (
              <Typography
                variant="body2"
                component="button"
                onClick={action.onClick}
                sx={{
                  background: 'none',
                  border: 'none',
                  color: 'primary.main',
                  cursor: 'pointer',
                  fontFamily: 'inherit',
                  fontSize: '0.875rem',
                  fontWeight: 600,
                  textDecoration: 'underline',
                  '&:hover': {
                    color: 'primary.light',
                  },
                }}
              >
                {action.label}
              </Typography>
            )}
            <IconButton
              size="small"
              onClick={() => onClose(id)}
              sx={{
                color: 'inherit',
                opacity: 0.7,
                '&:hover': {
                  opacity: 1,
                },
              }}
            >
              <Close fontSize="small" />
            </IconButton>
          </Box>
        }
      >
        <Box>
          {title && (
            <AlertTitle
              sx={{
                fontFamily: 'Cinzel, serif',
                fontSize: '0.925rem',
                fontWeight: 600,
                mb: title ? 0.5 : 0,
              }}
            >
              {title}
            </AlertTitle>
          )}
          <Typography
            variant="body2"
            sx={{
              fontSize: '0.875rem',
              lineHeight: 1.4,
              opacity: 0.95,
            }}
          >
            {message}
          </Typography>
        </Box>
      </StyledAlert>
    </Snackbar>
  );
};

const NotificationContainer: React.FC = () => {
  const { notifications, removeNotification } = useNotificationStore();

  return (
    <Box
      sx={{
        position: 'fixed',
        top: 80,
        right: 16,
        zIndex: 10000,
        display: 'flex',
        flexDirection: 'column',
        gap: 1,
        maxWidth: '400px',
      }}
    >
      {notifications.map((notification) => (
        <NotificationItem
          key={notification.id}
          notification={notification}
          onClose={removeNotification}
        />
      ))}
    </Box>
  );
};

export default NotificationContainer;