import { useCallback } from "react";
import useApiClient from "../core/useApiClient";
import type { UserProfileProps, UpdateProfilePayload, ChangePasswordPayload } from "./props";

const USER_URL = "/api/User";

const useUserApi = () => {
    const { axios } = useApiClient();

    const getProfile = useCallback(async (): Promise<UserProfileProps> => {
        const res = await axios.get<UserProfileProps>(`${USER_URL}/me`);
        return res.data;
    }, [axios]);

    const updateProfile = useCallback(async (payload: UpdateProfilePayload): Promise<UserProfileProps> => {
        const res = await axios.put<UserProfileProps>(`${USER_URL}/me`, payload);
        return res.data;
    }, [axios]);

    const changePassword = useCallback(async (payload: ChangePasswordPayload): Promise<void> => {
        await axios.put(`${USER_URL}/me/password`, payload);
    }, [axios]);

    const uploadAvatar = useCallback(async (file: File): Promise<void> => {
        const form = new FormData();
        form.append("file", file);
        await axios.post(`${USER_URL}/me/avatar`, form, {
            headers: { "Content-Type": "multipart/form-data" },
        });
    }, [axios]);

    const deleteAvatar = useCallback(async (): Promise<void> => {
        await axios.delete(`${USER_URL}/me/avatar`);
    }, [axios]);

    const getAvatarUrl = (userId: string, avatarVersion?: number) =>
        `${USER_URL}/${userId}/avatar${avatarVersion !== undefined ? `?v=${avatarVersion}` : ""}`;

    return { getProfile, updateProfile, changePassword, uploadAvatar, deleteAvatar, getAvatarUrl };
};

export default useUserApi;
