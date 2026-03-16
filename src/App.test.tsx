import React from 'react';
import { render, screen } from '@testing-library/react';
import App from './App';

jest.mock('./routes/AppRouter', () => ({
  __esModule: true,
  default: () => <div>app-router-ready</div>,
}));

jest.mock('./stores/authStore', () => ({
  useAuthStore: () => ({
    initializeAuth: jest.fn(),
    isInitialized: true,
    isLoading: false,
  }),
}));

test('renders app router when auth is initialized', () => {
  render(<App />);
  const routerElement = screen.getByText(/app-router-ready/i);
  expect(routerElement).toBeInTheDocument();
});
