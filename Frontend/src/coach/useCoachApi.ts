import useApiClient from "../core/useApiClient";
import type { CoachChatRequest, CoachChatResponse, SaveCoachSession, CoachSessionSummary, CoachSessionDetail } from "./props.ts";

const useCoachApi = () => {
    const { axios } = useApiClient();

    const chat = async (request: CoachChatRequest): Promise<CoachChatResponse> => {
        const { data } = await axios.post<CoachChatResponse>("/api/Coach/chat", request);
        return data;
    };

    const saveSession = async (session: SaveCoachSession): Promise<CoachSessionSummary> => {
        const { data } = await axios.post<CoachSessionSummary>("/api/Coach/sessions", session);
        return data;
    };

    const getSessions = async (): Promise<CoachSessionSummary[]> => {
        const { data } = await axios.get<CoachSessionSummary[]>("/api/Coach/sessions");
        return data;
    };

    const getSessionDetail = async (id: string): Promise<CoachSessionDetail> => {
        const { data } = await axios.get<CoachSessionDetail>(`/api/Coach/sessions/${id}`);
        return data;
    };

    return { chat, saveSession, getSessions, getSessionDetail };
};

export default useCoachApi;
