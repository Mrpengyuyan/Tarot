import React from 'react';
import {
  Box,
  CircularProgress,
  Typography,
  keyframes,
  styled,
} from '@mui/material';

const mysticalFloat = keyframes`
  0%, 100% {
    transform: translateY(0px) rotate(0deg);
  }
  25% {
    transform: translateY(-10px) rotate(90deg);
  }
  50% {
    transform: translateY(-20px) rotate(180deg);
  }
  75% {
    transform: translateY(-10px) rotate(270deg);
  }
`;

const mysticalGlow = keyframes`
  0%, 100% {
    box-shadow: 0 0 20px rgba(212, 175, 55, 0.3);
  }
  50% {
    box-shadow: 0 0 40px rgba(212, 175, 55, 0.6), 0 0 60px rgba(212, 175, 55, 0.3);
  }
`;

const StyledCircularProgress = styled(CircularProgress)(({ theme }) => ({
  animation: `${mysticalFloat} 3s ease-in-out infinite`,
  '& .MuiCircularProgress-circle': {
    strokeLinecap: 'round',
    stroke: 'url(#gradient)',
  },
}));

const LoadingContainer = styled(Box)(({ theme }) => ({
  display: 'flex',
  flexDirection: 'column',
  alignItems: 'center',
  justifyContent: 'center',
  gap: theme.spacing(3),
  padding: theme.spacing(4),
  textAlign: 'center',
  position: 'relative',

  '&::before': {
    content: '""',
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: '120px',
    height: '120px',
    borderRadius: '50%',
    background: 'radial-gradient(circle, rgba(212, 175, 55, 0.1) 0%, transparent 70%)',
    animation: `${mysticalGlow} 2s ease-in-out infinite`,
    zIndex: 0,
  },
}));

export interface LoadingProps {
  message?: string;
  size?: 'small' | 'medium' | 'large';
  variant?: 'mystical' | 'simple' | 'cosmic';
  fullScreen?: boolean;
  className?: string;
}

const Loading: React.FC<LoadingProps> = ({
  message = '神秘力量正在汇聚...',
  size = 'medium',
  variant = 'mystical',
  fullScreen = false,
  className,
}) => {
  const getSize = () => {
    switch (size) {
      case 'small': return 32;
      case 'large': return 64;
      default: return 48;
    }
  };

  const containerProps = fullScreen ? {
    position: 'fixed' as const,
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(10, 10, 15, 0.9)',
    backdropFilter: 'blur(10px)',
    zIndex: 9999,
  } : {
    minHeight: '200px',
  };

  const renderMysticalVariant = () => (
    <LoadingContainer sx={containerProps} className={className}>
      <Box position="relative" zIndex={1}>
        <svg width={0} height={0}>
          <defs>
            <linearGradient id="gradient" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stopColor="#D4AF37" />
              <stop offset="50%" stopColor="#FFD700" />
              <stop offset="100%" stopColor="#B8860B" />
            </linearGradient>
          </defs>
        </svg>

        <StyledCircularProgress
          size={getSize()}
          thickness={3}
        />
      </Box>

      <Typography
        variant="body1"
        sx={{
          color: 'text.secondary',
          fontFamily: 'Cinzel, serif',
          fontSize: size === 'small' ? '0.75rem' : size === 'large' ? '1.125rem' : '0.875rem',
          opacity: 0.9,
          zIndex: 1,
          position: 'relative',
        }}
      >
        {message}
      </Typography>
    </LoadingContainer>
  );

  const renderSimpleVariant = () => (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 2,
        p: 2,
        ...containerProps,
      }}
      className={className}
    >
      <CircularProgress
        size={getSize()}
        sx={{ color: 'primary.main' }}
      />
      {message && (
        <Typography variant="body2" color="text.secondary">
          {message}
        </Typography>
      )}
    </Box>
  );

  const renderCosmicVariant = () => (
    <LoadingContainer sx={containerProps} className={className}>
      <Box position="relative">
        {/* 外圈 */}
        <CircularProgress
          size={getSize() + 20}
          thickness={1}
          sx={{
            color: 'primary.main',
            opacity: 0.3,
            position: 'absolute',
            top: -10,
            left: -10,
            animation: `${mysticalFloat} 4s ease-in-out infinite reverse`,
          }}
        />
        {/* 中圈 */}
        <CircularProgress
          size={getSize() + 10}
          thickness={2}
          sx={{
            color: 'secondary.main',
            opacity: 0.5,
            position: 'absolute',
            top: -5,
            left: -5,
            animation: `${mysticalFloat} 3s ease-in-out infinite`,
          }}
        />
        {/* 内圈 */}
        <StyledCircularProgress
          size={getSize()}
          thickness={3}
        />
      </Box>

      <Typography
        variant="h6"
        sx={{
          color: 'primary.main',
          fontFamily: 'Cinzel, serif',
          fontSize: size === 'small' ? '0.875rem' : size === 'large' ? '1.25rem' : '1rem',
          fontWeight: 600,
          textShadow: '0 0 10px rgba(212, 175, 55, 0.5)',
          zIndex: 1,
          position: 'relative',
        }}
      >
        {message}
      </Typography>
    </LoadingContainer>
  );

  switch (variant) {
    case 'simple':
      return renderSimpleVariant();
    case 'cosmic':
      return renderCosmicVariant();
    default:
      return renderMysticalVariant();
  }
};

export default Loading;