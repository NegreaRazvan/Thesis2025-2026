import { useEffect, useState } from "react";
import useFlashcardSetApi from "../useFlashcardsApi";
import StudyModal from "./StudyModal";
import toast from "react-hot-toast";

interface StudyModalLoaderProps {
    packageId: string;
    onClose: () => void;
    onComplete?: () => void;
}

const StudyModalLoader = ({ packageId, onClose, onComplete }: StudyModalLoaderProps) => {
    const { getById } = useFlashcardSetApi();
    const [cards, setCards] = useState<{ front: string; back: string }[]>([]);
    const [ready, setReady] = useState(false);

    useEffect(() => {
        getById(packageId).then(d => {
            setCards(d.cards);
            setReady(true);
        });
    }, [packageId]);

    if (!ready) return null;
    if (!cards.length) { toast("This set has no cards yet!"); onClose(); return null; }
    return <StudyModal cards={cards} onClose={onClose} onComplete={onComplete} />;
};

export default StudyModalLoader;
