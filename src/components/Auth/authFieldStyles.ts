import { SxProps, Theme } from '@mui/material/styles';

export const AUTH_TEXT_FIELD_SX: SxProps<Theme> = {
  '& .MuiOutlinedInput-root': {
    position: 'relative',
    isolation: 'isolate',
    backgroundColor: 'rgba(10, 5, 24, 0.58)',
    borderRadius: '14px',
    fontSize: '1.1rem',
    color: '#fff',
    transition: 'background-color 90ms ease, border-color 90ms ease',
    willChange: 'background-color, opacity',
    transform: 'translateZ(0)',
    backfaceVisibility: 'hidden',
    contain: 'layout paint style',
    '&::after': {
      content: '""',
      position: 'absolute',
      inset: -1,
      borderRadius: '14px',
      pointerEvents: 'none',
      opacity: 0,
      border: '1px solid rgba(212, 175, 55, 0.18)',
      boxShadow: '0 0 10px rgba(212, 175, 55, 0.08)',
      transition: 'opacity 90ms ease',
      willChange: 'opacity',
    },
    '& fieldset': {
      borderWidth: '1.5px',
      borderColor: 'rgba(212, 175, 55, 0.28)',
      borderRadius: '14px',
      transition: 'border-color 90ms ease',
    },
    '&:hover': {
      backgroundColor: 'rgba(14, 8, 30, 0.64)',
      '& fieldset': {
        borderColor: 'rgba(212, 175, 55, 0.46)',
      },
    },
    '&.Mui-focused': {
      backgroundColor: 'rgba(16, 8, 36, 0.68)',
      '&::after': {
        opacity: 1,
      },
      '& fieldset': {
        borderWidth: '1.5px',
        borderColor: '#D4AF37',
      },
    },
    '& input': {
      color: '#FFFFFFEE',
      fontSize: '1.1rem',
      fontWeight: 400,
      letterSpacing: '0.02em',
      transform: 'translateZ(0)',
      contain: 'content',
      '&::placeholder': {
        color: 'rgba(255, 255, 255, 0.45)',
      },
    },
  },
  '& .MuiInputLabel-root': {
    color: 'rgba(212, 175, 55, 0.78)',
    fontSize: '1.14rem',
    fontWeight: 600,
    letterSpacing: '0.03em',
    transition: 'color 180ms ease',
    '&.Mui-focused': {
      color: '#D4AF37',
    },
  },
  '& .MuiFormHelperText-root': {
    marginLeft: 0.5,
    color: 'rgba(220, 214, 235, 0.72)',
  },
};
