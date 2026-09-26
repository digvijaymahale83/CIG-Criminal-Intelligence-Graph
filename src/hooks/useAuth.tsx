import React, { createContext, useContext, useEffect, useState } from 'react';
import { AuthSessionState, AuthUser, UserPermission, UserRole } from '../types/auth';
import { apiClient } from '../services/api/client';

export interface AuthContextType extends AuthSessionState {
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  hasPermission: (permission: UserPermission) => boolean;
  hasRole: (roles: UserRole | UserRole[]) => boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const TOKEN_KEY = 'mhp_cortex_token';
const USER_KEY = 'mhp_cortex_user';

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<AuthUser | null>(() => {
    try {
      const stored = localStorage.getItem(USER_KEY);
      return stored ? JSON.parse(stored) : null;
    } catch {
      return null;
    }
  });
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    const storedToken = localStorage.getItem(TOKEN_KEY);
    if (storedToken) {
      apiClient.setToken(storedToken);
      // Verify token with backend
      apiClient.get<{ user: any }>('/api/v1/auth/me')
        .catch(() => apiClient.get<{ user: any }>('/api/auth/me'))
        .catch(() => {
          // Token expired or invalid
          localStorage.removeItem(TOKEN_KEY);
          localStorage.removeItem(USER_KEY);
          setUser(null);
          apiClient.setToken(null);
        });
    }
  }, []);

  const login = async (email: string, password: string) => {
    setIsLoading(true);
    try {
      let response: { token: string; user: any };
      try {
        response = await apiClient.post<{ token: string; user: any }>('/api/v1/auth/login', { email, password });
      } catch {
        response = await apiClient.post<{ token: string; user: any }>('/api/auth/login', { email, password });
      }
      const { token, user: loggedInUser } = response;

      // Give user proper permissions based on role
      const permissions: UserPermission[] = [
        'view_investigation',
        'view_evidence',
        'upload_evidence',
        'review_entities',
        'review_findings',
        'run_analytics',
        'generate_reports',
        'export_data',
      ];
      if (loggedInUser.role === 'Administrator' || loggedInUser.role === 'Supervisor' || loggedInUser.role === 'ADMIN') {
        permissions.push('view_audit' as UserPermission);
      }

      const fullUser: AuthUser = {
        id: loggedInUser.id,
        name: loggedInUser.fullName || loggedInUser.name || 'Investigating Officer',
        email: loggedInUser.email,
        badgeNumber: loggedInUser.badgeNumber || loggedInUser.badge || '',
        role: loggedInUser.role,
        agency: loggedInUser.agency || 'Maharashtra Police',
        permissions,
        sessionExpiresAtUtc: new Date(Date.now() + 8 * 60 * 60 * 1000).toISOString(),
      };

      localStorage.setItem(TOKEN_KEY, token);
      localStorage.setItem(USER_KEY, JSON.stringify(fullUser));
      apiClient.setToken(token);
      setUser(fullUser);
    } finally {
      setIsLoading(false);
    }
  };

  const logout = async () => {
    setIsLoading(true);
    try {
      setUser(null);
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
      apiClient.setToken(null);
    } finally {
      setIsLoading(false);
    }
  };

  const hasPermission = (permission: UserPermission): boolean => {
    if (!user) return false;
    if (user.role === 'Administrator') return true;
    return user.permissions.includes(permission);
  };

  const hasRole = (roles: UserRole | UserRole[]): boolean => {
    if (!user) return false;
    const roleList = Array.isArray(roles) ? roles : [roles];
    return roleList.includes(user.role);
  };

  const storedToken = localStorage.getItem(TOKEN_KEY);

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        token: storedToken,
        login,
        logout,
        hasPermission,
        hasRole,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
