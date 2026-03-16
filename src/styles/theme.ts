import { createTheme } from '@mui/material/styles';

const tarotColors = {
  primary: {
    main: '#D4AF37', // Gold
    light: '#FFEAA7',
    dark: '#9A7B2C',
    contrastText: '#0A0A0F',
  },
  secondary: {
    main: '#00F0FF', // Glowing Cyan
    light: '#80F8FF',
    dark: '#00A8B3',
    contrastText: '#0A0A0F',
  },
  background: {
    default: '#0A0512', // Deepest cosmic purple/black
    paper: 'rgba(26, 11, 46, 0.7)', // Translucent dark purple
    card: 'rgba(22, 10, 40, 0.85)',
  },
  text: {
    primary: '#E8E8E8',
    secondary: '#A399B2',
    disabled: '#5C546A',
  },
  error: { main: '#FF3366' },
  warning: { main: '#FFB84D' },
  success: { main: '#00E676' },
};

export const darkTheme = createTheme({
  palette: {
    mode: 'dark',
    ...tarotColors,
  },
  typography: {
    fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
    h1: { fontFamily: '"Cinzel", serif', fontSize: '2.5rem', fontWeight: 700, color: tarotColors.primary.main, textShadow: '0 0 15px rgba(212, 175, 55, 0.4)' },
    h2: { fontFamily: '"Cinzel", serif', fontSize: '2rem', fontWeight: 600, color: tarotColors.primary.main, textShadow: '0 0 10px rgba(212, 175, 55, 0.3)' },
    h3: { fontFamily: '"Cinzel", serif', fontSize: '1.75rem', fontWeight: 600, color: tarotColors.primary.main },
    h4: { fontFamily: '"Cinzel", serif', fontSize: '1.5rem', fontWeight: 500, color: tarotColors.text.primary },
    h5: { fontFamily: '"Cinzel", serif', fontSize: '1.25rem', fontWeight: 600, color: tarotColors.primary.main },
    h6: { fontFamily: '"Cinzel", serif', fontSize: '1.125rem', fontWeight: 600, color: tarotColors.text.primary },
    body1: { fontSize: '1rem', lineHeight: 1.6, color: tarotColors.text.primary },
    body2: { fontSize: '0.875rem', lineHeight: 1.5, color: tarotColors.text.secondary },
  },
  components: {
    MuiCssBaseline: {
      styleOverrides: {
        body: {
          background: `linear-gradient(135deg, #0A0512 0%, #1A0B2E 100%)`,
          minHeight: '100vh',
          letterSpacing: '0.02em',
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          background: tarotColors.background.paper,
          backdropFilter: 'blur(12px)',
          WebkitBackdropFilter: 'blur(12px)',
          border: `1px solid rgba(212, 175, 55, 0.15)`,
          borderRadius: 16,
          boxShadow: '0 8px 32px rgba(0, 0, 0, 0.4), inset 0 0 20px rgba(26, 11, 46, 0.5)',
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: 8,
          textTransform: 'none',
          fontWeight: 600,
          letterSpacing: '0.05em',
          transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
          '&:hover': {
            transform: 'translateY(-2px)',
            boxShadow: '0 4px 20px rgba(0, 240, 255, 0.25)',
          },
        },
        contained: {
          background: `linear-gradient(45deg, ${tarotColors.primary.dark}, ${tarotColors.primary.main})`,
          color: tarotColors.primary.contrastText,
          boxShadow: '0 4px 15px rgba(212, 175, 55, 0.2)',
          '&:hover': {
            background: `linear-gradient(45deg, ${tarotColors.primary.main}, ${tarotColors.primary.light})`,
            boxShadow: '0 6px 20px rgba(212, 175, 55, 0.4)',
          },
        },
        outlined: {
          borderColor: 'rgba(0, 240, 255, 0.5)',
          color: tarotColors.secondary.main,
          '&:hover': {
            borderColor: tarotColors.secondary.main,
            background: 'rgba(0, 240, 255, 0.08)',
            boxShadow: '0 0 15px rgba(0, 240, 255, 0.2)',
          },
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          background: tarotColors.background.card,
          backdropFilter: 'blur(10px)',
          border: `1px solid rgba(212, 175, 55, 0.2)`,
          borderRadius: 16,
          overflow: 'hidden',
          transition: 'all 0.4s cubic-bezier(0.4, 0, 0.2, 1)',
          '&:hover': {
            transform: 'translateY(-6px)',
            boxShadow: '0 12px 40px rgba(0, 240, 255, 0.15), 0 0 20px rgba(212, 175, 55, 0.2)',
            borderColor: 'rgba(212, 175, 55, 0.5)',
          },
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          backdropFilter: 'blur(4px)',
          background: 'rgba(26, 11, 46, 0.4)',
        },
        outlined: {
          borderColor: 'rgba(0, 240, 255, 0.3)',
          color: tarotColors.secondary.main,
        }
      }
    }
  },
});

export const lightTheme = darkTheme; // Force dark theme for Mystic Nebula
export default darkTheme;