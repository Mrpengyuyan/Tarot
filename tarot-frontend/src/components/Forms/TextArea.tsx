import React from 'react';
import {
  TextField,
  FormControl,
  FormLabel,
  FormHelperText,
  Box,
  Typography,
  styled,
  alpha,
  IconButton,
} from '@mui/material';
import { AutoAwesome, Clear } from '@mui/icons-material';

const StyledTextField = styled(TextField)(({ theme }) => ({
  '& .MuiOutlinedInput-root': {
    backgroundColor: 'rgba(26, 26, 46, 0.6)',
    backdropFilter: 'blur(10px)',
    borderRadius: 12,
    transition: 'all 0.3s ease',
    border: '1px solid rgba(212, 175, 55, 0.2)',
    minHeight: 120,

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

    '& textarea': {
      color: theme.palette.text.primary,
      fontFamily: 'inherit',
      fontSize: '0.875rem',
      lineHeight: 1.6,
      resize: 'vertical',
      minHeight: '80px !important',

      '&::placeholder': {
        color: theme.palette.text.disabled,
        opacity: 0.7,
      },

      '&::-webkit-scrollbar': {
        width: '6px',
      },
      '&::-webkit-scrollbar-track': {
        background: 'rgba(255, 255, 255, 0.1)',
        borderRadius: '3px',
      },
      '&::-webkit-scrollbar-thumb': {
        background: theme.palette.primary.main,
        borderRadius: '3px',
        '&:hover': {
          background: theme.palette.primary.light,
        },
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
          radial-gradient(circle at 20% 20%, rgba(212, 175, 55, 0.1) 0%, transparent 50%),
          radial-gradient(circle at 80% 80%, rgba(106, 5, 114, 0.1) 0%, transparent 50%)
        `,
        pointerEvents: 'none',
        borderRadius: 12,
        zIndex: 0,
      },

      '& textarea': {
        position: 'relative',
        zIndex: 1,
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

const StyledFormControl = styled(FormControl)(({ theme }) => ({
  width: '100%',
  position: 'relative',
}));

const CharacterCountBox = styled(Box)(({ theme }) => ({
  position: 'absolute',
  bottom: theme.spacing(1),
  right: theme.spacing(2),
  fontSize: '0.75rem',
  color: theme.palette.text.secondary,
  backgroundColor: 'rgba(26, 26, 46, 0.8)',
  padding: theme.spacing(0.5, 1),
  borderRadius: 4,
  backdropFilter: 'blur(10px)',
  border: '1px solid rgba(212, 175, 55, 0.2)',
  pointerEvents: 'none',
  zIndex: 2,
}));

export interface TextAreaProps {
  name: string;
  label?: string;
  placeholder?: string;
  value?: string;
  rows?: number;
  minRows?: number;
  maxRows?: number;
  required?: boolean;
  disabled?: boolean;
  error?: boolean;
  helperText?: string;
  maxLength?: number;
  showCharacterCount?: boolean;
  clearable?: boolean;
  style?: 'default' | 'mystical';
  variant?: 'outlined' | 'filled' | 'standard';
  fullWidth?: boolean;
  autoFocus?: boolean;
  className?: string;
  onChange?: (event: React.ChangeEvent<HTMLTextAreaElement>) => void;
  onBlur?: (event: React.FocusEvent<HTMLTextAreaElement>) => void;
  onFocus?: (event: React.FocusEvent<HTMLTextAreaElement>) => void;
}

const TextArea: React.FC<TextAreaProps> = ({
  name,
  label,
  placeholder,
  value = '',
  rows = 4,
  minRows,
  maxRows,
  required = false,
  disabled = false,
  error = false,
  helperText,
  maxLength,
  showCharacterCount = false,
  clearable = false,
  style = 'default',
  variant = 'outlined',
  fullWidth = true,
  autoFocus = false,
  className,
  onChange,
  onBlur,
  onFocus,
}) => {
  const [focused, setFocused] = React.useState(false);
  const characterCount = value.length;
  const isOverLimit = Boolean(maxLength && characterCount > maxLength);

  const handleChange = (event: React.ChangeEvent<HTMLTextAreaElement>) => {
    const newValue = event.target.value;
    if (maxLength && newValue.length > maxLength) {
      return; // 阻止超出字符限制
    }
    onChange?.(event);
  };

  const handleClear = () => {
    if (onChange) {
      const syntheticEvent = {
        target: { value: '', name },
        currentTarget: { value: '', name },
      } as React.ChangeEvent<HTMLTextAreaElement>;
      onChange(syntheticEvent);
    }
  };

  const handleFocus = (event: React.FocusEvent<HTMLTextAreaElement>) => {
    setFocused(true);
    onFocus?.(event);
  };

  const handleBlur = (event: React.FocusEvent<HTMLTextAreaElement>) => {
    setFocused(false);
    onBlur?.(event);
  };

  if (variant === 'outlined') {
    return (
      <StyledFormControl className={className}>
        {label && (
          <StyledFormLabel
            htmlFor={name}
            className={`${required ? 'required' : ''} ${style}`}
          >
            {style === 'mystical' && <AutoAwesome sx={{ fontSize: '1rem' }} />}
            {label}
          </StyledFormLabel>
        )}

        <Box sx={{ position: 'relative' }}>
          <StyledTextField
            id={name}
            name={name}
            value={value}
            placeholder={placeholder}
            multiline
            rows={rows}
            minRows={minRows}
            maxRows={maxRows}
            required={required}
            disabled={disabled}
            error={error || isOverLimit}
            fullWidth={fullWidth}
            autoFocus={autoFocus}
            variant="outlined"
            className={style}
            onChange={handleChange}
            onFocus={handleFocus}
            onBlur={handleBlur}
            InputProps={{
              endAdornment: clearable && value && !disabled && (
                <Box
                  sx={{
                    position: 'absolute',
                    top: 8,
                    right: 8,
                    zIndex: 2,
                  }}
                >
                  <IconButton
                    size="small"
                    onClick={handleClear}
                    sx={{
                      color: 'text.secondary',
                      backgroundColor: 'rgba(26, 26, 46, 0.8)',
                      backdropFilter: 'blur(10px)',
                      border: '1px solid rgba(212, 175, 55, 0.2)',
                      '&:hover': {
                        color: 'primary.main',
                        borderColor: 'rgba(212, 175, 55, 0.4)',
                      },
                    }}
                  >
                    <Clear fontSize="small" />
                  </IconButton>
                </Box>
              ),
            }}
            sx={{
              mt: label ? 1 : 0,
            }}
          />

          {(showCharacterCount || maxLength) && (
            <CharacterCountBox>
              <Typography
                variant="caption"
                sx={{
                  color: isOverLimit ? 'error.main' : 'text.secondary',
                  fontWeight: isOverLimit ? 600 : 400,
                }}
              >
                {characterCount}
                {maxLength && `/${maxLength}`}
              </Typography>
            </CharacterCountBox>
          )}
        </Box>

        {(helperText || isOverLimit) && (
          <FormHelperText
            error={error || isOverLimit}
            sx={{ mx: 2, mt: 1 }}
          >
            {isOverLimit
              ? `字符数量超出限制 (${characterCount}/${maxLength})`
              : helperText
            }
          </FormHelperText>
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
      multiline
      rows={rows}
      minRows={minRows}
      maxRows={maxRows}
      required={required}
      disabled={disabled}
      error={error || isOverLimit}
      helperText={
        isOverLimit
          ? `字符数量超出限制 (${characterCount}/${maxLength})`
          : helperText
      }
      fullWidth={fullWidth}
      autoFocus={autoFocus}
      variant={variant}
      className={className}
      onChange={handleChange}
      onFocus={handleFocus}
      onBlur={handleBlur}
    />
  );
};

// 预设组件
export const MysticalTextArea: React.FC<Omit<TextAreaProps, 'style'>> = (props) => (
  <TextArea {...props} style="mystical" />
);

export const QuestionTextArea: React.FC<Omit<TextAreaProps, 'placeholder' | 'maxLength' | 'showCharacterCount'>> = (props) => (
  <TextArea
    {...props}
    placeholder="请详细描述您想要占卜的问题或困扰..."
    maxLength={500}
    showCharacterCount
    clearable
    style="mystical"
  />
);

export const FeedbackTextArea: React.FC<Omit<TextAreaProps, 'placeholder' | 'rows'>> = (props) => (
  <TextArea
    {...props}
    placeholder="请分享您的想法和建议..."
    rows={6}
    clearable
  />
);

export default TextArea;