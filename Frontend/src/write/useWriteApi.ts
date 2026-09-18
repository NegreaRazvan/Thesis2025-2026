import { useCallback } from "react";
import useApiClient from "../core/useApiClient";
import type { SubmissionResponseProps, SubmissionSummaryProps } from "./props";

const useWriteApi = () => {
  const { axios } = useApiClient();
  const url = "/api/Submission";

  const submitText = useCallback(
    async (text: string): Promise<SubmissionResponseProps> => {
      const res = await axios.post<SubmissionResponseProps>(url, { text });
      return res.data;
    }, [axios]);

  const getHistory = useCallback(
    async (): Promise<SubmissionSummaryProps[]> => {
      const res = await axios.get<SubmissionSummaryProps[]>(url);
      return res.data;
    }, [axios]);

  const getById = useCallback(
    async (id: string): Promise<SubmissionResponseProps> => {
      const res = await axios.get<SubmissionResponseProps>(`${url}/${id}`);
      return res.data;
    }, [axios]);

    const deleteSubmission = useCallback(
        async (id: string): Promise<void> => {
            const res = await axios.delete(`${url}/${id}`);
            return res.data;
            }, [axios]);

  return { submitText, getHistory, getById, deleteSubmission };
};

export default useWriteApi;
