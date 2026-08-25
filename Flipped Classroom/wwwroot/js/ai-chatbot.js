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
     * Khởi tạo hoặc lấy modal popup kết quả chấm điểm của AI
     */
    ensureAiGradeModal: function() {
        if (document.getElementById('aiGradeResultModal')) return;

        const modalHtml = `
<div class="modal fade" id="aiGradeResultModal" tabindex="-1" aria-labelledby="aiGradeResultModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered modal-lg">
        <div class="modal-content border-0 shadow-lg" style="border-radius: 20px; overflow: hidden; background-color: #ffffff;">
            <div class="modal-header border-bottom-0 text-white p-4" style="background: linear-gradient(135deg, #004ac6 0%, #2563eb 100%);">
                <h5 class="modal-title fw-bold d-flex align-items-center gap-2" id="aiGradeResultModalLabel">
                    <i class="bi bi-robot"></i> AI Sensei Chấm Điểm
                </h5>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body p-4" style="background-color: #f8fafc;">
                <div class="row g-4">
                    <!-- Cột trái: Điểm số gợi ý và Nút hành động -->
                    <div class="col-md-4 text-center d-flex flex-column align-items-center justify-content-center border-end pb-4 pb-md-0" style="border-color: #e2e8f0 !important;">
                        <div class="p-3 bg-white rounded-circle shadow-sm d-flex flex-column align-items-center justify-content-center mb-3" style="width: 150px; height: 150px; border: 4px solid #e2e8f0; margin: 0 auto;">
                            <span class="text-muted small fw-bold" style="font-size: 11px; letter-spacing: 1px;">ĐIỂM GỢI Ý</span>
                            <span class="fw-bold text-primary display-4 my-1" id="aiGradeResultScore" style="font-family: 'Outfit', 'Inter', sans-serif;">0.0</span>
                            <span class="text-secondary fw-semibold" style="font-size: 14px;">/ 10</span>
                        </div>
                        <div class="w-100 px-2 mt-2">
                            <button type="button" class="btn btn-primary w-100 rounded-pill py-2.5 mb-2 shadow-sm d-flex align-items-center justify-content-center gap-2" id="aiGradeAcceptBtn" style="font-weight: 600; font-size: 0.95rem;">
                                <i class="bi bi-check-circle-fill"></i> Chấp nhận điểm
                            </button>
                            <button type="button" class="btn btn-outline-secondary w-100 rounded-pill py-2.5 d-flex align-items-center justify-content-center gap-2" id="aiGradeEditBtn" style="font-weight: 600; font-size: 0.95rem;">
                                <i class="bi bi-pencil-square"></i> Chỉnh sửa điểm
                            </button>
                        </div>
                    </div>
                    
                    <!-- Cột phải: Nhận xét và Phân tích chi tiết -->
                    <div class="col-md-8 d-flex flex-column gap-3 text-start">
                        <div>
                            <h6 class="fw-bold text-secondary d-flex align-items-center gap-2 mb-2" style="font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px;">
                                <i class="bi bi-chat-left-quote-fill text-primary"></i> Nhận Xét Của AI Sensei
                            </h6>
                            <div class="p-3 bg-white rounded-3 shadow-sm border-start border-primary border-4" id="aiGradeResultFeedback" style="font-style: italic; color: #334155; line-height: 1.6; font-size: 0.95rem;">
                            </div>
                        </div>
                        
                        <div>
                            <h6 class="fw-bold text-secondary d-flex align-items-center gap-2 mb-2" style="font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px;">
                                <i class="bi bi-journal-text text-primary"></i> Phân Tích Chi Tiết
                            </h6>
                            <div class="p-3 bg-white rounded-3 shadow-sm border" id="aiGradeResultReasoning" style="color: #475569; font-size: 0.95rem; line-height: 1.7; max-height: 250px; overflow-y: auto;">
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>
        `;
        const wrapper = document.createElement('div');
        wrapper.innerHTML = modalHtml;
        document.body.appendChild(wrapper.firstElementChild);
    },

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
                // Đảm bảo modal HTML đã được thêm vào body
                this.ensureAiGradeModal();

                // Điền dữ liệu vào modal
                document.getElementById('aiGradeResultScore').innerText = data.suggestedScore.toFixed(1);
                document.getElementById('aiGradeResultFeedback').innerText = data.feedback;
                document.getElementById('aiGradeResultReasoning').innerHTML = data.reasoning.replace(/\n/g, '<br>');

                // Gán hành động cho nút "Chấp nhận điểm"
                const acceptBtn = document.getElementById('aiGradeAcceptBtn');
                acceptBtn.onclick = () => {
                    const modalEl = document.getElementById('aiGradeResultModal');
                    const modalInstance = bootstrap.Modal.getInstance(modalEl);
                    if (modalInstance) modalInstance.hide();
                    this.acceptScore(submissionId, data.suggestedScore, data.feedback);
                };

                // Gán hành động cho nút "Chỉnh sửa điểm"
                const editBtn = document.getElementById('aiGradeEditBtn');
                editBtn.onclick = () => {
                    const modalEl = document.getElementById('aiGradeResultModal');
                    const modalInstance = bootstrap.Modal.getInstance(modalEl);
                    if (modalInstance) modalInstance.hide();
                    
                    if (typeof window.openGradeModal === 'function') {
                        window.openGradeModal(submissionId, data.suggestedScore, data.feedback);
                    }
                };

                // Hiển thị modal
                const myModal = new bootstrap.Modal(document.getElementById('aiGradeResultModal'));
                myModal.show();
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
