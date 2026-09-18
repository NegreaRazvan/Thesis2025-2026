export function buildGermanCard(opts: {
    lemma: string;
    englishTranslation?: string | null;
    article?: string | null;
    plural?: string | null;
}): { front: string; back: string } {
    const front = opts.englishTranslation || opts.lemma;
    const back  = opts.article
        ? `${opts.article} ${opts.lemma}${opts.plural ? ` | die ${opts.plural}` : ""}`
        : opts.lemma;
    return { front, back };
}
