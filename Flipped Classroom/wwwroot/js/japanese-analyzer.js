/**
 * ============================================================
 *  JAPANESE LANGUAGE ANALYZER — Extension Tool
 *  Flipped Classroom Project
 * 
 *  Công cụ phân tích ngữ pháp tiếng Nhật client-side.
 *  4 Module: CharUtils → SentenceSplitter → WordClassifier → ConjugationEngine
 *  + UI Controller cho panel interaction.
 * ============================================================
 */
const JapaneseAnalyzer = (() => {
    'use strict';

    // ─────────────────────────────────────────────────────────
    // MODULE 1: CHARACTER UTILITIES
    // ─────────────────────────────────────────────────────────
    const CharUtils = {
        isHiragana(ch) {
            const code = ch.charCodeAt(0);
            return code >= 0x3040 && code <= 0x309F;
        },
        isKatakana(ch) {
            const code = ch.charCodeAt(0);
            return code >= 0x30A0 && code <= 0x30FF;
        },
        isKanji(ch) {
            const code = ch.charCodeAt(0);
            return (code >= 0x4E00 && code <= 0x9FFF) ||
                   (code >= 0x3400 && code <= 0x4DBF) ||
                   (code >= 0xF900 && code <= 0xFAFF);
        },
        isPunctuation(ch) {
            return '。、！？「」『』（）・…ー〜～．，：；'.includes(ch);
        },
        isJapanese(ch) {
            return this.isHiragana(ch) || this.isKatakana(ch) || this.isKanji(ch);
        },
        /** Convert single Katakana char to Hiragana */
        katakanaToHiragana(str) {
            return str.replace(/[\u30A1-\u30F6]/g, ch =>
                String.fromCharCode(ch.charCodeAt(0) - 0x60)
            );
        },
        /** Convert single Hiragana char to Katakana */
        hiraganaToKatakana(str) {
            return str.replace(/[\u3041-\u3096]/g, ch =>
                String.fromCharCode(ch.charCodeAt(0) + 0x60)
            );
        },
        /** Get character type name */
        getCharType(ch) {
            if (this.isKanji(ch)) return 'kanji';
            if (this.isHiragana(ch)) return 'hiragana';
            if (this.isKatakana(ch)) return 'katakana';
            if (this.isPunctuation(ch)) return 'punctuation';
            return 'other';
        }
    };

    // ─────────────────────────────────────────────────────────
    // MODULE 2: SENTENCE SPLITTER
    // ─────────────────────────────────────────────────────────
    const PARTICLES = [
        'から', 'まで', 'より', 'ので', 'のに', 'けど', 'けれど',
        'ながら', 'ために', 'として', 'について', 'にとって',
        'だけ', 'しか', 'ばかり', 'ほど', 'くらい', 'ぐらい',
        'など', 'でも', 'では',
        'は', 'が', 'を', 'に', 'で', 'と', 'も', 'の', 'へ',
        'か', 'よ', 'ね', 'な', 'わ', 'ぞ', 'ぜ',
    ];

    const VERB_ENDINGS = [
        'ませんでした', 'なかった', 'ません', 'ました',
        'ている', 'ていた', 'てある', 'ておく',
        'られる', 'させる', 'れる', 'せる',
        'ます', 'ない', 'たい',
        'って', 'んで', 'いて', 'いだ',
        'った', 'んだ', 'いた',
        'して', 'した',
        'ける', 'かれる',
        'て', 'た', 'で', 'だ',
    ];

    const COPULA_ENDINGS = [
        'ではありません', 'じゃありません',
        'ではなかった', 'じゃなかった',
        'ではない', 'じゃない',
        'でした', 'だった',
        'です', 'だ',
    ];

    const ADJ_ENDINGS = [
        'くなかった', 'くありません',
        'ければ', 'かった',
        'くない', 'くて',
        'すぎる',
    ];

    const SentenceSplitter = {
        /**
         * Split a Japanese sentence into tokens.
         * @param {string} text - Japanese sentence
         * @returns {Array<{text: string, type: string}>}
         */
        split(text) {
            if (!text || !text.trim()) return [];

            const tokens = [];
            let remaining = text.trim();

            while (remaining.length > 0) {
                // Skip whitespace
                if (/^\s+/.test(remaining)) {
                    remaining = remaining.replace(/^\s+/, '');
                    continue;
                }

                // 1. Try punctuation
                if (CharUtils.isPunctuation(remaining[0])) {
                    tokens.push({ text: remaining[0], type: 'punctuation' });
                    remaining = remaining.substring(1);
                    continue;
                }

                // 2. Try copula endings (before particles to catch では etc.)
                const copula = this._matchLongest(remaining, COPULA_ENDINGS);
                if (copula && tokens.length > 0) {
                    tokens.push({ text: copula, type: 'verb-ending' });
                    remaining = remaining.substring(copula.length);
                    continue;
                }

                // 3. Try adjective endings
                const adjEnd = this._matchLongest(remaining, ADJ_ENDINGS);
                if (adjEnd && tokens.length > 0) {
                    tokens.push({ text: adjEnd, type: 'verb-ending' });
                    remaining = remaining.substring(adjEnd.length);
                    continue;
                }

                // 4. Try verb endings
                const verbEnd = this._matchLongest(remaining, VERB_ENDINGS);
                if (verbEnd && tokens.length > 0) {
                    tokens.push({ text: verbEnd, type: 'verb-ending' });
                    remaining = remaining.substring(verbEnd.length);
                    continue;
                }

                // 5. Try particles (only after at least one token)
                const particle = this._matchLongest(remaining, PARTICLES);
                if (particle && tokens.length > 0) {
                    tokens.push({ text: particle, type: 'particle' });
                    remaining = remaining.substring(particle.length);
                    continue;
                }

                // 6. Collect Kanji block
                if (CharUtils.isKanji(remaining[0])) {
                    let kanjiBlock = '';
                    let i = 0;
                    while (i < remaining.length && CharUtils.isKanji(remaining[i])) {
                        kanjiBlock += remaining[i];
                        i++;
                    }
                    // Attach trailing okurigana (hiragana that are part of the word)
                    // but stop before particles
                    const afterKanji = remaining.substring(i);
                    let okurigana = '';
                    let j = 0;
                    while (j < afterKanji.length && CharUtils.isHiragana(afterKanji[j])) {
                        // Check if this hiragana sequence is a particle
                        const possibleParticle = this._matchLongest(afterKanji.substring(j), PARTICLES);
                        if (possibleParticle) break;
                        // Check if this is a verb/adj ending
                        const possibleEnding = this._matchLongest(afterKanji.substring(j), [...VERB_ENDINGS, ...ADJ_ENDINGS, ...COPULA_ENDINGS]);
                        if (possibleEnding) break;
                        okurigana += afterKanji[j];
                        j++;
                    }
                    tokens.push({ text: kanjiBlock + okurigana, type: 'kanji' });
                    remaining = remaining.substring(kanjiBlock.length + okurigana.length);
                    continue;
                }

                // 7. Collect Katakana block
                if (CharUtils.isKatakana(remaining[0]) || remaining[0] === 'ー') {
                    let block = '';
                    let i = 0;
                    while (i < remaining.length && (CharUtils.isKatakana(remaining[i]) || remaining[i] === 'ー')) {
                        block += remaining[i];
                        i++;
                    }
                    tokens.push({ text: block, type: 'katakana' });
                    remaining = remaining.substring(block.length);
                    continue;
                }

                // 8. Collect Hiragana block (non-particle)
                if (CharUtils.isHiragana(remaining[0])) {
                    let block = '';
                    let i = 0;
                    while (i < remaining.length && CharUtils.isHiragana(remaining[i])) {
                        if (i > 0) {
                            const sub = remaining.substring(i);
                            
                            // Check if this hiragana sequence starts a particle
                            const possibleParticle = this._matchLongest(sub, PARTICLES);
                            if (possibleParticle) {
                                // Check if block + particle forms a demonstrative (あの, この, その, どの)
                                const fullWord = block + possibleParticle;
                                const isDemo = ['あの', 'この', 'その', 'どの'].includes(fullWord);
                                if (!isDemo) {
                                    break;
                                }
                            }
                            
                            // Check if this hiragana sequence starts a verb/adj/copula ending
                            const possibleEnding = this._matchLongest(sub, [...VERB_ENDINGS, ...ADJ_ENDINGS, ...COPULA_ENDINGS]);
                            if (possibleEnding) {
                                break;
                            }
                        }
                        block += remaining[i];
                        i++;
                    }
                    tokens.push({ text: block, type: 'hiragana' });
                    remaining = remaining.substring(block.length);
                    continue;
                }

                // 9. Other characters (romaji, numbers, etc.)
                let otherBlock = '';
                let i = 0;
                while (i < remaining.length && !CharUtils.isJapanese(remaining[i]) && !CharUtils.isPunctuation(remaining[i]) && !/\s/.test(remaining[i])) {
                    otherBlock += remaining[i];
                    i++;
                }
                if (otherBlock) {
                    tokens.push({ text: otherBlock, type: 'unknown' });
                    remaining = remaining.substring(otherBlock.length);
                } else {
                    // Fallback: skip one character
                    tokens.push({ text: remaining[0], type: 'unknown' });
                    remaining = remaining.substring(1);
                }
            }

            return tokens;
        },

        /** Match the longest pattern from a list at the start of text */
        _matchLongest(text, patterns) {
            let longest = null;
            for (const p of patterns) {
                if (text.startsWith(p)) {
                    if (!longest || p.length > longest.length) {
                        longest = p;
                    }
                }
            }
            return longest;
        }
    };

    // ─────────────────────────────────────────────────────────
    // MODULE 3: WORD CLASSIFIER
    // ─────────────────────────────────────────────────────────

    // Godan verbs that look like Ichidan (ending in -iru or -eru but are Group 1)
    const GODAN_EXCEPTIONS = [
        '帰る', '切る', '知る', '走る', '入る', '要る', '焦る', '限る',
        '喋る', '滑る', '握る', '練る', '参る', '交じる', '混じる',
        '嘲る', '覆る', '翻る', '滅入る', '蘇る', '茂る', '契る',
        '散る', '照る', '湿る', '捻る', '翳る', '陥る', '罵る',
        // Common ones students encounter at N5-N3
        'しる', 'はしる', 'はいる', 'いる', 'かえる', 'きる',
        'しゃべる', 'すべる', 'にぎる', 'ねる', 'まいる',
        'ちる', 'てる',
    ];

    // Na-adjectives that end in い (look like i-adj but aren't)
    const NA_ADJ_WITH_I = [
        'きれい', '綺麗', 'きらい', '嫌い', 'ゆうめい', '有名',
    ];

    const ICHIDAN_VOWELS = 'いきしちにひみりえけせてねへめれ' +
                           'ぃぎじぢびぴ' +
                           'ぇげぜでべぺ';

    const WordClassifier = {
        /**
         * Validate a Japanese word for conjugation.
         * @param {string} word
         * @returns {string|null} Error message or null if valid
         */
        validate(word) {
            if (!word || !word.trim()) return 'Vui lòng nhập động từ hoặc tính từ.';
            word = word.trim();

            // 1. Kiểm tra ký tự tiếng Nhật
            for (let i = 0; i < word.length; i++) {
                if (!CharUtils.isJapanese(word[i])) {
                    return 'Từ nhập vào phải viết bằng tiếng Nhật (Kanji, Hiragana hoặc Katakana), không bao gồm chữ cái La-tinh, chữ số hay ký tự đặc biệt.';
                }
            }

            // 2. Kiểm tra độ dài
            if (word.length === 1) {
                if (!CharUtils.isKanji(word[0])) {
                    return 'Ký tự đơn lẻ không phải chữ Hán (Kanji) không thể là động từ hoặc tính từ hợp lệ.';
                }
            }

            // 3. Kiểm tra xem có nhập từ đã chia thì lịch sự (Masu) hay không
            const conjugatedEndings = ['ます', 'ません', 'ました', 'ませんでした', 'てください', 'でください'];
            for (const ending of conjugatedEndings) {
                if (word.endsWith(ending) && word.length > ending.length) {
                    return `Vui lòng nhập thể nguyên mẫu (thể từ điển). Từ bạn nhập dường như đã được chia ở thể lịch sự hoặc thể khác ("${ending}").`;
                }
            }

            // 4. Kiểm tra tính từ đã chia thì quá khứ/phủ định
            if (word.endsWith('かった') && word.length > 3) {
                return 'Tính từ dường như đã được chia ở thể quá khứ ("かった"). Vui lòng nhập thể nguyên mẫu kết thúc bằng "い".';
            }
            if (word.endsWith('くない') && word.length > 3) {
                return 'Tính từ dường như đã được chia ở thể phủ định ("くない"). Vui lòng nhập thể nguyên mẫu kết thúc bằng "い".';
            }

            return null;
        },

        /**
         * Classify a Japanese word.
         * @param {string} word - Dictionary form of the word
         * @returns {{type: string, group: string|null, root: string, suffix: string, display: string}}
         */
        classify(word) {
            if (!word || !word.trim()) return null;
            word = word.trim();

            // 1. Kiểm tra tất cả ký tự có phải tiếng Nhật không
            for (let i = 0; i < word.length; i++) {
                if (!CharUtils.isJapanese(word[i])) {
                    return null; // Có ký tự không phải tiếng Nhật
                }
            }

            // 2. Kiểm tra độ dài: Chỉ chấp nhận 1 ký tự nếu đó là chữ Hán (Kanji) có nghĩa (ví dụ: 変, 楽, 楽)
            if (word.length === 1) {
                if (!CharUtils.isKanji(word[0])) {
                    return null;
                }
            }

            // ── Check Group 3 verbs ──
            if (word === 'する' || word === '為る') {
                return { type: 'verb', group: '3', subgroup: 'する', root: '', suffix: 'する', display: 'Nhóm 3 (する — Bất quy tắc)' };
            }
            if (word === '来る' || word === 'くる') {
                return { type: 'verb', group: '3', subgroup: '来る', root: '', suffix: word, display: 'Nhóm 3 (来る — Bất quy tắc)' };
            }
            // Compound する verbs (e.g., 勉強する, 運転する)
            if (word.endsWith('する') && word.length > 2) {
                return { type: 'verb', group: '3', subgroup: 'する', root: word.slice(0, -2), suffix: 'する', display: 'Nhóm 3 (Danh động từ + する)' };
            }

            // ── Check い-adjective (before verb check since both can end in る) ──
            if (word.endsWith('い') && word.length >= 2) {
                // Check na-adj exceptions
                if (NA_ADJ_WITH_I.includes(word)) {
                    return { type: 'na-adj', group: null, root: word, suffix: '', display: 'Tính từ đuôi な (形容動詞)' };
                }
                // Check if it's the irregular いい
                if (word === 'いい' || word === '良い') {
                    return { type: 'i-adj', group: 'irregular', root: word === 'いい' ? '' : '良', suffix: word === 'いい' ? 'いい' : 'い', display: 'Tính từ đuôi い (いい — Bất quy tắc)' };
                }
                // Check last char before い — if it's a verb ending pattern, classify further
                const lastChar = word[word.length - 1];
                if (lastChar === 'い') {
                    // Heuristic: if word is 2+ chars ending in い and the char before is Hiragana or Kanji
                    // (and not a recognized verb pattern), treat as i-adjective
                    const beforeI = word[word.length - 2];
                    // Most i-adjectives have Kanji stem + い, or pure hiragana
                    // Verbs rarely end in just い in dictionary form (they end in う-row)
                    // BUT we need to check if it could be a verb ending in う row
                    // Since Japanese verbs in dictionary form never end in い (they end in う row),
                    // if the word ends in い it's an adjective
                    return { type: 'i-adj', group: null, root: word.slice(0, -1), suffix: 'い', display: 'Tính từ đuôi い (形容詞)' };
                }
            }

            // ── Check verbs (dictionary form ends in う-row kana) ──
            const VERB_ENDINGS_DICT = {
                'う': 'う', 'く': 'く', 'ぐ': 'ぐ', 'す': 'す',
                'つ': 'つ', 'ぬ': 'ぬ', 'ぶ': 'ぶ', 'む': 'む', 'る': 'る'
            };

            const lastChar = word[word.length - 1];

            if (VERB_ENDINGS_DICT[lastChar]) {
                if (lastChar === 'る' && word.length >= 2) {
                    // Check exception list first
                    if (GODAN_EXCEPTIONS.includes(word)) {
                        return { type: 'verb', group: '1', root: word.slice(0, -1), suffix: 'る', display: 'Nhóm 1 — Godan (五段) [Ngoại lệ]' };
                    }
                    // Check if char before る is in i-row or e-row
                    const charBeforeRu = word[word.length - 2];
                    if (ICHIDAN_VOWELS.includes(charBeforeRu)) {
                        // Ichidan (Group 2)
                        return { type: 'verb', group: '2', root: word.slice(0, -1), suffix: 'る', display: 'Nhóm 2 — Ichidan (一段)' };
                    }
                    // Otherwise Godan
                    return { type: 'verb', group: '1', root: word.slice(0, -1), suffix: 'る', display: 'Nhóm 1 — Godan (五段)' };
                }

                // All other endings → Godan
                if (lastChar !== 'る') {
                    return { type: 'verb', group: '1', root: word.slice(0, -1), suffix: lastChar, display: 'Nhóm 1 — Godan (五段)' };
                }
            }

            // ── Default: treat as な-adjective ──
            // Words that don't match verb patterns and don't end in い
            // are likely na-adjectives or nouns (we treat as na-adj for conjugation purposes)
            return { type: 'na-adj', group: null, root: word, suffix: '', display: 'Tính từ đuôi な (形容動詞) / Danh từ' };
        }
    };

    // ─────────────────────────────────────────────────────────
    // MODULE 4: CONJUGATION ENGINE
    // ─────────────────────────────────────────────────────────

    // Godan consonant stem mapping: dict-ending → { i, a, e, o, te, ta }
    const GODAN_MAP = {
        'う': { a: 'わ', i: 'い', e: 'え', o: 'お', te: 'って', ta: 'った', negative: 'わ' },
        'く': { a: 'か', i: 'き', e: 'け', o: 'こ', te: 'いて', ta: 'いた', negative: 'か' },
        'ぐ': { a: 'が', i: 'ぎ', e: 'げ', o: 'ご', te: 'いで', ta: 'いだ', negative: 'が' },
        'す': { a: 'さ', i: 'し', e: 'せ', o: 'そ', te: 'して', ta: 'した', negative: 'さ' },
        'つ': { a: 'た', i: 'ち', e: 'て', o: 'と', te: 'って', ta: 'った', negative: 'た' },
        'ぬ': { a: 'な', i: 'に', e: 'ね', o: 'の', te: 'んで', ta: 'んだ', negative: 'な' },
        'ぶ': { a: 'ば', i: 'び', e: 'べ', o: 'ぼ', te: 'んで', ta: 'んだ', negative: 'ば' },
        'む': { a: 'ま', i: 'み', e: 'め', o: 'も', te: 'んで', ta: 'んだ', negative: 'ま' },
        'る': { a: 'ら', i: 'り', e: 'れ', o: 'ろ', te: 'って', ta: 'った', negative: 'ら' },
    };

    // Special: 行く conjugates te/ta as いって/いった (not いいて/いいた)
    const SPECIAL_VERBS = {
        '行く': { te: 'いって', ta: 'いった' },
    };

    const ConjugationEngine = {
        /**
         * Generate full conjugation matrix for a classified word.
         * @param {string} word - Original dictionary form
         * @param {object} info - Classification result from WordClassifier
         * @returns {Array<{form: string, formJp: string, values: {affirmative: string, negative: string, pastAff: string, pastNeg: string}}>}
         */
        conjugate(word, info) {
            if (!info) return [];

            switch (info.type) {
                case 'verb':
                    if (info.group === '1') return this._conjugateGodan(word, info);
                    if (info.group === '2') return this._conjugateIchidan(word, info);
                    if (info.group === '3') return this._conjugateGroup3(word, info);
                    break;
                case 'i-adj':
                    return this._conjugateIAdj(word, info);
                case 'na-adj':
                    return this._conjugateNaAdj(word, info);
            }
            return [];
        },

        // ── Godan (Group 1) ──
        _conjugateGodan(word, info) {
            const root = info.root;
            const ending = info.suffix;
            const map = GODAN_MAP[ending];
            if (!map) return [];

            const special = SPECIAL_VERBS[word];
            const te = special ? special.te : map.te;
            const ta = special ? special.ta : map.ta;

            return [
                {
                    form: 'Thể từ điển (辞書形)',
                    values: {
                        affirmative: { root, suffix: ending },
                        negative: { root: root + map.a, suffix: 'ない' },
                        pastAff: { root, suffix: ta },
                        pastNeg: { root: root + map.a, suffix: 'なかった' }
                    }
                },
                {
                    form: 'Thể Masu (丁寧形)',
                    values: {
                        affirmative: { root: root + map.i, suffix: 'ます' },
                        negative: { root: root + map.i, suffix: 'ません' },
                        pastAff: { root: root + map.i, suffix: 'ました' },
                        pastNeg: { root: root + map.i, suffix: 'ませんでした' }
                    }
                },
                {
                    form: 'Thể Te (て形)',
                    values: {
                        affirmative: { root, suffix: te },
                        negative: { root: root + map.a, suffix: 'なくて' },
                        pastAff: null,
                        pastNeg: null
                    }
                },
                {
                    form: 'Thể khả năng (可能形)',
                    values: {
                        affirmative: { root: root + map.e, suffix: 'る' },
                        negative: { root: root + map.e, suffix: 'ない' },
                        pastAff: { root: root + map.e, suffix: 'た' },
                        pastNeg: { root: root + map.e, suffix: 'なかった' }
                    }
                },
                {
                    form: 'Thể bị động (受身形)',
                    values: {
                        affirmative: { root: root + map.a, suffix: 'れる' },
                        negative: { root: root + map.a, suffix: 'れない' },
                        pastAff: { root: root + map.a, suffix: 'れた' },
                        pastNeg: { root: root + map.a, suffix: 'れなかった' }
                    }
                },
                {
                    form: 'Thể sai khiến (使役形)',
                    values: {
                        affirmative: { root: root + map.a, suffix: 'せる' },
                        negative: { root: root + map.a, suffix: 'せない' },
                        pastAff: { root: root + map.a, suffix: 'せた' },
                        pastNeg: { root: root + map.a, suffix: 'せなかった' }
                    }
                },
                {
                    form: 'Thể điều kiện (仮定形)',
                    values: {
                        affirmative: { root: root + map.e, suffix: 'ば' },
                        negative: { root: root + map.a, suffix: 'なければ' },
                        pastAff: null,
                        pastNeg: null
                    }
                },
                {
                    form: 'Thể ý chí (意向形)',
                    values: {
                        affirmative: { root: root + map.o, suffix: 'う' },
                        negative: null,
                        pastAff: null,
                        pastNeg: null
                    }
                },
                {
                    form: 'Thể mệnh lệnh (命令形)',
                    values: {
                        affirmative: { root: root + map.e, suffix: '' },
                        negative: null,
                        pastAff: null,
                        pastNeg: null
                    }
                },
                {
                    form: 'Thể cấm đoán (禁止形)',
                    values: {
                        affirmative: { root: root + ending, suffix: 'な' },
                        negative: null,
                        pastAff: null,
                        pastNeg: null
                    }
                },
            ];
        },

        // ── Ichidan (Group 2) ──
        _conjugateIchidan(word, info) {
            const root = info.root; // word without る

            return [
                {
                    form: 'Thể từ điển (辞書形)',
                    values: {
                        affirmative: { root, suffix: 'る' },
                        negative: { root, suffix: 'ない' },
                        pastAff: { root, suffix: 'た' },
                        pastNeg: { root, suffix: 'なかった' }
                    }
                },
                {
                    form: 'Thể Masu (丁寧形)',
                    values: {
                        affirmative: { root, suffix: 'ます' },
                        negative: { root, suffix: 'ません' },
                        pastAff: { root, suffix: 'ました' },
                        pastNeg: { root, suffix: 'ませんでした' }
                    }
                },
                {
                    form: 'Thể Te (て形)',
                    values: {
                        affirmative: { root, suffix: 'て' },
                        negative: { root, suffix: 'なくて' },
                        pastAff: null,
                        pastNeg: null
                    }
                },
                {
                    form: 'Thể khả năng (可能形)',
                    values: {
                        affirmative: { root, suffix: 'られる' },
                        negative: { root, suffix: 'られない' },
                        pastAff: { root, suffix: 'られた' },
                        pastNeg: { root, suffix: 'られなかった' }
                    }
                },
                {
                    form: 'Thể bị động (受身形)',
                    values: {
                        affirmative: { root, suffix: 'られる' },
                        negative: { root, suffix: 'られない' },
                        pastAff: { root, suffix: 'られた' },
                        pastNeg: { root, suffix: 'られなかった' }
                    }
                },
                {
                    form: 'Thể sai khiến (使役形)',
                    values: {
                        affirmative: { root, suffix: 'させる' },
                        negative: { root, suffix: 'させない' },
                        pastAff: { root, suffix: 'させた' },
                        pastNeg: { root, suffix: 'させなかった' }
                    }
                },
                {
                    form: 'Thể điều kiện (仮定形)',
                    values: {
                        affirmative: { root, suffix: 'れば' },
                        negative: { root, suffix: 'なければ' },
                        pastAff: null,
                        pastNeg: null
                    }
                },
                {
                    form: 'Thể ý chí (意向形)',
                    values: {
                        affirmative: { root, suffix: 'よう' },
                        negative: null,
                        pastAff: null,
                        pastNeg: null
                    }
                },
                {
                    form: 'Thể mệnh lệnh (命令形)',
                    values: {
                        affirmative: { root, suffix: 'ろ' },
                        negative: null,
                        pastAff: null,
                        pastNeg: null
                    }
                },
                {
                    form: 'Thể cấm đoán (禁止形)',
                    values: {
                        affirmative: { root, suffix: 'るな' },
                        negative: null,
                        pastAff: null,
                        pastNeg: null
                    }
                },
            ];
        },

        // ── Group 3 (する / 来る) ──
        _conjugateGroup3(word, info) {
            if (info.subgroup === 'する') {
                const stem = info.root; // e.g., '勉強' for 勉強する, '' for する

                return [
                    {
                        form: 'Thể từ điển (辞書形)',
                        values: {
                            affirmative: { root: stem, suffix: 'する' },
                            negative: { root: stem, suffix: 'しない' },
                            pastAff: { root: stem, suffix: 'した' },
                            pastNeg: { root: stem, suffix: 'しなかった' }
                        }
                    },
                    {
                        form: 'Thể Masu (丁寧形)',
                        values: {
                            affirmative: { root: stem, suffix: 'します' },
                            negative: { root: stem, suffix: 'しません' },
                            pastAff: { root: stem, suffix: 'しました' },
                            pastNeg: { root: stem, suffix: 'しませんでした' }
                        }
                    },
                    {
                        form: 'Thể Te (て形)',
                        values: {
                            affirmative: { root: stem, suffix: 'して' },
                            negative: { root: stem, suffix: 'しなくて' },
                            pastAff: null,
                            pastNeg: null
                        }
                    },
                    {
                        form: 'Thể khả năng (可能形)',
                        values: {
                            affirmative: { root: stem, suffix: 'できる' },
                            negative: { root: stem, suffix: 'できない' },
                            pastAff: { root: stem, suffix: 'できた' },
                            pastNeg: { root: stem, suffix: 'できなかった' }
                        }
                    },
                    {
                        form: 'Thể bị động (受身形)',
                        values: {
                            affirmative: { root: stem, suffix: 'される' },
                            negative: { root: stem, suffix: 'されない' },
                            pastAff: { root: stem, suffix: 'された' },
                            pastNeg: { root: stem, suffix: 'されなかった' }
                        }
                    },
                    {
                        form: 'Thể sai khiến (使役形)',
                        values: {
                            affirmative: { root: stem, suffix: 'させる' },
                            negative: { root: stem, suffix: 'させない' },
                            pastAff: { root: stem, suffix: 'させた' },
                            pastNeg: { root: stem, suffix: 'させなかった' }
                        }
                    },
                    {
                        form: 'Thể điều kiện (仮定形)',
                        values: {
                            affirmative: { root: stem, suffix: 'すれば' },
                            negative: { root: stem, suffix: 'しなければ' },
                            pastAff: null,
                            pastNeg: null
                        }
                    },
                    {
                        form: 'Thể ý chí (意向形)',
                        values: {
                            affirmative: { root: stem, suffix: 'しよう' },
                            negative: null,
                            pastAff: null,
                            pastNeg: null
                        }
                    },
                    {
                        form: 'Thể mệnh lệnh (命令形)',
                        values: {
                            affirmative: { root: stem, suffix: 'しろ' },
                            negative: null,
                            pastAff: null,
                            pastNeg: null
                        }
                    },
                    {
                        form: 'Thể cấm đoán (禁止形)',
                        values: {
                            affirmative: { root: stem, suffix: 'するな' },
                            negative: null,
                            pastAff: null,
                            pastNeg: null
                        }
                    },
                ];
            }

            // 来る (kuru)
            const isKanji = word === '来る';
            const stem = isKanji ? '来' : '';

            return [
                {
                    form: 'Thể từ điển (辞書形)',
                    values: {
                        affirmative: { root: stem, suffix: isKanji ? 'る' : 'くる' },
                        negative: { root: stem, suffix: isKanji ? 'ない' : 'こない' },
                        pastAff: { root: stem, suffix: isKanji ? 'た' : 'きた' },
                        pastNeg: { root: stem, suffix: isKanji ? 'なかった' : 'こなかった' }
                    }
                },
                {
                    form: 'Thể Masu (丁寧形)',
                    values: {
                        affirmative: { root: stem, suffix: isKanji ? 'ます' : 'きます' },
                        negative: { root: stem, suffix: isKanji ? 'ません' : 'きません' },
                        pastAff: { root: stem, suffix: isKanji ? 'ました' : 'きました' },
                        pastNeg: { root: stem, suffix: isKanji ? 'ませんでした' : 'きませんでした' }
                    }
                },
                {
                    form: 'Thể Te (て形)',
                    values: {
                        affirmative: { root: stem, suffix: isKanji ? 'て' : 'きて' },
                        negative: { root: stem, suffix: isKanji ? 'なくて' : 'こなくて' },
                        pastAff: null,
                        pastNeg: null
                    }
                },
                {
                    form: 'Thể khả năng (可能形)',
                    values: {
                        affirmative: { root: stem, suffix: isKanji ? 'られる' : 'こられる' },
                        negative: { root: stem, suffix: isKanji ? 'られない' : 'こられない' },
                        pastAff: { root: stem, suffix: isKanji ? 'られた' : 'こられた' },
                        pastNeg: { root: stem, suffix: isKanji ? 'られなかった' : 'こられなかった' }
                    }
                },
                {
                    form: 'Thể bị động (受身形)',
                    values: {
                        affirmative: { root: stem, suffix: isKanji ? 'られる' : 'こられる' },
                        negative: { root: stem, suffix: isKanji ? 'られない' : 'こられない' },
                        pastAff: { root: stem, suffix: isKanji ? 'られた' : 'こられた' },
                        pastNeg: { root: stem, suffix: isKanji ? 'られなかった' : 'こられなかった' }
                    }
                },
                {
                    form: 'Thể sai khiến (使役形)',
                    values: {
                        affirmative: { root: stem, suffix: isKanji ? 'させる' : 'こさせる' },
                        negative: { root: stem, suffix: isKanji ? 'させない' : 'こさせない' },
                        pastAff: { root: stem, suffix: isKanji ? 'させた' : 'こさせた' },
                        pastNeg: { root: stem, suffix: isKanji ? 'させなかった' : 'こさせなかった' }
                    }
                },
                {
                    form: 'Thể điều kiện (仮定形)',
                    values: {
                        affirmative: { root: stem, suffix: isKanji ? 'れば' : 'くれば' },
                        negative: { root: stem, suffix: isKanji ? 'なければ' : 'こなければ' },
                        pastAff: null,
                        pastNeg: null
                    }
                },
                {
                    form: 'Thể ý chí (意向形)',
                    values: {
                        affirmative: { root: stem, suffix: isKanji ? 'よう' : 'こよう' },
                        negative: null,
                        pastAff: null,
                        pastNeg: null
                    }
                },
                {
                    form: 'Thể mệnh lệnh (命令形)',
                    values: {
                        affirmative: { root: stem, suffix: isKanji ? 'い' : 'こい' },
                        negative: null,
                        pastAff: null,
                        pastNeg: null
                    }
                },
                {
                    form: 'Thể cấm đoán (禁止形)',
                    values: {
                        affirmative: { root: stem, suffix: isKanji ? 'るな' : 'くるな' },
                        negative: null,
                        pastAff: null,
                        pastNeg: null
                    }
                },
            ];
        },

        // ── い-Adjective ──
        _conjugateIAdj(word, info) {
            // Special case: いい / 良い → stem becomes よ
            const isIrregular = info.group === 'irregular';
            const root = isIrregular ? (word === 'いい' ? '' : '良') : info.root;
            const stemForConj = isIrregular ? 'よ' : info.root;

            return [
                {
                    form: 'Thể thường (普通形)',
                    values: {
                        affirmative: { root, suffix: isIrregular ? (word === 'いい' ? 'いい' : 'い') : 'い' },
                        negative: { root: stemForConj, suffix: 'くない' },
                        pastAff: { root: stemForConj, suffix: 'かった' },
                        pastNeg: { root: stemForConj, suffix: 'くなかった' }
                    }
                },
                {
                    form: 'Thể lịch sự (丁寧形)',
                    values: {
                        affirmative: { root, suffix: isIrregular ? (word === 'いい' ? 'いいです' : 'いです') : 'いです' },
                        negative: { root: stemForConj, suffix: 'くないです' },
                        pastAff: { root: stemForConj, suffix: 'かったです' },
                        pastNeg: { root: stemForConj, suffix: 'くなかったです' }
                    }
                },
                {
                    form: 'Thể Te (て形)',
                    values: {
                        affirmative: { root: stemForConj, suffix: 'くて' },
                        negative: { root: stemForConj, suffix: 'くなくて' },
                        pastAff: null,
                        pastNeg: null
                    }
                },
                {
                    form: 'Thể điều kiện (仮定形)',
                    values: {
                        affirmative: { root: stemForConj, suffix: 'ければ' },
                        negative: { root: stemForConj, suffix: 'くなければ' },
                        pastAff: null,
                        pastNeg: null
                    }
                },
                {
                    form: 'Thể quá mức (すぎる)',
                    values: {
                        affirmative: { root: stemForConj, suffix: 'すぎる' },
                        negative: { root: stemForConj, suffix: 'すぎない' },
                        pastAff: null,
                        pastNeg: null
                    }
                },
            ];
        },

        // ── な-Adjective ──
        _conjugateNaAdj(word, info) {
            const root = info.root;

            return [
                {
                    form: 'Thể thường (普通形)',
                    values: {
                        affirmative: { root, suffix: 'だ' },
                        negative: { root, suffix: 'ではない' },
                        pastAff: { root, suffix: 'だった' },
                        pastNeg: { root, suffix: 'ではなかった' }
                    }
                },
                {
                    form: 'Thể lịch sự (丁寧形)',
                    values: {
                        affirmative: { root, suffix: 'です' },
                        negative: { root, suffix: 'ではありません' },
                        pastAff: { root, suffix: 'でした' },
                        pastNeg: { root, suffix: 'ではありませんでした' }
                    }
                },
                {
                    form: 'Thể Te (て形)',
                    values: {
                        affirmative: { root, suffix: 'で' },
                        negative: { root, suffix: 'ではなくて' },
                        pastAff: null,
                        pastNeg: null
                    }
                },
                {
                    form: 'Thể điều kiện (仮定形)',
                    values: {
                        affirmative: { root, suffix: 'なら(ば)' },
                        negative: { root, suffix: 'ではなければ' },
                        pastAff: null,
                        pastNeg: null
                    }
                },
                {
                    form: 'Bổ nghĩa danh từ (連体形)',
                    values: {
                        affirmative: { root, suffix: 'な + [名詞]' },
                        negative: null,
                        pastAff: null,
                        pastNeg: null
                    }
                },
            ];
        },
    };

    // ─────────────────────────────────────────────────────────
    // UI CONTROLLER
    // ─────────────────────────────────────────────────────────
    const UI = {
        _panel: null,
        _overlay: null,
        _fab: null,
        _isOpen: false,

        init() {
            this._panel = document.getElementById('jpAnalyzerPanel');
            this._overlay = document.getElementById('jpAnalyzerOverlay');
            this._fab = document.getElementById('jpAnalyzerFab');

            if (!this._panel || !this._fab) return;

            // FAB click
            this._fab.addEventListener('click', () => this.toggle());

            // Overlay click to close
            this._overlay?.addEventListener('click', () => this.close());

            // Close button
            const closeBtn = this._panel.querySelector('.jp-analyzer-close');
            closeBtn?.addEventListener('click', () => this.close());

            // Tab switching
            const tabs = this._panel.querySelectorAll('.jp-analyzer-tab');
            tabs.forEach(tab => {
                tab.addEventListener('click', () => this._switchTab(tab.dataset.tab));
            });

            // Analyze buttons
            const btnSplit = document.getElementById('jpBtnSplit');
            const btnAnalyze = document.getElementById('jpBtnAnalyze');
            btnSplit?.addEventListener('click', () => this._runSentenceSplit());
            btnAnalyze?.addEventListener('click', () => this._runWordAnalysis());

            // Enter key in textareas
            const inputSplit = document.getElementById('jpInputSplit');
            const inputWord = document.getElementById('jpInputWord');
            inputSplit?.addEventListener('keydown', (e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    this._runSentenceSplit();
                }
            });
            inputWord?.addEventListener('keydown', (e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    this._runWordAnalysis();
                }
            });

            // ESC to close
            document.addEventListener('keydown', (e) => {
                if (e.key === 'Escape' && this._isOpen) this.close();
            });
        },

        toggle() {
            this._isOpen ? this.close() : this.open();
        },

        open() {
            this._isOpen = true;
            this._panel.classList.add('open');
            this._overlay?.classList.add('visible');
            this._fab.classList.add('active');
            // Focus first input
            setTimeout(() => {
                const activeInput = this._panel.querySelector('.jp-analyzer-tab-content.active textarea');
                activeInput?.focus();
            }, 350);
        },

        close() {
            this._isOpen = false;
            this._panel.classList.remove('open');
            this._overlay?.classList.remove('visible');
            this._fab.classList.remove('active');
        },

        _switchTab(tabId) {
            // Update tab buttons
            this._panel.querySelectorAll('.jp-analyzer-tab').forEach(t => {
                t.classList.toggle('active', t.dataset.tab === tabId);
            });
            // Update tab contents
            this._panel.querySelectorAll('.jp-analyzer-tab-content').forEach(c => {
                c.classList.toggle('active', c.id === tabId);
            });
            // Focus the textarea in the active tab
            setTimeout(() => {
                const activeInput = this._panel.querySelector(`#${tabId} textarea`);
                activeInput?.focus();
            }, 100);
        },

        _runSentenceSplit() {
            const input = document.getElementById('jpInputSplit');
            const resultsDiv = document.getElementById('jpResultsSplit');
            const text = input?.value?.trim();

            if (!text) {
                resultsDiv.innerHTML = this._renderEmpty('Nhập một câu tiếng Nhật để tách');
                return;
            }

            const btn = document.getElementById('jpBtnSplit');
            btn?.classList.add('loading');

            // Small delay for animation feel
            setTimeout(() => {
                try {
                    const tokens = SentenceSplitter.split(text);
                    resultsDiv.innerHTML = this._renderTokens(tokens);
                    resultsDiv.classList.add('has-results');
                } catch (err) {
                    resultsDiv.innerHTML = this._renderError('Lỗi phân tích: ' + err.message);
                    resultsDiv.classList.add('has-results');
                }
                btn?.classList.remove('loading');
            }, 300);
        },

        _runWordAnalysis() {
            const input = document.getElementById('jpInputWord');
            const resultsDiv = document.getElementById('jpResultsWord');
            const text = input?.value?.trim();

            if (!text) {
                resultsDiv.innerHTML = this._renderEmpty('Nhập một động từ hoặc tính từ (thể nguyên thể)');
                return;
            }

            const btn = document.getElementById('jpBtnAnalyze');
            btn?.classList.add('loading');

            setTimeout(() => {
                try {
                    // Kiểm tra lỗi nhập liệu trước
                    const validationError = WordClassifier.validate(text);
                    if (validationError) {
                        resultsDiv.innerHTML = this._renderError(validationError);
                        resultsDiv.classList.add('has-results');
                        btn?.classList.remove('loading');
                        return;
                    }

                    const info = WordClassifier.classify(text);
                    if (!info) {
                        resultsDiv.innerHTML = this._renderError('Không nhận diện được động từ hoặc tính từ hợp lệ ở thể nguyên mẫu.');
                        resultsDiv.classList.add('has-results');
                        btn?.classList.remove('loading');
                        return;
                    }
                    const conjugations = ConjugationEngine.conjugate(text, info);
                    resultsDiv.innerHTML = this._renderWordAnalysis(text, info, conjugations);
                    resultsDiv.classList.add('has-results');
                } catch (err) {
                    resultsDiv.innerHTML = this._renderError('Lỗi phân tích: ' + err.message);
                    resultsDiv.classList.add('has-results');
                }
                btn?.classList.remove('loading');
            }, 300);
        },

        // ── Render helpers ──

        _renderEmpty(msg) {
            return `
                <div class="jp-analyzer-empty">
                    <div><i class="bi bi-translate"></i></div>
                    <p>${msg}</p>
                </div>`;
        },

        _renderError(msg) {
            return `<div class="jp-analyzer-error"><i class="bi bi-exclamation-triangle-fill me-2"></i>${msg}</div>`;
        },

        _renderTokens(tokens) {
            if (!tokens.length) return this._renderEmpty('Không tìm thấy token nào');

            const TYPE_LABELS = {
                'kanji': 'Kanji',
                'hiragana': 'Hiragana (Chữ mềm)',
                'katakana': 'Katakana (Chữ cứng)',
                'particle': 'Trợ từ',
                'verb-ending': 'Đuôi biến đổi',
                'punctuation': 'Dấu câu',
                'unknown': 'Khác',
            };

            // Legend
            let legendHtml = '<div class="jp-token-legend">';
            const usedTypes = [...new Set(tokens.map(t => t.type))];
            for (const type of usedTypes) {
                legendHtml += `<span class="jp-token-legend-item"><span class="jp-token-legend-dot jp-token--${type}"></span>${TYPE_LABELS[type] || type}</span>`;
            }
            legendHtml += '</div>';

            // Tokens
            let tokensHtml = '<div class="jp-token-list">';
            tokens.forEach((t, i) => {
                tokensHtml += `<span class="jp-token jp-token--${t.type}" style="animation-delay:${i * 50}ms" title="${TYPE_LABELS[t.type] || t.type}">${t.text}</span>`;
            });
            tokensHtml += '</div>';

            // Original sentence with color mapping
            let origHtml = '<div class="jp-original-sentence">';
            origHtml += '<small class="text-muted d-block mb-2">Câu gốc:</small>';
            tokens.forEach(t => {
                origHtml += `<span class="jp-inline-token jp-token--${t.type}">${t.text}</span>`;
            });
            origHtml += '</div>';

            return legendHtml + origHtml + '<hr class="my-3">' + tokensHtml;
        },

        _renderWordAnalysis(word, info, conjugations) {
            // Word info card
            const badgeClass = info.type === 'verb' ? 'verb' : (info.type === 'i-adj' ? 'i-adj' : 'na-adj');
            const typeLabel = info.type === 'verb' ? 'Động từ' : (info.type === 'i-adj' ? 'Tính từ đuôi い' : 'Tính từ đuôi な');

            let html = `
                <div class="jp-word-info">
                    <div class="d-flex align-items-center gap-2 mb-2 flex-wrap">
                        <span class="jp-word-badge jp-word-badge--${badgeClass}">${typeLabel}</span>
                        ${info.group ? `<span class="jp-word-badge jp-word-badge--${badgeClass}" style="opacity:.7">Nhóm ${info.group}</span>` : ''}
                    </div>
                    <div class="jp-word-display">
                        <span class="jp-word-root">${info.root || ''}</span><span class="jp-suffix">${info.suffix || ''}</span>
                    </div>
                    <div class="jp-word-desc">${info.display}</div>
                </div>`;

            // Conjugation table
            if (conjugations.length > 0) {
                html += `
                <div class="jp-conjugation-wrapper">
                    <table class="jp-conjugation-table">
                        <thead>
                            <tr>
                                <th>Thể / Form</th>
                                <th>Khẳng định</th>
                                <th>Phủ định</th>
                                <th>QK Khẳng định</th>
                                <th>QK Phủ định</th>
                            </tr>
                        </thead>
                        <tbody>`;

                for (const row of conjugations) {
                    html += `<tr>
                        <td class="jp-form-label">${row.form}</td>
                        <td>${this._renderConjValue(row.values.affirmative)}</td>
                        <td>${this._renderConjValue(row.values.negative)}</td>
                        <td>${this._renderConjValue(row.values.pastAff)}</td>
                        <td>${this._renderConjValue(row.values.pastNeg)}</td>
                    </tr>`;
                }

                html += `</tbody></table></div>`;
            }

            return html;
        },

        _renderConjValue(val) {
            if (!val) return '<span class="text-muted">—</span>';
            return `<span>${val.root}</span><span class="jp-suffix">${val.suffix}</span>`;
        }
    };

    // ─────────────────────────────────────────────────────────
    // PUBLIC API & INITIALIZATION
    // ─────────────────────────────────────────────────────────
    document.addEventListener('DOMContentLoaded', () => {
        UI.init();
    });

    return {
        CharUtils,
        SentenceSplitter,
        WordClassifier,
        ConjugationEngine,
        UI
    };
})();
