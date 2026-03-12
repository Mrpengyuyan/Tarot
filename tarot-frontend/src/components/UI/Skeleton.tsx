import React from 'react';
import {
  Box,
  Skeleton as MuiSkeleton,
  styled,
  keyframes,
} from '@mui/material';

const mysticalWave = keyframes`
  0% {
    transform: translateX(-100%);
  }
  50% {
    transform: translateX(100%);
  }
  100% {
    transform: translateX(100%);
  }
`;

const shimmerGlow = keyframes`
  0%, 100% {
    opacity: 0.1;
  }
  50% {
    opacity: 0.3;
  }
`;

const StyledSkeleton = styled(MuiSkeleton)(({ theme, variant }) => ({
  backgroundColor: 'rgba(255, 255, 255, 0.05)',
  position: 'relative',
  overflow: 'hidden',

  '&::after': {
    content: '""',
    position: 'absolute',
    top: 0,
    right: 0,
    bottom: 0,
    left: 0,
    transform: 'translateX(-100%)',
    background: `linear-gradient(
      90deg,
      transparent,
      rgba(212, 175, 55, 0.2),
      transparent
    )`,
    animation: `${mysticalWave} 2s infinite`,
  },

  '&.mystical': {
    backgroundColor: 'rgba(106, 5, 114, 0.1)',
    border: '1px solid rgba(212, 175, 55, 0.1)',

    '&::after': {
      background: `linear-gradient(
        90deg,
        transparent,
        rgba(212, 175, 55, 0.4),
        rgba(171, 131, 161, 0.3),
        transparent
      )`,
    },

    '&::before': {
      content: '""',
      position: 'absolute',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      background: `
        radial-gradient(circle at 20% 50%, rgba(212, 175, 55, 0.1) 0%, transparent 50%),
        radial-gradient(circle at 80% 50%, rgba(106, 5, 114, 0.1) 0%, transparent 50%)
      `,
      animation: `${shimmerGlow} 3s ease-in-out infinite`,
      pointerEvents: 'none',
    },
  },

  '&.cosmic': {
    backgroundColor: 'rgba(45, 27, 105, 0.1)',
    backgroundImage: `
      repeating-linear-gradient(
        45deg,
        transparent,
        transparent 10px,
        rgba(212, 175, 55, 0.05) 10px,
        rgba(212, 175, 55, 0.05) 20px
      )
    `,

    '&::after': {
      background: `linear-gradient(
        90deg,
        transparent,
        rgba(212, 175, 55, 0.3),
        rgba(45, 27, 105, 0.3),
        transparent
      )`,
      animation: `${mysticalWave} 2.5s infinite`,
    },
  },
}));

export interface SkeletonProps {
  variant?: 'text' | 'rectangular' | 'rounded' | 'circular';
  width?: number | string;
  height?: number | string;
  style?: 'default' | 'mystical' | 'cosmic';
  className?: string;
  animation?: 'pulse' | 'wave' | false;
  sx?: any;
}

const Skeleton: React.FC<SkeletonProps> = ({
  variant = 'text',
  width,
  height,
  style = 'default',
  className,
  animation = 'wave',
  sx,
}) => {
  return (
    <StyledSkeleton
      variant={variant}
      width={width}
      height={height}
      animation={animation}
      className={`${style} ${className || ''}`}
      sx={sx}
    />
  );
};

// 预设组件
export const CardSkeleton: React.FC<{
  style?: 'default' | 'mystical' | 'cosmic';
  width?: number | string;
  height?: number | string;
}> = ({
  style = 'mystical',
  width = '100%',
  height = 280,
}) => (
  <Box
    sx={{
      p: 2,
      border: '1px solid rgba(212, 175, 55, 0.1)',
      borderRadius: 2,
      backgroundColor: 'rgba(26, 26, 46, 0.3)',
    }}
  >
    <Skeleton
      variant="rectangular"
      width={width}
      height={height}
      style={style}
      sx={{ mb: 2, borderRadius: 1 }}
    />
    <Skeleton
      variant="text"
      width="80%"
      height={24}
      style={style}
      sx={{ mb: 1 }}
    />
    <Skeleton
      variant="text"
      width="60%"
      height={20}
      style={style}
    />
  </Box>
);

export const TarotCardSkeleton: React.FC<{
  style?: 'default' | 'mystical' | 'cosmic';
}> = ({ style = 'cosmic' }) => (
  <Box
    sx={{
      width: 160,
      height: 280,
      borderRadius: 2,
      overflow: 'hidden',
      border: '2px solid rgba(212, 175, 55, 0.2)',
      background: 'linear-gradient(135deg, rgba(26, 26, 46, 0.8) 0%, rgba(16, 33, 62, 0.8) 100%)',
      p: 1,
    }}
  >
    <Skeleton
      variant="rectangular"
      width="100%"
      height="70%"
      style={style}
      sx={{
        mb: 1,
        borderRadius: 1,
      }}
    />
    <Skeleton
      variant="text"
      width="100%"
      height={16}
      style={style}
      sx={{ mb: 0.5 }}
    />
    <Skeleton
      variant="text"
      width="70%"
      height={14}
      style={style}
    />
  </Box>
);

export const ProfileSkeleton: React.FC<{
  style?: 'default' | 'mystical' | 'cosmic';
}> = ({ style = 'mystical' }) => (
  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, p: 2 }}>
    <Skeleton
      variant="circular"
      width={64}
      height={64}
      style={style}
    />
    <Box sx={{ flexGrow: 1 }}>
      <Skeleton
        variant="text"
        width="60%"
        height={24}
        style={style}
        sx={{ mb: 0.5 }}
      />
      <Skeleton
        variant="text"
        width="40%"
        height={20}
        style={style}
        sx={{ mb: 0.5 }}
      />
      <Skeleton
        variant="text"
        width="80%"
        height={16}
        style={style}
      />
    </Box>
  </Box>
);

export const ListSkeleton: React.FC<{
  items?: number;
  style?: 'default' | 'mystical' | 'cosmic';
  showAvatar?: boolean;
}> = ({
  items = 5,
  style = 'default',
  showAvatar = false,
}) => (
  <Box>
    {Array.from({ length: items }).map((_, index) => (
      <Box
        key={index}
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 2,
          p: 2,
          borderBottom: '1px solid rgba(255, 255, 255, 0.1)',
        }}
      >
        {showAvatar && (
          <Skeleton
            variant="circular"
            width={40}
            height={40}
            style={style}
          />
        )}
        <Box sx={{ flexGrow: 1 }}>
          <Skeleton
            variant="text"
            width={`${60 + Math.random() * 30}%`}
            height={20}
            style={style}
            sx={{ mb: 0.5 }}
          />
          <Skeleton
            variant="text"
            width={`${40 + Math.random() * 40}%`}
            height={16}
            style={style}
          />
        </Box>
        <Skeleton
          variant="rectangular"
          width={60}
          height={24}
          style={style}
          sx={{ borderRadius: 1 }}
        />
      </Box>
    ))}
  </Box>
);

export const TableSkeleton: React.FC<{
  rows?: number;
  columns?: number;
  style?: 'default' | 'mystical' | 'cosmic';
}> = ({
  rows = 5,
  columns = 4,
  style = 'default',
}) => (
  <Box>
    {/* Header */}
    <Box
      sx={{
        display: 'grid',
        gridTemplateColumns: `repeat(${columns}, 1fr)`,
        gap: 2,
        p: 2,
        borderBottom: '2px solid rgba(212, 175, 55, 0.2)',
      }}
    >
      {Array.from({ length: columns }).map((_, index) => (
        <Skeleton
          key={index}
          variant="text"
          height={24}
          style={style}
        />
      ))}
    </Box>

    {/* Rows */}
    {Array.from({ length: rows }).map((_, rowIndex) => (
      <Box
        key={rowIndex}
        sx={{
          display: 'grid',
          gridTemplateColumns: `repeat(${columns}, 1fr)`,
          gap: 2,
          p: 2,
          borderBottom: '1px solid rgba(255, 255, 255, 0.1)',
        }}
      >
        {Array.from({ length: columns }).map((_, colIndex) => (
          <Skeleton
            key={colIndex}
            variant="text"
            height={20}
            style={style}
            width={`${70 + Math.random() * 20}%`}
          />
        ))}
      </Box>
    ))}
  </Box>
);

export default Skeleton;