import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import toast from "react-hot-toast";
import useVocabGameApi from "../useVocabGameApi";
import useFlashcardSetApi from "../../flashcards/useFlashcardsApi";
import { buildGermanCard } from "../../core/flashcardUtils";
import useVocabGameTimer from "../hooks/useVocabGameTimer";
import VocabGameIdle from "../components/VocabGameIdle";
import VocabGameFinished from "../components/VocabGameFinished";
import { Masthead } from "../../core/SharedComponents";
import type { GameState, MisspelledWord, WordResult } from "../props";
import type { FlashcardSummary } from "../../flashcards/props";
import "../vocabgame.css";

const GAME_DURATION = 60;

const initialState: GameState = {
    phase: "idle",
    category: null,
    timeLeft: GAME_DURATION,
    submittedWords: [],
    results: null,
    score: 0,
};

const VocabGamePage = () => {
    const { getCategory, validateBatch, saveSession } = useVocabGameApi();
    const { getAll, addCard } = useFlashcardSetApi();
    const { t } = useTranslation();

    const [game, setGame]           = useState<GameState>(initialState);
    const [input, setInput]         = useState("");
    const [starting, setStarting]   = useState(false);
    const [validating, setValidating] = useState(false);
    const [gameNum, setGameNum]     = useState(0);
    const inputRef = useRef<HTMLInputElement>(null);

    const [sets, setSets]               = useState<FlashcardSummary[]>([]);
    const [selectedSet, setSelectedSet] = useState("");
    const [addedWords, setAddedWords]   = useState<Set<string>>(new Set());
    const [addingWord, setAddingWord]   = useState<string | null>(null);

    useEffect(() => {
        const loadSets = async () => {
            try {
                const s = await getAll();
                setSets(s);
                if (s.length > 0) setSelectedSet(s[0].id);
            } catch {
                toast.error(t("write.loadSetsError"));
            }
        };
        loadSets();
    }, []);

    useVocabGameTimer(
        game.phase === "playing",
        updater => setGame(prev => ({ ...prev, timeLeft: updater(prev.timeLeft) })),
        () => setGame(prev => ({ ...prev, timeLeft: 0, phase: "finished" })),
    );

    useEffect(() => {
        if (game.phase !== "finished" || !game.category || game.results !== null) return;
        if (game.submittedWords.length === 0) {
            setGame(prev => ({ ...prev, results: [], score: 0 }));
            return;
        }

        const validate = async () => {
            setValidating(true);
            try {
                const response = await validateBatch(game.submittedWords, game.category!.categoryKey);
                setGame(prev => ({ ...prev, results: response.results, score: response.score }));
                const validWords = response.results.filter(r => r.valid).map(r => r.word);
                const misspelledWords: MisspelledWord[] = response.results
                    .filter(r => r.misspelled)
                    .map(r => ({
                        word: r.word,
                        suggestions: r.suggestions,
                        lemma: r.lemma,
                        englishTranslation: r.englishTranslation,
                        article: r.article,
                        plural: r.plural,
                    }));
                saveSession({
                    categoryKey: game.category!.categoryKey,
                    displayName: game.category!.displayName,
                    score: response.score,
                    validWords,
                    misspelledWords,
                }).catch(() => {});
            } catch {
                toast.error(t("vocabgame.validateError"));
            } finally {
                setValidating(false);
            }
        };
        validate();
    }, [game.phase]);

    const startGame = async () => {
        setStarting(true);
        try {
            const cat = await getCategory();
            setGame({ ...initialState, phase: "playing", category: cat });
            setInput("");
            setAddedWords(new Set());
            setGameNum(n => n + 1);
            setTimeout(() => inputRef.current?.focus(), 100);
        } catch {
            toast.error(t("vocabgame.startError"));
        } finally {
            setStarting(false);
        }
    };

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        const word = input.trim();
        if (!word || game.phase !== "playing") return;

        if (game.submittedWords.some(w => w.toLowerCase() === word.toLowerCase())) {
            toast(t("vocabgame.alreadySubmitted"));
            setInput("");
            inputRef.current?.focus();
            return;
        }

        setGame(prev => ({ ...prev, submittedWords: [...prev.submittedWords, word] }));
        setInput("");
        inputRef.current?.focus();
    };

    const handleAddToFlashcards = async (r: WordResult) => {
        if (!selectedSet) { toast.error(t("vocabgame.selectSetFirst")); return; }
        setAddingWord(r.word);
        try {
            const { front, back } = buildGermanCard({ lemma: r.lemma || r.word, englishTranslation: r.englishTranslation, article: r.article, plural: r.plural });
            await addCard(selectedSet, front, back);
            setAddedWords(prev => new Set(prev).add(r.word));
            toast.success(t("write.addCardSuccess", { lemma: r.lemma || r.suggestions[0] }));
        } catch {
            toast.error(t("vocabgame.addCardError"));
        } finally {
            setAddingWord(null);
        }
    };

    if (game.phase === "idle") return (
        <VocabGameIdle starting={starting} onStart={startGame} />
    );

    if (game.phase === "finished") return (
        <VocabGameFinished
            game={game}
            sets={sets}
            selectedSet={selectedSet}
            addedWords={addedWords}
            addingWord={addingWord}
            starting={starting}
            validating={validating}
            onSetChange={setSelectedSet}
            onAddToFlashcards={handleAddToFlashcards}
            onPlayAgain={startGame}
        />
    );

    const danger = game.timeLeft <= 10;

    return (
        <div className="page page-enter vg-page">
            <Masthead
                num="05"
                title={t("vocabgame.title")}
                sub={t("vocabgame.sub")}
                meta={[
                    { label: "EMBEDDING", value: "cc.de.300" },
                    { label: "ROUND",     value: String(gameNum) },
                    { label: "WORDS",     value: String(game.submittedWords.length) },
                ]}
            />

            <div className="vg-play-box">
                <div className="vg-play-left">
                    <div className="vg-cat-badge">{t("vocabgame.category")}</div>
                    <div className="vg-cat-name">{game.category?.displayName}</div>
                    <div className="vg-cat-key">{game.category?.categoryKey?.toUpperCase()}</div>

                    {game.submittedWords.length > 0 && (
                        <>
                            <div className="vg-submitted-hd">{t("vocabgame.submittedWords")}</div>
                            <div className="vg-submitted-chips">
                                {[...game.submittedWords].reverse().map(w => (
                                    <span key={w} className="vg-chip">{w}</span>
                                ))}
                            </div>
                        </>
                    )}
                </div>

                <div className="vg-play-right">
                    <div className={`vg-timer-box${danger ? " vg-timer-box--danger" : ""}`}>
                        <span className="vg-timer-dot">●</span>
                        <span className="vg-timer-num">{String(game.timeLeft).padStart(2, "0")}</span>
                        <span className="vg-timer-label">{t("vocabgame.secondsLeft")}</span>
                    </div>

                    <form onSubmit={handleSubmit} className="vg-input-row">
                        <input
                            ref={inputRef}
                            className="vg-input"
                            value={input}
                            onChange={e => setInput(e.target.value)}
                            placeholder={t("vocabgame.typeWord")}
                            autoFocus
                            autoComplete="off"
                        />
                        <button type="submit" className="vg-add-btn" disabled={!input.trim()}>
                            {t("vocabgame.addBtn")}
                        </button>
                    </form>

                    <div className="vg-stats-row">
                        <div className="vg-stat">
                            <span className="vg-stat-num">{game.submittedWords.length}</span>
                            <span className="vg-stat-lbl">{t("vocabgame.words")}</span>
                        </div>
                        <div className="vg-stat">
                            <span className="vg-stat-num">{GAME_DURATION - game.timeLeft}s</span>
                            <span className="vg-stat-lbl">{t("vocabgame.elapsed")}</span>
                        </div>
                    </div>

                    <div className="vg-log">
                        {game.submittedWords.length === 0 ? (
                            <span className="vg-log-empty">{t("vocabgame.logEmpty")}</span>
                        ) : (
                            [...game.submittedWords].reverse().map((w, i) => (
                                <div key={w} className="vg-log-entry">
                                    <span style={{ color: "var(--text-mute)", fontSize: 10, marginRight: 8 }}>
                                        {String(game.submittedWords.length - i).padStart(2, "0")}
                                    </span>
                                    {w}
                                </div>
                            ))
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
};

export default VocabGamePage;
