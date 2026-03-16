import React from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  IconButton,
  Typography,
  Box,
  Fade,
  styled,
  keyframes,
  Backdrop,
} from '@mui/material';
import { Close, AutoAwesome } from '@mui/icons-material';

const mysticalPulse = keyframes`
  0%, 100% {
    box-shadow: 0 0 20px rgba(212, 175, 55, 0.3);
  }
  50% {
    box-shadow: 0 0 40px rgba(212, 175, 55, 0.6), 0 0 60px rgba(212, 175, 55, 0.3);
  }
`;

const StyledDialog = styled(Dialog)(({ theme }) => ({
  '& .MuiDialog-paper': {
    background: `linear-gradient(135deg,
      rgba(26, 26, 46, 0.98) 0%,
      rgba(16, 33, 62, 0.98) 100%
    )`,
    backdropFilter: 'blur(20px)',
    border: `1px solid ${theme.palette.primary.main}40`,
    borderRadius: 16,
    minWidth: '300px',
    position: 'relative',
    overflow: 'hidden',

    '&::before': {
      content: '""',
      position: 'absolute',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      background: `
        radial-gradient(circle at 20% 20%, rgba(212, 175, 55, 0.1) 0%, transparent 50%),
        radial-gradient(circle at 80% 80%, rgba(106, 5, 114, 0.1) 0%, transparent 50%)
      `,
      pointerEvents: 'none',
      zIndex: 0,
    },

    '&.mystical': {
      animation: `${mysticalPulse} 3s ease-in-out infinite`,
    },
  },
}));

const StyledDialogTitle = styled(DialogTitle)(({ theme }) => ({
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'space-between',
  padding: theme.spacing(2, 3),
  background: `linear-gradient(90deg,
    transparent 0%,
    rgba(212, 175, 55, 0.1) 50%,
    transparent 100%
  )`,
  borderBottom: `1px solid ${theme.palette.primary.main}20`,
  position: 'relative',
  zIndex: 1,

  '& h2': {
    display: 'flex',
    alignItems: 'center',
    gap: theme.spacing(1),
    fontFamily: 'Cinzel, serif',
    fontSize: '1.25rem',
    fontWeight: 600,
    color: theme.palette.primary.main,
    textShadow: '0 0 10px rgba(212, 175, 55, 0.3)',
    margin: 0,
  },
}));

const StyledDialogContent = styled(DialogContent)(({ theme }) => ({
  padding: theme.spacing(3),
  position: 'relative',
  zIndex: 1,

  '&.scrollable': {
    maxHeight: '60vh',
    overflowY: 'auto',
    '&::-webkit-scrollbar': {
      width: '6px',
    },
    '&::-webkit-scrollbar-track': {
      background: 'rgba(255, 255, 255, 0.1)',
      borderRadius: '3px',
    },
    '&::-webkit-scrollbar-thumb': {
      background: theme.palette.primary.main,
      borderRadius: '3px',
      '&:hover': {
        background: theme.palette.primary.light,
      },
    },
  },
}));

const StyledDialogActions = styled(DialogActions)(({ theme }) => ({
  padding: theme.spacing(2, 3),
  borderTop: `1px solid ${theme.palette.primary.main}20`,
  background: `linear-gradient(90deg,
    transparent 0%,
    rgba(212, 175, 55, 0.05) 50%,
    transparent 100%
  )`,
  position: 'relative',
  zIndex: 1,
  gap: theme.spacing(1),
}));

const StyledBackdrop = styled(Backdrop)(({ theme }) => ({
  backgroundColor: 'rgba(10, 10, 15, 0.8)',
  backdropFilter: 'blur(8px)',
}));

export interface ModalProps {
  open: boolean;
  onClose: () => void;
  title?: string;
  children: React.ReactNode;
  actions?: React.ReactNode;
  maxWidth?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
  fullWidth?: boolean;
  fullScreen?: boolean;
  scrollable?: boolean;
  mystical?: boolean;
  closable?: boolean;
  className?: string;
  titleIcon?: React.ReactNode;
}

const Modal: React.FC<ModalProps> = ({
  open,
  onClose,
  title,
  children,
  actions,
  maxWidth = 'sm',
  fullWidth = true,
  fullScreen = false,
  scrollable = false,
  mystical = false,
  closable = true,
  className,
  titleIcon,
}) => {
  return (
    <StyledDialog
      open={open}
      onClose={closable ? onClose : undefined}
      maxWidth={maxWidth}
      fullWidth={fullWidth}
      fullScreen={fullScreen}
      className={`${mystical ? 'mystical' : ''} ${className || ''}`}
      TransitionComponent={Fade}
      TransitionProps={{
        timeout: 400,
      }}
      slots={{
        backdrop: StyledBackdrop,
      }}
      slotProps={{
        backdrop: {
          timeout: 500,
        },
      }}
      aria-labelledby="modal-title"
      aria-describedby="modal-content"
    >
      {title && (
        <StyledDialogTitle id="modal-title">
          <Typography component="h2">
            {titleIcon || (mystical && <AutoAwesome />)}
            {title}
          </Typography>
          {closable && (
            <IconButton
              onClick={onClose}
              sx={{
                color: 'text.secondary',
                transition: 'all 0.2s ease',
                '&:hover': {
                  color: 'primary.main',
                  transform: 'scale(1.1)',
                },
              }}
              aria-label="关闭"
            >
              <Close />
            </IconButton>
          )}
        </StyledDialogTitle>
      )}

      <StyledDialogContent
        id="modal-content"
        className={scrollable ? 'scrollable' : ''}
        dividers={scrollable}
      >
        {children}
      </StyledDialogContent>

      {actions && (
        <StyledDialogActions>
          {actions}
        </StyledDialogActions>
      )}
    </StyledDialog>
  );
};

// 预设的模态框组件
export const ConfirmModal: React.FC<{
  open: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title?: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  danger?: boolean;
}> = ({
  open,
  onClose,
  onConfirm,
  title = '确认操作',
  message,
  confirmText = '确认',
  cancelText = '取消',
  danger = false,
}) => {
  const handleConfirm = () => {
    onConfirm();
    onClose();
  };

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={title}
      maxWidth="xs"
      mystical={true}
      actions={
        <Box display="flex" gap={1}>
          <Box
            component="button"
            onClick={onClose}
            sx={{
              px: 3,
              py: 1,
              background: 'transparent',
              border: '1px solid',
              borderColor: 'text.secondary',
              borderRadius: 2,
              color: 'text.secondary',
              fontSize: '0.875rem',
              cursor: 'pointer',
              transition: 'all 0.2s ease',
              '&:hover': {
                borderColor: 'text.primary',
                color: 'text.primary',
              },
            }}
          >
            {cancelText}
          </Box>
          <Box
            component="button"
            onClick={handleConfirm}
            sx={{
              px: 3,
              py: 1,
              background: danger
                ? 'linear-gradient(45deg, #FF6B6B, #FF5252)'
                : 'linear-gradient(45deg, #D4AF37, #FFD700)',
              border: 'none',
              borderRadius: 2,
              color: danger ? 'white' : 'black',
              fontSize: '0.875rem',
              fontWeight: 600,
              cursor: 'pointer',
              transition: 'all 0.2s ease',
              '&:hover': {
                transform: 'translateY(-1px)',
                boxShadow: danger
                  ? '0 4px 16px rgba(255, 107, 107, 0.3)'
                  : '0 4px 16px rgba(212, 175, 55, 0.3)',
              },
            }}
          >
            {confirmText}
          </Box>
        </Box>
      }
    >
      <Typography
        variant="body1"
        sx={{
          color: 'text.primary',
          lineHeight: 1.6,
          textAlign: 'center',
          py: 2,
        }}
      >
        {message}
      </Typography>
    </Modal>
  );
};

export default Modal;