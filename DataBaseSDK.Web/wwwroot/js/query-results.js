// Resultado propio por pestaña, como las ventanas de consulta de SSMS: cada pestaña del editor conserva el último
// resultado (o error) que ejecutó, y al cambiar de pestaña se ve el suyo.
//
// Cómo: ejecutar es un POST que recarga la página; el servidor marca el bloque de resultados con la pestaña que lo pidió
// (data-tab). Aquí se guarda una copia en sessionStorage (qc-results-<kind>) y, mientras la página vive, el bloque
// original en memoria (con la edición de celdas funcionando). Al volver a una pestaña después de recargar se muestra la
// copia: de solo lectura (la edición de la grilla necesita el resultado recién consultado; se vuelve a ejecutar).
//
// sessionStorage y no localStorage a propósito: son datos reales de las bases; se borran al cerrar la ventana y nunca
// van al repo. Si un resultado no entra (es enorme), se avisa en su lugar que hay que volver a ejecutarlo.
(function () {
    var QC = window.QC;
    var root = QC && QC.root;
    if (!root || !QC.editor || !QC.workspace) return;

    var area = root.querySelector('[data-role="results-area"]');
    if (!area) return;

    var W = QC.workspace;
    var storeKey = 'qc-results-' + QC.kind;
    var MAX_HTML = 1500000;   // ~3 MB en UTF-16: más que eso no se guarda (avisa que hay que volver a ejecutar)

    var live = {};            // tabId → nodo vivo de esta carga de página (conserva la edición de celdas)
    var shownId = null;

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

    function render(tabId) {
        if (live[tabId]) return live[tabId];

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

    function show(tabId) {
        if (tabId === shownId) return;
        shownId = tabId;

        // El nodo que se oculta se guarda tal cual si es el vivo (no pierde las celdas editadas a medias).
        while (area.firstChild) area.removeChild(area.firstChild);
        var node = tabId ? render(tabId) : null;
        if (node) area.appendChild(node);
    }

    // ---------- al cargar: el bloque que trajo el servidor es de la pestaña que ejecutó ----------

    var hasContent = area.children.length > 0;
    // Sin pestaña marcada (p. ej. un formulario de una versión anterior de la página): es de la activa.
    var ranTab = area.dataset.tab || (hasContent ? QC.editor.tabsInfo().activeId : '');

    if (ranTab && hasContent) {
        var node = holder();
        while (area.firstChild) node.appendChild(area.firstChild);
        live[ranTab] = node;

        var html = readOnlyCopy(node);
        var entry = {
            at: Date.now(),
            alias: area.dataset.alias || null, database: area.dataset.database || null, schema: area.dataset.schema || null,
            editable: !!node.querySelector('[data-edit]')
        };
        if (html.length > MAX_HTML) entry.tooBig = true; else entry.html = html;

        writeCache(W.putResult(readCache(), ranTab, entry), ranTab);
        area.appendChild(node);
        shownId = ranTab;
    } else {
        while (area.firstChild) area.removeChild(area.firstChild);
    }

    // ---------- seguir a las pestañas ----------

    var knownIds = null;

    QC.editor.onTabsChanged(function (info) {
        show(info.activeId);

        // Pestaña cerrada → se olvida su resultado (renderTabs corre en cada tecla: solo se escribe si cambió la lista).
        var ids = (info.ids || []).join('|');
        if (ids === knownIds) return;
        knownIds = ids;

        var cache = readCache();
        var cleaned = W.removeResults(cache, info.ids);
        if (cleaned.order.length !== cache.order.length) writeCache(cleaned, null);
        Object.keys(live).forEach(function (id) { if ((info.ids || []).indexOf(id) < 0) delete live[id]; });
    });
})();
