import useApiClient from "../core/useApiClient";
import { useCallback } from "react";
import type {
    VocabGameCategoryResponse,
    BatchValidateResponse,
    VocabGameSaveSession,
    VocabGameSession,
} from "./props";

const useVocabGameApi = () => {
    const { axios } = useApiClient();
    const base = "/api/VocabGame";

    const getCategory = useCallback(async (): Promise<VocabGameCategoryResponse> => {
        const res = await axios.get<VocabGameCategoryResponse>(`${base}/category`);
        return res.data;
    }, [axios]);

    const validateBatch = useCallback(async (words: string[], categoryKey: string): Promise<BatchValidateResponse> => {
        const res = await axios.post<BatchValidateResponse>(`${base}/validate-batch`, { words, categoryKey });
        return res.data;
    }, [axios]);

    const saveSession = useCallback(async (data: VocabGameSaveSession): Promise<VocabGameSession> => {
        const res = await axios.post<VocabGameSession>(`${base}/session`, data);
        return res.data;
    }, [axios]);

    const getHistory = useCallback(async (): Promise<VocabGameSession[]> => {
        const res = await axios.get<VocabGameSession[]>(`${base}/history`);
        return res.data;
    }, [axios]);

    return { getCategory, validateBatch, saveSession, getHistory };
};

export default useVocabGameApi;
