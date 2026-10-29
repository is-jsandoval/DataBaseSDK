// Sesión de trabajo versionada (db-workspace.json en la raíz del repo): qué archivos .sql estaban abiertos en cada
// consola, con qué conexión (base / esquema) consulta cada uno —como las ventanas de SSMS— y cuál era el activo. La caché
// del navegador (qc-session-<kind>) sigue siendo la que guarda el texto sin guardar; este archivo solo lleva rutas y
// nombres, para que al hacer "git pull" en otra computadora aparezcan las mismas pestañas.
//
// Lógica pura (sin DOM) para poder probarla con node --test: qué abrir y qué cerrar cuando el archivo del repo cambió
// por fuera, la "firma" que evita mandar al servidor lo mismo en cada tecla, y la caché de resultados por pestaña.
(function () {
    function key(path) { return String(path || '').toLowerCase(); }

    function find(list, path) {
        var k = key(path);
        return (list || []).filter(function (f) { return key(f && f.path) === k; })[0];
    }

    function context(t) {
        return { alias: t.alias || null, database: t.database || null, schema: t.schema || null };
    }

    // Lo que hay que mandar al servidor: los archivos de las pestañas (los que tienen ruta real y siguen en disco), cada
    // uno con su conexión, y el activo.
    function snapshot(tabs, activePath) {
        var files = [];

        (tabs || []).forEach(function (t) {
            if (t.path && !t.missing && !find(files, t.path)) files.push(Object.assign({ path: t.path }, context(t)));
        });

        return { files: files, activeFile: activePath && find(files, activePath) ? activePath : null };
    }

    function signature(snap) {
        return JSON.stringify({
            files: (snap.files || []).map(function (f) { return [key(f.path), f.alias || '', f.database || '', f.schema || '']; }),
            activeFile: key(snap.activeFile)
        });
    }

    // El archivo del repo cambió desde la última vez que este navegador lo vio (git pull): las pestañas pasan a ser las del
    // repo. Nunca se cierra trabajo: las pestañas con cambios sin guardar, las consultas sin archivo y las que ya no
    // existen en disco (conservan su copia de la caché) se quedan aunque el repo no las tenga.
    // tabs: [{ path, dirty, missing }], remote: { files[{path, alias, database, schema}], activeFile }
    //   → { open[{path, ...contexto}], close[path], contexts[{path, ...contexto}] (para las que ya estaban), activeFile }
    function reconcile(tabs, remote) {
        var files = (remote && remote.files) || [];
        var open = [], contexts = [];

        files.forEach(function (f) {
            var already = (tabs || []).some(function (t) { return t.path && key(t.path) === key(f.path); });
            (already ? contexts : open).push(f);
        });

        var close = (tabs || []).filter(function (t) {
            return t.path && !t.dirty && !t.missing && !find(files, t.path);
        }).map(function (t) { return t.path; });

        return { open: open, close: close, contexts: contexts, activeFile: remote && remote.activeFile ? remote.activeFile : null };
    }

    // ---------- caché de resultados por pestaña (sessionStorage) ----------
    // cache: { order: [tabId, ... el más reciente al final], items: { tabId: entrada } }. Devuelve una caché nueva.

    var MAX_RESULTS = 12;

    function normalizeCache(cache) {
        var items = cache && typeof cache.items === 'object' && cache.items ? cache.items : {};
        var order = (cache && Array.isArray(cache.order) ? cache.order : []).filter(function (id) { return items[id]; });
        var clean = {};
        order.forEach(function (id) { clean[id] = items[id]; });
        return { order: order, items: clean };
    }

    function putResult(cache, tabId, entry, max) {
        var next = normalizeCache(cache);
        next.order = next.order.filter(function (id) { return id !== tabId; });
        next.order.push(tabId);
        next.items[tabId] = entry;

        while (next.order.length > (max || MAX_RESULTS)) delete next.items[next.order.shift()];
        return next;
    }

    // Saca el más viejo (cuando no entra en el almacenamiento); nunca el que se acaba de guardar.
    function dropOldest(cache, keepId) {
        var next = normalizeCache(cache);
        var victim = next.order.filter(function (id) { return id !== keepId; })[0];
        if (!victim) return null;
        next.order = next.order.filter(function (id) { return id !== victim; });
        delete next.items[victim];
        return next;
    }

    function removeResults(cache, keepIds) {
        var next = normalizeCache(cache);
        var keep = {};
        (keepIds || []).forEach(function (id) { keep[id] = true; });
        next.order = next.order.filter(function (id) { if (keep[id]) return true; delete next.items[id]; return false; });
        return next;
    }

    var api = {
        snapshot: snapshot, signature: signature, reconcile: reconcile,
        putResult: putResult, dropOldest: dropOldest, removeResults: removeResults, normalizeCache: normalizeCache
    };

    if (typeof module !== 'undefined' && module.exports) module.exports = api;
    if (typeof window !== 'undefined' && window.QC) window.QC.workspace = api;
})();
