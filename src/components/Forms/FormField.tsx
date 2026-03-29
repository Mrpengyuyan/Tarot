import React, { useMemo, useState } from 'react';
import {
  FormControl,
  FormHelperText,
  FormLabel,
  IconButton,
  InputAdornment,
  OutlinedInput,
  TextField,
} from '@mui/material';
import { AutoAwesome, Visibility, VisibilityOff } from 'icons';

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

const mysticalFieldSx = {
  '& .MuiOutlinedInput-root': {
    backgroundColor: 'rgba(16, 8, 32, 0.7)',
    borderRadius: 2,
    '&:hover .MuiOutlinedInput-notchedOutline': {
      borderColor: 'primary.main',
    },
  },
};

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
  const [showPassword, setShowPassword] = useState(false);
  const inputType = type === 'password' && showPassword ? 'text' : type;

  const resolvedEndAdornment = useMemo(() => {
    if (type === 'password') {
      return (
        <InputAdornment position="end">
          <IconButton
            edge="end"
            onClick={() => setShowPassword((prev) => !prev)}
            aria-label={showPassword ? '闅愯棌瀵嗙爜' : '鏄剧ず瀵嗙爜'}
          >
            {showPassword ? <VisibilityOff /> : <Visibility />}
          </IconButton>
          {endAdornment}
        </InputAdornment>
      );
    }

    return endAdornment ? <InputAdornment position="end">{endAdornment}</InputAdornment> : undefined;
  }, [endAdornment, showPassword, type]);

  const resolvedStartAdornment = startAdornment
    ? <InputAdornment position="start">{startAdornment}</InputAdornment>
    : undefined;

  if (variant === 'outlined') {
    return (
      <FormControl
        fullWidth={fullWidth}
        error={error}
        disabled={disabled}
        required={required}
        size={size}
        className={className}
        sx={style === 'mystical' ? mysticalFieldSx : undefined}
      >
        {label && (
          <FormLabel
            htmlFor={name}
            sx={{
              mb: 1,
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

        <OutlinedInput
          id={name}
          name={name}
          value={value}
          type={inputType}
          placeholder={placeholder}
          multiline={multiline}
          rows={rows}
          notched={Boolean(label)}
          startAdornment={resolvedStartAdornment}
          endAdornment={resolvedEndAdornment}
          autoComplete={autoComplete}
          autoFocus={autoFocus}
          onChange={onChange}
          onBlur={onBlur}
          onFocus={onFocus}
          sx={{ minHeight: multiline ? 'auto' : undefined }}
        />

        {helperText && <FormHelperText>{helperText}</FormHelperText>}
      </FormControl>
    );
  }

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
        startAdornment: resolvedStartAdornment,
        endAdornment: resolvedEndAdornment,
      }}
      sx={style === 'mystical' ? mysticalFieldSx : undefined}
    />
  );
};

export const MysticalFormField: React.FC<Omit<FormFieldProps, 'style'>> = (props) => (
  <FormField {...props} style="mystical" />
);

export const PasswordField: React.FC<Omit<FormFieldProps, 'type'>> = (props) => (
  <FormField {...props} type="password" />
);

export const EmailField: React.FC<Omit<FormFieldProps, 'type'>> = (props) => (
  <FormField {...props} type="email" autoComplete="email" />
);

export const SearchField: React.FC<Omit<FormFieldProps, 'type'>> = (props) => (
  <FormField {...props} type="text" />
);

export default FormField;

