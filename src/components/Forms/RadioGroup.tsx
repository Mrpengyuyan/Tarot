import React from 'react';
import {
  Box,
  FormControl,
  FormControlLabel,
  FormHelperText,
  FormLabel,
  Paper,
  Radio,
  RadioGroup as MuiRadioGroup,
  Typography,
} from '@mui/material';
import { AutoAwesome } from 'icons';

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
  options,
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
  const isCardVariant = variant === 'card';

  return (
    <FormControl
      required={required}
      disabled={disabled}
      error={error}
      className={className}
      fullWidth
    >
      {label && (
        <FormLabel
          sx={{
            mb: 1.5,
            display: 'flex',
            alignItems: 'center',
            gap: 0.75,
            fontWeight: 600,
            color: style === 'mystical' ? 'primary.main' : 'text.primary',
          }}
        >
          {style === 'mystical' && <AutoAwesome sx={{ fontSize: 16 }} />}
          {label}
        </FormLabel>
      )}

      <MuiRadioGroup
        name={name}
        value={String(value)}
        onChange={onChange}
        row={row && !isCardVariant}
        sx={
          isCardVariant
            ? {
                display: 'grid',
                gridTemplateColumns: row ? 'repeat(auto-fit, minmax(220px, 1fr))' : '1fr',
                gap: 1.5,
              }
            : undefined
        }
      >
        {options.map((option) => {
          const selected = String(value) === String(option.value);

          if (isCardVariant) {
            return (
              <Paper
                key={String(option.value)}
                variant="outlined"
                sx={{
                  p: 1.5,
                  borderColor: selected ? 'primary.main' : 'divider',
                  backgroundColor: style === 'mystical' ? 'rgba(16, 8, 32, 0.65)' : undefined,
                }}
              >
                <FormControlLabel
                  value={String(option.value)}
                  disabled={option.disabled}
                  control={<Radio size={size} />}
                  label={
                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.5 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        {option.icon}
                        <Typography variant="body2" fontWeight={selected ? 700 : 500}>
                          {option.label}
                        </Typography>
                      </Box>
                      {option.description && (
                        <Typography variant="caption" color="text.secondary">
                          {option.description}
                        </Typography>
                      )}
                    </Box>
                  }
                />
              </Paper>
            );
          }

          return (
            <FormControlLabel
              key={String(option.value)}
              value={String(option.value)}
              disabled={option.disabled}
              control={<Radio size={size} />}
              label={
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  {option.icon}
                  <Box>
                    <Typography variant="body2">{option.label}</Typography>
                    {option.description && (
                      <Typography variant="caption" color="text.secondary">
                        {option.description}
                      </Typography>
                    )}
                  </Box>
                </Box>
              }
            />
          );
        })}
      </MuiRadioGroup>

      {helperText && <FormHelperText>{helperText}</FormHelperText>}
    </FormControl>
  );
};

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

