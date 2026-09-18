import { useCallback } from "react";
import useApiClient from "../core/useApiClient";

export interface TranscriptionResult {
    text: string;
    language: string;
    durationSeconds: number;
}

const useTranscriptionApi = () => {
    const { axios } = useApiClient();

    const transcribe = useCallback(async (blob: Blob, mimeType: string): Promise<TranscriptionResult> => {
        const form = new FormData();
        form.append("audio", blob, `recording.${mimeType.includes("mp4") ? "mp4" : "webm"}`);
        const res = await axios.post<TranscriptionResult>("/api/Transcription", form, {
            headers: { "Content-Type": "multipart/form-data" },
        });
        return res.data;
    }, [axios]);

    return { transcribe };
};

export default useTranscriptionApi;
