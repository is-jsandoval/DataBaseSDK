// Ejecutar y resultado propio por pestaña, como las ventanas de consulta de SSMS.
//
// Ejecutar (botón ▶, Ctrl+Enter, Ctrl+E) NO recarga la página: se intercepta el submit del formulario y se manda con fetch
// al handler RunPartial, que devuelve solo el bloque de resultados (_QueryResults.cshtml). Así el árbol, las pestañas y los
// scripts no se vuelven a crear. Cada pestaña puede tener su consulta en curso (con "Cancelar": abortar el fetch corta la
// consulta en el servidor) y conserva el último resultado (o error) que ejecutó; al cambiar de pestaña se ve el suyo.
// El CSV sigue siendo un submit normal (descarga un archivo; la página no cambia).
//
// Cada resultado vive en memoria mientras la página está abierta (con la edición de celdas funcionando, ver
// QC.editGrid en query-edit.js) y además se guarda una copia de solo lectura en sessionStorage (qc-results-<kind>) para
// cuando se recarga la página o se vuelve de la otra consola. sessionStorage y no localStorage a propósito: son datos
// reales de las bases; se borran al cerrar la ventana y nunca van al repo.
(function () {
    var QC = window.QC;
    var root = QC && QC.root;
    if (!root || !QC.editor || !QC.workspace) return;

    var area = root.querySelector('[data-role="results-area"]');
    var form = root.querySelector('[data-role="form"]');
    var runButton = root.querySelector('[data-role="run"]');
    var tabIdInput = root.querySelector('[data-role="tab-id"]');
    if (!area || !form) return;

    var W = QC.workspace;
    var storeKey = 'qc-results-' + QC.kind;
    var MAX_HTML = 1500000;   // ~3 MB en UTF-16: más que eso no se guarda (avisa que hay que volver a ejecutar)

    var live = {};            // tabId → nodo de esta carga de página (el resultado vivo, o "ejecutando…")
    var running = {};         // tabId → AbortController de la consulta en curso
    var shownId = null;

    // ---------- copia en sessionStorage ----------

    function readCache() {
        try { return W.normalizeCache(JSON.parse(sessionStorage.getItem(storeKey) || 'null')); } catch (e) { return W.normalizeCache(null); }
    }

    function writeCache(cache, keepId) {
        while (cache) {
            try { sessionStorage.setItem(storeKey, JSON.stringify(cache)); return true; } catch (e) { cache = W.dropOldest(cache, keepId); }
        }
        return false;
    }

    // Copia para guardar: sin lo que solo sirve con el resultado vivo (edición de celdas).
    function readOnlyCopy(node) {
        var copy = node.cloneNode(true);
        Array.prototype.forEach.call(copy.querySelectorAll('[data-edit]'), function (n) { n.removeAttribute('data-edit'); });
        Array.prototype.forEach.call(copy.querySelectorAll('[data-role="edit-help"], [data-role="edit-bar"]'), function (n) { n.remove(); });
        return copy.innerHTML;
    }

    function remember(tabId, node, ctx) {
        var html = readOnlyCopy(node);
        var entry = {
            at: Date.now(),
            alias: ctx.alias || null, database: ctx.database || null, schema: ctx.schema || null,
            editable: !!node.querySelector('[data-edit]')
        };
        if (html.length > MAX_HTML) entry.tooBig = true; else entry.html = html;

        writeCache(W.putResult(readCache(), tabId, entry), tabId);
    }

    // ---------- dibujo ----------

    function holder() {
        var div = document.createElement('div');
        div.className = 'qc-tab-results';
        return div;
    }

    function caption(entry) {
        var when = new Date(entry.at);
        var time = isNaN(when) ? '' : when.toLocaleTimeString();
        var where = [entry.alias, entry.database || entry.schema].filter(Boolean).join(' · ');
        return QC.el('div', 'small text-muted mt-2', '🕘 Resultado conservado de las ' + time + (where ? ' (' + where + ')' : '') +
            '. Vuelve a ejecutar para actualizarlo' + (entry.editable ? ' o editar celdas.' : '.'));
    }

    function fromCache(tabId) {
        var entry = readCache().items[tabId];
        if (!entry) return null;

        var node = holder();
        node.appendChild(caption(entry));

        if (entry.tooBig) {
            node.appendChild(QC.el('div', 'alert alert-secondary mt-2 small', 'El resultado era demasiado grande para conservarlo al recargar la página.'));
        } else {
            var body = holder();
            body.innerHTML = entry.html;   // HTML que generó el propio servidor (Razor ya escapó cada celda)
            while (body.firstChild) node.appendChild(body.firstChild);
        }

        return node;
    }

    function show(tabId, force) {
        if (tabId === shownId && !force) return;
        shownId = tabId;

        // El nodo que se oculta se conserva tal cual en `live` (no pierde las celdas editadas a medias).
        while (area.firstChild) area.removeChild(area.firstChild);
        var node = tabId ? (live[tabId] || fromCache(tabId)) : null;
        if (node) area.appendChild(node);
    }

    function syncRunButton() {
        if (!runButton) return;
        var busy = !!running[shownId];
        runButton.disabled = busy;
        runButton.textContent = busy ? '⏳ Ejecutando…' : '▶ Ejecutar';
    }

    function setLive(tabId, node) {
        if (live[tabId] && live[tabId] !== node && QC.editGrid) QC.editGrid.release(live[tabId]);
        live[tabId] = node;
        if (tabId === shownId) show(tabId, true);
    }

    // ---------- ejecutar sin recargar ----------

    function progress(controller) {
        var node = holder();
        var box = QC.el('div', 'd-flex align-items-center gap-2 mt-2 small text-muted');
        box.appendChild(QC.el('span', 'spinner-border spinner-border-sm'));
        box.appendChild(QC.el('span', '', 'Ejecutando…'));

        var cancel = QC.el('button', 'btn btn-outline-secondary btn-sm py-0', 'Cancelar');
        cancel.type = 'button';
        cancel.addEventListener('click', function () { controller.abort(); });
        box.appendChild(cancel);

        node.appendChild(box);
        return node;
    }

    function message(type, text) {
        var node = holder();
        var alert = QC.el('div', 'alert alert-' + type + ' mt-2 mb-0', text);
        alert.style.whiteSpace = 'pre-wrap';
        node.appendChild(alert);
        return node;
    }

    function run() {
        var tabId = (tabIdInput && tabIdInput.value) || QC.editor.tabsInfo().activeId;

        if (running[tabId]) { QC.toast('Esta pestaña ya está ejecutando una consulta.', 'secondary'); return; }

        var previous = live[tabId];
        if (previous && QC.editGrid && QC.editGrid.pendingIn(previous) &&
            !confirm('La grilla de esta pestaña tiene cambios sin guardar. ¿Ejecutar igual y descartarlos?')) return;

        // FormData lleva todo lo del formulario: Sql y TabId (los puso el editor), conexión, base/esquema, filas y el token antiforgery.
        var data = new FormData(form);
        var ctx = { alias: data.get('Alias'), database: data.get('Database'), schema: data.get('Schema') };
        var controller = new AbortController();

        running[tabId] = controller;
        setLive(tabId, progress(controller));
        syncRunButton();

        fetch(root.dataset.baseUrl + '?handler=RunPartial', { method: 'POST', body: data, signal: controller.signal })
            .then(function (response) {
                if (!response.ok) throw new Error('El servidor respondió ' + response.status + '.');
                return response.text();
            })
            .then(function (html) {
                var node = holder();
                node.innerHTML = html;   // HTML de _QueryResults.cshtml: Razor ya escapó cada celda
                setLive(tabId, node);
                remember(tabId, node, ctx);
                if (QC.editGrid) QC.editGrid.attach(node.querySelector('[data-role="results-grid"][data-edit]'));
            })
            .catch(function (error) {
                setLive(tabId, controller.signal.aborted
                    ? message('secondary', 'Consulta cancelada.')
                    : message('danger', 'No se pudo ejecutar: ' + error.message + ' ¿Sigue corriendo el sitio?'));
            })
            .then(function () {
                delete running[tabId];
                syncRunButton();
            });
    }

    // El editor ya puso Sql y TabId en su listener de captura; el historial (query-console.js) también escucha este submit.
    form.addEventListener('submit', function (event) {
        var target = event.submitter && event.submitter.getAttribute('formaction');
        if (target && /handler=Csv/i.test(target)) return;   // descarga normal

        event.preventDefault();
        run();
    });

    // ---------- al cargar: el bloque que trajo el servidor (si lo hay) es de la pestaña que ejecutó ----------

    var hasContent = area.children.length > 0;
    // Sin pestaña marcada (p. ej. un formulario de una versión anterior de la página): es de la activa.
    var ranTab = area.dataset.tab || (hasContent ? QC.editor.tabsInfo().activeId : '');

    if (ranTab && hasContent) {
        var first = holder();
        while (area.firstChild) first.appendChild(area.firstChild);
        live[ranTab] = first;
        remember(ranTab, first, { alias: area.dataset.alias, database: area.dataset.database, schema: area.dataset.schema });
        area.appendChild(first);
        shownId = ranTab;
    } else {
        while (area.firstChild) area.removeChild(area.firstChild);
    }

    // ---------- seguir a las pestañas ----------

    var knownIds = null;

    QC.editor.onTabsChanged(function (info) {
        show(info.activeId);
        syncRunButton();

        // Pestaña cerrada → se olvida su resultado y se corta su consulta (renderTabs corre en cada tecla: solo si cambió la lista).
        var ids = (info.ids || []).join('|');
        if (ids === knownIds) return;
        knownIds = ids;

        var cache = readCache();
        var cleaned = W.removeResults(cache, info.ids);
        if (cleaned.order.length !== cache.order.length) writeCache(cleaned, null);

        Object.keys(live).forEach(function (id) {
            if ((info.ids || []).indexOf(id) >= 0) return;
            if (running[id]) running[id].abort();
            if (QC.editGrid) QC.editGrid.release(live[id]);
            delete live[id];
        });
    });
})();
