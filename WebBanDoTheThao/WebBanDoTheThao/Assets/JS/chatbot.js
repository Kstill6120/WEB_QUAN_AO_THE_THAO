function sendMessage() {
    const input = document.getElementById("userMessage");
    const chatWindow = document.getElementById("chatWindow");
    const message = input.value.trim();

    if (message === "") return;

    // 1. Thêm tin nhắn User (Dùng class mới)
    const userMsg = document.createElement("div");
    userMsg.className = "msg user-msg";
    userMsg.innerText = message;
    chatWindow.appendChild(userMsg);

    input.value = "";
    chatWindow.scrollTop = chatWindow.scrollHeight; // Cuộn xuống dưới cùng

    // ======================================================
    // 2. Gửi về Server (ĐÃ SỬA ĐỂ GỌI API)
    // ======================================================
    fetch("/api/chatbot/ask", { // Đổi đường dẫn sang API
        method: "POST",
        headers: {
            "Content-Type": "application/json" // Đổi header sang JSON
        },
        // Đổi body sang chuỗi JSON (khớp với class ChatRequest trong C#)
        body: JSON.stringify({ message: message })
    })
        .then(res => {
            if (!res.ok) {
                throw new Error('Lỗi mạng hoặc server');
            }
            return res.json();
        })
        .then(data => {
            // 3. Thêm tin nhắn Bot
            const botMsg = document.createElement("div");
            botMsg.className = "msg bot-msg";

            // Data trả về từ API vẫn là { reply: "..." } nên dùng data.reply
            botMsg.innerHTML = data.reply;

            chatWindow.appendChild(botMsg);
            chatWindow.scrollTop = chatWindow.scrollHeight;
        })
        .catch(err => {
            console.error("Lỗi:", err);
            // (Tùy chọn) Hiện thông báo lỗi lên khung chat để dễ debug
            const errorMsg = document.createElement("div");
            errorMsg.className = "msg bot-msg";
            errorMsg.innerText = "Xin lỗi, server đang bận. Bạn thử lại sau nhé!";
            chatWindow.appendChild(errorMsg);
        });
}

function toggleChat() {
    const chatBox = document.getElementById("chatBotBox");
    if (chatBox.style.display === "flex") {
        chatBox.style.display = "none";
    } else {
        chatBox.style.display = "flex";
        document.getElementById("userMessage").focus();
    }
}

// Bắt sự kiện nhấn Enter
document.getElementById("userMessage").addEventListener("keypress", function (e) {
    if (e.key === "Enter") {
        sendMessage();
    }
});