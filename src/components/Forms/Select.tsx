import React from 'react';
import {
  FormControl,
  InputLabel,
  Select as MuiSelect,
  MenuItem,
  FormHelperText,
  ListSubheader,
  Chip,
  Box,
  Typography,
  styled,
  alpha,
  SelectChangeEvent,
} from '@mui/material';
import { Check, ExpandMore, AutoAwesome } from '@mui/icons-material';

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

      '& .MuiOutlinedInput-notchedOutline': {
        borderColor: 'transparent',
      },
    },

    '&.Mui-error': {
      borderColor: theme.palette.error.main,
      boxShadow: `0 0 0 2px ${alpha(theme.palette.error.main, 0.2)}`,
    },

    '& .MuiOutlinedInput-notchedOutline': {
      border: 'none',
    },

    '& .MuiSelect-select': {
      color: theme.palette.text.primary,
      display: 'flex',
      alignItems: 'center',
      gap: theme.spacing(1),
    },

    '& .MuiSelect-icon': {
      color: theme.palette.primary.main,
      transition: 'transform 0.3s ease',
    },

    '&.Mui-focused .MuiSelect-icon': {
      transform: 'rotate(180deg)',
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

const StyledMenuItem = styled(MenuItem)(({ theme }) => ({
  fontSize: '0.875rem',
  minHeight: 48,
  padding: theme.spacing(1.5, 2),
  display: 'flex',
  alignItems: 'center',
  gap: theme.spacing(1),
  transition: 'all 0.2s ease',

  '&:hover': {
    backgroundColor: 'rgba(212, 175, 55, 0.1)',
    color: theme.palette.primary.main,
  },

  '&.Mui-selected': {
    backgroundColor: 'rgba(212, 175, 55, 0.2)',
    color: theme.palette.primary.main,
    fontWeight: 600,

    '&:hover': {
      backgroundColor: 'rgba(212, 175, 55, 0.3)',
    },
  },

  '&.mystical': {
    borderBottom: '1px solid rgba(212, 175, 55, 0.1)',

    '&:hover': {
      background: 'linear-gradient(90deg, rgba(212, 175, 55, 0.1) 0%, rgba(106, 5, 114, 0.1) 100%)',
    },

    '&.Mui-selected': {
      background: 'linear-gradient(90deg, rgba(212, 175, 55, 0.2) 0%, rgba(106, 5, 114, 0.2) 100%)',
    },
  },
}));

export interface SelectOption {
  value: string | number;
  label: string;
  disabled?: boolean;
  icon?: React.ReactNode;
  description?: string;
}

export interface SelectOptionGroup {
  label: string;
  options: SelectOption[];
}

export interface SelectProps {
  name: string;
  label?: string;
  value?: string | number | string[] | number[];
  options?: SelectOption[] | SelectOptionGroup[];
  placeholder?: string;
  multiple?: boolean;
  required?: boolean;
  disabled?: boolean;
  error?: boolean;
  helperText?: string;
  size?: 'small' | 'medium';
  variant?: 'outlined' | 'filled' | 'standard';
  style?: 'default' | 'mystical';
  fullWidth?: boolean;
  autoFocus?: boolean;
  className?: string;
  renderValue?: (selected: unknown) => React.ReactNode;
  onChange?: (event: SelectChangeEvent<string | number | string[] | number[]>) => void;
  onBlur?: (event: React.FocusEvent<HTMLInputElement>) => void;
  onFocus?: (event: React.FocusEvent<HTMLInputElement>) => void;
}

const Select: React.FC<SelectProps> = ({
  name,
  label,
  value,
  options = [],
  placeholder,
  multiple = false,
  required = false,
  disabled = false,
  error = false,
  helperText,
  size = 'medium',
  variant = 'outlined',
  style = 'default',
  fullWidth = true,
  autoFocus = false,
  className,
  renderValue,
  onChange,
  onBlur,
  onFocus,
}) => {
  // 处理默认值
  const actualValue: string | number | string[] | number[] = value !== undefined ? value : (multiple ? [] : '');

  const isOptionGroup = (option: SelectOption | SelectOptionGroup): option is SelectOptionGroup => {
    return 'options' in option;
  };

  const defaultRenderValue = (selected: unknown) => {
    if (multiple && Array.isArray(selected)) {
      if (selected.length === 0) {
        return (
          <Typography variant="body2" sx={{ color: 'text.disabled', opacity: 0.7 }}>
            {placeholder || '请选择...'}
          </Typography>
        );
      }

      return (
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
          {selected.map((val) => {
            const option = getAllOptions().find(opt => opt.value === val);
            return (
              <Chip
                key={val}
                label={option?.label || val}
                size="small"
                sx={{
                  backgroundColor: 'rgba(212, 175, 55, 0.2)',
                  color: 'primary.main',
                  border: '1px solid rgba(212, 175, 55, 0.3)',
                  '& .MuiChip-deleteIcon': {
                    color: 'primary.main',
                    '&:hover': {
                      color: 'primary.light',
                    },
                  },
                }}
              />
            );
          })}
        </Box>
      );
    }

    if (!selected) {
      return (
        <Typography variant="body2" sx={{ color: 'text.disabled', opacity: 0.7 }}>
          {placeholder || '请选择...'}
        </Typography>
      );
    }

    const option = getAllOptions().find(opt => opt.value === selected);
    return (
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <>
          {option?.icon}
          {option?.label || selected}
        </>
      </Box>
    );
  };

  const getAllOptions = (): SelectOption[] => {
    return options.flatMap(option =>
      isOptionGroup(option) ? option.options : option
    );
  };

  const renderOptions = () => {
    return options.map((option, index) => {
      if (isOptionGroup(option)) {
        return [
          <ListSubheader
            key={`group-${index}`}
            sx={{
              backgroundColor: 'rgba(212, 175, 55, 0.1)',
              color: 'primary.main',
              fontWeight: 600,
              fontSize: '0.875rem',
              lineHeight: '2.5rem',
            }}
          >
            {option.label}
          </ListSubheader>,
          ...option.options.map((subOption) => (
            <StyledMenuItem
              key={subOption.value}
              value={subOption.value}
              disabled={subOption.disabled}
              className={style}
            >
              {multiple && (
                <Box
                  sx={{
                    width: 20,
                    height: 20,
                    border: '2px solid',
                    borderColor: Array.isArray(actualValue) && (actualValue as any[]).includes(subOption.value)
                      ? 'primary.main'
                      : 'text.secondary',
                    borderRadius: 1,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    mr: 1,
                    backgroundColor: Array.isArray(actualValue) && (actualValue as any[]).includes(subOption.value)
                      ? 'primary.main'
                      : 'transparent',
                  }}
                >
                  {Array.isArray(actualValue) && (actualValue as any[]).includes(subOption.value) && (
                    <Check sx={{ fontSize: 14, color: 'primary.contrastText' }} />
                  )}
                </Box>
              )}
              {subOption.icon}
              <Box sx={{ flexGrow: 1 }}>
                <Typography variant="body2">
                  {subOption.label}
                </Typography>
                {subOption.description && (
                  <Typography variant="caption" sx={{ color: 'text.secondary', display: 'block' }}>
                    {subOption.description}
                  </Typography>
                )}
              </Box>
            </StyledMenuItem>
          ))
        ];
      }

      return (
        <StyledMenuItem
          key={option.value}
          value={option.value}
          disabled={option.disabled}
          className={style}
        >
          {multiple && (
            <Box
              sx={{
                width: 20,
                height: 20,
                border: '2px solid',
                borderColor: Array.isArray(actualValue) && (actualValue as any[]).includes(option.value)
                  ? 'primary.main'
                  : 'text.secondary',
                borderRadius: 1,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                mr: 1,
                backgroundColor: Array.isArray(actualValue) && (actualValue as any[]).includes(option.value)
                  ? 'primary.main'
                  : 'transparent',
              }}
            >
              {Array.isArray(actualValue) && (actualValue as any[]).includes(option.value) && (
                <Check sx={{ fontSize: 14, color: 'primary.contrastText' }} />
              )}
            </Box>
          )}
          {option.icon}
          <Box sx={{ flexGrow: 1 }}>
            <Typography variant="body2">
              {option.label}
            </Typography>
            {option.description && (
              <Typography variant="caption" sx={{ color: 'text.secondary', display: 'block' }}>
                {option.description}
              </Typography>
            )}
          </Box>
        </StyledMenuItem>
      );
    });
  };

  return (
    <StyledFormControl
      variant={variant}
      size={size}
      required={required}
      disabled={disabled}
      error={error}
      fullWidth={fullWidth}
      className={`${style} ${className || ''}`}
    >
      {label && (
        <InputLabel
          id={`${name}-label`}
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 0.5,
          }}
        >
          {style === 'mystical' && <AutoAwesome sx={{ fontSize: '1rem' }} />}
          {label}
        </InputLabel>
      )}

      <MuiSelect
        labelId={label ? `${name}-label` : undefined}
        id={name}
        name={name}
        value={actualValue}
        label={label}
        multiple={multiple}
        autoFocus={autoFocus}
        displayEmpty={!!placeholder}
        renderValue={renderValue || defaultRenderValue}
        IconComponent={ExpandMore}
        onChange={onChange}
        onBlur={onBlur}
        onFocus={onFocus}
        MenuProps={{
          PaperProps: {
            sx: {
              backgroundColor: 'rgba(26, 26, 46, 0.95)',
              backdropFilter: 'blur(10px)',
              border: '1px solid rgba(212, 175, 55, 0.3)',
              borderRadius: 2,
              maxHeight: 300,
              '&::-webkit-scrollbar': {
                width: '6px',
              },
              '&::-webkit-scrollbar-track': {
                background: 'rgba(255, 255, 255, 0.1)',
                borderRadius: '3px',
              },
              '&::-webkit-scrollbar-thumb': {
                background: 'primary.main',
                borderRadius: '3px',
                '&:hover': {
                  background: 'primary.light',
                },
              },
            },
          },
          transformOrigin: {
            vertical: 'top',
            horizontal: 'left',
          },
          anchorOrigin: {
            vertical: 'bottom',
            horizontal: 'left',
          },
        }}
      >
        {renderOptions()}
      </MuiSelect>

      {helperText && (
        <FormHelperText error={error} sx={{ mx: 2, mt: 1 }}>
          {helperText}
        </FormHelperText>
      )}
    </StyledFormControl>
  );
};

// 预设组件
export const MysticalSelect: React.FC<Omit<SelectProps, 'style'>> = (props) => (
  <Select {...props} style="mystical" />
);

export const MultiSelect: React.FC<Omit<SelectProps, 'multiple'>> = (props) => (
  <Select {...props} multiple />
);

export default Select;