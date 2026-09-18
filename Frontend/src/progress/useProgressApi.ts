import { useCallback } from "react";
import useApiClient from "../core/useApiClient";
import type { ProgressPointProps, ErrorPatternProps, GamificationProps } from "./props";

const useProgressApi = () => {
  const { axios } = useApiClient();
  const url = "/api/Progress";

  const getTimeline = useCallback(
    async (): Promise<ProgressPointProps[]> => {
      const res = await axios.get<ProgressPointProps[]>(`${url}/timeline`);
      return res.data;
    }, [axios]);

  const getErrorPatterns = useCallback(
    async (): Promise<ErrorPatternProps[]> => {
      const res = await axios.get<ErrorPatternProps[]>(`${url}/error-patterns`);
      return res.data;
    }, [axios]);

  const getGamification = useCallback(
    async (): Promise<GamificationProps> => {
      const res = await axios.get<GamificationProps>(`${url}/gamification`);
      return res.data;
    }, [axios]);

  const downloadReport = useCallback(async () => {
    const res = await axios.get(`${url}/report`, { responseType: "blob" });
    const blob = new Blob([res.data], { type: "application/pdf" });
    const link = document.createElement("a");
    link.href = URL.createObjectURL(blob);
    link.download = `LinguaForge_Report.pdf`;
    link.click();
    URL.revokeObjectURL(link.href);
  }, [axios]);

  return { getTimeline, getErrorPatterns, getGamification, downloadReport };
};

export default useProgressApi;
