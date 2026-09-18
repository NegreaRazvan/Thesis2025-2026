import { useEffect, useRef } from "react";

const useVocabGameTimer = (
    active: boolean,
    onTick: (updater: (prev: number) => number) => void,
    onExpire: () => void,
) => {
    const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

    useEffect(() => {
        if (!active) return;

        timerRef.current = setInterval(() => {
            onTick(prev => {
                if (prev <= 1) {
                    clearInterval(timerRef.current!);
                    onExpire();
                    return 0;
                }
                return prev - 1;
            });
        }, 1000);

        return () => {
            if (timerRef.current) clearInterval(timerRef.current);
        };
    }, [active]);
};

export default useVocabGameTimer;
