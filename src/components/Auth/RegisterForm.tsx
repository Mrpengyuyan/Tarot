import React, { useRef, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  CircularProgress,
  FormControlLabel,
  IconButton,
  InputAdornment,
  Link,
  TextField,
  Typography,
} from '@mui/material';
import {
  Badge,
  Email,
  Lock,
  Person,
  PersonAdd,
  Visibility,
  VisibilityOff,
} from 'icons';
import { useNavigate } from 'react-router-dom';
import { ROUTES } from '../../routes/routeConfig';
import { authService } from '../../services/authService';
import { useAuthStore } from '../../stores/authStore';
import { AUTH_TEXT_FIELD_SX } from './authFieldStyles';

interface RegisterFormData {
  username: string;
  email: string;
  password: string;
  confirmPassword: string;
  nickname?: string;
  agreeToTerms: boolean;
}

interface ValidationErrors {
  username?: string;
  email?: string;
  password?: string;
  confirmPassword?: string;
  nickname?: string;
  agreeToTerms?: string;
}

interface RegisterFormProps {
  onSwitchToLogin?: () => void;
}

const RegisterForm: React.FC<RegisterFormProps> = ({ onSwitchToLogin }) => {
  const navigate = useNavigate();
  const { setLoading, setError, error, isLoading } = useAuthStore();

  const formDataRef = useRef<RegisterFormData>({
    username: '',
    email: '',
    password: '',
    confirmPassword: '',
    nickname: '',
    agreeToTerms: false,
  });
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [agreeToTerms, setAgreeToTerms] = useState(false);
  const [validationErrors, setValidationErrors] = useState<ValidationErrors>({});

  const handleInputChange =
    (field: keyof RegisterFormData) =>
    (event: React.ChangeEvent<HTMLInputElement>) => {
      const value = field === 'agreeToTerms' ? event.target.checked : event.target.value;
      formDataRef.current[field] = value as never;

      if (field === 'agreeToTerms') {
        setAgreeToTerms(Boolean(value));
      }

      if (validationErrors[field]) {
        setValidationErrors((prev) => ({
          ...prev,
          [field]: undefined,
        }));
      }
    };

  const validateForm = () => {
    const errors: ValidationErrors = {};
    const formData = formDataRef.current;

    if (!formData.username.trim()) {
      errors.username = '请输入用户名';
    } else if (formData.username.length < 3) {
      errors.username = '用户名至少需要 3 个字符';
    } else if (!/^[a-zA-Z0-9_]+$/.test(formData.username)) {
      errors.username = '用户名只能包含字母、数字和下划线';
    }

    if (!formData.email.trim()) {
      errors.email = '请输入邮箱地址';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      errors.email = '请输入有效的邮箱地址';
    }

    if (!formData.password) {
      errors.password = '请输入密码';
    } else if (formData.password.length < 8) {
      errors.password = '密码至少需要 8 位字符';
    } else if (!/(?=.*[a-zA-Z])(?=.*\d)/.test(formData.password)) {
      errors.password = '密码必须包含字母和数字';
    }

    if (!formData.confirmPassword) {
      errors.confirmPassword = '请确认密码';
    } else if (formData.password !== formData.confirmPassword) {
      errors.confirmPassword = '两次输入的密码不一致';
    }

    if (!formData.agreeToTerms) {
      errors.agreeToTerms = '请同意服务条款和隐私政策';
    }

    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!validateForm()) {
      return;
    }

    setLoading(true);
    setError(null);

    try {
      await authService.register({
        username: formDataRef.current.username,
        email: formDataRef.current.email,
        password: formDataRef.current.password,
        nickname: formDataRef.current.nickname || undefined,
      });

      navigate(ROUTES.LOGIN, {
        state: {
          message: '注册成功，请使用你的新账号登录。',
          type: 'success',
        },
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : '注册失败，请重试');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box
      component="form"
      onSubmit={handleSubmit}
      sx={{ width: '100%', maxWidth: 450, mx: 'auto', contain: 'layout paint style', isolation: 'isolate' }}
    >
      <Box sx={{ textAlign: 'center', mb: 4 }}>
        <PersonAdd
          sx={{
            fontSize: '3rem',
            color: 'primary.main',
            mb: 2,
            filter: 'drop-shadow(0 0 8px rgba(212, 175, 55, 0.24))',
          }}
        />
        <Typography
          variant="h4"
          component="h1"
          sx={{
            fontFamily: 'Cinzel, serif',
            fontWeight: 700,
            background: 'linear-gradient(45deg, #D4AF37, #FFFFFF)',
            backgroundClip: 'text',
            WebkitBackgroundClip: 'text',
            WebkitTextFillColor: 'transparent',
            mb: 1,
          }}
        >
          加入塔罗之境
        </Typography>
        <Typography variant="body1" sx={{ color: 'text.secondary', fontStyle: 'italic' }}>
          开启你的第一段神秘占卜旅程
        </Typography>
      </Box>

      {error && (
        <Alert
          severity="error"
          sx={{
            mb: 3,
            background: 'rgba(255, 107, 107, 0.1)',
            border: '1px solid rgba(255, 107, 107, 0.3)',
            color: 'error.main',
            '& .MuiAlert-icon': {
              color: 'error.main',
            },
          }}
        >
          {error}
        </Alert>
      )}

      <TextField
        fullWidth
        defaultValue=""
        label="用户名"
        onChange={handleInputChange('username')}
        error={!!validationErrors.username}
        helperText={validationErrors.username || '3-20 个字符，只能包含字母、数字和下划线'}
        disabled={isLoading}
        autoComplete="username"
        inputProps={{
          spellCheck: false,
          autoCapitalize: 'none',
          autoCorrect: 'off',
        }}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <Person sx={{ color: 'primary.main' }} />
            </InputAdornment>
          ),
        }}
        sx={{ ...AUTH_TEXT_FIELD_SX, mb: 2 }}
      />

      <TextField
        fullWidth
        defaultValue=""
        label="昵称（可选）"
        onChange={handleInputChange('nickname')}
        disabled={isLoading}
        inputProps={{
          spellCheck: false,
        }}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <Badge sx={{ color: 'primary.main' }} />
            </InputAdornment>
          ),
        }}
        sx={{ ...AUTH_TEXT_FIELD_SX, mb: 2 }}
      />

      <TextField
        fullWidth
        defaultValue=""
        label="邮箱地址"
        type="email"
        onChange={handleInputChange('email')}
        error={!!validationErrors.email}
        helperText={validationErrors.email}
        disabled={isLoading}
        autoComplete="email"
        inputProps={{
          spellCheck: false,
          autoCapitalize: 'none',
          autoCorrect: 'off',
        }}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <Email sx={{ color: 'primary.main' }} />
            </InputAdornment>
          ),
        }}
        sx={{ ...AUTH_TEXT_FIELD_SX, mb: 3 }}
      />

      <TextField
        fullWidth
        defaultValue=""
        label="密码"
        type={showPassword ? 'text' : 'password'}
        onChange={handleInputChange('password')}
        error={!!validationErrors.password}
        helperText={validationErrors.password || '至少 8 位，必须包含字母和数字'}
        disabled={isLoading}
        autoComplete="new-password"
        inputProps={{
          spellCheck: false,
        }}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <Lock sx={{ color: 'primary.main' }} />
            </InputAdornment>
          ),
          endAdornment: (
            <InputAdornment position="end">
              <IconButton
                aria-label="toggle password visibility"
                onClick={() => setShowPassword((prev) => !prev)}
                edge="end"
                sx={{ color: 'rgba(212, 175, 55, 0.74)' }}
              >
                {showPassword ? <VisibilityOff /> : <Visibility />}
              </IconButton>
            </InputAdornment>
          ),
        }}
        sx={{ ...AUTH_TEXT_FIELD_SX, mb: 2 }}
      />

      <TextField
        fullWidth
        defaultValue=""
        label="确认密码"
        type={showConfirmPassword ? 'text' : 'password'}
        onChange={handleInputChange('confirmPassword')}
        error={!!validationErrors.confirmPassword}
        helperText={validationErrors.confirmPassword}
        disabled={isLoading}
        autoComplete="new-password"
        inputProps={{
          spellCheck: false,
        }}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <Lock sx={{ color: 'primary.main' }} />
            </InputAdornment>
          ),
          endAdornment: (
            <InputAdornment position="end">
              <IconButton
                aria-label="toggle confirm password visibility"
                onClick={() => setShowConfirmPassword((prev) => !prev)}
                edge="end"
                sx={{ color: 'rgba(212, 175, 55, 0.74)' }}
              >
                {showConfirmPassword ? <VisibilityOff /> : <Visibility />}
              </IconButton>
            </InputAdornment>
          ),
        }}
        sx={{ ...AUTH_TEXT_FIELD_SX, mb: 3 }}
      />

      <FormControlLabel
        control={
          <Checkbox
            checked={agreeToTerms}
            onChange={handleInputChange('agreeToTerms')}
            sx={{
              color: 'primary.main',
              '&.Mui-checked': {
                color: 'primary.main',
              },
            }}
          />
        }
        label={
          <Typography variant="body2" sx={{ color: 'text.secondary' }}>
            我已阅读并同意
            <Link href="#" sx={{ color: 'primary.main', mx: 0.5 }}>
              服务条款
            </Link>
            和
            <Link href="#" sx={{ color: 'primary.main', mx: 0.5 }}>
              隐私政策
            </Link>
          </Typography>
        }
        sx={{ mb: 1 }}
      />

      {validationErrors.agreeToTerms && (
        <Typography variant="caption" sx={{ color: 'error.main', display: 'block', mb: 2.5 }}>
          {validationErrors.agreeToTerms}
        </Typography>
      )}

      <Button
        type="submit"
        fullWidth
        variant="contained"
        size="large"
        disabled={isLoading}
        startIcon={isLoading ? <CircularProgress size={20} /> : <PersonAdd />}
        sx={{
          py: 1.5,
          mb: 3,
          fontSize: '1.08rem',
          fontFamily: 'Cinzel, serif',
          fontWeight: 700,
          background: 'linear-gradient(45deg, #1A0B2E, #D4AF37, #1A0B2E)',
          backgroundSize: '180% auto',
          color: '#FFF',
          border: '1px solid rgba(212, 175, 55, 0.42)',
          boxShadow: '0 10px 24px rgba(0, 0, 0, 0.24)',
          transition: 'background-position 180ms ease, transform 180ms ease, box-shadow 180ms ease',
          '&:hover': {
            backgroundPosition: 'right center',
            boxShadow: '0 14px 28px rgba(0, 0, 0, 0.28)',
            transform: 'translateY(-1px)',
            borderColor: 'rgba(0, 240, 255, 0.32)',
          },
          '&:disabled': {
            background: 'rgba(26, 11, 46, 0.6)',
            color: 'rgba(255, 255, 255, 0.3)',
            borderColor: 'rgba(255, 255, 255, 0.1)',
            boxShadow: 'none',
          },
        }}
      >
        {isLoading ? '正在注册...' : '创建账户'}
      </Button>

      <Box sx={{ textAlign: 'center' }}>
        <Typography variant="body2" sx={{ color: 'text.secondary', mb: 1 }}>
          已有账号？
        </Typography>
        <Link
          component="button"
          type="button"
          onClick={onSwitchToLogin}
          variant="body2"
          sx={{
            color: 'primary.main',
            textDecoration: 'none',
            fontWeight: 500,
            '&:hover': {
              textDecoration: 'underline',
              color: 'primary.light',
            },
          }}
        >
          立即登录
        </Link>
      </Box>
    </Box>
  );
};

export default RegisterForm;
