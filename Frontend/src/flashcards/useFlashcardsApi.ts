import useApiClient from "../core/useApiClient";
import { useCallback } from "react";
import {FlashcardCard, FlashcardDetail, FlashcardSummary} from "./props";

const useFlashcardSetApi = () => {
    const { axios } = useApiClient();
    const base = "/api/FlashcardSet";

    const getAll = useCallback(async (): Promise<FlashcardSummary[]> => {
        const res = await axios.get<FlashcardSummary[]>(base);
        return res.data;
    }, [axios]);

    const getById = useCallback(async (id: string): Promise<FlashcardDetail> => {
        const res = await axios.get<FlashcardDetail>(`${base}/${id}`);
        return res.data;
    }, [axios]);

    const create = useCallback(async (name: string, description?: string): Promise<FlashcardSummary> => {
        const res = await axios.post<FlashcardSummary>(base, { name, description });
        return res.data;
    }, [axios]);

    const remove = useCallback(async (id: string): Promise<void> => {
        await axios.delete(`${base}/${id}`);
    }, [axios]);

    const addCard = useCallback(async (setId: string, front: string, back: string): Promise<FlashcardCard> => {
        const res = await axios.post<FlashcardCard>(`${base}/${setId}/cards`, { front, back });
        return res.data;
    }, [axios]);

    const updateCard = useCallback(async (setId: string, id: string, front: string, back: string): Promise<FlashcardCard> => {
        const res = await axios.put<FlashcardCard>(`${base}/${setId}/cards`, { id, front, back });
        return res.data;
    }, [axios]);

    const deleteCard = useCallback(async (setId: string, cardId: string): Promise<void> => {
        await axios.delete(`${base}/${setId}/cards/${cardId}`);
    }, [axios]);

    const bulkAdd = useCallback(async (setId: string, cards: { front: string; back: string }[]): Promise<FlashcardCard[]> => {
        const res = await axios.post<FlashcardCard[]>(`${base}/${setId}/cards/bulk`, {
            cards: cards.map(c => ({ id: null, front: c.front, back: c.back })),
        });
        return res.data;
    }, [axios]);

    const markReviewed = useCallback(async (id: string): Promise<void> => {
        await axios.post(`${base}/${id}/complete`);
    }, [axios]);

    return { getAll, getById, create, remove, addCard, updateCard, deleteCard, bulkAdd, markReviewed };
};

export default useFlashcardSetApi;