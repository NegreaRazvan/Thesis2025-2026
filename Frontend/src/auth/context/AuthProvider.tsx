import { useState, useEffect, useRef, useCallback, type ReactNode } from "react";
import { AuthContext, type AuthUser } from "./AuthContext";

const TOKEN_KEY    = "ga_token";
const USERNAME_KEY = "ga_username";
const USERID_KEY   = "ga_userId";
const EMAIL_KEY    = "ga_email";
const DISPLAYNAME_KEY = "ga_displayName";
const AVATAR_VER_KEY  = "ga_avatarVersion";

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser]           = useState<AuthUser | null>(null);
  const [loading, setLoading]     = useState(true);
  const resolversRef              = useRef<Array<(token: string | null) => void>>([]);

  useEffect(() => {
    const token       = localStorage.getItem(TOKEN_KEY);
    const username    = localStorage.getItem(USERNAME_KEY);
    const userId      = localStorage.getItem(USERID_KEY);
    const email       = localStorage.getItem(EMAIL_KEY) ?? undefined;
    const displayName = localStorage.getItem(DISPLAYNAME_KEY) ?? undefined;
    const avatarVer   = localStorage.getItem(AVATAR_VER_KEY);

    if (token && username && userId) {
      setUser({
        token, username, userId, email, displayName,
        avatarVersion: avatarVer ? Number(avatarVer) : undefined,
      });
    }
    setLoading(false);
  }, []);

  const login = (data: AuthUser) => {
    localStorage.setItem(TOKEN_KEY,    data.token);
    localStorage.setItem(USERNAME_KEY, data.username);
    localStorage.setItem(USERID_KEY,   data.userId);
    if (data.email)       localStorage.setItem(EMAIL_KEY, data.email);
    if (data.displayName) localStorage.setItem(DISPLAYNAME_KEY, data.displayName);
    if (data.avatarVersion !== undefined)
      localStorage.setItem(AVATAR_VER_KEY, String(data.avatarVersion));
    setUser(data);

    resolversRef.current.forEach(r => r(data.token));
    resolversRef.current = [];
  };

  const logout = () => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USERNAME_KEY);
    localStorage.removeItem(USERID_KEY);
    localStorage.removeItem(EMAIL_KEY);
    localStorage.removeItem(DISPLAYNAME_KEY);
    localStorage.removeItem(AVATAR_VER_KEY);
    setUser(null);
  };

  const updateUser = (partial: Partial<Omit<AuthUser, "token">>) => {
    setUser(prev => {
      if (!prev) return prev;
      const next = { ...prev, ...partial };
      if (partial.username !== undefined) localStorage.setItem(USERNAME_KEY, partial.username);
      if (partial.email !== undefined) localStorage.setItem(EMAIL_KEY, partial.email);
      if (partial.displayName !== undefined) localStorage.setItem(DISPLAYNAME_KEY, partial.displayName);
      if (partial.avatarVersion !== undefined)
        localStorage.setItem(AVATAR_VER_KEY, String(partial.avatarVersion));
      return next;
    });
  };

  const userRef = useRef(user);
  userRef.current = user;

  const waitForAccessToken = useCallback((): Promise<string | null> => {
    if (userRef.current?.token) return Promise.resolve(userRef.current.token);

    return new Promise<string | null>(resolve => {
      resolversRef.current.push(resolve);
      setTimeout(() => {
        resolversRef.current = resolversRef.current.filter(r => r !== resolve);
        resolve(null);
      }, 10_000);
    });
  }, []);

  return (
    <AuthContext.Provider value={{
      accessToken: user?.token ?? null,
      user,
      loading,
      login,
      logout,
      updateUser,
      waitForAccessToken,
    }}>
      {children}
    </AuthContext.Provider>
  );
};
