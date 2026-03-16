import React from 'react';
import {
  Box,
  LinearProgress,
  Typography,
  styled,
  keyframes,
} from '@mui/material';

const mysticalShimmer = keyframes`
  0% {
    background-position: -200px 0;
  }
  100% {
    background-position: calc(200px + 100%) 0;
  }
`;

const pulseGlow = keyframes`
  0%, 100% {
    box-shadow: 0 0 5px rgba(212, 175, 55, 0.3);
  }
  50% {
    box-shadow: 0 0 20px rgba(212, 175, 55, 0.6), 0 0 30px rgba(212, 175, 55, 0.3);
  }
`;

const StyledLinearProgress = styled(LinearProgress)(({ theme, variant }) => ({
  height: 8,
  borderRadius: 4,
  backgroundColor: 'rgba(255, 255, 255, 0.1)',
  overflow: 'hidden',
  position: 'relative',

  '& .MuiLinearProgress-bar': {
    borderRadius: 4,
    background: `linear-gradient(90deg,
      ${theme.palette.primary.dark} 0%,
      ${theme.palette.primary.main} 50%,
      ${theme.palette.primary.light} 100%
    )`,
    position: 'relative',

    '&::after': {
      content: '""',
      position: 'absolute',
      top: 0,
      left: 0,
      bottom: 0,
      right: 0,
      background: `linear-gradient(
        90deg,
        transparent,
        rgba(255, 255, 255, 0.4),
        transparent
      )`,
      animation: variant === 'indeterminate'
        ? `${mysticalShimmer} 1.5s infinite linear`
        : 'none',
    },
  },

  '&.mystical': {
    backgroundColor: 'rgba(106, 5, 114, 0.2)',
    animation: `${pulseGlow} 2s ease-in-out infinite`,

    '& .MuiLinearProgress-bar': {
      background: `linear-gradient(90deg,
        #6A0572 0%,
        #D4AF37 50%,
        #AB83A1 100%
      )`,
    },
  },

  '&.cosmic': {
    height: 12,
    backgroundColor: 'rgba(45, 27, 105, 0.3)',
    border: '1px solid rgba(212, 175, 55, 0.3)',

    '& .MuiLinearProgress-bar': {
      background: `linear-gradient(90deg,
        #2D1B69 0%,
        #D4AF37 25%,
        #6A0572 50%,
        #AB83A1 75%,
        #D4AF37 100%
      )`,
      backgroundSize: '200% 100%',
      animation: `${mysticalShimmer} 3s ease-in-out infinite`,
    },
  },
}));

const StyledCircularProgress = styled(Box)(({ theme }) => ({
  position: 'relative',
  display: 'inline-flex',
  alignItems: 'center',
  justifyContent: 'center',

  '& .circle-bg': {
    fill: 'none',
    stroke: 'rgba(255, 255, 255, 0.1)',
    strokeWidth: 3,
  },

  '& .circle-progress': {
    fill: 'none',
    strokeWidth: 3,
    strokeLinecap: 'round',
    transform: 'rotate(-90deg)',
    transformOrigin: '50% 50%',
    transition: 'stroke-dasharray 0.3s ease',
    stroke: 'url(#progressGradient)',
  },

  '&.mystical .circle-progress': {
    stroke: 'url(#mysticalGradient)',
    filter: 'drop-shadow(0 0 3px rgba(212, 175, 55, 0.5))',
  },
}));

export interface ProgressBarProps {
  value?: number;
  variant?: 'determinate' | 'indeterminate';
  size?: 'small' | 'medium' | 'large';
  style?: 'default' | 'mystical' | 'cosmic';
  type?: 'linear' | 'circular';
  showLabel?: boolean;
  label?: string;
  color?: 'primary' | 'secondary' | 'success' | 'error' | 'warning';
  className?: string;
  thickness?: number;
}

const ProgressBar: React.FC<ProgressBarProps> = ({
  value = 0,
  variant = 'determinate',
  size = 'medium',
  style = 'default',
  type = 'linear',
  showLabel = false,
  label,
  color = 'primary',
  className,
  thickness,
}) => {
  const getHeight = () => {
    if (thickness) return thickness;
    switch (size) {
      case 'small': return 4;
      case 'large': return 12;
      default: return 8;
    }
  };

  const getCircularSize = () => {
    switch (size) {
      case 'small': return 40;
      case 'large': return 80;
      default: return 60;
    }
  };

  const normalizedValue = Math.min(Math.max(value, 0), 100);
  const circularSize = getCircularSize();
  const radius = (circularSize - 8) / 2;
  const circumference = 2 * Math.PI * radius;
  const strokeDasharray = circumference;
  const strokeDashoffset = variant === 'determinate'
    ? circumference - (normalizedValue / 100) * circumference
    : 0;

  if (type === 'circular') {
    return (
      <Box
        sx={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          gap: 1,
        }}
        className={className}
      >
        <StyledCircularProgress
          sx={{ width: circularSize, height: circularSize }}
          className={style === 'mystical' ? 'mystical' : ''}
        >
          <svg width={circularSize} height={circularSize}>
            <defs>
              <linearGradient id="progressGradient" x1="0%" y1="0%" x2="100%" y2="0%">
                <stop offset="0%" stopColor="#D4AF37" />
                <stop offset="100%" stopColor="#FFD700" />
              </linearGradient>
              <linearGradient id="mysticalGradient" x1="0%" y1="0%" x2="100%" y2="0%">
                <stop offset="0%" stopColor="#6A0572" />
                <stop offset="50%" stopColor="#D4AF37" />
                <stop offset="100%" stopColor="#AB83A1" />
              </linearGradient>
            </defs>
            <circle
              className="circle-bg"
              cx={circularSize / 2}
              cy={circularSize / 2}
              r={radius}
            />
            <circle
              className="circle-progress"
              cx={circularSize / 2}
              cy={circularSize / 2}
              r={radius}
              strokeDasharray={strokeDasharray}
              strokeDashoffset={strokeDashoffset}
            />
          </svg>

          {(showLabel || label) && (
            <Box
              sx={{
                position: 'absolute',
                top: '50%',
                left: '50%',
                transform: 'translate(-50%, -50%)',
                textAlign: 'center',
              }}
            >
              <Typography
                variant="body2"
                sx={{
                  color: 'text.primary',
                  fontWeight: 600,
                  fontSize: size === 'small' ? '0.75rem' : size === 'large' ? '1.125rem' : '0.875rem',
                }}
              >
                {label || `${Math.round(normalizedValue)}%`}
              </Typography>
            </Box>
          )}
        </StyledCircularProgress>
      </Box>
    );
  }

  return (
    <Box className={className}>
      {(showLabel || label) && (
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            mb: 1,
          }}
        >
          <Typography
            variant="body2"
            sx={{
              color: 'text.primary',
              fontSize: size === 'small' ? '0.75rem' : size === 'large' ? '1rem' : '0.875rem',
              fontFamily: style === 'mystical' ? 'Cinzel, serif' : 'inherit',
            }}
          >
            {label || '进度'}
          </Typography>
          <Typography
            variant="body2"
            sx={{
              color: 'primary.main',
              fontWeight: 600,
              fontSize: size === 'small' ? '0.75rem' : size === 'large' ? '1rem' : '0.875rem',
            }}
          >
            {variant === 'determinate' ? `${Math.round(normalizedValue)}%` : ''}
          </Typography>
        </Box>
      )}

      <StyledLinearProgress
        variant={variant}
        value={variant === 'determinate' ? normalizedValue : undefined}
        className={style}
        sx={{
          height: getHeight(),
          ...(color !== 'primary' && {
            '& .MuiLinearProgress-bar': {
              backgroundColor: `${color}.main`,
            },
          }),
        }}
      />
    </Box>
  );
};

// 预设组件
export const MysticalProgress: React.FC<Omit<ProgressBarProps, 'style'>> = (props) => (
  <ProgressBar {...props} style="mystical" />
);

export const CosmicProgress: React.FC<Omit<ProgressBarProps, 'style'>> = (props) => (
  <ProgressBar {...props} style="cosmic" />
);

export const CircularProgress: React.FC<Omit<ProgressBarProps, 'type'>> = (props) => (
  <ProgressBar {...props} type="circular" />
);

export default ProgressBar;