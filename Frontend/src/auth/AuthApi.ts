import { useCallback } from "react";
import useApiClient from "../core/useApiClient";
import type { AuthResponseProps } from "./props";

const useAuthApi = () => {
  const { axios } = useApiClient();
  const authUrl = "/api/Auth";

  const register = useCallback(
    async (email: string, username: string, password: string): Promise<AuthResponseProps> => {
      const response = await axios.post<AuthResponseProps>(`${authUrl}/register`, {
        email, username, password,
      });
      return response.data;
    }, [axios]);

  const login = useCallback(
    async (email: string, password: string): Promise<AuthResponseProps> => {
      const response = await axios.post<AuthResponseProps>(`${authUrl}/login`, {
        email, password,
      });
      return response.data;
    }, [axios]);

  return { register, login };
};

export default useAuthApi;
