import { createContext, useContext } from "react";

export interface AuthUser {
  userId:        string;
  username:      string;
  token:         string;
  email?:        string;
  displayName?:  string;
  avatarVersion?: number;
}

export interface AuthContextType {
  accessToken:        string | null;
  user:               AuthUser | null;
  loading:            boolean;
  login:              (data: AuthUser) => void;
  logout:             () => void;
  updateUser:         (partial: Partial<Omit<AuthUser, "token">>) => void;
  waitForAccessToken: () => Promise<string | null>;
}

export const AuthContext = createContext<AuthContextType | null>(null);

export const useAuthContext = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuthContext must be used inside <AuthProvider>");
  return ctx;
};
