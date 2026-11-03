/* Chat cục bộ: DOM textContent cho toàn bộ nội dung AI/người dùng, không render HTML từ phản hồi. */
(() => {
    'use strict';
    const root = document.getElementById('local-chat');
    if (!root) return;
    const el = id => root.querySelector('#lc-' + id);
    const token = root.querySelector('input[name="__RequestVerificationToken"]').value;
    let selected = null, state = null, loading = false, submitting = false, lastMessages = '', initialized = false;
    let requestKey = crypto.randomUUID().replaceAll('-', '');
    const drafts = new Map();
    const error = text => { el('error').textContent = text || ''; el('error').hidden = !text; };
    const notice = text => { el('notice').textContent = text || ''; el('notice').hidden = !text; };
    const readableTime = at => new Date(at).toLocaleString('vi-VN');
    async function api(handler, data, get = false) {
        const query = new URLSearchParams({ handler });
        if (get && data) Object.entries(data).forEach(([key, value]) => query.set(key, value));
        const response = await fetch('/Chat?' + query, {
            method: get ? 'GET' : 'POST', credentials: 'same-origin', cache: 'no-store',
            headers: get ? {} : { RequestVerificationToken: token },
            body: get ? undefined : new URLSearchParams(data || {})
        });
        let result;
        try { result = await response.json(); } catch { throw new Error('Hub chưa trả dữ liệu hợp lệ. Tải lại trang nếu phiên bảo mật đã hết hạn.'); }
        if (!response.ok) throw new Error(result.error || 'Không thực hiện được yêu cầu.');
        return result;
    }
    function setBusy() {
        const busy = state?.conversations.find(c => c.id === selected)?.busy || false;
        el('send').disabled = !state || busy || submitting;
        el('text').disabled = submitting;
        el('stop').hidden = !busy;
        el('busy').hidden = !busy;
        el('delete').disabled = !selected || busy;
        el('export').hidden = !selected;
        if (selected) el('export').href = '/Chat?handler=Export&conversationId=' + encodeURIComponent(selected);
    }
    function renderMessages(messages) {
        const fingerprint = JSON.stringify([selected, messages]);
        if (fingerprint === lastMessages) return;
        lastMessages = fingerprint;
        const list = el('messages');
        const nearBottom = list.scrollHeight - list.scrollTop - list.clientHeight < 100;
        list.replaceChildren();
        if (!messages.length) {
            const empty = document.createElement('div'); empty.className = 'lc-empty';
            const title = document.createElement('h3'); title.textContent = 'Anh muốn trao đổi việc gì?';
            const text = document.createElement('p'); text.textContent = 'Nội dung trò chuyện được lưu trên máy. AI nhận ngữ cảnh gần nhất mỗi lần anh gửi.';
            empty.append(title, text); list.append(empty);
        }
        for (const message of messages) {
            const article = document.createElement('article'); article.className = 'lc-message ' + (message.role === 'user' ? 'user' : 'assistant');
            const label = document.createElement('div'); label.className = 'lc-message-label'; label.textContent = message.role === 'user' ? 'Anh' : (state.demo ? 'AI minh họa' : 'ChatGPT · Codex');
            const content = document.createElement('div'); content.className = 'lc-message-text';
            content.textContent = message.text || (message.status === 'queued' ? 'Đang chờ lượt…' : message.status === 'generating' ? 'Đang suy nghĩ…' : '');
            article.append(label, content);
            if (message.error) { const problem = document.createElement('p'); problem.className = 'lc-message-error'; problem.textContent = message.error; article.append(problem); }
            const meta = document.createElement('div'); meta.className = 'lc-message-meta';
            meta.textContent = readableTime(message.at) + (message.role === 'assistant' ? ' · ' + (message.model || 'Model mặc định CLI') + ' · ' + (message.tokens == null ? 'Token: chưa đo được' : message.tokens.toLocaleString('vi-VN') + ' token') : '');
            article.append(meta);
            if (message.text) {
                const copy = document.createElement('button'); copy.type = 'button'; copy.className = 'lc-copy'; copy.textContent = 'Sao chép';
                copy.onclick = async () => { try { await navigator.clipboard.writeText(message.text); notice('Đã sao chép tin nhắn.'); } catch { error('Trình duyệt chưa cho sao chép. Anh có thể chọn và copy nội dung trực tiếp.'); } };
                article.append(copy);
            }
            list.append(article);
        }
        if (nearBottom || !initialized) list.scrollTop = list.scrollHeight;
    }
    function renderHistory() {
        const list = el('conversations'); list.replaceChildren();
        if (!state.conversations.length) { const empty = document.createElement('p'); empty.className = 'muted'; empty.textContent = 'Chưa có hội thoại.'; list.append(empty); }
        for (const c of state.conversations) {
            const button = document.createElement('button'); button.type = 'button'; button.className = 'lc-history-item'; button.setAttribute('aria-current', String(c.id === selected));
            const title = document.createElement('span'); title.textContent = c.title;
            const meta = document.createElement('small'); meta.textContent = (c.busy ? '● Đang trả lời · ' : '') + readableTime(c.updatedAt);
            button.append(title, meta); button.onclick = () => switchConversation(c.id); list.append(button);
        }
    }
    async function refresh() {
        if (loading) return;
        loading = true;
        try {
            let next = await api('State', selected ? { conversationId: selected } : {}, true);
            if (!initialized && !selected && next.conversations.length) {
                selected = next.conversations[0].id;
                next = await api('State', { conversationId: selected }, true);
            }
            state = next;
            el('mode').textContent = state.demo ? 'Minh họa · không gọi AI thật' : 'ChatGPT · lịch sử lưu tại máy';
            el('location').textContent = 'Database: ' + state.location;
            el('context-note').textContent = state.contextNote + ' Hiện ' + state.messages.length + ' tin nhắn gần nhất (xuất JSON để xem toàn bộ).';
            el('title').textContent = state.conversations.find(c => c.id === selected)?.title || 'Bắt đầu trò chuyện';
            el('token-summary').textContent = 'Đã dùng ' + state.tokenSummary.totalTokens.toLocaleString('vi-VN') + ' token'
                + (state.tokenSummary.unmeasuredTurns ? ' · ' + state.tokenSummary.unmeasuredTurns + ' lượt chưa đo được' : '');
            if (!initialized) {
                for (const [key, value] of Object.entries(state.settings)) {
                    const input = el('settings').elements.namedItem(key); if (input) input.value = value;
                }
            }
            renderHistory(); renderMessages(state.messages); setBusy(); initialized = true;
        } catch (ex) { error(ex.message); }
        finally { loading = false; }
    }
    async function switchConversation(id) {
        if (loading || submitting) return;
        if (selected) drafts.set(selected, el('text').value);
        selected = id; lastMessages = ''; el('text').value = drafts.get(id) || '';
        requestKey = crypto.randomUUID().replaceAll('-', '');
        error(''); notice(''); await refresh();
        el('messages').scrollTop = el('messages').scrollHeight;
    }
    el('new').onclick = async () => {
        if (loading || submitting) return;
        try { error(''); const created = await api('Create'); await switchConversation(created.id); el('text').focus(); }
        catch (ex) { error(ex.message); }
    };
    el('compose').onsubmit = async event => {
        event.preventDefault(); if (submitting || !state) return;
        const text = el('text').value.trim(); if (!text) return;
        submitting = true; setBusy(); error(''); notice('');
        try {
            if (!selected) { const created = await api('Create'); selected = created.id; }
            await api('Send', { conversationId: selected, text, requestKey });
            requestKey = crypto.randomUUID().replaceAll('-', '');
            el('text').value = ''; drafts.delete(selected);
            await refresh(); el('messages').scrollTop = el('messages').scrollHeight;
        } catch (ex) { error(ex.message); }
        finally { submitting = false; setBusy(); }
    };
    el('text').onkeydown = event => { if ((event.ctrlKey || event.metaKey) && event.key === 'Enter') { event.preventDefault(); if (!el('send').disabled) el('compose').requestSubmit(); } };
    el('text').oninput = () => { if (!submitting) requestKey = crypto.randomUUID().replaceAll('-', ''); };
    el('stop').onclick = async () => { try { await api('Cancel', { conversationId: selected }); await refresh(); } catch (ex) { error(ex.message); } };
    el('delete').onclick = async () => {
        if (!selected || !confirm('Xóa hội thoại này và toàn bộ tin nhắn khỏi Hub? Thao tác không thể hoàn tác. Anh có thể tải JSON trước khi xóa.')) return;
        try { await api('Delete', { conversationId: selected }); drafts.delete(selected); selected = null; el('text').value = ''; await refresh(); }
        catch (ex) { error(ex.message); }
    };
    el('settings-toggle').onclick = () => { const form = el('settings'); form.hidden = !form.hidden; el('settings-toggle').setAttribute('aria-expanded', String(!form.hidden)); };
    if (location.hash === '#settings') { el('settings').hidden = false; el('settings-toggle').setAttribute('aria-expanded', 'true'); }
    el('settings').onsubmit = async event => {
        event.preventDefault(); error('');
        try { await api('Settings', Object.fromEntries(new FormData(el('settings')))); notice(state.demo ? 'Đã lưu trong bộ nhớ minh họa. Không thay đổi cấu hình máy thật.' : 'Đã lưu cấu hình trong SQLite trên máy. Các lượt tiếp theo dùng cấu hình mới.'); await refresh(); }
        catch (ex) { error(ex.message); }
    };
    el('check').onclick = async () => {
        el('check').disabled = true; el('connection').textContent = 'Đang kiểm tra cấu hình đã lưu…';
        try { const result = await api('Check'); el('connection').textContent = result.detail; }
        catch (ex) { el('connection').textContent = 'Chưa kiểm tra được'; error(ex.message); }
        finally { el('check').disabled = false; }
    };
    refresh();
    const poll = setInterval(() => { if (!document.hidden && !loading && !submitting) refresh(); }, 2000);
    window.addEventListener('pagehide', () => clearInterval(poll), { once: true });
})();
