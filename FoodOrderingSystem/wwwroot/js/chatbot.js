// PB27 AI chatbot widget (markup in Pages/Shared/_ChatWidget.cshtml).
// Sends the conversation to /Chatbot and reads the answer back as Server-Sent Events, so progress
// ("Checking the menu...") and the answer appear as they happen. Stop aborts the request, which also
// cancels the call to Gemini on the server. The conversation is kept in this tab only (sessionStorage).
(function () {
    "use strict";

    const root = document.querySelector("[data-chat]");
    if (!root) return;

    const HISTORY_KEY = "rr-chat-history";
    const MAX_HISTORY = 12;   // same limit as ChatbotService.MaxHistory

    // Pages the bot is allowed to link to. Anything else in an answer stays plain text.
    const LINKS = {
        "/Menu": "Menu",
        "/Reservations/Create": "Book a table",
        "/Reservations": "My bookings",
        "/Orders": "My orders"
    };

    const endpoint = root.dataset.endpoint;
    const token = root.querySelector("input[name='__RequestVerificationToken']").value;
    const launcher = root.querySelector("[data-chat-open]");
    const panel = root.querySelector(".rr-chat-panel");
    const log = root.querySelector("[data-chat-log]");
    const form = root.querySelector("[data-chat-form]");
    const input = root.querySelector("[data-chat-input]");
    const sendButton = root.querySelector("[data-chat-send]");
    const stopButton = root.querySelector("[data-chat-stop]");
    const suggestions = root.querySelector("[data-chat-suggestions]");
    const greeting = log.innerHTML;

    let history = loadHistory();
    let controller = null;   // AbortController of the answer being streamed, null when idle

    // ---- Open, close, new chat ----------------------------------------------------------------

    function setOpen(open) {
        panel.hidden = !open;
        launcher.setAttribute("aria-expanded", String(open));
        if (open) input.focus();
    }

    launcher.addEventListener("click", () => setOpen(panel.hidden));
    root.querySelector("[data-chat-close]").addEventListener("click", () => setOpen(false));
    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape" && !panel.hidden) setOpen(false);
    });

    root.querySelector("[data-chat-reset]").addEventListener("click", () => {
        if (controller) controller.abort();
        history = [];
        saveHistory();
        log.innerHTML = greeting;
        suggestions.hidden = false;
        input.focus();
    });

    // ---- Sending ------------------------------------------------------------------------------

    form.addEventListener("submit", (e) => {
        e.preventDefault();
        send(input.value);
    });

    // Enter sends, Shift+Enter makes a new line
    input.addEventListener("keydown", (e) => {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            send(input.value);
        }
    });

    // The textarea grows with the text, up to the CSS max-height
    input.addEventListener("input", () => {
        input.style.height = "auto";
        input.style.height = input.scrollHeight + "px";
    });

    suggestions.addEventListener("click", (e) => {
        const button = e.target.closest("[data-chat-suggest]");
        if (button) send(button.textContent);
    });

    stopButton.addEventListener("click", () => {
        if (controller) controller.abort();
    });

    async function send(text) {
        text = text.trim();
        if (!text || controller) return;

        input.value = "";
        input.style.height = "auto";
        suggestions.hidden = true;

        addBubble("user", text);
        history.push({ role: "user", text: text });

        const bubble = addBubble("bot", "");
        const status = showStatus(bubble, "Thinking...");
        let answer = "";
        let failed = false;

        setBusy(true);
        controller = new AbortController();

        try {
            const response = await fetch(endpoint, {
                method: "POST",
                headers: { "Content-Type": "application/json", "RequestVerificationToken": token },
                body: JSON.stringify({ messages: history.slice(-MAX_HISTORY) }),
                signal: controller.signal
            });

            if (!response.ok || !response.body) throw new Error("HTTP " + response.status);

            // Read the stream: events are "data: {json}" blocks separated by a blank line
            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let buffer = "";

            while (true) {
                const { value, done } = await reader.read();
                if (done) break;

                buffer += decoder.decode(value, { stream: true });
                const blocks = buffer.split("\n\n");
                buffer = blocks.pop();   // the last block may be incomplete; keep it for the next read

                for (const block of blocks) {
                    if (!block.startsWith("data:")) continue;
                    const event = JSON.parse(block.slice(5));

                    if (event.type === "status") {
                        status.update(event.text);
                    } else if (event.type === "text") {
                        status.hide();
                        answer += event.text;
                        renderAnswer(bubble, answer);
                    } else if (event.type === "error") {
                        failed = true;
                        status.hide();
                        bubble.classList.add("rr-chat-error");
                        renderAnswer(bubble, answer ? answer + "\n\n" + event.text : event.text);
                    } else if (event.type === "done") {
                        status.hide();
                    }
                    scrollDown();
                }
            }
        } catch (err) {
            status.hide();
            if (err.name === "AbortError") {
                // Stopped by the customer: keep what was written so far
                if (!answer) renderAnswer(bubble, "");
                const note = document.createElement("span");
                note.className = "rr-chat-stopped";
                note.textContent = "Stopped";
                bubble.appendChild(note);
            } else {
                failed = true;
                bubble.classList.add("rr-chat-error");
                renderAnswer(bubble, "Sorry, the chat could not connect. Please check your internet and try again.");
            }
        } finally {
            controller = null;
            setBusy(false);
            scrollDown();
        }

        // Only real answers go into the history the bot sees next time
        if (answer && !failed) {
            history.push({ role: "assistant", text: answer });
        } else {
            history.pop();   // drop the unanswered question so the next request still alternates user/bot
        }
        history = history.slice(-MAX_HISTORY);
        saveHistory();
    }

    // ---- Rendering ----------------------------------------------------------------------------

    function addBubble(who, text) {
        const bubble = document.createElement("div");
        bubble.className = "rr-chat-msg " + (who === "user" ? "rr-chat-user" : "rr-chat-bot");
        if (who === "user") bubble.textContent = text;
        else renderAnswer(bubble, text);
        log.appendChild(bubble);
        scrollDown();
        return bubble;
    }

    // The answer is set as text, never as HTML, so nothing the AI writes can run as code.
    // Only the known page paths in LINKS become links.
    function renderAnswer(bubble, text) {
        const keep = bubble.querySelector(".rr-chat-status");
        bubble.textContent = "";

        const paths = Object.keys(LINKS).sort((a, b) => b.length - a.length);   // longest first: /Reservations/Create before /Reservations
        const pattern = new RegExp("(" + paths.map((p) => p.replace(/\//g, "\\/")).join("|") + ")(?![\\w/])", "g");

        let last = 0;
        for (const match of text.matchAll(pattern)) {
            bubble.appendChild(document.createTextNode(text.slice(last, match.index)));
            const link = document.createElement("a");
            link.href = match[0];
            link.textContent = LINKS[match[0]];
            bubble.appendChild(link);
            last = match.index + match[0].length;
        }
        bubble.appendChild(document.createTextNode(text.slice(last)));

        if (keep) bubble.appendChild(keep);
    }

    function showStatus(bubble, text) {
        const line = document.createElement("span");
        line.className = "rr-chat-status";
        line.innerHTML = '<span class="rr-chat-dots" aria-hidden="true"><i></i><i></i><i></i></span><span></span>';
        line.lastChild.textContent = text;
        bubble.appendChild(line);

        return {
            update(newText) {
                line.lastChild.textContent = newText;
                if (!line.isConnected) bubble.appendChild(line);
            },
            hide() {
                line.remove();
            }
        };
    }

    function setBusy(busy) {
        sendButton.hidden = busy;
        stopButton.hidden = !busy;
        input.disabled = busy;
        if (!busy) input.focus();
    }

    function scrollDown() {
        log.scrollTop = log.scrollHeight;
    }

    // ---- History in this tab only -------------------------------------------------------------

    function loadHistory() {
        try {
            const saved = JSON.parse(sessionStorage.getItem(HISTORY_KEY) || "[]");
            return Array.isArray(saved) ? saved : [];
        } catch {
            return [];
        }
    }

    function saveHistory() {
        try {
            sessionStorage.setItem(HISTORY_KEY, JSON.stringify(history));
        } catch {
            // Private mode or storage blocked: the chat still works, it just forgets on reload
        }
    }

    // Show the saved conversation when the customer moves to another page
    if (history.length > 0) {
        suggestions.hidden = true;
        for (const message of history) addBubble(message.role === "user" ? "user" : "bot", message.text);
    }
})();
