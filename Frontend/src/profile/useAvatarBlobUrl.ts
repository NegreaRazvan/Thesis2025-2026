import { useEffect, useState } from "react";
import useApiClient from "../core/useApiClient";

const useAvatarBlobUrl = (hasAvatar: boolean, version: number | undefined): string | null => {
    const [blobUrl, setBlobUrl] = useState<string | null>(null);
    const { axios } = useApiClient();

    useEffect(() => {
        if (!hasAvatar) {
            setBlobUrl(null);
            return;
        }

        let objectUrl: string | null = null;

        axios.get("/api/User/me/avatar", { responseType: "blob" })
            .then(res => {
                objectUrl = URL.createObjectURL(res.data);
                setBlobUrl(objectUrl);
            })
            .catch(() => setBlobUrl(null));

        return () => {
            if (objectUrl) URL.revokeObjectURL(objectUrl);
        };
    }, [hasAvatar, version, axios]);

    return blobUrl;
};

export default useAvatarBlobUrl;
