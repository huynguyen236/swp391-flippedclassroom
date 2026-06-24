/**
 * ============================================================
 *  SPEECH PRACTICE — Luyện nói & phân tích phát âm tiếng Nhật
 *  Flipped Classroom Project
 *
 *  Mỗi khối .speech-practice là một bài luyện nói độc lập
 *  (một trang có thể có nhiều material "speech").
 *  - Speech-to-Text: Web Speech API (Chrome/Edge)
 *  - Text-to-Speech: SpeechSynthesis (nghe mẫu)
 *  - Chấm điểm: gọi handler CompareSpeech ở backend (MeCab + Levenshtein)
 * ============================================================
 */
(function () {
    'use strict';

    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('.speech-practice').forEach(initSpeechBlock);
    });

    function initSpeechBlock(block) {
        const recordBtn = block.querySelector('.speech-record-btn');
        const ttsBtn = block.querySelector('.speech-tts-btn');
        const statusEl = block.querySelector('.speech-status');
        const targetEl = block.querySelector('.speech-target');
        const resultEl = block.querySelector('.speech-result');
        const scoreCircle = block.querySelector('.speech-score-circle');

        const materialId = block.dataset.materialId;
        const compareUrl = block.dataset.compareUrl;

        let isRecording = false;

        function setStatus(text, cls) {
            statusEl.textContent = text;
            statusEl.className = 'speech-status fw-bold small text-uppercase ' + cls;
        }

        // ── Text-to-Speech: nghe mẫu ──
        ttsBtn?.addEventListener('click', function () {
            const u = new SpeechSynthesisUtterance(targetEl.textContent.trim());
            u.lang = 'ja-JP';
            const ja = window.speechSynthesis.getVoices().find(v => v.lang && v.lang.startsWith('ja'));
            if (ja) u.voice = ja;
            window.speechSynthesis.cancel();
            window.speechSynthesis.speak(u);
        });

        // ── Speech-to-Text ──
        if (!SpeechRecognition) {
            setStatus('Trình duyệt không hỗ trợ. Dùng Chrome/Edge.', 'text-danger');
            recordBtn.disabled = true;
            return;
        }

        const recognition = new SpeechRecognition();
        recognition.lang = 'ja-JP';
        recognition.continuous = false;
        recognition.interimResults = false;

        recognition.onstart = function () {
            isRecording = true;
            recordBtn.classList.add('recording');
            setStatus('Đang nghe... Hãy nói ngay bây giờ', 'text-danger');
        };
        recognition.onend = function () {
            isRecording = false;
            recordBtn.classList.remove('recording');
        };
        recognition.onerror = function (e) {
            console.error(e.error);
            setStatus('Lỗi thu âm: ' + e.error, 'text-danger');
        };
        recognition.onresult = function (e) {
            const spokenText = e.results[0][0].transcript;
            setStatus('Nhận diện thành công! Đang chấm điểm...', 'text-success');
            sendToBackend(spokenText);
        };

        recordBtn.addEventListener('click', function () {
            if (isRecording) {
                recognition.stop();
            } else {
                try { recognition.start(); }
                catch (err) { console.error(err); }
            }
        });

        // ── Gửi AJAX chấm điểm ──
        async function sendToBackend(spoken) {
            try {
                const params = new URLSearchParams({
                    materialId: materialId,
                    spokenText: spoken
                });
                const res = await fetch(compareUrl + '&' + params.toString());
                if (!res.ok) throw new Error('HTTP ' + res.status);
                const data = await res.json();

                if (data.success) {
                    displayResult(spoken, data);
                } else {
                    setStatus(data.message || 'Lỗi xử lý ở Server.', 'text-danger');
                }
            } catch (err) {
                console.error(err);
                setStatus('Không thể kết nối máy chủ.', 'text-danger');
            }
        }

        // ── Hiển thị kết quả ──
        function displayResult(spoken, data) {
            block.querySelector('.speech-score-text').textContent = data.score;
            block.querySelector('.speech-spoken-text').textContent = spoken;
            block.querySelector('.speech-target-hira').textContent = data.targetHiragana;
            block.querySelector('.speech-spoken-hira').textContent = data.spokenHiragana;

            const alignEl = block.querySelector('.speech-alignment');
            alignEl.innerHTML = '';
            (data.alignment || []).forEach(function (tok) {
                const span = document.createElement('span');
                if (tok.type === 'match') {
                    span.className = 'align-match';
                    span.textContent = tok.character;
                } else if (tok.type === 'mismatch') {
                    span.className = 'align-mismatch';
                    span.textContent = tok.character;
                    span.title = 'Bạn phát âm nhầm thành: "' + tok.spokenCharacter + '"';
                } else if (tok.type === 'delete') {
                    span.className = 'align-delete';
                    span.textContent = tok.character;
                    span.title = 'Bạn bỏ quên âm này';
                } else if (tok.type === 'insert') {
                    span.className = 'align-insert';
                    span.textContent = tok.spokenCharacter;
                    span.title = 'Bạn nói dư âm này';
                }
                alignEl.appendChild(span);
            });

            resultEl.style.display = 'block';

            const r = scoreCircle.r.baseVal.value;
            const c = 2 * Math.PI * r;
            scoreCircle.style.strokeDashoffset = c - (data.score / 100) * c;
            scoreCircle.setAttribute('stroke',
                data.score >= 80 ? '#2ec4b6' : data.score >= 50 ? '#ffb703' : '#e63946');

            setStatus('Chấm điểm thành công!', 'text-success');
        }
    }
})();
