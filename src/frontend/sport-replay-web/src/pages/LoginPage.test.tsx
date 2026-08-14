import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { LoginPage } from '../pages/LoginPage';
import { AuthProvider } from '../hooks/useAuth';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

describe('LoginPage', () => {
  it('renders login form', () => {
    render(
      <QueryClientProvider client={new QueryClient()}>
        <MemoryRouter>
          <AuthProvider>
            <LoginPage />
          </AuthProvider>
        </MemoryRouter>
      </QueryClientProvider>
    );
    expect(screen.getByText(/Entrar al club/i)).toBeInTheDocument();
  });
});
