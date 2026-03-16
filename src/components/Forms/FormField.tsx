import React from 'react';
import {
  TextField,
  FormControl,
  FormLabel,
  FormHelperText,
  InputLabel,
  OutlinedInput,
  InputAdornment,
  Box,
  Typography,
  styled,
  alpha,
} from '@mui/material';
import { Visibility, VisibilityOff, AutoAwesome } from '@mui/icons-material';

const StyledFormControl = styled(FormControl)(({ theme }) => ({
  '& .MuiOutlinedInput-root': {
    backgroundColor: 'rgba(26, 26, 46, 0.6)',
    backdropFilter: 'blur(10px)',
    borderRadius: 12,
    transition: 'all 0.3s ease',
    border: '1px solid rgba(212, 175, 55, 0.2)',

    '&:hover': {
      borderColor: 'rgba(212, 175, 55, 0.4)',
      backgroundColor: 'rgba(26, 26, 46, 0.8)',
    },

    '&.Mui-focused': {
      borderColor: theme.palette.primary.main,
      backgroundColor: 'rgba(26, 26, 46, 0.9)',
      boxShadow: `0 0 0 2px ${alpha(theme.palette.primary.main, 0.2)}`,
    },

    '&.Mui-error': {
      borderColor: theme.palette.error.main,
      boxShadow: `0 0 0 2px ${alpha(theme.palette.error.main, 0.2)}`,
    },

    '& .MuiOutlinedInput-notchedOutline': {
      border: 'none',
    },

    '& input': {
      color: theme.palette.text.primary,
      fontFamily: 'inherit',

      '&::placeholder': {
        color: theme.palette.text.disabled,
        opacity: 0.7,
      },

      '&:-webkit-autofill': {
        WebkitBoxShadow: '0 0 0 100px rgba(26, 26, 46, 0.9) inset',
        WebkitTextFillColor: theme.palette.text.primary,
        borderRadius: 12,
      },
    },
  },

  '& .MuiInputLabel-root': {
    color: theme.palette.text.secondary,
    fontWeight: 500,
    transform: 'translate(14px, 16px) scale(1)',

    '&.Mui-focused': {
      color: theme.palette.primary.main,
    },

    '&.MuiInputLabel-shrink': {
      transform: 'translate(14px, -6px) scale(0.85)',
      backgroundColor: 'rgba(26, 26, 46, 0.9)',
      padding: '0 8px',
      borderRadius: 4,
    },
  },

  '&.mystical': {
    '& .MuiOutlinedInput-root': {
      background: 'linear-gradient(135deg, rgba(26, 26, 46, 0.8) 0%, rgba(16, 33, 62, 0.8) 100%)',
      border: '1px solid rgba(212, 175, 55, 0.3)',
      position: 'relative',

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
        pointerEvents: 'none',
        borderRadius: 12,
      },

      '&:hover': {
        borderColor: 'rgba(212, 175, 55, 0.5)',
        boxShadow: '0 0 20px rgba(212, 175, 55, 0.1)',
      },

      '&.Mui-focused': {
        borderColor: theme.palette.primary.main,
        boxShadow: '0 0 20px rgba(212, 175, 55, 0.2)',
      },
    },

    '& .MuiInputLabel-shrink': {
      background: 'linear-gradient(135deg, rgba(26, 26, 46, 0.9) 0%, rgba(16, 33, 62, 0.9) 100%)',
      borderRadius: 4,
    },
  },
}));

const StyledFormLabel = styled(FormLabel)(({ theme }) => ({
  color: theme.palette.text.primary,
  fontWeight: 600,
  marginBottom: theme.spacing(1),
  display: 'flex',
  alignItems: 'center',
  gap: theme.spacing(0.5),
  fontFamily: 'Cinzel, serif',

  '&.required::after': {
    content: '"*"',
    color: theme.palette.error.main,
    marginLeft: theme.spacing(0.5),
  },

  '&.mystical': {
    color: theme.palette.primary.main,
    textShadow: '0 0 5px rgba(212, 175, 55, 0.3)',
  },
}));

const StyledHelperText = styled(FormHelperText)(({ theme }) => ({
  marginTop: theme.spacing(1),
  marginLeft: theme.spacing(1.5),
  fontSize: '0.875rem',

  '&.Mui-error': {
    color: theme.palette.error.main,
  },
}));

export interface FormFieldProps {
  name: string;
  label?: string;
  placeholder?: string;
  value?: string | number;
  type?: 'text' | 'email' | 'password' | 'number' | 'tel' | 'url';
  variant?: 'outlined' | 'filled' | 'standard';
  size?: 'small' | 'medium';
  required?: boolean;
  disabled?: boolean;
  multiline?: boolean;
  rows?: number;
  maxRows?: number;
  error?: boolean;
  helperText?: string;
  startAdornment?: React.ReactNode;
  endAdornment?: React.ReactNode;
  style?: 'default' | 'mystical';
  fullWidth?: boolean;
  autoComplete?: string;
  autoFocus?: boolean;
  className?: string;
  onChange?: (event: React.ChangeEvent<HTMLInputElement>) => void;
  onBlur?: (event: React.FocusEvent<HTMLInputElement>) => void;
  onFocus?: (event: React.FocusEvent<HTMLInputElement>) => void;
}

const FormField: React.FC<FormFieldProps> = ({
  name,
  label,
  placeholder,
  value = '',
  type = 'text',
  variant = 'outlined',
  size = 'medium',
  required = false,
  disabled = false,
  multiline = false,
  rows,
  maxRows,
  error = false,
  helperText,
  startAdornment,
  endAdornment,
  style = 'default',
  fullWidth = true,
  autoComplete,
  autoFocus = false,
  className,
  onChange,
  onBlur,
  onFocus,
}) => {
  const [showPassword, setShowPassword] = React.useState(false);

  const handleClickShowPassword = () => {
    setShowPassword(!showPassword);
  };

  const inputType = type === 'password' && showPassword ? 'text' : type;

  // Password field with visibility toggle
  const getEndAdornment = () => {
    if (type === 'password') {
      return (
        <InputAdornment position="end">
          <Box
            component="button"
            type="button"
            onClick={handleClickShowPassword}
            sx={{
              background: 'none',
              border: 'none',
              color: 'text.secondary',
              cursor: 'pointer',
              padding: '8px',
              borderRadius: '50%',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              transition: 'all 0.2s ease',
              '&:hover': {
                color: 'primary.main',
                backgroundColor: 'rgba(212, 175, 55, 0.1)',
              },
            }}
            aria-label={showPassword ? '隐藏密码' : '显示密码'}
          >
            {showPassword ? <VisibilityOff /> : <Visibility />}
          </Box>
          {endAdornment}
        </InputAdornment>
      );
    }
    return endAdornment ? <InputAdornment position="end">{endAdornment}</InputAdornment> : null;
  };

  const getStartAdornment = () => {
    return startAdornment ? <InputAdornment position="start">{startAdornment}</InputAdornment> : null;
  };

  if (variant === 'outlined') {
    return (
      <StyledFormControl
        variant="outlined"
        fullWidth={fullWidth}
        error={error}
        disabled={disabled}
        size={size}
        required={required}
        className={`${style} ${className || ''}`}
      >
        {label && (
          <StyledFormLabel
            htmlFor={name}
            className={`${required ? 'required' : ''} ${style}`}
          >
            {style === 'mystical' && <AutoAwesome sx={{ fontSize: '1rem' }} />}
            {label}
          </StyledFormLabel>
        )}

        <OutlinedInput
          id={name}
          name={name}
          type={inputType}
          value={value}
          placeholder={placeholder}
          multiline={multiline}
          rows={rows}
          maxRows={maxRows}
          autoComplete={autoComplete}
          autoFocus={autoFocus}
          startAdornment={getStartAdornment()}
          endAdornment={getEndAdornment()}
          onChange={onChange}
          onBlur={onBlur}
          onFocus={onFocus}
          sx={{
            mt: label ? 1 : 0,
          }}
        />

        {helperText && (
          <StyledHelperText error={error}>
            {helperText}
          </StyledHelperText>
        )}
      </StyledFormControl>
    );
  }

  // Fallback to standard TextField for other variants
  return (
    <TextField
      name={name}
      label={label}
      placeholder={placeholder}
      value={value}
      type={inputType}
      variant={variant}
      size={size}
      required={required}
      disabled={disabled}
      multiline={multiline}
      rows={rows}
      maxRows={maxRows}
      error={error}
      helperText={helperText}
      fullWidth={fullWidth}
      autoComplete={autoComplete}
      autoFocus={autoFocus}
      className={className}
      onChange={onChange}
      onBlur={onBlur}
      onFocus={onFocus}
      InputProps={{
        startAdornment: getStartAdornment(),
        endAdornment: getEndAdornment(),
      }}
    />
  );
};

// 预设组件
export const MysticalFormField: React.FC<Omit<FormFieldProps, 'style'>> = (props) => (
  <FormField {...props} style="mystical" />
);

export const PasswordField: React.FC<Omit<FormFieldProps, 'type'>> = (props) => (
  <FormField {...props} type="password" />
);

export const EmailField: React.FC<Omit<FormFieldProps, 'type'>> = (props) => (
  <FormField {...props} type="email" autoComplete="email" />
);

export const SearchField: React.FC<Omit<FormFieldProps, 'type' | 'startAdornment'>> = (props) => (
  <FormField
    {...props}
    type="text"
    startAdornment={
      <Box sx={{ color: 'text.secondary', display: 'flex', alignItems: 'center' }}>
        🔍
      </Box>
    }
  />
);

export default FormField;