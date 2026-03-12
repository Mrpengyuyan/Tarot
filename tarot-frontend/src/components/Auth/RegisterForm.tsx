import React, { useState } from 'react';
import {
  Box,
  TextField,
  Button,
  Typography,
  Alert,
  InputAdornment,
  IconButton,
  Link,
  CircularProgress,
  Checkbox,
  FormControlLabel,
} from '@mui/material';
import {
  Person,
  Email,
  Lock,
  Visibility,
  VisibilityOff,
  PersonAdd,
  Badge,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../stores/authStore';
import { authService } from '../../services/authService';
import { ROUTES } from '../../routes/routeConfig';

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

  const [formData, setFormData] = useState<RegisterFormData>({
    username: '',
    email: '',
    password: '',
    confirmPassword: '',
    nickname: '',
    agreeToTerms: false,
  });
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [validationErrors, setValidationErrors] = useState<ValidationErrors>({});

  const handleInputChange = (field: keyof RegisterFormData) => (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    const value = field === 'agreeToTerms' ? event.target.checked : event.target.value;

    setFormData(prev => ({
      ...prev,
      [field]: value,
    }));

    // 清除该字段的验证错误
    if (validationErrors[field]) {
      setValidationErrors(prev => ({
        ...prev,
        [field]: undefined,
      }));
    }
  };

  const validateForm = (): boolean => {
    const errors: ValidationErrors = {};

    // 用户名验证
    if (!formData.username.trim()) {
      errors.username = '请输入用户名';
    } else if (formData.username.length < 3) {
      errors.username = '用户名至少需要3个字符';
    } else if (!/^[a-zA-Z0-9_]+$/.test(formData.username)) {
      errors.username = '用户名只能包含字母、数字和下划线';
    }

    // 邮箱验证
    if (!formData.email.trim()) {
      errors.email = '请输入邮箱地址';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      errors.email = '请输入有效的邮箱地址';
    }

    // 密码验证
    if (!formData.password) {
      errors.password = '请输入密码';
    } else if (formData.password.length < 6) {
      errors.password = '密码至少需要6位字符';
    } else if (!/(?=.*[a-zA-Z])(?=.*\d)/.test(formData.password)) {
      errors.password = '密码必须包含字母和数字';
    }

    // 确认密码验证
    if (!formData.confirmPassword) {
      errors.confirmPassword = '请确认密码';
    } else if (formData.password !== formData.confirmPassword) {
      errors.confirmPassword = '两次输入的密码不一致';
    }

    // 服务条款验证
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
        username: formData.username,
        email: formData.email,
        password: formData.password,
        nickname: formData.nickname || undefined,
      });

      // 注册成功后自动登录
      await authService.login({
        username: formData.username,
        password: formData.password,
      });

      await authService.getCurrentUser();

      // 设置登录状态
      // login(userInfo, authResponse.access_token);

      // 跳转到登录页面并显示成功消息
      navigate(ROUTES.LOGIN, {
        state: {
          message: '注册成功！请使用您的账户登录。',
          type: 'success'
        }
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : '注册失败，请重试');
    } finally {
      setLoading(false);
    }
  };

  const handleTogglePasswordVisibility = () => {
    setShowPassword(!showPassword);
  };

  const handleToggleConfirmPasswordVisibility = () => {
    setShowConfirmPassword(!showConfirmPassword);
  };

  return (
    <Box
      component="form"
      onSubmit={handleSubmit}
      sx={{
        width: '100%',
        maxWidth: 450,
        mx: 'auto',
      }}
    >
      {/* 表单标题 */}
      <Box sx={{ textAlign: 'center', mb: 4 }}>
        <PersonAdd
          sx={{
            fontSize: '3rem',
            color: 'primary.main',
            filter: 'drop-shadow(0 0 10px rgba(212, 175, 55, 0.5))',
            mb: 2,
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
          加入塔罗之境
        </Typography>
        <Typography
          variant="body1"
          sx={{
            color: 'text.secondary',
            fontStyle: 'italic',
          }}
        >
          开启您的神秘占卜之旅
        </Typography>
      </Box>

      {/* 错误提示 */}
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

      {/* 用户名输入框 */}
      <TextField
        fullWidth
        label="用户名"
        value={formData.username}
        onChange={handleInputChange('username')}
        error={!!validationErrors.username}
        helperText={validationErrors.username || '3-20个字符，只能包含字母、数字和下划线'}
        disabled={isLoading}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <Person sx={{ color: 'primary.main' }} />
            </InputAdornment>
          ),
        }}
        sx={{
          mb: 2,
          '& .MuiOutlinedInput-root': {
            background: 'rgba(16, 8, 32, 0.6)',
            transition: 'all 0.3s ease',
            '&:hover': {
              background: 'rgba(26, 11, 46, 0.8)',
            },
            '&:hover .MuiOutlinedInput-notchedOutline': {
              borderColor: 'primary.main',
              boxShadow: '0 0 8px rgba(212, 175, 55, 0.3)',
            },
            '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
              borderColor: 'primary.main',
              borderWidth: '2px',
              boxShadow: '0 0 15px rgba(212, 175, 55, 0.5)',
            },
          },
          '& .MuiInputLabel-root': {
            color: 'rgba(255, 255, 255, 0.7)',
            fontSize: '1.1rem',
            letterSpacing: '0.5px',
          },
          '& .MuiInputLabel-root.Mui-focused': {
            color: 'primary.main',
            textShadow: '0 0 8px rgba(212, 175, 55, 0.5)',
          },
          '& .MuiOutlinedInput-input': {
            color: '#E8E8E8',
            fontSize: '1.1rem',
            padding: '16px 14px',
          }
        }}
      />

      {/* 昵称输入框 */}
      <TextField
        fullWidth
        label="昵称（可选）"
        value={formData.nickname}
        onChange={handleInputChange('nickname')}
        disabled={isLoading}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <Badge sx={{ color: 'primary.main' }} />
            </InputAdornment>
          ),
        }}
        sx={{
          mb: 2,
          '& .MuiOutlinedInput-root': {
            background: 'rgba(16, 8, 32, 0.6)',
            transition: 'all 0.3s ease',
            '&:hover': {
              background: 'rgba(26, 11, 46, 0.8)',
            },
            '&:hover .MuiOutlinedInput-notchedOutline': {
              borderColor: 'primary.main',
              boxShadow: '0 0 8px rgba(212, 175, 55, 0.3)',
            },
            '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
              borderColor: 'primary.main',
              borderWidth: '2px',
              boxShadow: '0 0 15px rgba(212, 175, 55, 0.5)',
            },
          },
          '& .MuiInputLabel-root': {
            color: 'rgba(255, 255, 255, 0.7)',
            fontSize: '1.1rem',
            letterSpacing: '0.5px',
          },
          '& .MuiInputLabel-root.Mui-focused': {
            color: 'primary.main',
            textShadow: '0 0 8px rgba(212, 175, 55, 0.5)',
          },
          '& .MuiOutlinedInput-input': {
            color: '#E8E8E8',
            fontSize: '1.1rem',
            padding: '16px 14px',
          }
        }}
      />

      {/* 邮箱输入框 */}
      <TextField
        fullWidth
        label="邮箱地址"
        type="email"
        value={formData.email}
        onChange={handleInputChange('email')}
        error={!!validationErrors.email}
        helperText={validationErrors.email}
        disabled={isLoading}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <Email sx={{ color: 'primary.main' }} />
            </InputAdornment>
          ),
        }}
        sx={{
          mb: 3,
          '& .MuiOutlinedInput-root': {
            background: 'rgba(16, 8, 32, 0.6)',
            transition: 'all 0.3s ease',
            '&:hover': {
              background: 'rgba(26, 11, 46, 0.8)',
            },
            '&:hover .MuiOutlinedInput-notchedOutline': {
              borderColor: 'primary.main',
              boxShadow: '0 0 8px rgba(212, 175, 55, 0.3)',
            },
            '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
              borderColor: 'primary.main',
              borderWidth: '2px',
              boxShadow: '0 0 15px rgba(212, 175, 55, 0.5)',
            },
          },
          '& .MuiInputLabel-root': {
            color: 'rgba(255, 255, 255, 0.7)',
            fontSize: '1.1rem',
            letterSpacing: '0.5px',
          },
          '& .MuiInputLabel-root.Mui-focused': {
            color: 'primary.main',
            textShadow: '0 0 8px rgba(212, 175, 55, 0.5)',
          },
          '& .MuiOutlinedInput-input': {
            color: '#E8E8E8',
            fontSize: '1.1rem',
            padding: '16px 14px',
          }
        }}
      />

      {/* 密码输入框 */}
      <TextField
        fullWidth
        label="密码"
        type={showPassword ? 'text' : 'password'}
        value={formData.password}
        onChange={handleInputChange('password')}
        error={!!validationErrors.password}
        helperText={validationErrors.password || '至少6位，必须包含字母和数字'}
        disabled={isLoading}
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
                onClick={handleTogglePasswordVisibility}
                edge="end"
                sx={{ color: 'primary.main' }}
              >
                {showPassword ? <VisibilityOff /> : <Visibility />}
              </IconButton>
            </InputAdornment>
          ),
        }}
        sx={{
          mb: 2,
          '& .MuiOutlinedInput-root': {
            background: 'rgba(16, 8, 32, 0.6)',
            transition: 'all 0.3s ease',
            '&:hover': {
              background: 'rgba(26, 11, 46, 0.8)',
            },
            '&:hover .MuiOutlinedInput-notchedOutline': {
              borderColor: 'primary.main',
              boxShadow: '0 0 8px rgba(212, 175, 55, 0.3)',
            },
            '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
              borderColor: 'primary.main',
              borderWidth: '2px',
              boxShadow: '0 0 15px rgba(212, 175, 55, 0.5)',
            },
          },
          '& .MuiInputLabel-root': {
            color: 'rgba(255, 255, 255, 0.7)',
            fontSize: '1.1rem',
            letterSpacing: '0.5px',
          },
          '& .MuiInputLabel-root.Mui-focused': {
            color: 'primary.main',
            textShadow: '0 0 8px rgba(212, 175, 55, 0.5)',
          },
          '& .MuiOutlinedInput-input': {
            color: '#E8E8E8',
            fontSize: '1.1rem',
            padding: '16px 14px',
          }
        }}
      />

      {/* 确认密码输入框 */}
      <TextField
        fullWidth
        label="确认密码"
        type={showConfirmPassword ? 'text' : 'password'}
        value={formData.confirmPassword}
        onChange={handleInputChange('confirmPassword')}
        error={!!validationErrors.confirmPassword}
        helperText={validationErrors.confirmPassword}
        disabled={isLoading}
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
                onClick={handleToggleConfirmPasswordVisibility}
                edge="end"
                sx={{ color: 'primary.main' }}
              >
                {showConfirmPassword ? <VisibilityOff /> : <Visibility />}
              </IconButton>
            </InputAdornment>
          ),
        }}
        sx={{
          mb: 3,
          '& .MuiOutlinedInput-root': {
            background: 'rgba(16, 8, 32, 0.6)',
            transition: 'all 0.3s ease',
            '&:hover': {
              background: 'rgba(26, 11, 46, 0.8)',
            },
            '&:hover .MuiOutlinedInput-notchedOutline': {
              borderColor: 'primary.main',
              boxShadow: '0 0 8px rgba(212, 175, 55, 0.3)',
            },
            '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
              borderColor: 'primary.main',
              borderWidth: '2px',
              boxShadow: '0 0 15px rgba(212, 175, 55, 0.5)',
            },
          },
          '& .MuiInputLabel-root': {
            color: 'rgba(255, 255, 255, 0.7)',
            fontSize: '1.1rem',
            letterSpacing: '0.5px',
          },
          '& .MuiInputLabel-root.Mui-focused': {
            color: 'primary.main',
            textShadow: '0 0 8px rgba(212, 175, 55, 0.5)',
          },
          '& .MuiOutlinedInput-input': {
            color: '#E8E8E8',
            fontSize: '1.1rem',
            padding: '16px 14px',
          }
        }}
      />

      {/* 服务条款复选框 */}
      <FormControlLabel
        control={
          <Checkbox
            checked={formData.agreeToTerms}
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
        sx={{ mb: 3 }}
      />

      {/* 注册按钮 */}
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
          fontSize: '1.2rem',
          fontFamily: 'Cinzel, serif',
          fontWeight: 700,
          background: 'linear-gradient(45deg, #1A0B2E, #D4AF37, #1A0B2E)',
          backgroundSize: '200% auto',
          color: '#FFF',
          border: '1px solid rgba(212, 175, 55, 0.5)',
          boxShadow: '0 4px 20px rgba(0, 240, 255, 0.2), inset 0 0 10px rgba(212, 175, 55, 0.2)',
          transition: 'all 0.4s ease',
          '&:hover': {
            backgroundPosition: 'right center',
            boxShadow: '0 6px 25px rgba(0, 240, 255, 0.4), inset 0 0 15px rgba(212, 175, 55, 0.4)',
            transform: 'translateY(-2px)',
            borderColor: '#00F0FF',
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

      {/* 登录链接 */}
      <Box sx={{ textAlign: 'center' }}>
        <Typography
          variant="body2"
          sx={{ color: 'text.secondary', mb: 1 }}
        >
          已有账户？
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