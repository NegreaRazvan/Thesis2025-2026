import { useRef } from "react";
import useAvatarBlobUrl from "../useAvatarBlobUrl";

interface AvatarEditorProps {
    initials:      string;
    hasAvatar:     boolean;
    avatarVersion: number | undefined;
    uploading:     boolean;
    onUpload:      (file: File) => void;
    onRemove:      () => void;
}

const AvatarEditor = ({ initials, hasAvatar, avatarVersion, uploading, onUpload, onRemove }: AvatarEditorProps) => {
    const inputRef = useRef<HTMLInputElement>(null);
    const blobUrl  = useAvatarBlobUrl(hasAvatar, avatarVersion);

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (file) onUpload(file);
        e.target.value = "";
    };

    return (
        <div className="profile-avatar-wrap">
            <div className="profile-avatar">
                {blobUrl
                    ? <img src={blobUrl} alt="avatar" />
                    : initials
                }
                <div
                    className="profile-avatar-overlay"
                    onClick={() => !uploading && inputRef.current?.click()}
                >
                    <span className="profile-avatar-overlay-label">
                        {uploading ? "Uploading…" : "Change"}
                    </span>
                    {hasAvatar && !uploading && (
                        <button
                            className="profile-avatar-remove"
                            onClick={e => { e.stopPropagation(); onRemove(); }}
                            type="button"
                        >
                            Remove
                        </button>
                    )}
                </div>
            </div>
            <input
                ref={inputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp"
                className="profile-avatar-input"
                onChange={handleFileChange}
            />
        </div>
    );
};

export default AvatarEditor;
