import { createContext, useContext, useState, useEffect, useCallback, type ReactNode } from 'react';
import * as authApi from '../api/auth';
import { getSetupStatus } from '../api/setup';
import type { LoginCredentials, RegisterCredentials } from '../types';

interface AuthUser {
  username: string;
  email: string;
  role: string;
  profileImage?: string;
}

interface AuthContextType {
  user: AuthUser | null;
  token: string | null;
  isAdmin: boolean;
  needsSetup: boolean | null;
  login: (credentials: LoginCredentials) => Promise<void>;
  register: (credentials: RegisterCredentials) => Promise<void>;
  logout: () => void;
  setTokenAndUser: (token: string, user: AuthUser) => void;
  updateProfileImage: (profileImage: string | null) => void;
  updateUsername: (username: string) => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

function decodeTokenPayload(token: string): AuthUser | null {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return {
      username: payload.unique_name ?? payload.sub ?? '',
      email: payload.email ?? '',
      role: payload.role ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? 'Standard',
    };
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('token'));
  const [user, setUser] = useState<AuthUser | null>(() => {
    const t = localStorage.getItem('token');
    return t ? decodeTokenPayload(t) : null;
  });
  const [needsSetup, setNeedsSetup] = useState<boolean | null>(null);

  useEffect(() => {
    getSetupStatus()
      .then((s) => setNeedsSetup(s.needsSetup))
      .catch(() => setNeedsSetup(false));
  }, []);

  useEffect(() => {
    if (token) {
      setUser(decodeTokenPayload(token));
    } else {
      setUser(null);
    }
  }, [token]);

  const login = useCallback(async (credentials: LoginCredentials) => {
    const res = await authApi.login(credentials);
    setToken(res.token);
    setUser({ username: res.username, email: res.email, role: res.role, profileImage: res.profileImage });
  }, []);

  const register = useCallback(async (credentials: RegisterCredentials) => {
    const res = await authApi.register(credentials);
    setToken(res.token);
    setUser({ username: res.username, email: res.email, role: res.role, profileImage: res.profileImage });
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem('token');
    setToken(null);
    setUser(null);
  }, []);

  const setTokenAndUser = useCallback((newToken: string, newUser: AuthUser) => {
    localStorage.setItem('token', newToken);
    setToken(newToken);
    setUser(newUser);
    setNeedsSetup(false);
  }, []);

  const updateProfileImage = useCallback((profileImage: string | null) => {
    setUser((prev) => prev ? { ...prev, profileImage: profileImage ?? undefined } : prev);
  }, []);

  const updateUsername = useCallback((username: string) => {
    setUser((prev) => prev ? { ...prev, username } : prev);
  }, []);

  // Fetch full profile from DB on mount when token exists
  useEffect(() => {
    if (!token) return;
    authApi.getProfile().then((profile) => {
      setUser((prev) => prev ? { ...prev, username: profile.username, email: profile.email, role: profile.role, profileImage: profile.profileImage } : prev);
    }).catch(() => {});
  }, [token]);

  const isAdmin = user?.role === 'Admin';

  return (
    <AuthContext.Provider value={{ user, token, isAdmin, needsSetup, login, register, logout, setTokenAndUser, updateProfileImage, updateUsername }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextType {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
