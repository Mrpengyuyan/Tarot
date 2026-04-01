import React, { Suspense } from 'react';
import {
  Navigate,
  Outlet,
  RouterProvider,
  createBrowserRouter,
} from 'react-router-dom';
import { Box, CircularProgress } from '@mui/material';
import { useAuthStore } from '../stores/authStore';
import { ROUTES } from './routeConfig';

const AppLayout = React.lazy(() => import('../components/Layout/AppLayout'));
const HomePage = React.lazy(() => import('../pages/Dashboard/HomePage'));
const LoginPage = React.lazy(() => import('../pages/Auth/LoginPage'));
const RegisterPage = React.lazy(() => import('../pages/Auth/RegisterPage'));
const NewReading = React.lazy(() => import('../pages/Reading/NewReading'));
const DrawCards = React.lazy(() => import('../pages/Reading/DrawCards'));
const ReadingDetail = React.lazy(() => import('../pages/Reading/ReadingDetail'));
const HistoryPage = React.lazy(() => import('../pages/History/HistoryPage'));
const ProfilePage = React.lazy(() => import('../pages/Profile/ProfilePage'));
const TarotLibraryPage = React.lazy(() => import('../pages/Library/TarotLibraryPage'));
const SpreadCatalogPage = React.lazy(() => import('../pages/Library/SpreadCatalogPage'));
const FavoritesPage = React.lazy(() => import('../pages/Favorites/FavoritesPage'));
const DemoPage = React.lazy(() => import('../pages/Demo/DemoPage'));
const TarotCardDemo = React.lazy(() => import('../pages/Demo/TarotCardDemo'));
const SystemStatus = React.lazy(() => import('../pages/Debug/SystemStatus'));

const LoadingFallback: React.FC = () => (
  <Box
    display="flex"
    justifyContent="center"
    alignItems="center"
    minHeight="200px"
    flexDirection="column"
    gap={2}
  >
    <CircularProgress
      size={40}
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
        fontSize: '0.875rem',
        fontFamily: 'Cinzel, serif',
      }}
    >
      神秘力量正在汇聚...
    </Box>
  </Box>
);

const ProtectedRoute: React.FC = () => {
  const { isLoggedIn } = useAuthStore();
  if (!isLoggedIn) {
    return <Navigate to={ROUTES.LOGIN} replace />;
  }
  return <Outlet />;
};

const PublicRoute: React.FC = () => {
  const { isLoggedIn } = useAuthStore();
  if (isLoggedIn) {
    return <Navigate to={ROUTES.DASHBOARD} replace />;
  }
  return <Outlet />;
};

const RootLayout: React.FC = () => (
  <Suspense fallback={<LoadingFallback />}>
    <AppLayout>
      <Outlet />
    </AppLayout>
  </Suspense>
);

const router = createBrowserRouter([
  {
    path: '/',
    element: <RootLayout />,
    children: [
      {
        index: true,
        element: <Navigate to={ROUTES.DASHBOARD} replace />,
      },
      {
        path: 'auth',
        element: <PublicRoute />,
        children: [
          {
            path: 'login',
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <LoginPage />
              </Suspense>
            ),
          },
          {
            path: 'register',
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <RegisterPage />
              </Suspense>
            ),
          },
        ],
      },
      {
        element: <ProtectedRoute />,
        children: [
          {
            path: 'dashboard',
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <HomePage />
              </Suspense>
            ),
          },
          {
            path: 'reading',
            children: [
              {
                path: 'new',
                element: (
                  <Suspense fallback={<LoadingFallback />}>
                    <NewReading />
                  </Suspense>
                ),
              },
              {
                path: 'draw',
                element: (
                  <Suspense fallback={<LoadingFallback />}>
                    <DrawCards />
                  </Suspense>
                ),
              },
              {
                path: ':id',
                element: (
                  <Suspense fallback={<LoadingFallback />}>
                    <ReadingDetail />
                  </Suspense>
                ),
              },
            ],
          },
          {
            path: 'history',
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <HistoryPage />
              </Suspense>
            ),
          },
          {
            path: 'profile',
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <ProfilePage />
              </Suspense>
            ),
          },
          {
            path: 'cards',
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <TarotLibraryPage />
              </Suspense>
            ),
          },
          {
            path: 'spreads',
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <SpreadCatalogPage />
              </Suspense>
            ),
          },
          {
            path: 'favorites',
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <FavoritesPage />
              </Suspense>
            ),
          },
          {
            path: 'system-status',
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <SystemStatus />
              </Suspense>
            ),
          },
        ],
      },
      {
        path: 'demo',
        element: (
          <Suspense fallback={<LoadingFallback />}>
            <DemoPage />
          </Suspense>
        ),
      },
      {
        path: 'card-demo',
        element: (
          <Suspense fallback={<LoadingFallback />}>
            <TarotCardDemo />
          </Suspense>
        ),
      },
      {
        path: '*',
        element: (
          <Box
            display="flex"
            flexDirection="column"
            alignItems="center"
            justifyContent="center"
            minHeight="400px"
            textAlign="center"
            p={3}
          >
            <Box
              component="h1"
              sx={{
                fontSize: '4rem',
                fontFamily: 'Cinzel, serif',
                background: 'linear-gradient(45deg, #D4AF37, #FFD700)',
                backgroundClip: 'text',
                WebkitBackgroundClip: 'text',
                WebkitTextFillColor: 'transparent',
                mb: 2,
              }}
            >
              404
            </Box>
            <Box
              component="h2"
              sx={{
                fontSize: '1.5rem',
                color: 'text.primary',
                mb: 1,
              }}
            >
              页面迷失在星辰之中
            </Box>
            <Box
              component="p"
              sx={{
                fontSize: '1rem',
                color: 'text.secondary',
                mb: 3,
              }}
            >
              您寻找的页面似乎不存在，或许是宇宙的安排。
            </Box>
            <Box
              component="button"
              onClick={() => window.history.back()}
              sx={{
                px: 3,
                py: 1,
                background: 'linear-gradient(45deg, #D4AF37, #FFD700)',
                color: 'black',
                border: 'none',
                borderRadius: '8px',
                fontSize: '1rem',
                fontFamily: 'Cinzel, serif',
                cursor: 'pointer',
                transition: 'all 0.3s ease',
                '&:hover': {
                  transform: 'translateY(-2px)',
                  boxShadow: '0 4px 16px rgba(212, 175, 55, 0.3)',
                },
              }}
            >
              返回上一页
            </Box>
          </Box>
        ),
      },
    ],
  },
]);

const AppRouter: React.FC = () => <RouterProvider router={router} />;

export default AppRouter;
