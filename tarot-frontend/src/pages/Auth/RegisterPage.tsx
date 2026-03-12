import React from 'react';
import { Navigate } from 'react-router-dom';
import { ROUTES } from '../../routes/routeConfig';

// 注册页面重定向到登录页面（因为我们使用标签页形式）
const RegisterPage: React.FC = () => {
  return <Navigate to={ROUTES.LOGIN} replace />;
};

export default RegisterPage;