import { useRef, useState } from "react";
import toast from "react-hot-toast";

const useRecorder = (onBlob: (blob: Blob, mime: string) => void) => {
    const [recording, setRecording] = useState(false);
    const recorderRef = useRef<MediaRecorder | null>(null);
    const chunksRef   = useRef<Blob[]>([]);
    const onBlobRef   = useRef(onBlob);
    onBlobRef.current = onBlob;

    const start = async () => {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            const mime   = MediaRecorder.isTypeSupported("audio/webm;codecs=opus")
                ? "audio/webm;codecs=opus" : "audio/webm";
            const recorder = new MediaRecorder(stream, { mimeType: mime });
            chunksRef.current = [];
            recorder.ondataavailable = e => { if (e.data.size > 0) chunksRef.current.push(e.data); };
            recorder.onstop = () => {
                const blob = new Blob(chunksRef.current, { type: mime });
                stream.getTracks().forEach(t => t.stop());
                onBlobRef.current(blob, mime);
            };
            recorder.start();
            recorderRef.current = recorder;
            setRecording(true);
        } catch {
            toast.error("Microphone access denied.");
        }
    };

    const stop = () => {
        recorderRef.current?.stop();
        setRecording(false);
    };

    return { recording, start, stop };
};

export default useRecorder;
