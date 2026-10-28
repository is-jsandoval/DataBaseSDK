// Utilidades compartidas de las consolas SQL (Oracle / SQL Server): llamadas al servidor, nodos de árbol,
// avisos. Lo usan query-editor.js (pestañas, archivos .sql) y query-console.js (conexiones y explorador).
window.QC = (function () {
    var root = document.querySelector('[data-query-console]');

    function el(tag, className, text) {
        var node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined) node.textContent = text;
        return node;
    }

    function qs(params) {
        return Object.keys(params).filter(function (k) { return params[k] !== undefined && params[k] !== null && params[k] !== ''; })
            .map(function (k) { return encodeURIComponent(k) + '=' + encodeURIComponent(params[k]); }).join('&');
    }

    function readJson(key, fallback) {
        try { return JSON.parse(localStorage.getItem(key)) || fallback; } catch (e) { return fallback; }
    }

    // false si no se pudo guardar (modo privado, caché del navegador llena).
    function writeJson(key, value) {
        try { localStorage.setItem(key, JSON.stringify(value)); return true; } catch (e) { return false; }
    }

    // Todo error del servidor llega como Error con .code ("not_found", "conflict"...) y .status.
    function handle(response) {
        return response.text().then(function (text) {
            var body = null;
            try { body = text ? JSON.parse(text) : null; } catch (e) { /* respuesta no JSON */ }

            if (!response.ok) {
                var error = new Error((body && body.error) || ('Error ' + response.status));
                error.code = body && body.code;
                error.status = response.status;
                throw error;
            }

            return body;
        });
    }

    function network(error) {
        if (error && error.status) throw error;
        var wrapped = new Error('No se pudo contactar con el servidor (¿sigue corriendo el sitio?).');
        wrapped.code = 'network';
        throw wrapped;
    }

    function baseUrl() { return root ? root.dataset.baseUrl : ''; }

    function get(handler, params) {
        params = params || {};
        params.handler = handler;
        return fetch(baseUrl() + '?' + qs(params)).then(handle, network);
    }

    function post(handler, body) {
        var token = document.querySelector('input[name="__RequestVerificationToken"]');
        return fetch(baseUrl() + '?handler=' + handler, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token ? token.value : '' },
            body: JSON.stringify(body)
        }).then(handle, network);
    }

    function muted(text) { return el('div', 'text-muted', text); }

    // ---------- avisos flotantes ----------

    var toastBox = null;

    function toast(text, type) {
        if (!toastBox) {
            toastBox = el('div');
            toastBox.style.cssText = 'position:fixed;right:16px;bottom:16px;z-index:2000;display:flex;flex-direction:column;gap:6px;max-width:420px;';
            document.body.appendChild(toastBox);
        }

        var item = el('div', 'alert alert-' + (type || 'secondary') + ' py-2 px-3 mb-0 small shadow', text);
        item.style.whiteSpace = 'pre-wrap';
        toastBox.appendChild(item);
        setTimeout(function () { item.remove(); }, type === 'danger' || type === 'warning' ? 9000 : 3500);
    }

    // ---------- árbol ----------

    var selectedRow = null;

    function selectRow(row) {
        if (selectedRow) selectedRow.classList.remove('qc-selected');
        selectedRow = row;
        row.classList.add('qc-selected');
    }

    function clearSelection() { selectedRow = null; }

    // opts: icon, label, detail, title, badge, expandable, load() → Promise<nodo[]>, onSelect, onDblClick, actions[{text,title,run}]
    function node(opts) {
        var wrap = el('div', 'qc-node');
        var row = el('div', 'qc-row');
        var caret = el('span', 'qc-caret', opts.expandable ? '▸' : '');
        row.appendChild(caret);

        if (opts.title) row.title = opts.title;
        if (opts.icon) row.appendChild(el('span', '', opts.icon));
        row.appendChild(el('span', 'qc-label', opts.label));
        if (opts.badge) row.appendChild(el('span', 'badge bg-secondary', opts.badge));

        if (opts.detail) {
            var detail = el('span', 'qc-detail', opts.detail);
            detail.title = opts.detail;
            row.appendChild(detail);
        }

        if (opts.actions && opts.actions.length) {
            var actions = el('span', 'qc-actions');

            opts.actions.forEach(function (action) {
                var link = el('a', '', action.text);
                link.href = '#';
                link.title = action.title;
                link.addEventListener('click', function (event) {
                    event.preventDefault();
                    event.stopPropagation();
                    action.run();
                });
                actions.appendChild(link);
            });

            row.appendChild(actions);
        }

        var children = el('div', 'qc-children');
        children.hidden = true;

        var loaded = false, open = false;

        function loadChildren() {
            children.innerHTML = '';
            children.appendChild(muted('Cargando...'));

            opts.load().then(function (nodes) {
                loaded = true;
                children.innerHTML = '';
                if (!nodes.length) children.appendChild(muted('(vacío)'));
                nodes.forEach(function (child) { children.appendChild(child); });
            }).catch(function (error) {
                children.innerHTML = '';
                var message = el('div', 'text-danger', error.message + ' ');
                var retry = el('a', '', 'reintentar');
                retry.href = '#';
                retry.addEventListener('click', function (event) { event.preventDefault(); loadChildren(); });
                message.appendChild(retry);
                children.appendChild(message);
            });
        }

        function toggle() {
            if (!opts.expandable) return;
            open = !open;
            children.hidden = !open;
            caret.textContent = open ? '▾' : '▸';
            if (open && !loaded) loadChildren();
        }

        row.addEventListener('click', function () {
            selectRow(row);
            if (opts.onSelect) opts.onSelect();
            toggle();
        });

        if (opts.onDblClick) {
            row.addEventListener('dblclick', function (event) {
                event.preventDefault();
                opts.onDblClick();
            });
        }

        wrap.appendChild(row);
        wrap.appendChild(children);
        return wrap;
    }

    return {
        root: root, kind: root ? root.dataset.kind : '',
        el: el, qs: qs, readJson: readJson, writeJson: writeJson, get: get, post: post, muted: muted,
        toast: toast, node: node, selectRow: selectRow, clearSelection: clearSelection,
        editor: null   // lo define query-editor.js
    };
})();
