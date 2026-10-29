// Editor de las consolas SQL: pestañas, archivos .sql en disco (abrir / guardar / guardar como), gutter con las
// líneas modificadas, caché local de lo que estaba abierto y panel lateral que se oculta con Ctrl+B.
//
// Estado:
//  - Las pestañas abiertas (rutas, cambios sin guardar, posición) viven en localStorage del navegador: al volver
//    a la página se restauran. Si un archivo cacheado ya no existe en disco se avisa y se conserva el contenido.
//  - Las rutas de las pestañas (sin texto) también van a db-workspace.json, versionado en el repo: si cambió por un
//    git pull, al arrancar se abren/cierran pestañas para igualarlo (ver qc-workspace.js).
//  - Los archivos se leen y guardan en el disco de esta máquina a través del sitio (solo .sql; ver SqlFileService).
(function () {
    var QC = window.QC;
    var root = QC && QC.root;
    if (!root) return;

    var kind = QC.kind;
    var el = QC.el;

    function $(role) { return root.querySelector('[data-role="' + role + '"]'); }

    var form = $('form');
    var textarea = $('sql');
    var hiddenSql = $('sql-submit');
    var canvas = $('gutter');
    var tabsBox = $('tabs');
    var pathLabel = $('file-path');
    var banner = $('file-banner');
    var statusBar = $('statusbar');
    var runButton = $('run');
    var layout = $('layout');
    var splitter = $('splitter');
    var sideToggle = $('toggle-side');

    if (!form || !textarea || !canvas) return;

    var LINE_HEIGHT = 20, PAD_TOP = 6, GUTTER_WIDTH = 58;
    var COLOR_UNSAVED = '#e2a03f', COLOR_SAVED = '#4cc38a', COLOR_NUMBER = '#858585', COLOR_NUMBER_ACTIVE = '#d4d4d4';

    var sessionKey = 'qc-session-' + kind;
    var tabs = [];
    var activeId = null;
    var tabListeners = [];   // quienes quieren enterarse de que cambiaron las pestañas (el explorador de archivos): ver QC.editor.onTabsChanged
    var mru = [];            // ids de pestañas por uso reciente, la activa primero (Ctrl+Tab); la lógica está en qc-mru.js

    // ---------- modelo de pestaña ----------

    function newTab(overrides) {
        return Object.assign({
            id: 't' + Date.now().toString(36) + Math.random().toString(36).slice(2, 6),
            title: '', path: null, text: '', opened: '', saved: '', version: null,
            eol: '\r\n', encoding: 'utf8', missing: false, diskChanged: false, notice: null,
            scroll: 0, selStart: 0, selEnd: 0,
            // Conexión con la que consulta ESTA pestaña (como cada ventana de SSMS); null = todavía no eligió: hereda la actual.
            alias: null, database: null, schema: null
        }, overrides);
    }

    // "opened" es lo que había al abrir (referencia de lo "guardado"); "saved" es lo último que quedó en disco.
    function isDirty(tab) { return tab.text !== tab.saved; }

    // ¿Perder esta pestaña pierde trabajo? Un archivo (o uno arrastrado sin ruta) solo si cambió; una consulta nueva si tiene texto.
    function hasUnsavedWork(tab) {
        return tab.path || tab.fromDrop ? isDirty(tab) : tab.text.trim() !== '';
    }
    function active() { return tabs.filter(function (t) { return t.id === activeId; })[0]; }
    function baseName(path) { return path.split(/[\\/]/).pop(); }
    function dirName(path) { var i = Math.max(path.lastIndexOf('\\'), path.lastIndexOf('/')); return i > 0 ? path.slice(0, i) : ''; }

    function nextScratchTitle() {
        var used = {};
        tabs.forEach(function (t) { if (!t.path) used[t.title] = true; });
        for (var n = 1; ; n++) { if (!used['Consulta ' + n]) return 'Consulta ' + n; }
    }

    function newScratch(text) {
        var tab = newTab({ title: nextScratchTitle(), text: text || '' });
        tabs.push(tab);
        activate(tab.id);
        return tab;
    }

    // ---------- caché local (localStorage) ----------

    function pack(t) {
        return {
            id: t.id, title: t.title, path: t.path, version: t.version, eol: t.eol, encoding: t.encoding, missing: t.missing, fromDrop: t.fromDrop,
            text: t.text,
            saved: t.saved === t.text ? null : t.saved,
            opened: t.opened === t.saved ? null : t.opened,
            scroll: t.scroll, selStart: t.selStart, selEnd: t.selEnd,
            alias: t.alias, database: t.database, schema: t.schema
        };
    }

    function unpack(p) {
        var saved = p.saved == null ? p.text : p.saved;
        var opened = p.opened == null ? saved : p.opened;
        return newTab({
            id: p.id, title: p.title, path: p.path, version: p.version, eol: p.eol || '\r\n', encoding: p.encoding || 'utf8',
            missing: !!p.missing, fromDrop: !!p.fromDrop, text: p.text || '', saved: saved, opened: opened,
            scroll: p.scroll || 0, selStart: p.selStart || 0, selEnd: p.selEnd || 0,
            alias: p.alias || null, database: p.database || null, schema: p.schema || null
        });
    }

    var warnedQuota = false;

    function persist() {
        syncFromView();
        scheduleWorkspace();
        var data = { activeId: activeId, mru: mru, tabs: tabs.map(pack), workspaceStamp: workspaceStamp };

        if (QC.writeJson(sessionKey, data)) return;

        // Caché llena: se descartan las referencias de "guardado" (solo se pierde la marca verde del gutter).
        data.tabs.forEach(function (t) { t.opened = null; });

        if (!QC.writeJson(sessionKey, data) && !warnedQuota) {
            warnedQuota = true;
            QC.toast('No hay espacio en la caché del navegador: los cambios sin guardar de las pestañas no se conservarán si recargas. Guarda los archivos.', 'warning');
        }
    }

    var persistTimer = null;
    function schedulePersist() { clearTimeout(persistTimer); persistTimer = setTimeout(persist, 400); }

    // ---------- sesión versionada (db-workspace.json; lógica en qc-workspace.js) ----------

    var workspaceStamp = null;   // sello del archivo del repo la última vez que este navegador lo vio o lo escribió
    var workspaceReady = false;  // hasta comparar con el repo al arrancar no se manda nada (pisaría lo que trajo un git pull)
    var workspaceSent = null;    // firma de lo último mandado: persist corre en cada tecla, el servidor solo se entera si cambió algo
    var workspaceTimer = null;

    function scheduleWorkspace() {
        if (!workspaceReady) return;
        clearTimeout(workspaceTimer);
        workspaceTimer = setTimeout(sendWorkspace, 600);
    }

    function sendWorkspace() {
        var current = active();
        var snap = QC.workspace.snapshot(tabs, current && current.path);
        var signature = QC.workspace.signature(snap);
        if (signature === workspaceSent) return;

        workspaceSent = signature;
        QC.post('SaveWorkspace', snap).then(function (state) {
            workspaceStamp = state.stamp;
            QC.writeJson(sessionKey, Object.assign(QC.readJson(sessionKey, {}), { workspaceStamp: workspaceStamp }));
        }).catch(function () { workspaceSent = null; /* sin servidor: se reintenta en el próximo cambio */ });
    }

    // Abre un archivo sin activarlo ni avisar si falta: viene del repo y puede ser una ruta de otra computadora.
    function openQuietly(path) {
        if (findTab(path)) return Promise.resolve();

        return QC.get('ReadFile', { path: path }).then(function (file) {
            tabs.push(newTab({
                title: file.name, path: file.path, text: file.text, opened: file.text, saved: file.text,
                version: file.version, eol: file.eol, encoding: file.encoding
            }));
        }).catch(function () { /* no existe en esta máquina: simplemente no se abre */ });
    }

    // Si el archivo del repo cambió desde la última vez (git pull, otra computadora), las pestañas pasan a ser las suyas.
    function adoptWorkspace(remote, lastStamp) {
        var nothingInRepo = !remote.files.length && !remote.activeFile;
        if (remote.stamp === lastStamp || nothingInRepo) return Promise.resolve();

        var plan = QC.workspace.reconcile(tabs.map(function (t) {
            return { path: t.path, dirty: isDirty(t), missing: t.missing };
        }), remote);

        return plan.open.reduce(function (chain, file) {
            return chain.then(function () { return openQuietly(file.path); });
        }, Promise.resolve()).then(function () {
            // Cada archivo toma la conexión con la que se consultaba en la otra computadora.
            plan.open.concat(plan.contexts).forEach(function (file) {
                var tab = findTab(file.path);
                if (tab && file.alias) { tab.alias = file.alias; tab.database = file.database || null; tab.schema = file.schema || null; }
            });

            plan.close.forEach(function (path) { var tab = findTab(path); if (tab) closeTab(tab.id, true); });

            // La "Consulta 1" vacía con la que arranca una máquina nueva sobra si llegaron archivos.
            tabs.filter(function (t) { return !t.path && !t.fromDrop && !t.text.trim(); }).forEach(function (t) {
                if (tabs.length > 1) closeTab(t.id, true);
            });

            var target = plan.activeFile && findTab(plan.activeFile);
            activate(target ? target.id : activeId);
        });
    }

    // ---------- diff por líneas (para el gutter) ----------

    // Índices de las líneas de b que no están en a (agregadas o cambiadas): LCS con los extremos comunes recortados.
    function diffAdded(a, b) {
        var result = new Set();
        var n = a.length, m = b.length, start = 0;

        while (start < n && start < m && a[start] === b[start]) start++;

        var endA = n, endB = m;
        while (endA > start && endB > start && a[endA - 1] === b[endB - 1]) { endA--; endB--; }

        var A = a.slice(start, endA), B = b.slice(start, endB);
        if (!B.length) return result;

        // Sin base o demasiado grande para el LCS completo: todo el tramo se marca como cambiado.
        if (!A.length || A.length * B.length > 4000000) {
            for (var k = start; k < endB; k++) result.add(k);
            return result;
        }

        var width = B.length + 1;
        var table = new Uint16Array((A.length + 1) * width);

        for (var i = 1; i <= A.length; i++) {
            for (var j = 1; j <= B.length; j++) {
                table[i * width + j] = A[i - 1] === B[j - 1]
                    ? table[(i - 1) * width + (j - 1)] + 1
                    : Math.max(table[(i - 1) * width + j], table[i * width + (j - 1)]);
            }
        }

        var matched = new Uint8Array(B.length);
        var x = A.length, y = B.length;

        while (x > 0 && y > 0) {
            if (A[x - 1] === B[y - 1]) { matched[y - 1] = 1; x--; y--; }
            else if (table[(x - 1) * width + y] >= table[x * width + (y - 1)]) x--;
            else y--;
        }

        for (var q = 0; q < B.length; q++) { if (!matched[q]) result.add(start + q); }
        return result;
    }

    var marks = { unsaved: new Set(), saved: new Set() };
    var markTimer = null;

    function computeMarks() {
        var tab = active();
        if (!tab) return;

        var current = textarea.value.split('\n');
        marks.unsaved = diffAdded(tab.saved.split('\n'), current);
        marks.saved = new Set();

        // Verde: cambió respecto de lo que había al abrir, y esa versión ya está guardada en disco.
        if (tab.opened !== tab.saved) {
            diffAdded(tab.opened.split('\n'), current).forEach(function (line) {
                if (!marks.unsaved.has(line)) marks.saved.add(line);
            });
        }
    }

    function scheduleMarks() {
        clearTimeout(markTimer);
        markTimer = setTimeout(function () { computeMarks(); drawGutter(); updateStatus(); }, 120);
    }

    // ---------- gutter (números de línea + barra de color) ----------

    var drawQueued = false;

    function drawGutter() {
        if (drawQueued) return;
        drawQueued = true;

        requestAnimationFrame(function () {
            drawQueued = false;

            var dpr = window.devicePixelRatio || 1;
            var height = textarea.clientHeight;
            if (canvas.width !== GUTTER_WIDTH * dpr || canvas.height !== height * dpr) {
                canvas.width = GUTTER_WIDTH * dpr;
                canvas.height = height * dpr;
                canvas.style.width = GUTTER_WIDTH + 'px';
                canvas.style.height = height + 'px';
            }

            var ctx = canvas.getContext('2d');
            ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
            ctx.clearRect(0, 0, GUTTER_WIDTH, height);
            ctx.font = '12px Consolas, "Courier New", monospace';
            ctx.textAlign = 'right';
            ctx.textBaseline = 'middle';

            var lineCount = textarea.value.split('\n').length;
            var caretLine = textarea.value.slice(0, textarea.selectionStart).split('\n').length - 1;
            var first = Math.max(0, Math.floor((textarea.scrollTop - PAD_TOP) / LINE_HEIGHT));
            var last = Math.min(lineCount - 1, Math.ceil((textarea.scrollTop + height) / LINE_HEIGHT));

            for (var line = first; line <= last; line++) {
                var y = PAD_TOP + line * LINE_HEIGHT - textarea.scrollTop;

                var color = marks.unsaved.has(line) ? COLOR_UNSAVED : (marks.saved.has(line) ? COLOR_SAVED : null);
                if (color) {
                    ctx.fillStyle = color;
                    ctx.fillRect(GUTTER_WIDTH - 5, y, 3, LINE_HEIGHT);
                }

                ctx.fillStyle = line === caretLine ? COLOR_NUMBER_ACTIVE : COLOR_NUMBER;
                ctx.fillText(String(line + 1), GUTTER_WIDTH - 12, y + LINE_HEIGHT / 2);
            }
        });
    }

    // ---------- vista (textarea) <-> pestaña ----------

    function syncFromView() {
        var tab = active();
        if (!tab) return;
        tab.text = textarea.value;
        tab.scroll = textarea.scrollTop;
        tab.selStart = textarea.selectionStart;
        tab.selEnd = textarea.selectionEnd;
    }

    function loadIntoView(tab) {
        textarea.value = tab.text;
        textarea.scrollTop = tab.scroll;
        try { textarea.setSelectionRange(tab.selStart, tab.selEnd); } catch (e) { /* posición fuera de rango */ }
        computeMarks();
        drawGutter();
        updateBanner();
        updateStatus();
        updatePathLabel();
    }

    function activate(id) {
        syncFromView();
        activeId = id;
        mru = QC.mru.touch(mru, id);
        var tab = active();
        if (tab) loadIntoView(tab);
        renderTabs();
        persist();
    }

    function updatePathLabel() {
        var tab = active();
        pathLabel.textContent = tab && tab.path ? tab.path
            : tab && tab.fromDrop ? 'Archivo arrastrado (sin ruta conocida) — Ctrl+S te deja elegir dónde guardarlo'
            : 'Consulta sin guardar — Ctrl+S la guarda como archivo .sql';
        pathLabel.title = pathLabel.textContent;
    }

    function updateStatus() {
        var tab = active();
        if (!tab) { statusBar.textContent = ''; return; }

        var upToCaret = textarea.value.slice(0, textarea.selectionStart).split('\n');
        var parts = [
            'Ln ' + upToCaret.length + ', Col ' + (upToCaret[upToCaret.length - 1].length + 1),
            textarea.value.split('\n').length + ' líneas'
        ];

        if (tab.path) parts.push(tab.encoding.toUpperCase(), tab.eol === '\n' ? 'LF' : 'CRLF');

        statusBar.textContent = parts.join(' · ');

        if (marks.unsaved.size) {
            var unsaved = el('span', '', ' · ▌' + marks.unsaved.size + ' sin guardar');
            unsaved.style.color = COLOR_UNSAVED;
            statusBar.appendChild(unsaved);
        }

        if (marks.saved.size) {
            var saved = el('span', '', ' · ▌' + marks.saved.size + ' guardadas');
            saved.style.color = COLOR_SAVED;
            statusBar.appendChild(saved);
        }
    }

    // ---------- pestañas ----------

    function renderTabs() {
        tabsBox.innerHTML = '';

        tabs.forEach(function (tab) {
            var item = el('div', 'qc-tab' + (tab.id === activeId ? ' qc-tab-active' : '') + (tab.missing ? ' qc-tab-missing' : ''));
            item.title = tab.path || 'Consulta sin guardar';

            var title = el('span', 'qc-tab-title', (tab.missing ? '⚠ ' : '') + tab.title);
            item.appendChild(title);

            if (isDirty(tab) && hasUnsavedWork(tab)) item.appendChild(el('span', 'qc-tab-dirty', '●'));

            var close = el('a', 'qc-tab-close', '✕');
            close.href = '#';
            close.title = 'Cerrar';
            close.addEventListener('click', function (event) { event.preventDefault(); event.stopPropagation(); closeTab(tab.id); });
            item.appendChild(close);

            item.addEventListener('click', function () { if (tab.id !== activeId) activate(tab.id); });
            item.addEventListener('auxclick', function (event) { if (event.button === 1) { event.preventDefault(); closeTab(tab.id); } });

            tabsBox.appendChild(item);
        });

        var add = el('a', 'qc-tab-add', '＋');
        add.href = '#';
        add.title = 'Nueva consulta';
        add.addEventListener('click', function (event) { event.preventDefault(); newScratch(''); textarea.focus(); });
        tabsBox.appendChild(add);
        notifyTabs();
    }

    // Resumen de las pestañas para quien necesite saberlo: cuál es la activa y qué archivos están abiertos / con cambios sin guardar.
    function tabsInfo() {
        var current = active();

        return {
            activeId: activeId,
            ids: tabs.map(function (t) { return t.id; }),
            activePath: current && current.path ? current.path : '',
            open: tabs.filter(function (t) { return t.path; }).map(function (t) { return t.path; }),
            dirty: tabs.filter(function (t) { return t.path && isDirty(t); }).map(function (t) { return t.path; })
        };
    }

    // renderTabs corre en cada tecla: los oyentes deben ser baratos (el explorador solo toca el DOM si algo cambió de verdad).
    function notifyTabs() {
        var info = tabsInfo();
        tabListeners.forEach(function (listener) {
            try { listener(info); } catch (e) { /* un oyente roto no debe frenar las pestañas */ }
        });
    }

    function closeTab(id, force) {
        var index = tabs.map(function (t) { return t.id; }).indexOf(id);
        if (index < 0) return;

        var tab = tabs[index];
        syncFromView();

        var hasContent = hasUnsavedWork(tab);
        if (!force && hasContent && !confirm('"' + tab.title + '" tiene cambios sin guardar.\n¿Cerrarla y descartarlos?')) return;

        tabs.splice(index, 1);
        mru = QC.mru.remove(mru, id);

        if (!tabs.length) { newScratch(''); return; }
        if (id === activeId) activate(tabs[Math.min(index, tabs.length - 1)].id);
        else { renderTabs(); persist(); }
    }

    // ---------- avisos de la pestaña activa ----------

    function showBanner(type, text, actions) {
        banner.hidden = false;
        banner.className = 'alert alert-' + type + ' py-2 px-3 mb-2 small d-flex flex-wrap gap-2 align-items-center';
        banner.innerHTML = '';
        banner.appendChild(el('span', 'me-auto', text));

        (actions || []).forEach(function (action) {
            var button = el('button', 'btn btn-sm btn-outline-dark py-0', action.text);
            button.type = 'button';
            button.addEventListener('click', action.run);
            banner.appendChild(button);
        });
    }

    function updateBanner() {
        var tab = active();
        banner.hidden = true;
        banner.innerHTML = '';
        if (!tab) return;

        if (tab.missing) {
            showBanner('warning',
                '⚠ No se encontró el archivo "' + tab.path + '" (se movió, se renombró o se borró). Lo que ves es la copia de la caché local de este navegador.',
                [
                    { text: 'Reintentar', run: function () { revalidate(tab); } },
                    { text: 'Guardar como...', run: function () { save(tab, true); } },
                    { text: 'Cerrar pestaña', run: function () { closeTab(tab.id, true); } }
                ]);
        } else if (tab.diskChanged) {
            showBanner('warning',
                'Este archivo cambió en disco desde la última vez que lo abriste y tienes cambios sin guardar aquí.',
                [
                    { text: 'Recargar de disco (descarta lo mío)', run: function () { reload(tab).then(function () { QC.toast('Recargado desde disco.', 'secondary'); }); } },
                    { text: 'Conservar mi versión', run: function () { tab.diskChanged = false; updateBanner(); persist(); } }
                ]);
        } else if (tab.notice) {
            showBanner(tab.notice.type, tab.notice.text, [{ text: 'Cerrar aviso', run: function () { tab.notice = null; updateBanner(); persist(); } }]);
        }
    }

    // ---------- archivos ----------

    var RECENT_KEY = 'qc-recent';
    var ROOTS_KEY = 'qc-roots';

    function addRecent(path) {
        var items = QC.readJson(RECENT_KEY, []).filter(function (p) { return p.toLowerCase() !== path.toLowerCase(); });
        items.unshift(path);
        QC.writeJson(RECENT_KEY, items.slice(0, 12));
        renderRecent();
    }

    function removeRecent(path) {
        QC.writeJson(RECENT_KEY, QC.readJson(RECENT_KEY, []).filter(function (p) { return p.toLowerCase() !== path.toLowerCase(); }));
        renderRecent();
    }

    function findTab(path) {
        return tabs.filter(function (t) { return t.path && t.path.toLowerCase() === path.toLowerCase(); })[0];
    }

    function openFile(path) {
        var existing = findTab(path);

        if (existing) {
            return (existing.missing ? revalidate(existing) : Promise.resolve()).then(function () { activate(existing.id); });
        }

        return QC.get('ReadFile', { path: path }).then(function (file) {
            var tab = newTab({
                title: file.name, path: file.path, text: file.text, opened: file.text, saved: file.text,
                version: file.version, eol: file.eol, encoding: file.encoding
            });

            tabs.push(tab);
            activate(tab.id);
            addRecent(file.path);
            textarea.focus();
        }).catch(function (error) {
            QC.toast('No se pudo abrir "' + path + '": ' + error.message, 'danger');
            if (error.code === 'not_found') removeRecent(path);
        });
    }

    // Vuelve a leer el archivo de disco (descarta lo que había en la pestaña).
    function reload(tab) {
        return QC.get('ReadFile', { path: tab.path }).then(function (file) {
            tab.text = file.text; tab.opened = file.text; tab.saved = file.text;
            tab.version = file.version; tab.eol = file.eol; tab.encoding = file.encoding;
            tab.missing = false; tab.diskChanged = false; tab.notice = null;
            if (tab.id === activeId) loadIntoView(tab);
            renderTabs();
            persist();
        }).catch(function (error) { handleFileError(tab, error); });
    }

    // Comprueba que el archivo de una pestaña restaurada de la caché siga en disco y sin cambios.
    function revalidate(tab) {
        if (!tab.path) return Promise.resolve();

        return QC.get('StatFile', { path: tab.path }).then(function (stat) {
            if (!stat.exists) {
                tab.missing = true;
            } else {
                tab.missing = false;

                if (tab.version && stat.version !== tab.version) {
                    if (isDirty(tab)) {
                        tab.diskChanged = true;
                    } else {
                        return reload(tab).then(function () {
                            tab.notice = { type: 'info', text: 'El archivo cambió en disco desde la última vez y se recargó.' };
                            if (tab.id === activeId) updateBanner();
                        });
                    }
                } else if (!tab.version) {
                    tab.version = stat.version;
                }
            }
        }).catch(function (error) { handleFileError(tab, error); }).then(function () {
            renderTabs();
            if (tab.id === activeId) updateBanner();
            persist();
        });
    }

    // Un archivo cacheado puede haber desaparecido, cambiado de permisos o la carpeta ya no estar: nunca se pierde el contenido.
    function handleFileError(tab, error) {
        if (error.code === 'not_found') {
            tab.missing = true;
        } else {
            tab.notice = { type: 'warning', text: 'No se pudo verificar "' + tab.path + '": ' + error.message };
        }

        renderTabs();
        if (tab.id === activeId) updateBanner();
        persist();
    }

    function save(tab, asNew) {
        syncFromView();
        tab = tab || active();
        if (!tab) return Promise.resolve();

        var chooser;

        if (!tab.path || asNew) {
            chooser = Picker.open({ mode: 'save', title: 'Guardar como', folder: tab.path ? dirName(tab.path) : '', filename: tab.path ? baseName(tab.path) : (/\.sql$/i.test(tab.title) ? tab.title : tab.title.replace(/\s+/g, '_') + '.sql') })
                .then(function (path) { return path ? { path: path, expected: null, overwrite: false } : null; });
        } else if (tab.missing) {
            // El archivo ya no está: guardarlo lo vuelve a crear en la misma ruta, así que se pregunta.
            chooser = Promise.resolve(confirm('"' + tab.path + '" ya no existe en disco.\n¿Crearlo de nuevo en esa misma ruta?')
                ? { path: tab.path, expected: null, overwrite: true }
                : null);
        } else {
            chooser = Promise.resolve({ path: tab.path, expected: tab.version, overwrite: false });
        }

        return chooser.then(function (target) { return target ? writeFile(tab, target) : null; });
    }

    function writeFile(tab, target) {
        var sent = tab.text;

        return QC.post('SaveFile', {
            path: target.path, text: sent, eol: tab.eol, encoding: tab.encoding,
            expectedVersion: target.expected, overwrite: target.overwrite
        }).then(function (stat) {
            var renamed = !tab.path || tab.path.toLowerCase() !== target.path.toLowerCase();

            if (renamed) {
                tab.path = target.path;
                tab.title = baseName(target.path);
                tab.opened = sent;          // archivo nuevo: sin marcas verdes de historia anterior
            }

            tab.saved = sent;
            tab.version = stat.version;
            tab.missing = false; tab.diskChanged = false; tab.notice = null;
            tab.fromDrop = false;   // ya tiene ruta propia

            computeMarks();
            drawGutter();
            updateStatus();
            updatePathLabel();
            updateBanner();
            renderTabs();
            persist();
            addRecent(tab.path);
            QC.toast('Guardado: ' + tab.title, 'success');
        }).catch(function (error) {
            if (error.code === 'conflict') {
                if (confirm(error.message + '\n\n¿Sobrescribirlo con tu versión?\n(Cancelar conserva lo tuyo sin guardar; puedes usar "Guardar como...".)'))
                    return writeFile(tab, { path: target.path, expected: null, overwrite: true });
                tab.diskChanged = true;
                updateBanner();
                return null;
            }

            if (error.code === 'exists') {
                if (confirm(error.message + '\n\n¿Reemplazarlo?'))
                    return writeFile(tab, { path: target.path, expected: null, overwrite: true });
                return null;
            }

            if (error.code === 'not_found') {
                // La carpeta (o el archivo) desapareció: se ofrece otro destino.
                QC.toast(error.message, 'danger');
                if (confirm(error.message + '\n\n¿Guardarlo en otra ruta?')) return save(tab, true);
                return null;
            }

            QC.toast('No se pudo guardar: ' + error.message, 'danger');
            return null;
        });
    }

    // ---------- selector de rutas (explorador de carpetas del disco local) ----------

    var Picker = (function () {
        var modalEl = $('picker');
        var modal = new bootstrap.Modal(modalEl);
        var pathInput = $('picker-path');
        var list = $('picker-list');
        var placesBox = $('picker-places');
        var nameRow = $('picker-name-row');
        var nameInput = $('picker-name');
        var okButton = $('picker-ok');
        var titleEl = $('picker-title');
        var message = $('picker-msg');

        var state = { mode: 'file', current: '', selected: null, resolve: null };
        var placesLoaded = false;

        function showError(text) {
            message.hidden = !text;
            message.textContent = text || '';
        }

        function join(folder, name) {
            return folder.replace(/[\\/]+$/, '') + '\\' + name;
        }

        function updateOk() {
            if (state.mode === 'file') okButton.disabled = !state.selected;
            else if (state.mode === 'folder') okButton.disabled = !state.current;
            else okButton.disabled = !state.current || !nameInput.value.trim();
        }

        function render(listing) {
            state.current = listing.path;
            state.selected = null;
            pathInput.value = listing.path;
            list.innerHTML = '';

            if (listing.parent !== null) {
                var up = el('div', 'qc-pick-row', '⬆  ..  (carpeta superior)');
                up.addEventListener('click', function () { go(listing.parent); });
                list.appendChild(up);
            }

            listing.entries.forEach(function (entry) {
                var row = el('div', 'qc-pick-row' + (entry.isFolder ? '' : ' qc-pick-file'));
                row.appendChild(el('span', '', entry.isFolder ? '📁 ' : '📄 '));
                row.appendChild(el('span', 'qc-label', entry.name));

                if (!entry.isFolder && entry.size != null) row.appendChild(el('span', 'qc-detail ms-2', Math.max(1, Math.round(entry.size / 1024)) + ' KB'));

                if (entry.isFolder) {
                    row.addEventListener('click', function () { go(entry.path); });
                } else if (state.mode !== 'folder') {
                    row.addEventListener('click', function () {
                        Array.prototype.forEach.call(list.querySelectorAll('.qc-pick-selected'), function (r) { r.classList.remove('qc-pick-selected'); });
                        row.classList.add('qc-pick-selected');
                        state.selected = entry.path;
                        if (state.mode === 'save') nameInput.value = entry.name;
                        updateOk();
                    });
                    row.addEventListener('dblclick', function () { if (state.mode === 'file') finish(entry.path); });
                }

                list.appendChild(row);
            });

            if (!listing.entries.length) list.appendChild(QC.muted(state.mode === 'folder' ? '(sin subcarpetas)' : '(esta carpeta no tiene subcarpetas ni archivos .sql)'));
            if (listing.truncated) list.appendChild(QC.muted('(se muestran los primeros 2000 elementos)'));

            updateOk();
        }

        function go(path) {
            showError('');
            list.innerHTML = '';
            list.appendChild(QC.muted('Cargando...'));

            return QC.get('Browse', { path: path }).then(function (listing) {
                render(listing);
                if (listing.path) QC.writeJson('qc-lastdir', listing.path);
            }).catch(function (error) {
                showError(error.message);
                // Se queda donde estaba (o en la lista de unidades si no hay nada cargado).
                if (!state.current && path) return go('');
                list.innerHTML = '';
            });
        }

        function loadPlaces() {
            if (placesLoaded) return;
            placesLoaded = true;
            placesBox.innerHTML = '';

            var computer = el('a', 'btn btn-outline-secondary btn-sm py-0', '🖥 Este equipo');
            computer.href = '#';
            computer.addEventListener('click', function (event) { event.preventDefault(); go(''); });
            placesBox.appendChild(computer);

            QC.get('Places').then(function (places) {
                places.forEach(function (place) {
                    var link = el('a', 'btn btn-outline-secondary btn-sm py-0', place.name);
                    link.href = '#';
                    link.title = place.path;
                    link.addEventListener('click', function (event) { event.preventDefault(); go(place.path); });
                    placesBox.appendChild(link);
                });
            }).catch(function () { /* los accesos rápidos son opcionales */ });
        }

        function finish(path) {
            var resolve = state.resolve;
            state.resolve = null;
            modal.hide();
            if (resolve) resolve(path);
        }

        okButton.addEventListener('click', function () {
            if (state.mode === 'file') return finish(state.selected);
            if (state.mode === 'folder') return finish(state.current);

            var name = nameInput.value.trim();
            if (!/\.sql$/i.test(name)) name += '.sql';
            finish(join(state.current, name));
        });

        nameInput.addEventListener('input', updateOk);
        pathInput.addEventListener('keydown', function (event) { if (event.key === 'Enter') { event.preventDefault(); go(pathInput.value.trim()); } });
        $('picker-go').addEventListener('click', function () { go(pathInput.value.trim()); });

        // Cerrar con la X / Escape / clic fuera = cancelar (resuelve null).
        modalEl.addEventListener('hidden.bs.modal', function () {
            var resolve = state.resolve;
            state.resolve = null;
            if (resolve) resolve(null);
        });

        // opts: mode 'file' | 'folder' | 'save', title, folder (carpeta inicial), filename (para 'save')
        function open(opts) {
            return new Promise(function (resolve) {
                state = { mode: opts.mode, current: '', selected: null, resolve: resolve };
                titleEl.textContent = opts.title;
                nameRow.hidden = opts.mode !== 'save';
                nameInput.value = opts.filename || '';
                okButton.textContent = opts.mode === 'file' ? 'Abrir' : (opts.mode === 'folder' ? 'Elegir esta carpeta' : 'Guardar');
                okButton.disabled = true;
                showError('');
                loadPlaces();
                modal.show();

                var start = opts.folder || QC.readJson('qc-lastdir', '') || '';
                go(start);
            });
        }

        return { open: open };
    })();

    // ---------- panel "Archivos" (carpetas de trabajo + recientes) ----------

    var rootsBox = $('roots');
    var recentBox = $('recent');

    function getRoots() { return QC.readJson(ROOTS_KEY, []); }

    function addRoot(path) {
        var roots = getRoots();
        if (roots.some(function (p) { return p.toLowerCase() === path.toLowerCase(); })) return;
        roots.push(path);
        QC.writeJson(ROOTS_KEY, roots);
        renderRoots();
    }

    function removeRoot(path) {
        QC.writeJson(ROOTS_KEY, getRoots().filter(function (p) { return p.toLowerCase() !== path.toLowerCase(); }));
        renderRoots();
    }

    function folderNode(path, name, isRoot) {
        var actions = isRoot ? [{ text: '✕', title: 'Quitar esta carpeta de la lista (no borra nada)', run: function () { removeRoot(path); } }] : [];

        return QC.node({
            icon: '📁', label: name, detail: isRoot ? path : '', title: path, expandable: true, actions: actions,
            load: function () {
                return QC.get('Browse', { path: path }).then(function (listing) {
                    return listing.entries.map(function (entry) {
                        return entry.isFolder
                            ? folderNode(entry.path, entry.name, false)
                            : QC.node({ icon: '📄', label: entry.name, title: entry.path, onSelect: function () { openFile(entry.path); } });
                    });
                });
            }
        });
    }

    function renderRoots() {
        rootsBox.innerHTML = '';
        var roots = getRoots();

        if (!roots.length) {
            rootsBox.appendChild(QC.muted('Aún no agregaste carpetas. Usa "Agregar carpeta..." para ver tus scripts .sql aquí.'));
            return;
        }

        roots.forEach(function (path) { rootsBox.appendChild(folderNode(path, baseName(path) || path, true)); });
    }

    function renderRecent() {
        recentBox.innerHTML = '';
        var items = QC.readJson(RECENT_KEY, []);

        if (!items.length) { recentBox.appendChild(QC.muted('(ninguno)')); return; }

        items.forEach(function (path) {
            recentBox.appendChild(QC.node({
                icon: '📄', label: baseName(path), detail: dirName(path), title: path,
                onSelect: function () { openFile(path); },
                actions: [{ text: '✕', title: 'Quitar de recientes', run: function () { removeRecent(path); } }]
            }));
        });
    }

    $('open-folder').addEventListener('click', function () {
        Picker.open({ mode: 'folder', title: 'Agregar carpeta de scripts', folder: '' }).then(function (path) { if (path) addRoot(path); });
    });

    function openDialog() {
        Picker.open({ mode: 'file', title: 'Abrir archivo .sql', folder: active() && active().path ? dirName(active().path) : '' })
            .then(function (path) { if (path) openFile(path); });
    }

    $('open-file').addEventListener('click', openDialog);
    $('btn-open').addEventListener('click', openDialog);
    $('btn-save').addEventListener('click', function () { save(active(), false); });
    $('btn-saveas').addEventListener('click', function () { save(active(), true); });
    $('btn-new').addEventListener('click', function () { newScratch(''); textarea.focus(); });

    // Pestañas del panel lateral (Objetos | Archivos).
    var paneKey = 'qc-pane';
    function showPane(name) {
        Array.prototype.forEach.call(root.querySelectorAll('[data-pane]'), function (pane) { pane.hidden = pane.dataset.pane !== name; });
        Array.prototype.forEach.call(root.querySelectorAll('[data-pane-tab]'), function (tab) { tab.classList.toggle('active', tab.dataset.paneTab === name); });
        try { localStorage.setItem(paneKey, name); } catch (e) { }
    }

    Array.prototype.forEach.call(root.querySelectorAll('[data-pane-tab]'), function (tab) {
        tab.addEventListener('click', function (event) { event.preventDefault(); showPane(tab.dataset.paneTab); });
    });

    // ---------- panel lateral: Ctrl+B y ancho ajustable ----------

    var sideKey = 'qc-side';
    var sideState = QC.readJson(sideKey, { hidden: false, width: 380 });

    function applySide() {
        layout.classList.toggle('qc-side-hidden', !!sideState.hidden);
        layout.style.setProperty('--qc-side-w', Math.min(Math.max(sideState.width || 380, 220), 720) + 'px');
        sideToggle.title = (sideState.hidden ? 'Mostrar' : 'Ocultar') + ' el panel izquierdo (Ctrl+B)';
        sideToggle.textContent = sideState.hidden ? '☰ Mostrar panel' : '☰';
        QC.writeJson(sideKey, sideState);
        drawGutter();
    }

    function toggleSide() { sideState.hidden = !sideState.hidden; applySide(); }

    sideToggle.addEventListener('click', toggleSide);

    splitter.addEventListener('mousedown', function (event) {
        event.preventDefault();
        var startX = event.clientX, startWidth = sideState.width || 380;

        function move(e) { sideState.width = Math.min(Math.max(startWidth + e.clientX - startX, 220), 720); layout.style.setProperty('--qc-side-w', sideState.width + 'px'); drawGutter(); }
        function up() { document.removeEventListener('mousemove', move); document.removeEventListener('mouseup', up); QC.writeJson(sideKey, sideState); }

        document.addEventListener('mousemove', move);
        document.addEventListener('mouseup', up);
    });

    splitter.addEventListener('dblclick', function () { sideState.width = 380; applySide(); });

    // ---------- atajos ----------

    document.addEventListener('keydown', function (event) {
        var mod = event.ctrlKey || event.metaKey;
        if (!mod || event.altKey) return;

        var key = event.key.toLowerCase();

        if (key === 'b' && !event.shiftKey) { event.preventDefault(); toggleSide(); }
        else if (key === 's') { event.preventDefault(); save(active(), event.shiftKey); }
        else if (key === 'o' && !event.shiftKey) { event.preventDefault(); openDialog(); }
        // Ctrl+E ejecuta como en SSMS (el navegador lo usaría para la barra de búsqueda: se le gana con preventDefault).
        else if (key === 'e' && !event.shiftKey) { event.preventDefault(); form.requestSubmit(runButton); }
    });

    textarea.addEventListener('keydown', function (event) {
        if ((event.ctrlKey || event.metaKey) && event.key === 'Enter') {
            event.preventDefault();
            form.requestSubmit(runButton);
        } else if (event.key === 'Tab' && !event.ctrlKey && !event.altKey && !event.shiftKey) {
            // Como cualquier editor: Tab indenta en vez de saltar a otro control (execCommand conserva el Ctrl+Z).
            event.preventDefault();
            document.execCommand('insertText', false, '    ');
        }
    });

    // ---------- eventos del editor ----------

    textarea.addEventListener('input', function () {
        var tab = active();
        if (!tab) return;
        tab.text = textarea.value;
        renderTabs();
        drawGutter();
        scheduleMarks();
        updateStatus();
        schedulePersist();
    });

    textarea.addEventListener('scroll', drawGutter);
    ['keyup', 'click', 'select'].forEach(function (name) { textarea.addEventListener(name, function () { drawGutter(); updateStatus(); }); });
    window.addEventListener('resize', drawGutter);
    if (window.ResizeObserver) new ResizeObserver(drawGutter).observe(textarea);

    window.addEventListener('pagehide', persist);
    window.addEventListener('beforeunload', persist);

    // Ejecutar: lo seleccionado, o todo el texto (como SSMS). El campo oculto "Sql" es lo que viaja al servidor.
    form.addEventListener('submit', function () {
        syncFromView();
        var selected = textarea.value.substring(textarea.selectionStart, textarea.selectionEnd);
        hiddenSql.value = selected.trim() ? selected : textarea.value;
        // El resultado que vuelva es de esta pestaña (query-results.js lo guarda para ella).
        var tabIdInput = $('tab-id');
        if (tabIdInput) tabIdInput.value = activeId;
        persist();
    }, true);

    // ---------- Ctrl+Tab: cambiar al archivo anterior (como SSMS) ----------
    // Ctrl+Tab salta a la pestaña usada justo antes (alterna entre las dos últimas). Manteniendo Ctrl y pulsando Tab otra vez se
    // recorren las anteriores (Mayús+Tab, en sentido contrario); al soltar Ctrl se cambia a la elegida y Esc cancela. Si Ctrl se
    // mantiene un instante aparece la lista "Archivos activos" en orden de uso reciente. OJO: el navegador reserva Ctrl+Tab en las
    // pestañas normales (la página ni lo recibe), pero NO en una ventana tipo aplicación (msedge --app, la que abre start.cmd).

    var SWITCHER_DELAY_MS = 220;
    var switcher = null;   // { list: [ids], index, timer, box } mientras Ctrl sigue apretado

    function tabById(id) { return tabs.filter(function (t) { return t.id === id; })[0]; }

    function renderSwitcher() {
        if (!switcher || !switcher.box) return;

        Array.prototype.forEach.call(switcher.box.querySelectorAll('.qc-switcher-row'), function (row, i) {
            row.classList.toggle('qc-switcher-selected', i === switcher.index);
        });

        var selectedRow = switcher.box.querySelector('.qc-switcher-selected');
        if (selectedRow) selectedRow.scrollIntoView({ block: 'nearest' });
    }

    function showSwitcher() {
        if (!switcher || switcher.box) return;

        var box = el('div', 'qc-switcher');
        box.appendChild(el('div', 'qc-switcher-title', 'Archivos activos'));

        var list = el('div', 'qc-switcher-list');

        switcher.list.forEach(function (id, i) {
            var tab = tabById(id);
            var row = el('div', 'qc-switcher-row');
            row.appendChild(el('span', 'qc-switcher-name', (tab.missing ? '⚠ ' : '') + tab.title + (isDirty(tab) && hasUnsavedWork(tab) ? ' ●' : '')));
            if (tab.path) row.appendChild(el('span', 'qc-switcher-path', dirName(tab.path)));

            // mousedown y no click: no hay que quitarle el foco al editor.
            row.addEventListener('mousedown', function (event) {
                event.preventDefault();
                switcher.index = i;
                closeSwitcher(true);
            });

            list.appendChild(row);
        });

        box.appendChild(list);
        box.appendChild(el('div', 'qc-switcher-hint', 'Ctrl+Tab siguiente · Ctrl+Mayús+Tab anterior · soltar Ctrl cambia · Esc cancela'));

        document.body.appendChild(box);
        switcher.box = box;
        renderSwitcher();
    }

    function closeSwitcher(commit) {
        if (!switcher) return;

        var state = switcher;
        switcher = null;
        clearTimeout(state.timer);
        if (state.box) state.box.remove();

        var target = commit && state.index >= 0 ? state.list[state.index] : null;
        if (target && target !== activeId && tabById(target)) activate(target);
        textarea.focus();
    }

    function stepSwitcher(direction) {
        if (!switcher) {
            var list = mru.filter(tabById);
            var start = QC.mru.startIndex(list.length, direction);
            if (start < 0) return;   // una sola pestaña: no hay a dónde ir

            switcher = { list: list, index: start, timer: null, box: null };
            switcher.timer = setTimeout(showSwitcher, SWITCHER_DELAY_MS);
            return;
        }

        switcher.index = QC.mru.step(switcher.list.length, switcher.index, direction);
        renderSwitcher();
    }

    document.addEventListener('keydown', function (event) {
        if (switcher && event.key === 'Escape') { event.preventDefault(); closeSwitcher(false); return; }

        // Red de seguridad: si Ctrl ya no está apretado y no vimos su keyup, la lista no se queda colgada.
        if (switcher && !event.ctrlKey) closeSwitcher(true);

        if (event.key !== 'Tab' || !event.ctrlKey || event.altKey || event.metaKey) return;
        if (document.body.classList.contains('modal-open')) return;   // con un diálogo abierto no se cambia de pestaña

        event.preventDefault();
        event.stopPropagation();
        if (!event.repeat) stepSwitcher(event.shiftKey ? -1 : 1);
    }, true);

    document.addEventListener('keyup', function (event) { if (event.key === 'Control' || !event.ctrlKey) closeSwitcher(true); }, true);

    // Si la ventana pierde el foco con Ctrl apretado no llegará el keyup: se cancela en vez de dejar la lista colgada.
    window.addEventListener('blur', function () { closeSwitcher(false); });

    // ---------- arrastrar archivos .sql a la página ----------
    // El navegador entrega el CONTENIDO de un archivo arrastrado pero nunca su ruta. Por eso primero se intenta
    // enlazarlo con el archivo real (el servidor lo busca por nombre y tamaño en las carpetas de trabajo y recientes y
    // se confirma que el contenido sea idéntico): si aparece, queda abierto como cualquier otro archivo y "Guardar" lo
    // guarda en su lugar. Si no, se abre igual con su contenido y "Guardar" pregunta dónde.

    var MAX_DROP_BYTES = 5 * 1024 * 1024;
    var dropOverlay = el('div', 'qc-drop-overlay', '📂 Suelta el archivo .sql para abrirlo');
    dropOverlay.hidden = true;
    document.body.appendChild(dropOverlay);

    var dragDepth = 0;

    function dragHasFiles(event) {
        return !!event.dataTransfer && Array.prototype.indexOf.call(event.dataTransfer.types || [], 'Files') >= 0;
    }

    function decodeDropped(buffer) {
        var bytes = new Uint8Array(buffer);

        if (bytes[0] === 0xEF && bytes[1] === 0xBB && bytes[2] === 0xBF) return { text: new TextDecoder('utf-8').decode(bytes.subarray(3)), encoding: 'utf8-bom' };
        if (bytes[0] === 0xFF && bytes[1] === 0xFE) return { text: new TextDecoder('utf-16le').decode(bytes.subarray(2)), encoding: 'utf16le' };
        if (bytes[0] === 0xFE && bytes[1] === 0xFF) return { text: new TextDecoder('utf-16be').decode(bytes.subarray(2)), encoding: 'utf16be' };

        try {
            return { text: new TextDecoder('utf-8', { fatal: true }).decode(bytes), encoding: 'utf8' };
        } catch (e) {
            // Script viejo en Windows-1252. Si trae símbolos que Latin-1 no puede guardar (€, “ ”), se guardaría mal: se pasa a UTF-8.
            var text = new TextDecoder('windows-1252').decode(bytes);
            return { text: text, encoding: /[^\u0000-ÿ]/.test(text) ? 'utf8' : 'latin1' };
        }
    }

    function normalizeEol(text) { return text.replace(/\r\n?/g, '\n'); }

    // Carpetas donde buscar el archivo: las de trabajo, las de los recientes y las de las pestañas abiertas.
    function searchFolders() {
        var folders = QC.readJson(ROOTS_KEY, []).concat(QC.readJson(RECENT_KEY, []).map(dirName));

        tabs.forEach(function (t) { if (t.path) folders.push(dirName(t.path)); });

        var last = QC.readJson('qc-lastdir', '');
        if (last) folders.push(last);

        var seen = {};
        return folders.filter(function (f) {
            var key = (f || '').toLowerCase();
            if (!key || seen[key]) return false;
            seen[key] = true;
            return true;
        });
    }

    // Devuelve la ruta real del archivo arrastrado, o null si no se encuentra (o no es el mismo contenido).
    function locateDropped(file, text) {
        var folders = searchFolders();
        if (!folders.length) return Promise.resolve(null);

        return QC.post('LocateFile', { name: file.name, size: file.size, folders: folders }).then(function (result) {
            var candidates = (result.matches || []).slice(0, 5);
            var wanted = normalizeEol(text.replace(/^﻿/, ''));

            // Mismo nombre y tamaño en dos carpetas: se elige la que tiene el mismo contenido.
            return candidates.reduce(function (chain, path) {
                return chain.then(function (found) {
                    if (found) return found;
                    return QC.get('ReadFile', { path: path }).then(function (disk) { return normalizeEol(disk.text) === wanted ? disk.path : null; }, function () { return null; });
                });
            }, Promise.resolve(null));
        }).catch(function () { return null; });   // la búsqueda es una ayuda: si falla, se abre sin enlazar
    }

    function openDroppedFile(file) {
        if (file.size > MAX_DROP_BYTES) {
            QC.toast('"' + file.name + '" pesa más de 5 MB: no se abre aquí.', 'danger');
            return Promise.resolve();
        }

        return file.arrayBuffer().then(function (buffer) {
            var decoded = decodeDropped(buffer);

            return locateDropped(file, decoded.text).then(function (path) {
                if (path) return openFile(path);

                // Sin ruta: se abre con el contenido; "Guardar" preguntará dónde.
                var text = normalizeEol(decoded.text.replace(/^﻿/, ''));
                var crlf = (decoded.text.match(/\r\n/g) || []).length;
                var lf = (decoded.text.match(/\n/g) || []).length - crlf;
                var eol = lf > crlf ? '\n' : '\r\n';   // igual que el servidor: gana el dominante, y por defecto CRLF

                var tab = newTab({
                    title: file.name, text: text, opened: text, saved: text, eol: eol, encoding: decoded.encoding, fromDrop: true,
                    notice: {
                        type: 'info',
                        text: 'Abierto desde el archivo arrastrado. El navegador no informa dónde está en tu disco, así que no se pudo enlazar con el original: ' +
                            'al guardar te pregunto la carpeta. (Si agregas la carpeta de tus scripts en "Archivos .sql", la próxima vez se enlaza solo.)'
                    }
                });

                tabs.push(tab);
                activate(tab.id);
                textarea.focus();
            });
        }).catch(function (error) {
            QC.toast('No se pudo leer "' + file.name + '": ' + error.message, 'danger');
        });
    }

    function openDropped(files) {
        var sql = files.filter(function (f) { return /\.sql$/i.test(f.name); });

        if (sql.length < files.length) QC.toast((files.length - sql.length) + ' archivo(s) ignorado(s): solo se abren archivos .sql.', 'warning');
        if (sql.length > 10) QC.toast('Se abren solo los primeros 10 archivos.', 'warning');

        // De a uno: cada apertura puede consultar al servidor y activa su pestaña.
        return sql.slice(0, 10).reduce(function (chain, file) { return chain.then(function () { return openDroppedFile(file); }); }, Promise.resolve());
    }

    // En todo el documento: si no se frena el drop, el navegador abriría el archivo y la página se perdería.
    document.addEventListener('dragenter', function (event) {
        if (!dragHasFiles(event)) return;
        event.preventDefault();
        dragDepth++;
        dropOverlay.hidden = false;
    });

    document.addEventListener('dragover', function (event) {
        if (!dragHasFiles(event)) return;
        event.preventDefault();
        event.dataTransfer.dropEffect = 'copy';
    });

    document.addEventListener('dragleave', function (event) {
        if (!dragHasFiles(event)) return;
        dragDepth = Math.max(0, dragDepth - 1);
        if (!dragDepth) dropOverlay.hidden = true;
    });

    document.addEventListener('drop', function (event) {
        if (!dragHasFiles(event)) return;
        event.preventDefault();
        dragDepth = 0;
        dropOverlay.hidden = true;
        openDropped(Array.prototype.slice.call(event.dataTransfer.files));
    });

    // ---------- API para query-console.js ----------

    QC.editor = {
        // Pone una consulta en el editor sin pisar un archivo: si la pestaña activa es un archivo, abre una nueva.
        showQuery: function (text) {
            var tab = active();
            if (tab && !tab.path) {
                textarea.value = text;
                tab.text = text;
                computeMarks(); drawGutter(); updateStatus(); renderTabs(); persist();
            } else {
                newScratch(text);
            }
        },
        insertAtCursor: function (text) {
            textarea.focus();
            document.execCommand('insertText', false, text);
        },
        getRunText: function () {
            var selected = textarea.value.substring(textarea.selectionStart, textarea.selectionEnd);
            return selected.trim() ? selected : textarea.value;
        },
        focus: function () { textarea.focus(); },

        // Para el explorador de archivos (query-explorer.js):
        // abre (o activa si ya estaba abierto) un archivo .sql por su ruta completa.
        openFile: openFile,
        // Abre el selector de rutas en modo carpeta; resuelve con la ruta elegida o null si se cancela. opts: { title, folder }.
        pickFolder: function (opts) {
            opts = opts || {};
            return Picker.open({ mode: 'folder', title: opts.title || 'Abrir carpeta', folder: opts.folder || '' });
        },
        tabsInfo: tabsInfo,

        // Conexión de la pestaña activa (query-console.js la aplica a los selectores al cambiar de pestaña) y su cambio
        // cuando el usuario elige otra conexión, base o esquema: solo afecta a esta pestaña, como en SSMS.
        activeContext: function () {
            var tab = active();
            return tab && tab.alias ? { alias: tab.alias, database: tab.database, schema: tab.schema } : null;
        },
        setActiveContext: function (ctx) {
            var tab = active();
            if (!tab || !ctx) return;
            tab.alias = ctx.alias || null;
            tab.database = ctx.database || null;
            tab.schema = ctx.schema || null;
            persist();
        },
        // fn({ activePath, open[], dirty[] }) se llama ya mismo y cada vez que cambian las pestañas.
        onTabsChanged: function (fn) {
            tabListeners.push(fn);
            try { fn(tabsInfo()); } catch (e) { /* ver notifyTabs */ }
        }
    };

    // ---------- arranque ----------

    var initialSql = textarea.value;
    var session = QC.readJson(sessionKey, null);
    workspaceStamp = (session && session.workspaceStamp) || null;

    if (session && Array.isArray(session.tabs) && session.tabs.length) {
        tabs = session.tabs.map(unpack);
        activeId = tabs.some(function (t) { return t.id === session.activeId; }) ? session.activeId : tabs[0].id;
    } else {
        var first = newTab({ title: 'Consulta 1', text: initialSql });
        tabs = [first];
        activeId = first.id;
    }

    // El orden de uso reciente sobrevive a las recargas (cada consulta ejecutada recarga la página).
    mru = QC.mru.restore(session && session.mru, tabs.map(function (t) { return t.id; }), activeId);

    applySide();
    var savedPane = null;
    try { savedPane = localStorage.getItem(paneKey); } catch (e) { /* sin localStorage */ }
    showPane(savedPane === 'files' ? 'files' : 'objects');
    renderRoots();
    renderRecent();
    loadIntoView(active());
    renderTabs();

    // Los archivos que estaban abiertos pueden haber cambiado o desaparecido mientras la página estaba cerrada.
    Promise.all(tabs.filter(function (t) { return t.path; }).map(revalidate)).then(function () {
        var missing = tabs.filter(function (t) { return t.missing; });
        if (missing.length) QC.toast(missing.length + ' archivo(s) de la sesión anterior ya no existen en disco; sus pestañas conservan el contenido de la caché.', 'warning');

        // Después de revisar el disco (así una pestaña que ya no existe no se cierra por "limpia"), se compara con el repo.
        return QC.get('Workspace').then(function (remote) {
            return adoptWorkspace(remote, session && session.workspaceStamp).then(function () { workspaceStamp = remote.stamp; });
        });
    }).catch(function () { /* sin sesión versionada: la caché local alcanza */ }).then(function () {
        workspaceReady = true;
        persist();
    });
})();
