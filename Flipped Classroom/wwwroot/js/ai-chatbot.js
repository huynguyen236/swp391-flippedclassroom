/**
 * AI Chatbot — Nihongo Sensei
 * Floating chatbot widget cho hệ thống Flipped Classroom
 * Tích hợp Gemini AI, đồng bộ với Theme hệ thống
 */
(function () {
    'use strict';

    // ─── Config ───
    const CHAT_API = '/Api/AiChat';
    const MAX_HISTORY = 10;

    // ─── State ───
    let isOpen = false;
    let isLoading = false;
    let chatHistory = [];

    // Detect context từ URL
    function detectContext() {
        const path = window.location.pathname;
        const params = new URLSearchParams(window.location.search);
        const ctx = { classId: null, nodeId: null };

        // /StudentClasses/Details?id=5 hoặc /TeacherClasses/Details?id=5
        if (path.includes('/Classes/') || path.includes('Details')) {
            ctx.classId = parseInt(params.get('id')) || null;
        }

        // /StudentClasses/Lesson?id=5&nodeId=12
        if (params.has('nodeId')) {
            ctx.nodeId = parseInt(params.get('nodeId')) || null;
        }

        // data attributes trên body
        const body = document.body;
        if (body.dataset.classId) ctx.classId = parseInt(body.dataset.classId);
        if (body.dataset.nodeId) ctx.nodeId = parseInt(body.dataset.nodeId);

        return ctx;
    }

    // ─── Build DOM ───
    function createChatWidget() {
        // Floating bubble
        const bubble = document.createElement('button');
        bubble.className = 'ai-chatbot-bubble';
        bubble.id = 'aiChatBubble';
        bubble.setAttribute('aria-label', 'Mở trợ lý AI Sensei');
        bubble.innerHTML = `
            <span class="ai-bubble-icon"><i class="bi bi-robot"></i></span>
        `;

        // Chat window
        const chatWindow = document.createElement('div');
        chatWindow.className = 'ai-chat-window';
        chatWindow.id = 'aiChatWindow';
        chatWindow.innerHTML = `
            <div class="ai-chat-header">
                <div class="ai-chat-avatar">
                    <i class="bi bi-mortarboard-fill text-white"></i>
                </div>
                <div class="ai-chat-header-info">
                    <div class="ai-chat-header-title">
                        <span>Nihongo Sensei</span>
                        <span class="ai-chip">Gemini AI</span>
                    </div>
                    <div class="ai-chat-header-status">
                        <span class="status-dot"></span>
                        <span>Trợ lý học tiếng Nhật 24/7</span>
                    </div>
                </div>
                <div class="ai-chat-header-actions">
                    <button class="ai-header-btn" id="aiChatClear" title="Xóa lịch sử hội thoại" aria-label="Xóa hội thoại">
                        <i class="bi bi-trash3"></i>
                    </button>
                    <button class="ai-header-btn" id="aiChatClose" title="Đóng cửa sổ" aria-label="Đóng chat">
                        <i class="bi bi-x-lg"></i>
                    </button>
                </div>
            </div>
            <div class="ai-chat-messages" id="aiChatMessages">
                <div class="ai-welcome-card">
                    <span class="ai-welcome-emoji">🌸</span>
                    <div class="ai-welcome-title">Xin chào! Tôi là Nihongo Sensei</div>
                    <div class="ai-welcome-text">
                        Trợ lý AI sẵn sàng giải đáp ngữ pháp, tra từ vựng, phân tích lỗi sai và luyện tập hội thoại tiếng Nhật cùng bạn.
                    </div>
                    <div class="ai-quick-actions">
                        <button class="ai-quick-btn" data-msg="Giải thích từ vựng bài học hiện tại"><i class="bi bi-translate"></i> Từ vựng bài này</button>
                        <button class="ai-quick-btn" data-msg="Phân tích lỗi sai gần đây của tôi và gợi ý cách cải thiện"><i class="bi bi-exclamation-triangle"></i> Phân tích lỗi sai</button>
                        <button class="ai-quick-btn" data-msg="Cho tôi 3 câu trắc nghiệm ngữ pháp tiếng Nhật N5 kèm đáp án"><i class="bi bi-ui-checks"></i> Bài tập trắc nghiệm</button>
                        <button class="ai-quick-btn" data-msg="Giải thích cách dùng các trợ từ は, が, を, に trong tiếng Nhật"><i class="bi bi-book"></i> Ngữ pháp trợ từ</button>
                    </div>
                </div>
            </div>
            <div class="ai-chat-input-area">
                <div class="ai-chat-input-wrapper">
                    <textarea 
                        class="ai-chat-input" 
                        id="aiChatInput" 
                        placeholder="Hỏi Sensei về từ vựng, ngữ pháp tiếng Nhật..."
                        rows="1"
                        maxlength="1000"
                    ></textarea>
                    <button class="ai-chat-send" id="aiChatSend" aria-label="Gửi câu hỏi" title="Gửi (Enter)">
                        <i class="bi bi-send-fill"></i>
                    </button>
                </div>
                <div class="ai-chat-hint">
                    Nhấn <strong>Enter</strong> để gửi • <strong>Shift + Enter</strong> để xuống dòng
                </div>
            </div>
        `;

        document.body.appendChild(chatWindow);
        document.body.appendChild(bubble);

        return { bubble, chatWindow };
    }

    // ─── Render Messages ───
    function addMessage(role, content) {
        const messagesEl = document.getElementById('aiChatMessages');
        const msgEl = document.createElement('div');
        msgEl.className = `ai-msg ${role === 'user' ? 'user' : 'bot'}`;

        const avatarHtml = role === 'user' 
            ? '<i class="bi bi-person-fill"></i>' 
            : '<i class="bi bi-robot"></i>';
        
        const formattedContent = role === 'user' ? escapeHtml(content) : formatMarkdown(content);

        const actionsHtml = role === 'bot' ? `
            <div class="ai-msg-actions">
                <button class="ai-copy-btn" title="Sao chép câu trả lời">
                    <i class="bi bi-clipboard"></i> Sao chép
                </button>
            </div>
        ` : '';

        msgEl.innerHTML = `
            <div class="ai-msg-avatar">${avatarHtml}</div>
            <div class="ai-msg-wrapper">
                <div class="ai-msg-bubble">${formattedContent}</div>
                ${actionsHtml}
            </div>
        `;

        // Gắn sự kiện sao chép
        if (role === 'bot') {
            const copyBtn = msgEl.querySelector('.ai-copy-btn');
            if (copyBtn) {
                copyBtn.addEventListener('click', () => {
                    navigator.clipboard.writeText(content).then(() => {
                        copyBtn.innerHTML = '<i class="bi bi-check2 text-success"></i> Đã chép!';
                        setTimeout(() => {
                            copyBtn.innerHTML = '<i class="bi bi-clipboard"></i> Sao chép';
                        }, 2000);
                    });
                });
            }
        }

        messagesEl.appendChild(msgEl);
        scrollToBottom();
    }

    function showTyping() {
        const messagesEl = document.getElementById('aiChatMessages');
        const typingEl = document.createElement('div');
        typingEl.className = 'ai-typing';
        typingEl.id = 'aiTypingIndicator';
        typingEl.innerHTML = `
            <div class="ai-msg-avatar" style="background: var(--ai-gradient); color: #fff;">
                <i class="bi bi-robot"></i>
            </div>
            <div class="ai-typing-dots">
                <span></span><span></span><span></span>
            </div>
        `;
        messagesEl.appendChild(typingEl);
        scrollToBottom();
    }

    function hideTyping() {
        const el = document.getElementById('aiTypingIndicator');
        if (el) el.remove();
    }

    function scrollToBottom() {
        const el = document.getElementById('aiChatMessages');
        if (!el) return;
        setTimeout(() => {
            el.scrollTop = el.scrollHeight;
        }, 50);
    }

    // ─── Format helpers ───
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function formatMarkdown(text) {
        if (!text) return '';
        let formatted = text
            // Escape HTML trước
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            // Code block
            .replace(/```([a-zA-Z]*)\n([\s\S]*?)```/g, '<pre><code class="language-$1">$2</code></pre>')
            // Inline code
            .replace(/`([^`]+)`/g, '<code>$1</code>')
            // Bold
            .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
            // Italic
            .replace(/\*(.*?)\*/g, '<em>$1</em>')
            // Unordered list
            .replace(/^[-•*]\s+(.+)$/gm, '<li>$1</li>')
            // Numbered list
            .replace(/^\d+\.\s+(.+)$/gm, '<li>$1</li>')
            // Wrap consecutive <li> in <ul>
            .replace(/((?:<li>.*<\/li>\n?)+)/g, '<ul>$1</ul>')
            // Headers
            .replace(/^### (.*$)/gm, '<h6 class="fw-bold mt-2 mb-1 text-primary">$1</h6>')
            .replace(/^## (.*$)/gm, '<h5 class="fw-bold mt-2 mb-1 text-primary">$1</h5>')
            .replace(/^# (.*$)/gm, '<h4 class="fw-bold mt-2 mb-1 text-primary">$1</h4>')
            // Paragraphs & Linebreaks
            .replace(/\n\n/g, '</p><p>')
            .replace(/\n/g, '<br>')
            .replace(/^(.+)$/, '<p>$1</p>')
            .replace(/<p><\/p>/g, '')
            .replace(/<p><ul>/g, '<ul>')
            .replace(/<\/ul><\/p>/g, '</ul>')
            .replace(/<p><pre>/g, '<pre>')
            .replace(/<\/pre><\/p>/g, '</pre>');

        return formatted;
    }

    // ─── API Call ───
    async function sendMessage(message) {
        if (isLoading || !message || !message.trim()) return;

        isLoading = true;
        const sendBtn = document.getElementById('aiChatSend');
        if (sendBtn) sendBtn.disabled = true;

        // Ẩn welcome card
        const welcomeCard = document.querySelector('.ai-welcome-card');
        if (welcomeCard) welcomeCard.style.display = 'none';

        // Hiển thị tin nhắn user
        addMessage('user', message);

        // Lưu history
        chatHistory.push({ role: 'user', content: message, timestamp: new Date().toISOString() });

        // Hiển thị typing
        showTyping();

        try {
            const ctx = detectContext();
            const requestBody = {
                message: message,
                classId: ctx.classId,
                nodeId: ctx.nodeId,
                history: chatHistory.slice(-MAX_HISTORY)
            };

            const response = await fetch(CHAT_API, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken()
                },
                body: JSON.stringify(requestBody)
            });

            const data = await response.json();

            hideTyping();

            if (data.success) {
                addMessage('bot', data.reply);
                chatHistory.push({ role: 'model', content: data.reply, timestamp: new Date().toISOString() });
            } else {
                addMessage('bot', `⚠️ ${data.error || 'Đã xảy ra lỗi khi trao đổi với Sensei. Vui lòng thử lại.'}`);
            }
        } catch (err) {
            hideTyping();
            addMessage('bot', '⚠️ Không thể kết nối tới máy chủ AI. Vui lòng kiểm tra lại mạng.');
            console.error('AI Chat error:', err);
        }

        isLoading = false;
        if (sendBtn) sendBtn.disabled = false;
    }

    function getAntiForgeryToken() {
        const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenEl ? tokenEl.value : '';
    }

    // ─── Clear Chat ───
    function clearChat() {
        if (!confirm('Bạn có chắc chắn muốn xóa toàn bộ lịch sử trò chuyện này?')) return;
        chatHistory = [];
        try {
            sessionStorage.removeItem('aiChatHistory');
        } catch (e) {}

        const messagesEl = document.getElementById('aiChatMessages');
        if (messagesEl) {
            messagesEl.innerHTML = `
                <div class="ai-welcome-card">
                    <span class="ai-welcome-emoji">🌸</span>
                    <div class="ai-welcome-title">Xin chào! Tôi là Nihongo Sensei</div>
                    <div class="ai-welcome-text">
                        Lịch sử trò chuyện đã được làm mới. Hãy đặt câu hỏi mới nhé!
                    </div>
                    <div class="ai-quick-actions">
                        <button class="ai-quick-btn" data-msg="Giải thích từ vựng bài học hiện tại"><i class="bi bi-translate"></i> Từ vựng bài này</button>
                        <button class="ai-quick-btn" data-msg="Phân tích lỗi sai gần đây của tôi và gợi ý cách cải thiện"><i class="bi bi-exclamation-triangle"></i> Phân tích lỗi sai</button>
                        <button class="ai-quick-btn" data-msg="Cho tôi 3 câu trắc nghiệm ngữ pháp tiếng Nhật N5 kèm đáp án"><i class="bi bi-ui-checks"></i> Bài tập trắc nghiệm</button>
                    </div>
                </div>
            `;
        }
    }

    // ─── Toggle ───
    function toggleChat() {
        isOpen = !isOpen;
        const bubble = document.getElementById('aiChatBubble');
        const chatWindow = document.getElementById('aiChatWindow');

        if (!bubble || !chatWindow) return;

        bubble.classList.toggle('open', isOpen);
        chatWindow.classList.toggle('open', isOpen);

        if (isOpen) {
            bubble.querySelector('.ai-bubble-icon').innerHTML = '<i class="bi bi-x-lg"></i>';
            setTimeout(() => {
                const input = document.getElementById('aiChatInput');
                if (input) input.focus();
            }, 350);
        } else {
            bubble.querySelector('.ai-bubble-icon').innerHTML = '<i class="bi bi-robot"></i>';
        }
    }

    // ─── Auto-resize textarea ───
    function autoResize(textarea) {
        textarea.style.height = 'auto';
        textarea.style.height = Math.min(textarea.scrollHeight, 90) + 'px';
    }

    // ─── Init ───
    function init() {
        const { bubble } = createChatWidget();

        // Toggle chat
        bubble.addEventListener('click', toggleChat);
        document.getElementById('aiChatClose').addEventListener('click', toggleChat);
        document.getElementById('aiChatClear').addEventListener('click', clearChat);

        // Send message
        const sendBtn = document.getElementById('aiChatSend');
        const input = document.getElementById('aiChatInput');

        if (sendBtn && input) {
            sendBtn.addEventListener('click', () => {
                sendMessage(input.value);
                input.value = '';
                autoResize(input);
            });

            input.addEventListener('keydown', (e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    sendMessage(input.value);
                    input.value = '';
                    autoResize(input);
                }
            });

            input.addEventListener('input', () => autoResize(input));
        }

        // Quick action buttons
        document.addEventListener('click', (e) => {
            const btn = e.target.closest('.ai-quick-btn');
            if (btn) {
                const msg = btn.dataset.msg;
                if (msg) sendMessage(msg);
            }
        });

        // Restore session history
        try {
            const saved = sessionStorage.getItem('aiChatHistory');
            if (saved) {
                chatHistory = JSON.parse(saved);
                if (chatHistory.length > 0) {
                    const welcomeCard = document.querySelector('.ai-welcome-card');
                    if (welcomeCard) welcomeCard.style.display = 'none';
                    chatHistory.forEach(msg => {
                        addMessage(msg.role === 'user' ? 'user' : 'bot', msg.content);
                    });
                }
            }
        } catch (e) { /* ignore */ }

        // Save history on page unload
        window.addEventListener('beforeunload', () => {
            try {
                sessionStorage.setItem('aiChatHistory', JSON.stringify(chatHistory.slice(-MAX_HISTORY)));
            } catch (e) { /* ignore */ }
        });
    }

    // Wait for DOM
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // ─── Expose global API ───
    window.AiChatbot = {
        open: () => { if (!isOpen) toggleChat(); },
        close: () => { if (isOpen) toggleChat(); },
        sendMessage: sendMessage
    };
})();

/**
 * AI Grade — Hàm helper gọi AI chấm điểm từ UI Teacher
 */
window.AiGrade = {
    /**
     * Gọi AI chấm điểm cho một submission
     */
    gradeSubmission: async function (submissionId, resultContainer, triggerBtn) {
        if (triggerBtn) {
            triggerBtn.disabled = true;
            triggerBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status"></span> Đang chấm...';
        }

        try {
            const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
            const token = tokenEl ? tokenEl.value : '';

            const response = await fetch(`/Api/AiGrade?submissionId=${submissionId}`, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token
                }
            });

            const data = await response.json();

            if (data.success) {
                if (resultContainer) {
                    resultContainer.innerHTML = `
                        <div class="ai-grade-result">
                            <div class="d-flex align-items-center gap-3 mb-2">
                                <div>
                                    <div class="ai-grade-label">Điểm AI Gợi Ý</div>
                                    <div class="ai-grade-score">${data.suggestedScore.toFixed(1)}<span style="font-size:16px; color:#737686;">/10</span></div>
                                </div>
                                <div class="flex-grow-1">
                                    <div class="ai-grade-label">Nhận Xét Của AI Sensei</div>
                                    <div class="ai-grade-feedback">${data.feedback}</div>
                                </div>
                            </div>
                            <div class="ai-grade-reasoning">
                                <strong><i class="bi bi-card-checklist text-primary"></i> Phân tích chi tiết:</strong><br>
                                ${data.reasoning}
                            </div>
                            <div class="ai-grade-actions">
                                <button type="button" class="btn btn-primary btn-sm rounded-pill px-3" onclick="AiGrade.acceptScore(${submissionId}, ${data.suggestedScore.toFixed(1)}, '${escapeJsString(data.feedback)}')">
                                    <i class="bi bi-check-circle-fill me-1"></i> Chấp nhận điểm AI
                                </button>
                                <button type="button" class="btn btn-outline-secondary btn-sm rounded-pill px-3" onclick="openGradeModal(${submissionId}, ${data.suggestedScore.toFixed(1)}, '${escapeJsString(data.feedback)}')">
                                    <i class="bi bi-pencil-square me-1"></i> Chỉnh sửa
                                </button>
                            </div>
                        </div>
                    `;
                    resultContainer.style.display = 'block';
                }
            } else {
                alert('⚠️ ' + (data.error || 'AI không thể chấm điểm bài này.'));
            }
        } catch (err) {
            console.error('AI Grade error:', err);
            alert('⚠️ Lỗi kết nối AI. Vui lòng thử lại.');
        }

        if (triggerBtn) {
            triggerBtn.disabled = false;
            triggerBtn.innerHTML = '<i class="bi bi-robot me-1"></i> AI Chấm';
        }
    },

    acceptScore: function (submissionId, score, feedback) {
        if (typeof window.openGradeModal === 'function') {
            window.openGradeModal(submissionId, score, feedback);
            setTimeout(() => {
                const form = document.querySelector('#gradeSubmissionModal form');
                if (form) form.submit();
            }, 300);
        }
    }
};

function escapeJsString(str) {
    if (!str) return '';
    return str.replace(/\\/g, '\\\\').replace(/'/g, "\\'").replace(/\n/g, '\\n').replace(/\r/g, '');
}
