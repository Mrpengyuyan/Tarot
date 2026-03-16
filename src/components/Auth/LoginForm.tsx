import React, { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  IconButton,
  InputAdornment,
  Link,
  TextField,
  Typography,
} from '@mui/material';
import {
  Lock,
  Login as LoginIcon,
  Person,
  Visibility,
  VisibilityOff,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';

import { ROUTES } from '../../routes/routeConfig';
import { authService } from '../../services/authService';
import { useAuthStore } from '../../stores/authStore';

interface LoginFormData {
  username: string;
  password: string;
}

interface ValidationErrors {
  username?: string;
  password?: string;
}

interface LoginFormProps {
  onSwitchToRegister?: () => void;
}

const LoginForm: React.FC<LoginFormProps> = ({ onSwitchToRegister }) => {
  const navigate = useNavigate();
  const { login, setLoading, setError, error, isLoading } = useAuthStore();

  const [formData, setFormData] = useState<LoginFormData>({
    username: '',
    password: '',
  });
  const [showPassword, setShowPassword] = useState(false);
  const [validationErrors, setValidationErrors] = useState<ValidationErrors>({});

  const handleInputChange =
    (field: keyof LoginFormData) =>
    (event: React.ChangeEvent<HTMLInputElement>) => {
      setFormData((prev) => ({ ...prev, [field]: event.target.value }));
      if (validationErrors[field]) {
        setValidationErrors((prev) => ({ ...prev, [field]: undefined }));
      }
    };

  const validateForm = (): boolean => {
    const errors: ValidationErrors = {};

    if (!formData.username.trim()) {
      errors.username = '请输入用户名或邮箱';
    }

    if (!formData.password) {
      errors.password = '请输入密码';
    } else if (formData.password.length < 6) {
      errors.password = '密码长度至少 6 位';
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
      const authResponse = await authService.login(formData);

      if (!authResponse.access_token) {
        throw new Error('登录响应缺少 access token');
      }

      const userInfo = await authService.getCurrentUser();
      login(userInfo, authResponse.access_token);
      navigate(ROUTES.DASHBOARD);
    } catch (err) {
      setError(err instanceof Error ? err.message : '登录失败，请检查用户名和密码');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box component="form" onSubmit={handleSubmit} sx={{ width: '100%', maxWidth: 400, mx: 'auto' }}>
      <Box sx={{ textAlign: 'center', mb: 4 }}>
        <LoginIcon
          sx={{
            fontSize: '3rem',
            color: 'primary.main',
            mb: 2,
            filter: 'drop-shadow(0 0 10px rgba(212, 175, 55, 0.5))',
            animation: 'float 3s ease-in-out infinite',
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
            textShadow: '0 0 20px rgba(212, 175, 55, 0.3)',
            mb: 1,
          }}
        >
          欢迎回来
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
            '& .MuiAlert-icon': { color: 'error.main' },
          }}
        >
          {error}
        </Alert>
      )}

      <TextField
        fullWidth
        label="用户名或邮箱"
        value={formData.username}
        onChange={handleInputChange('username')}
        error={!!validationErrors.username}
        helperText={validationErrors.username}
        disabled={isLoading}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <Person sx={{ color: 'primary.main' }} />
            </InputAdornment>
          ),
        }}
        sx={{ mb: 3 }}
      />

      <TextField
        fullWidth
        type={showPassword ? 'text' : 'password'}
        label="密码"
        value={formData.password}
        onChange={handleInputChange('password')}
        error={!!validationErrors.password}
        helperText={validationErrors.password}
        disabled={isLoading}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <Lock sx={{ color: 'primary.main' }} />
            </InputAdornment>
          ),
          endAdornment: (
            <InputAdornment position="end">
              <IconButton onClick={() => setShowPassword((prev) => !prev)} edge="end" disabled={isLoading}>
                {showPassword ? <VisibilityOff /> : <Visibility />}
              </IconButton>
            </InputAdornment>
          ),
        }}
        sx={{ mb: 3 }}
      />

      <Button
        type="submit"
        fullWidth
        variant="contained"
        size="large"
        disabled={isLoading}
        sx={{ py: 1.5, mb: 2, fontWeight: 600 }}
      >
        {isLoading ? <CircularProgress size={24} color="inherit" /> : '登录'}
      </Button>

      <Box sx={{ textAlign: 'center' }}>
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          还没有账号？{' '}
          <Link component="button" type="button" onClick={onSwitchToRegister} sx={{ color: 'primary.main' }}>
            立即注册
          </Link>
        </Typography>
      </Box>
    </Box>
  );
};

export default LoginForm;
