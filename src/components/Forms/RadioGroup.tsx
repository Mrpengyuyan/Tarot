import React from 'react';
import {
  FormControl,
  FormLabel,
  RadioGroup as MuiRadioGroup,
  FormControlLabel,
  Radio,
  FormHelperText,
  Box,
  Typography,
  styled,
  Paper,
} from '@mui/material';
import { AutoAwesome } from '@mui/icons-material';

const StyledFormControl = styled(FormControl)(({ theme }) => ({
  width: '100%',

  '& .MuiFormLabel-root': {
    color: theme.palette.text.primary,
    fontWeight: 600,
    marginBottom: theme.spacing(2),
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
  },

  '& .MuiRadioGroup-root': {
    gap: theme.spacing(1),
  },
}));

const StyledRadio = styled(Radio)(({ theme }) => ({
  color: theme.palette.text.secondary,
  '&.Mui-checked': {
    color: theme.palette.primary.main,
  },
  '&.Mui-disabled': {
    color: theme.palette.text.disabled,
  },
  '& .MuiSvgIcon-root': {
    fontSize: '1.25rem',
  },

  '&.mystical': {
    '&.Mui-checked': {
      color: theme.palette.primary.main,
      filter: 'drop-shadow(0 0 5px rgba(212, 175, 55, 0.3))',
    },
  },
}));

const StyledFormControlLabel = styled(FormControlLabel)(({ theme }) => ({
  margin: 0,
  padding: theme.spacing(1.5),
  borderRadius: 8,
  transition: 'all 0.3s ease',
  border: '1px solid transparent',
  backgroundColor: 'rgba(26, 26, 46, 0.3)',

  '&:hover': {
    backgroundColor: 'rgba(26, 26, 46, 0.5)',
    borderColor: 'rgba(212, 175, 55, 0.3)',
  },

  '& .MuiFormControlLabel-label': {
    fontSize: '0.875rem',
    fontWeight: 500,
    color: theme.palette.text.primary,
    marginLeft: theme.spacing(1),
  },

  '&.Mui-disabled': {
    opacity: 0.5,
    '& .MuiFormControlLabel-label': {
      color: theme.palette.text.disabled,
    },
  },

  '&.selected': {
    backgroundColor: 'rgba(212, 175, 55, 0.1)',
    borderColor: theme.palette.primary.main,

    '& .MuiFormControlLabel-label': {
      color: theme.palette.primary.main,
      fontWeight: 600,
    },
  },

  '&.mystical': {
    background: 'linear-gradient(135deg, rgba(26, 26, 46, 0.4) 0%, rgba(16, 33, 62, 0.4) 100%)',
    border: '1px solid rgba(212, 175, 55, 0.2)',
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
        radial-gradient(circle at 20% 50%, rgba(212, 175, 55, 0.05) 0%, transparent 50%),
        radial-gradient(circle at 80% 50%, rgba(106, 5, 114, 0.05) 0%, transparent 50%)
      `,
      pointerEvents: 'none',
    },

    '&:hover': {
      borderColor: 'rgba(212, 175, 55, 0.4)',
      boxShadow: '0 0 15px rgba(212, 175, 55, 0.1)',
    },

    '&.selected': {
      background: 'linear-gradient(135deg, rgba(212, 175, 55, 0.2) 0%, rgba(106, 5, 114, 0.1) 100%)',
      borderColor: theme.palette.primary.main,
      boxShadow: '0 0 20px rgba(212, 175, 55, 0.2)',
    },
  },

  '&.card-style': {
    padding: theme.spacing(2),
    borderRadius: 12,
    flexDirection: 'column',
    alignItems: 'flex-start',
    minHeight: 80,

    '& .MuiRadio-root': {
      position: 'absolute',
      top: theme.spacing(1),
      right: theme.spacing(1),
    },

    '& .MuiFormControlLabel-label': {
      marginLeft: 0,
      marginTop: theme.spacing(0.5),
      width: '100%',
    },
  },
}));

export interface RadioOption {
  value: string | number;
  label: string;
  disabled?: boolean;
  description?: string;
  icon?: React.ReactNode;
}

export interface RadioGroupProps {
  name: string;
  label?: string;
  value?: string | number;
  options: RadioOption[];
  required?: boolean;
  disabled?: boolean;
  error?: boolean;
  helperText?: string;
  row?: boolean;
  size?: 'small' | 'medium';
  style?: 'default' | 'mystical';
  variant?: 'default' | 'card';
  className?: string;
  onChange?: (event: React.ChangeEvent<HTMLInputElement>, value: string) => void;
}

const RadioGroup: React.FC<RadioGroupProps> = ({
  name,
  label,
  value = '',
  options = [],
  required = false,
  disabled = false,
  error = false,
  helperText,
  row = false,
  size = 'medium',
  style = 'default',
  variant = 'default',
  className,
  onChange,
}) => {
  return (
    <StyledFormControl
      error={error}
      disabled={disabled}
      required={required}
      className={className}
    >
      {label && (
        <FormLabel
          component="legend"
          className={`${required ? 'required' : ''} ${style}`}
        >
          {style === 'mystical' && <AutoAwesome sx={{ fontSize: '1rem' }} />}
          {label}
        </FormLabel>
      )}

      <MuiRadioGroup
        name={name}
        value={value}
        onChange={onChange}
        row={row && variant !== 'card'}
        sx={{
          gap: variant === 'card' ? 2 : 1,
          ...(variant === 'card' && {
            display: 'grid',
            gridTemplateColumns: row ? 'repeat(auto-fit, minmax(200px, 1fr))' : '1fr',
            gap: 2,
          }),
        }}
      >
        {options.map((option) => (
          <StyledFormControlLabel
            key={option.value}
            value={option.value}
            disabled={disabled || option.disabled}
            className={`
              ${style}
              ${variant === 'card' ? 'card-style' : ''}
              ${value === option.value ? 'selected' : ''}
            `}
            control={
              <StyledRadio
                size={size}
                className={style}
              />
            }
            label={
              variant === 'card' ? (
                <Box sx={{ width: '100%' }}>
                  {option.icon && (
                    <Box
                      sx={{
                        display: 'flex',
                        alignItems: 'center',
                        mb: 1,
                        color: value === option.value ? 'primary.main' : 'text.secondary',
                        '& > *': {
                          fontSize: '1.5rem',
                        },
                      }}
                    >
                      {option.icon}
                    </Box>
                  )}
                  <Typography
                    variant="body2"
                    sx={{
                      fontWeight: value === option.value ? 600 : 500,
                      color: value === option.value ? 'primary.main' : 'text.primary',
                      mb: option.description ? 0.5 : 0,
                    }}
                  >
                    {option.label}
                  </Typography>
                  {option.description && (
                    <Typography
                      variant="caption"
                      sx={{
                        color: 'text.secondary',
                        display: 'block',
                        lineHeight: 1.3,
                      }}
                    >
                      {option.description}
                    </Typography>
                  )}
                </Box>
              ) : (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, width: '100%' }}>
                  {option.icon}
                  <Box>
                    <Typography variant="body2" component="span">
                      {option.label}
                    </Typography>
                    {option.description && (
                      <Typography
                        variant="caption"
                        sx={{
                          color: 'text.secondary',
                          display: 'block',
                          lineHeight: 1.3,
                        }}
                      >
                        {option.description}
                      </Typography>
                    )}
                  </Box>
                </Box>
              )
            }
          />
        ))}
      </MuiRadioGroup>

      {helperText && (
        <FormHelperText error={error} sx={{ mt: 1 }}>
          {helperText}
        </FormHelperText>
      )}
    </StyledFormControl>
  );
};

// 预设组件
export const MysticalRadioGroup: React.FC<Omit<RadioGroupProps, 'style'>> = (props) => (
  <RadioGroup {...props} style="mystical" />
);

export const CardRadioGroup: React.FC<Omit<RadioGroupProps, 'variant'>> = (props) => (
  <RadioGroup {...props} variant="card" />
);

export const MysticalCardRadioGroup: React.FC<Omit<RadioGroupProps, 'style' | 'variant'>> = (props) => (
  <RadioGroup {...props} style="mystical" variant="card" />
);

export default RadioGroup;