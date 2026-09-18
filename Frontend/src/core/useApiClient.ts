import axios, { type AxiosError } from "axios";
import { useEffect, useMemo } from "react";
import { useAuthContext } from "../auth/context/AuthContext";

const BASE_URL = import.meta.env.VITE_API_URL ?? "";

const useApiClient = () => {
  const { accessToken, waitForAccessToken } = useAuthContext();

  const apiClient = useMemo(() =>
    axios.create({
      baseURL: BASE_URL,
    }), []);

  useEffect(() => {
    const reqInterceptor = apiClient.interceptors.request.use(async config => {
      if (accessToken) {
        config.headers.Authorization = `Bearer ${accessToken}`;
        return config;
      }

      const url = (config.url ?? "").toLowerCase();
      if (url.includes("/auth/login") || url.includes("/auth/register")) {
        return config;
      }

      const token = await waitForAccessToken();
      if (token) config.headers.Authorization = `Bearer ${token}`;
      return config;
    });

    const resInterceptor = apiClient.interceptors.response.use(
      r => r,
      (error: AxiosError<{ message?: string }>) => {
        const backendMessage = error.response?.data?.message;
        return Promise.reject(
          backendMessage
            ? Object.assign(new Error(backendMessage), { status: error.response?.status })
            : error
        );
      }
    );

    return () => {
      apiClient.interceptors.request.eject(reqInterceptor);
      apiClient.interceptors.response.eject(resInterceptor);
    };
  }, [accessToken, apiClient, waitForAccessToken]);

  return { axios: apiClient };
};

export default useApiClient;
