import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';
import api from '../api/client';
import type { AuthResponse, Role } from '../types';

interface AuthState {
  user: AuthResponse | null;
  login: (email: string, password: string) => Promise<void>;
  register: (payload: { firstName: string; lastName: string; email: string; password: string; role?: Role }) => Promise<void>;
  logout: () => Promise<void>;
}

const Ctx = createContext<AuthState | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthResponse | null>(() => {
    const raw = localStorage.getItem('sr.auth');
    return raw ? (JSON.parse(raw) as AuthResponse) : null;
  });

  const value = useMemo<AuthState>(
    () => ({
      user,
      login: async (email, password) => {
        const { data } = await api.post<AuthResponse>('/auth/login', { email, password });
        localStorage.setItem('sr.auth', JSON.stringify(data));
        setUser(data);
      },
      register: async (payload) => {
        const { data } = await api.post<AuthResponse>('/auth/register', payload);
        localStorage.setItem('sr.auth', JSON.stringify(data));
        setUser(data);
      },
      logout: async () => {
        if (user?.refreshToken) {
          await api.post('/auth/logout', { refreshToken: user.refreshToken }).catch(() => undefined);
        }
        localStorage.removeItem('sr.auth');
        setUser(null);
      }
    }),
    [user]
  );

  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}

export function useAuth() {
  const ctx = useContext(Ctx);
  if (!ctx) throw new Error('AuthProvider missing');
  return ctx;
}
