// Chatbox functionality
class ChatBox {
    constructor() {
        this.messages = [];
        this.isOpen = false;
        this.init();
        this.loadMessages();
    }

    init() {
        // Create chatbox HTML
        this.createChatboxHTML();
        
        // Event listeners
        document.getElementById('chatIcon').addEventListener('click', () => this.toggle());
        document.getElementById('chatClose').addEventListener('click', () => this.close());
        document.getElementById('chatInput').addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                this.sendMessage();
            }
        });
        document.getElementById('chatSend').addEventListener('click', () => this.sendMessage());
        
        // Quick reply buttons
        document.querySelectorAll('.quick-reply-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                this.sendMessage(e.target.dataset.message);
            });
        });

        // Welcome message
        if (this.messages.length === 0) {
            setTimeout(() => {
                this.addBotMessage('Xin chào! Tôi là trợ lý UniStyle. Tôi có thể giúp gì cho bạn?');
            }, 1000);
        }
    }

    createChatboxHTML() {
        const chatboxHTML = `
            <!-- Chat Icon -->
            <div class="chat-icon" id="chatIcon">
                <i class="fas fa-comments"></i>
            </div>

            <!-- Chat Box -->
            <div class="chat-box" id="chatBox">
                <div class="chat-header">
                    <div class="chat-header-info">
                        <div class="chat-header-avatar">
                            <i class="fas fa-headset"></i>
                        </div>
                        <div class="chat-header-text">
                            <h5>UniStyle Support</h5>
                            <p>Luôn sẵn sàng hỗ trợ bạn</p>
                        </div>
                    </div>
                    <button class="chat-close" id="chatClose">
                        <i class="fas fa-times"></i>
                    </button>
                </div>

                <div class="chat-body" id="chatBody">
                    <div class="typing-indicator" id="typingIndicator">
                        <span class="typing-dot"></span>
                        <span class="typing-dot"></span>
                        <span class="typing-dot"></span>
                    </div>
                </div>

                <div class="chat-footer">
                    <div class="quick-replies">
                        <button class="quick-reply-btn" data-message="Tôi muốn xem sản phẩm">
                            <i class="fas fa-shopping-bag"></i> Xem sản phẩm
                        </button>
                        <button class="quick-reply-btn" data-message="Kiểm tra đơn hàng">
                            <i class="fas fa-box"></i> Kiểm tra đơn hàng
                        </button>
                        <button class="quick-reply-btn" data-message="Liên hệ hỗ trợ">
                            <i class="fas fa-phone"></i> Liên hệ
                        </button>
                    </div>
                    <div class="chat-input-group">
                        <input type="text" class="chat-input" id="chatInput" placeholder="Nhập tin nhắn...">
                        <button class="chat-send" id="chatSend">
                            <i class="fas fa-paper-plane"></i>
                        </button>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', chatboxHTML);
    }

    toggle() {
        this.isOpen = !this.isOpen;
        const chatBox = document.getElementById('chatBox');
        
        if (this.isOpen) {
            chatBox.classList.add('active');
            document.getElementById('chatInput').focus();
        } else {
            chatBox.classList.remove('active');
        }
    }

    close() {
        this.isOpen = false;
        document.getElementById('chatBox').classList.remove('active');
    }

    sendMessage(text = null) {
        const input = document.getElementById('chatInput');
        const message = text || input.value.trim();

        if (!message) return;

        // Add user message
        this.addMessage('user', message);
        input.value = '';

        // Show typing indicator
        this.showTyping();

        // Simulate bot response
        setTimeout(() => {
            this.hideTyping();
            this.getBotResponse(message);
        }, 1000 + Math.random() * 1000);
    }

    addMessage(type, text) {
        const time = new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
        const message = { type, text, time };
        this.messages.push(message);
        this.saveMessages();
        this.renderMessage(message);
    }

    addBotMessage(text) {
        this.addMessage('bot', text);
    }

    renderMessage(message) {
        const chatBody = document.getElementById('chatBody');
        const messageHTML = `
            <div class="chat-message ${message.type}">
                <div class="message-content">
                    ${message.text}
                    <div class="message-time">${message.time}</div>
                </div>
            </div>
        `;

        // Insert before typing indicator
        const typingIndicator = document.getElementById('typingIndicator');
        typingIndicator.insertAdjacentHTML('beforebegin', messageHTML);
        
        // Scroll to bottom
        chatBody.scrollTop = chatBody.scrollHeight;
    }

    showTyping() {
        document.getElementById('typingIndicator').classList.add('active');
        const chatBody = document.getElementById('chatBody');
        chatBody.scrollTop = chatBody.scrollHeight;
    }

    hideTyping() {
        document.getElementById('typingIndicator').classList.remove('active');
    }

    getBotResponse(userMessage) {
        const lowerMessage = userMessage.toLowerCase();
        let response = '';

        // Simple keyword-based responses
        if (lowerMessage.includes('xin chào') || lowerMessage.includes('chào') || lowerMessage.includes('hello') || lowerMessage.includes('hi')) {
            response = 'Xin chào! Rất vui được hỗ trợ bạn. Bạn cần tìm hiểu về sản phẩm nào?';
        }
        else if (lowerMessage.includes('sản phẩm') || lowerMessage.includes('áo') || lowerMessage.includes('quần')) {
            response = 'Chúng tôi có nhiều sản phẩm thời trang unisex chất lượng cao! Bạn có thể xem danh sách sản phẩm <a href="/Product/Shop" target="_blank">tại đây</a> hoặc cho tôi biết bạn đang tìm loại sản phẩm nào?';
        }
        else if (lowerMessage.includes('giá') || lowerMessage.includes('bao nhiêu')) {
            response = 'Giá sản phẩm của chúng tôi rất cạnh tranh, từ 200.000₫ đến 800.000₫. Bạn muốn xem sản phẩm nào cụ thể?';
        }
        else if (lowerMessage.includes('đơn hàng') || lowerMessage.includes('order') || lowerMessage.includes('kiểm tra')) {
            response = 'Để kiểm tra đơn hàng, bạn vui lòng đăng nhập và vào mục "Đơn hàng của tôi". Hoặc liên hệ hotline: 1900-xxxx để được hỗ trợ trực tiếp.';
        }
        else if (lowerMessage.includes('giao hàng') || lowerMessage.includes('ship') || lowerMessage.includes('vận chuyển')) {
            response = 'Chúng tôi hỗ trợ giao hàng toàn quốc. Miễn phí ship cho đơn hàng trên 500.000₫. Thời gian giao hàng: 2-5 ngày làm việc.';
        }
        else if (lowerMessage.includes('liên hệ') || lowerMessage.includes('hotline') || lowerMessage.includes('số điện thoại')) {
            response = 'Bạn có thể liên hệ với chúng tôi qua:<br>📞 Hotline: 1900-xxxx<br>📧 Email: support@unistyle.vn<br>🏢 Địa chỉ: 123 Đường ABC, TP.HCM';
        }
        else if (lowerMessage.includes('cảm ơn') || lowerMessage.includes('thank')) {
            response = 'Rất vui được hỗ trợ bạn! Nếu còn thắc mắc gì, đừng ngại hỏi nhé! 😊';
        }
        else if (lowerMessage.includes('giỏ hàng') || lowerMessage.includes('cart')) {
            response = 'Bạn có thể xem giỏ hàng của mình <a href="/Cart" target="_blank">tại đây</a>. Nếu cần hỗ trợ về giỏ hàng, hãy cho tôi biết nhé!';
        }
        else if (lowerMessage.includes('đổi trả') || lowerMessage.includes('hoàn tiền')) {
            response = 'Chính sách đổi trả của UniStyle:<br>• Đổi trả trong vòng 7 ngày<br>• Sản phẩm còn nguyên tem mác<br>• Miễn phí đổi size<br>Bạn có thể liên hệ 1900-xxxx để được hỗ trợ.';
        }
        else {
            const responses = [
                'Tôi hiểu câu hỏi của bạn. Bạn có thể cung cấp thêm thông tin để tôi hỗ trợ tốt hơn không?',
                'Để được hỗ trợ tốt nhất, bạn có thể liên hệ hotline: 1900-xxxx hoặc chat với nhân viên tư vấn.',
                'Cảm ơn bạn đã liên hệ! Vui lòng cho tôi biết rõ hơn về vấn đề bạn cần hỗ trợ nhé.',
                'Bạn có thể xem thêm thông tin tại trang <a href="/Product/Shop" target="_blank">Cửa hàng</a> hoặc hỏi tôi bất kỳ điều gì!'
            ];
            response = responses[Math.floor(Math.random() * responses.length)];
        }

        this.addBotMessage(response);
    }

    saveMessages() {
        localStorage.setItem('chatMessages', JSON.stringify(this.messages));
    }

    loadMessages() {
        const savedMessages = localStorage.getItem('chatMessages');
        if (savedMessages) {
            this.messages = JSON.parse(savedMessages);
            this.messages.forEach(msg => this.renderMessage(msg));
        }
    }

    clearHistory() {
        this.messages = [];
        localStorage.removeItem('chatMessages');
        const chatBody = document.getElementById('chatBody');
        chatBody.innerHTML = '<div class="typing-indicator" id="typingIndicator"><span class="typing-dot"></span><span class="typing-dot"></span><span class="typing-dot"></span></div>';
    }
}

// Initialize chatbox when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    window.chatBox = new ChatBox();
});
