// R-014 UI-09 (Claude): khung chat nổi + trang /Assistant, nối thật với IChatAssistant.
// Chữ từ AI luôn gắn bằng textContent (không bao giờ innerHTML) ⇒ không chèn được HTML / script.
(function () {
    'use strict';
    var KEY_MODE = 'hub-chat-mode', KEY_CONV = 'hub-chat-conv';
    function get(k) { try { return localStorage.getItem(k); } catch (e) { return null; } }
    function set(k, v) { try { if (v == null) localStorage.removeItem(k); else localStorage.setItem(k, v); } catch (e) { } }

    function setup(panel) {
        var form = panel.querySelector('[data-chat-form]');
        var text = form.querySelector('textarea');
        var log = panel.querySelector('.chatp-log');
        var hint = panel.querySelector('[data-hint]');
        var sendBtn = panel.querySelector('[data-chat-send]');
        var stopBtn = panel.querySelector('[data-chat-stop]');
        var count = panel.querySelector('[data-chat-count]');
        var tabs = panel.querySelectorAll('[data-m]');
        var keep = true;
        var mode = get(KEY_MODE) || 'ask';
        var busy = null;

        function selectMode(m) {
            mode = m;
            tabs.forEach(function (t) { t.setAttribute('aria-selected', t.dataset.m === m ? 'true' : 'false'); });
            panel.querySelectorAll('[data-for]').forEach(function (el) { el.hidden = el.dataset.for !== m; });
            text.placeholder = m === 'task' ? 'Mô tả việc cần làm… (Claude nhận và tạo phiếu)' : 'Nhập câu hỏi… (Enter gửi, Shift+Enter xuống dòng)';
            set(KEY_MODE, m);
        }

        function bubble(role, body, extraClass) {
            if (hint) hint.hidden = true;
            var li = document.createElement('li');
            li.className = 'chatp-msg ' + role + (extraClass ? ' ' + extraClass : '');
            var who = document.createElement('span');
            who.className = 'chatp-who';
            who.textContent = role === 'user' ? 'Anh' : role === 'err' ? 'Lỗi' : 'Trợ lý';
            var p = document.createElement('div');
            p.className = 'chatp-text';
            p.textContent = body;
            li.appendChild(who); li.appendChild(p);
            log.appendChild(li);
            log.scrollTop = log.scrollHeight;
            return li;
        }

        function history() {
            var c = get(KEY_CONV);
            if (!keep || !c) return;
            fetch('/Assistant?handler=History&c=' + encodeURIComponent(c), { headers: { 'Accept': 'application/json' } })
                .then(function (r) { return r.ok ? r.json() : []; })
                .then(function (items) {
                    items.forEach(function (m) {
                        if (m.error) bubble('err', m.error);
                        else bubble(m.role === 'user' ? 'user' : 'ai', m.text);
                    });
                })
                .catch(function () { });
        }

        function send(body) {
            if (busy || !body.trim()) return;
            bubble('user', body);
            var pending = bubble('ai', 'Đang trả lời… 0 giây', 'pending');
            var started = Date.now();
            var timer = setInterval(function () {
                pending.querySelector('.chatp-text').textContent = 'Đang trả lời… ' + Math.round((Date.now() - started) / 1000) + ' giây';
            }, 1000);
            var ctrl = new AbortController();
            busy = ctrl;
            sendBtn.disabled = true; stopBtn.hidden = false;

            var data = new FormData();
            data.append('text', body);
            data.append('mode', mode);
            var ai = panel.querySelector('select[name=ai]'), project = panel.querySelector('select[name=project]');
            if (mode === 'ask' && ai) data.append('ai', ai.value);
            if (mode === 'task' && project) data.append('project', project.value);
            var conv = get(KEY_CONV);
            if (keep && conv && mode === 'ask') data.append('conversationId', conv);
            data.append('__RequestVerificationToken', panel.dataset.csrf || '');

            function done() { clearInterval(timer); busy = null; sendBtn.disabled = false; stopBtn.hidden = true; pending.remove(); }

            fetch('/Assistant?handler=Send', { method: 'POST', body: data, signal: ctrl.signal, headers: { 'Accept': 'application/json' } })
                .then(function (r) {
                    return r.json().catch(function () { return { ok: false, error: 'Máy chủ trả lỗi ' + r.status + '.' }; });
                })
                .then(function (res) {
                    done();
                    if (res.resetConversation) set(KEY_CONV, null);
                    if (res.conversationId && keep) set(KEY_CONV, res.conversationId);
                    if (res.ok) {
                        bubble('ai', res.text || '(trả lời rỗng)');
                    } else {
                        var li = bubble('err', res.error || 'Không gửi được.');
                        var retry = document.createElement('button');
                        retry.type = 'button'; retry.className = 'btn sm ghost'; retry.textContent = 'Thử lại';
                        retry.addEventListener('click', function () { li.remove(); send(body); });
                        li.appendChild(retry);
                    }
                })
                .catch(function (e) {
                    done();
                    bubble('err', e && e.name === 'AbortError' ? 'Đã dừng.' : 'Mất kết nối tới Hub.');
                });
        }

        tabs.forEach(function (t) { t.addEventListener('click', function () { selectMode(t.dataset.m); text.focus(); }); });
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            var body = text.value;
            if (!body.trim()) { text.focus(); return; }
            text.value = ''; count.textContent = '0/4000';
            send(body);
        });
        text.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey && !e.isComposing) { e.preventDefault(); form.requestSubmit(); }
        });
        text.addEventListener('input', function () { count.textContent = text.value.length + '/4000'; });
        stopBtn.addEventListener('click', function () { if (busy) busy.abort(); });
        panel.querySelector('[data-chat-new]').addEventListener('click', function () {
            set(KEY_CONV, null);
            log.querySelectorAll('.chatp-msg').forEach(function (m) { m.remove(); });
            if (hint) hint.hidden = false;
            text.focus();
        });

        function fill(select, items, selected) {
            select.textContent = '';
            if (items.length === 0) {
                var none = document.createElement('option');
                none.value = ''; none.textContent = select.name === 'ai' ? '(chưa có AI chat — thêm ở Tài khoản AI)' : '(chưa có dự án)';
                select.appendChild(none);
            }
            items.forEach(function (it) {
                var o = document.createElement('option');
                o.value = it.id; o.textContent = it.name + (it.available === false ? ' — ' + (it.reason || 'không dùng được') : '');
                o.disabled = it.available === false;
                o.selected = it.id === selected;
                select.appendChild(o);
            });
        }

        // Cài đặt + danh sách AI / dự án tải 1 lần (không làm chậm mọi trang).
        fetch('/Assistant?handler=Options', { headers: { 'Accept': 'application/json' } })
            .then(function (r) { return r.json(); })
            .then(function (o) {
                keep = o.keepHistory !== false;
                fill(panel.querySelector('select[name=ai]'), o.ais, o.defaultAi);
                fill(panel.querySelector('select[name=project]'), o.projects.map(function (p) { return { id: p, name: p }; }), 'AITeam');
                var r = panel.querySelector('[data-research]');
                if (r) r.textContent = 'Hỏi cùng lúc: ' + (o.researchAis.length ? o.researchAis.join(', ') : 'chưa chọn');
                if (!get(KEY_MODE) && o.defaultMode) selectMode(o.defaultMode);
                if (o.error) bubble('err', o.error);
                history();
            })
            .catch(function () { bubble('err', 'Không tải được cài đặt trợ lý.'); });

        selectMode(mode);
        return { selectMode: selectMode, focus: function () { text.focus(); } };
    }

    document.addEventListener('DOMContentLoaded', function () {
        var full = document.getElementById('chatfull');
        if (full) setup(full).focus();

        var panel = document.getElementById('chatp'), fab = document.getElementById('fab');
        if (!panel || !fab) return;
        var api = null, loading = null;
        function load() {
            if (!panel.dataset.lazy) return Promise.resolve();
            if (!loading) {
                loading = fetch('/Assistant?handler=Panel', { headers: { 'Accept': 'text/html' } })
                    .then(function (r) { if (!r.ok) throw new Error(String(r.status)); return r.text(); })
                    .then(function (html) {
                        var box = document.createElement('div');
                        box.innerHTML = html;   // HTML do chính Hub dựng (Razor đã mã hóa), không chứa chữ từ AI
                        var fresh = box.querySelector('#chatp');
                        panel.replaceWith(fresh);
                        panel = fresh;
                        panel.querySelector('[data-chat-close]').addEventListener('click', close);
                    })
                    .catch(function () { loading = null; panel.textContent = 'Không mở được trợ lý. Thử lại.'; });
            }
            return loading;
        }
        function open(m) {
            panel.classList.add('on'); fab.setAttribute('aria-expanded', 'true');
            load().then(function () {
                panel.classList.add('on');
                if (!panel.querySelector('[data-chat-form]')) return;
                if (!api) api = setup(panel);
                if (m) api.selectMode(m);
                api.focus();
            });
        }
        function close() { panel.classList.remove('on'); fab.setAttribute('aria-expanded', 'false'); fab.focus(); }
        fab.addEventListener('click', function () { panel.classList.contains('on') ? close() : open(); });
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape' && panel.classList.contains('on')) close(); });
        document.querySelectorAll('[data-chat]').forEach(function (b) {
            b.addEventListener('click', function (e) { e.preventDefault(); open(b.dataset.chat); });
        });
    });
})();
