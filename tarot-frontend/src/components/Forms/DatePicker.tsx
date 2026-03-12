import React from 'react';
import {
  TextField,
  FormControl,
  FormLabel,
  FormHelperText,
  InputAdornment,
  Box,
  IconButton,
  styled,
  alpha,
} from '@mui/material';
import { CalendarToday, Clear, AutoAwesome } from '@mui/icons-material';

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
      cursor: 'pointer',

      '&::placeholder': {
        color: theme.palette.text.disabled,
        opacity: 0.7,
      },

      '&::-webkit-calendar-picker-indicator': {
        display: 'none',
      },
    },
  },

  '& .MuiInputLabel-root': {
    color: theme.palette.text.secondary,
    fontWeight: 500,
    fontFamily: 'Cinzel, serif',

    '&.Mui-focused': {
      color: theme.palette.primary.main,
    },

    '&.MuiInputLabel-shrink': {
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

export interface DatePickerProps {
  name: string;
  label?: string;
  value?: string;
  placeholder?: string;
  required?: boolean;
  disabled?: boolean;
  error?: boolean;
  helperText?: string;
  min?: string;
  max?: string;
  size?: 'small' | 'medium';
  variant?: 'outlined' | 'filled' | 'standard';
  style?: 'default' | 'mystical';
  type?: 'date' | 'datetime-local' | 'time';
  clearable?: boolean;
  fullWidth?: boolean;
  autoFocus?: boolean;
  className?: string;
  onChange?: (event: React.ChangeEvent<HTMLInputElement>) => void;
  onBlur?: (event: React.FocusEvent<HTMLInputElement>) => void;
  onFocus?: (event: React.FocusEvent<HTMLInputElement>) => void;
}

const DatePicker: React.FC<DatePickerProps> = ({
  name,
  label,
  value = '',
  placeholder,
  required = false,
  disabled = false,
  error = false,
  helperText,
  min,
  max,
  size = 'medium',
  variant = 'outlined',
  style = 'default',
  type = 'date',
  clearable = false,
  fullWidth = true,
  autoFocus = false,
  className,
  onChange,
  onBlur,
  onFocus,
}) => {
  const inputRef = React.useRef<HTMLInputElement>(null);

  const handleClear = () => {
    if (onChange && !disabled) {
      const syntheticEvent = {
        target: { value: '', name },
        currentTarget: { value: '', name },
      } as React.ChangeEvent<HTMLInputElement>;
      onChange(syntheticEvent);
    }
  };

  const handleCalendarClick = () => {
    if (inputRef.current && !disabled) {
      inputRef.current.showPicker?.();
    }
  };

  const getIcon = () => {
    switch (type) {
      case 'datetime-local':
        return '🗓️';
      case 'time':
        return '🕐';
      default:
        return <CalendarToday />;
    }
  };

  const getPlaceholder = () => {
    if (placeholder) return placeholder;
    switch (type) {
      case 'datetime-local':
        return '选择日期和时间...';
      case 'time':
        return '选择时间...';
      default:
        return '选择日期...';
    }
  };

  const formatDisplayValue = (inputValue: string) => {
    if (!inputValue) return '';

    try {
      const date = new Date(inputValue);
      if (isNaN(date.getTime())) return inputValue;

      switch (type) {
        case 'datetime-local':
          return date.toLocaleString('zh-CN', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
          });
        case 'time':
          return date.toLocaleTimeString('zh-CN', {
            hour: '2-digit',
            minute: '2-digit',
          });
        default:
          return date.toLocaleDateString('zh-CN');
      }
    } catch {
      return inputValue;
    }
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

        <TextField
          inputRef={inputRef}
          id={name}
          name={name}
          type={type}
          value={value}
          placeholder={getPlaceholder()}
          required={required}
          disabled={disabled}
          error={error}
          size={size}
          variant="outlined"
          fullWidth={fullWidth}
          autoFocus={autoFocus}
          inputProps={{
            min,
            max,
          }}
          onChange={onChange}
          onBlur={onBlur}
          onFocus={onFocus}
          sx={{
            mt: label ? 1 : 0,
          }}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <IconButton
                  edge="start"
                  onClick={handleCalendarClick}
                  disabled={disabled}
                  sx={{
                    color: 'primary.main',
                    opacity: disabled ? 0.5 : 1,
                    '&:hover': {
                      backgroundColor: 'rgba(212, 175, 55, 0.1)',
                    },
                  }}
                  aria-label="打开日期选择器"
                >
                  {getIcon()}
                </IconButton>
              </InputAdornment>
            ),
            endAdornment: clearable && value && !disabled && (
              <InputAdornment position="end">
                <IconButton
                  edge="end"
                  onClick={handleClear}
                  sx={{
                    color: 'text.secondary',
                    '&:hover': {
                      color: 'primary.main',
                      backgroundColor: 'rgba(212, 175, 55, 0.1)',
                    },
                  }}
                  aria-label="清除日期"
                >
                  <Clear />
                </IconButton>
              </InputAdornment>
            ),
          }}
        />

        {helperText && (
          <FormHelperText error={error} sx={{ mx: 2, mt: 1 }}>
            {helperText}
          </FormHelperText>
        )}
      </StyledFormControl>
    );
  }

  // Fallback to standard TextField for other variants
  return (
    <TextField
      inputRef={inputRef}
      name={name}
      label={label}
      value={value}
      type={type}
      placeholder={getPlaceholder()}
      required={required}
      disabled={disabled}
      error={error}
      helperText={helperText}
      size={size}
      variant={variant}
      fullWidth={fullWidth}
      autoFocus={autoFocus}
      className={className}
      inputProps={{
        min,
        max,
      }}
      onChange={onChange}
      onBlur={onBlur}
      onFocus={onFocus}
      InputProps={{
        startAdornment: (
          <InputAdornment position="start">
            <IconButton
              edge="start"
              onClick={handleCalendarClick}
              disabled={disabled}
              sx={{
                color: 'primary.main',
                opacity: disabled ? 0.5 : 1,
              }}
            >
              {getIcon()}
            </IconButton>
          </InputAdornment>
        ),
        endAdornment: clearable && value && !disabled && (
          <InputAdornment position="end">
            <IconButton
              edge="end"
              onClick={handleClear}
              sx={{
                color: 'text.secondary',
                '&:hover': {
                  color: 'primary.main',
                },
              }}
            >
              <Clear />
            </IconButton>
          </InputAdornment>
        ),
      }}
    />
  );
};

// 预设组件
export const MysticalDatePicker: React.FC<Omit<DatePickerProps, 'style'>> = (props) => (
  <DatePicker {...props} style="mystical" />
);

export const DateTimePicker: React.FC<Omit<DatePickerProps, 'type'>> = (props) => (
  <DatePicker {...props} type="datetime-local" />
);

export const TimePicker: React.FC<Omit<DatePickerProps, 'type'>> = (props) => (
  <DatePicker {...props} type="time" />
);

export const BirthDatePicker: React.FC<Omit<DatePickerProps, 'type' | 'max' | 'placeholder'>> = (props) => (
  <DatePicker
    {...props}
    type="date"
    max={new Date().toISOString().split('T')[0]}
    placeholder="选择您的出生日期..."
    style="mystical"
  />
);

export default DatePicker;