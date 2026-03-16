import React, { useEffect } from 'react';
import { Box, CircularProgress } from '@mui/material';

import AppRouter from './routes/AppRouter';
import { useAuthStore } from './stores/authStore';
import './styles/globals.css';

function App() {
  const { initializeAuth, isInitialized, isLoading } = useAuthStore();

  useEffect(() => {
    void initializeAuth();
  }, [initializeAuth]);

  if (!isInitialized || isLoading) {
    return (
      <Box
        display="flex"
        justifyContent="center"
        alignItems="center"
        minHeight="100vh"
        bgcolor="background.default"
        flexDirection="column"
        gap={2}
      >
        <CircularProgress
          size={60}
          sx={{
            color: 'primary.main',
            '& .MuiCircularProgress-circle': {
              strokeLinecap: 'round',
            },
          }}
        />
        <Box
          component="span"
          sx={{
            color: 'text.secondary',
            fontSize: '1rem',
            fontFamily: 'Cinzel, serif',
          }}
        >
          正在初始化会话...
        </Box>
      </Box>
    );
  }

  return <AppRouter />;
}

export default App;
