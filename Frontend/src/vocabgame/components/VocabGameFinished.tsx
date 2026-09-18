import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Masthead } from "../../core/SharedComponents";
import type { GameState, WordResult } from "../props";
import type { FlashcardSummary } from "../../flashcards/props";
import "../vocabgame.css";

interface VocabGameFinishedProps {
    game: GameState;
    sets: FlashcardSummary[];
    selectedSet: string;
    addedWords: Set<string>;
    addingWord: string | null;
    starting: boolean;
    validating: boolean;
    onSetChange: (id: string) => void;
    onAddToFlashcards: (r: WordResult) => void;
    onPlayAgain: () => void;
}

const VocabGameFinished = ({
    game, sets, selectedSet, addedWords, addingWord,
    starting, validating, onSetChange, onAddToFlashcards, onPlayAgain,
}: VocabGameFinishedProps) => {
    const { t } = useTranslation();
    const validResults      = game.results?.filter(r => r.valid)      ?? [];
    const invalidResults    = game.results?.filter(r => !r.valid)     ?? [];
    const misspelledResults = game.results?.filter(r => r.misspelled) ?? [];

    if (validating) {
        return (
            <div className="page page-enter vg-page">
                <Masthead num="05" title={t("vocabgame.title")} sub={t("vocabgame.validating")} />
                <div className="vg-validating">
                    <div className="vg-validating-bar">
                        <div className="vg-validating-fill" />
                    </div>
                    {t("vocabgame.checkingFastText")}
                </div>
            </div>
        );
    }

    return (
        <div className="page page-enter vg-page">
            <Masthead
                num="05"
                title={t("vocabgame.title")}
                sub={t("vocabgame.roundComplete")}
                meta={[
                    { label: "SCORE",    value: String(game.score) },
                    { label: "VALID",    value: String(validResults.length) },
                    { label: "REJECTED", value: String(invalidResults.length) },
                ]}
            />

            <div className="vg-finished-box">
                <div className="vg-finished-left">
                    <div className="vg-finished-score-lbl">{t("vocabgame.score")}</div>
                    <div className="vg-finished-score">{game.score}</div>
                    <div className="vg-finished-score-sub">
                        {t("vocabgame.wordsAccepted", { n: validResults.length, s: validResults.length !== 1 ? "s" : "" })}
                    </div>

                    <div className="vg-finished-meta">
                        <strong>{t("vocabgame.categoryLabel")}</strong><br />
                        {game.category?.displayName}<br /><br />
                        <strong>{t("vocabgame.submittedLabel")}</strong><br />
                        {game.submittedWords.length}<br /><br />
                        <strong>{t("vocabgame.rejectedLabel")}</strong><br />
                        {invalidResults.length}
                    </div>

                    <button
                        className="vg-play-again-btn"
                        onClick={onPlayAgain}
                        disabled={starting}
                    >
                        {starting ? t("vocabgame.loading") : t("vocabgame.playAgain")}
                    </button>
                </div>

                <div className="vg-finished-right">
                    {validResults.length > 0 && (
                        <div className="vg-results-section">
                            <div className="vg-results-hd vg-results-hd--valid">
                                {t("vocabgame.validWords", { n: validResults.length })}
                            </div>
                            <div className="vg-results-chips">
                                {validResults.map(r => (
                                    <span key={r.word} className="vg-chip vg-chip--valid">{r.word}</span>
                                ))}
                            </div>
                        </div>
                    )}

                    {invalidResults.length > 0 && (
                        <div className="vg-results-section">
                            <div className="vg-results-hd vg-results-hd--invalid">
                                {t("vocabgame.rejected", { n: invalidResults.length })}
                            </div>
                            <div>
                                {invalidResults.map(r => (
                                    <div key={r.word} className="vg-rejected-item">
                                        <span className="vg-chip vg-chip--invalid">{r.word}</span>
                                        <span className="vg-rejected-reason">
                                            {t(`vocabgame.${r.reason}`, { defaultValue: t("vocabgame.not_related") })}
                                        </span>
                                    </div>
                                ))}
                            </div>
                        </div>
                    )}

                    {misspelledResults.length > 0 && (
                        <div className="vg-results-section">
                            <div className="vg-results-hd vg-results-hd--spell">
                                {t("vocabgame.misspelled", { n: misspelledResults.length })}
                            </div>

                            {sets.length > 0 && (
                                <div className="vg-set-picker">
                                    <span>{t("vocabgame.addToDeck")}</span>
                                    <select value={selectedSet} onChange={e => onSetChange(e.target.value)}>
                                        {sets.map(s => (
                                            <option key={s.id} value={s.id}>{s.name}</option>
                                        ))}
                                    </select>
                                </div>
                            )}

                            <div>
                                {misspelledResults.map(r => {
                                    const isAdded  = addedWords.has(r.word);
                                    const isAdding = addingWord === r.word;
                                    return (
                                        <div key={r.word} className="vg-spell-row">
                                            <span>
                                                <span className="vg-spell-wrong">{r.word}</span>
                                                {r.suggestions[0] && (
                                                    <>
                                                        {" → "}
                                                        <span className="vg-spell-fix">{r.suggestions[0]}</span>
                                                    </>
                                                )}
                                            </span>
                                            <button
                                                className={`vg-spell-add-btn${isAdded ? " vg-spell-add-btn--done" : ""}`}
                                                onClick={() => onAddToFlashcards(r)}
                                                disabled={isAdded || isAdding || !selectedSet}
                                            >
                                                {isAdding ? t("vocabgame.adding") : isAdded ? t("vocabgame.added") : t("vocabgame.addFlashcard")}
                                            </button>
                                        </div>
                                    );
                                })}
                            </div>
                        </div>
                    )}

                    {game.results?.length === 0 && (
                        <div className="vg-results-section">
                            <div className="vg-results-hd vg-results-hd--spell">{t("vocabgame.noWordsSubmitted")}</div>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

export default VocabGameFinished;
