// Explorador de archivos del lado derecho, como el de VS Code: una CARPETA ABIERTA mostrada como árbol, que se muestra y
// se oculta con Ctrl+K. Click en un archivo .sql lo abre en una pestaña del editor; click en una carpeta la despliega.
// El archivo de la pestaña activa se resalta (y se despliegan sus carpetas, "auto reveal"); ● marca los que tienen cambios sin guardar.
//
//  - Solo se listan subcarpetas y archivos .sql (es lo único que el servidor deja abrir; ver SqlFileService.Browse).
//  - Todo se lee con los endpoints que ya existen (Browse); no hay nada nuevo en el servidor.
//  - El estado (carpeta abierta, carpetas desplegadas, ancho, si está visible, scroll) vive en localStorage (`qc-explorer`),
//    compartido por las consolas de Oracle y SQL Server. Cada ejecución de una consulta recarga la página, así que el árbol
//    se reconstruye desde ese estado (y, si el panel está oculto, no se pide nada al servidor).
//
// Este archivo tiene dos partes: lógica pura (rutas, estado guardado, teclado — sin DOM, probada con `node --test`, ver
// js-tests/) y la interfaz, que solo se arma en el navegador.
(function (root) {
    'use strict';

    // ======================= Lógica pura (sin DOM) =======================

    var SEP = '\\';
    var WIDTH = { min: 200, max: 640, def: 300 };
    var MAX_RECENT = 8;

    // Rutas de Windows: separador "\" y sin separadores al final (salvo la raíz de una unidad: "C:\").
    function normalizePath(path) {
        var p = String(path || '').replace(/\//g, SEP);
        while (p.length > 3 && p.charAt(p.length - 1) === SEP) p = p.slice(0, -1);
        return p;
    }

    // Clave para comparar rutas: Windows no distingue mayúsculas.
    function key(path) { return normalizePath(path).toLowerCase(); }

    function withSeparator(path) {
        var p = normalizePath(path);
        return p.charAt(p.length - 1) === SEP ? p : p + SEP;
    }

    // ¿"path" está estrictamente dentro de "folder" (a cualquier profundidad)?
    function isInside(folder, path) {
        if (!folder || !path) return false;
        var prefix = withSeparator(folder).toLowerCase();
        var target = key(path);
        return target.length > prefix.length && target.indexOf(prefix) === 0;
    }

    // Las partes de "path" por debajo de "folder", con su texto original (null si no está dentro).
    function relativeSegments(folder, path) {
        if (!isInside(folder, path)) return null;
        return normalizePath(path).slice(withSeparator(folder).length).split(SEP).filter(Boolean);
    }

    // Nombre para mostrar de una carpeta: su último segmento (o la unidad: "C:\").
    function folderName(path) {
        var p = normalizePath(path);
        var last = p.split(SEP).filter(Boolean).pop();
        return last && !/^[A-Za-z]:$/.test(last) ? last : p;
    }

    function clampWidth(width) { return Math.min(Math.max(width, WIDTH.min), WIDTH.max); }

    function uniquePaths(list) {
        var seen = {};
        return list.filter(function (p) {
            if (typeof p !== 'string' || !p.trim() || seen[key(p)]) return false;
            seen[key(p)] = true;
            return true;
        }).map(normalizePath);
    }

    // Lo guardado puede venir de una versión vieja o estar dañado: cualquier cosa rara vuelve a los valores por defecto.
    function normalizeState(raw) {
        var s = raw && typeof raw === 'object' ? raw : {};
        var width = Number(s.width);

        return {
            open: !!s.open,
            width: clampWidth(isFinite(width) && width > 0 ? width : WIDTH.def),
            folder: typeof s.folder === 'string' ? normalizePath(s.folder) : '',
            expanded: uniquePaths(Array.isArray(s.expanded) ? s.expanded : []),
            recent: uniquePaths(Array.isArray(s.recent) ? s.recent : []).slice(0, MAX_RECENT),
            scroll: Math.max(0, Number(s.scroll) || 0)
        };
    }

    // La carpeta más reciente primero, sin repetir, con tope.
    function rememberFolder(recent, path) {
        var rest = (recent || []).filter(function (r) { return key(r) !== key(path); });
        rest.unshift(normalizePath(path));
        return rest.slice(0, MAX_RECENT);
    }

    // Agrupa las carpetas desplegadas por profundidad (1 = hijas directas de la carpeta abierta): al restaurar, cada nivel
    // se carga en paralelo y los niveles de a uno (un hijo solo existe cuando su padre ya se cargó).
    function groupByDepth(folder, paths) {
        var levels = [];

        paths.forEach(function (p) {
            var relative = relativeSegments(folder, p);
            if (!relative) return;
            var level = relative.length - 1;
            levels[level] = levels[level] || [];
            levels[level].push(p);
        });

        return levels.filter(Boolean);
    }

    // Teclado del árbol. rows: las filas VISIBLES en orden, [{ isFolder, open, depth }]; index: la seleccionada (-1 = ninguna).
    // Devuelve qué hacer: { type: 'select' | 'expand' | 'collapse' | 'toggle' | 'open' | 'none', index }.
    function keyAction(rows, index, keyName) {
        var none = { type: 'none', index: index };
        if (!rows.length) return none;

        if (index < 0) {
            if (keyName === 'ArrowDown' || keyName === 'Home') return { type: 'select', index: 0 };
            if (keyName === 'ArrowUp' || keyName === 'End') return { type: 'select', index: rows.length - 1 };
            return none;
        }

        var i = Math.min(index, rows.length - 1);
        var row = rows[i];

        switch (keyName) {
            case 'ArrowDown': return { type: 'select', index: Math.min(i + 1, rows.length - 1) };
            case 'ArrowUp': return { type: 'select', index: Math.max(i - 1, 0) };
            case 'Home': return { type: 'select', index: 0 };
            case 'End': return { type: 'select', index: rows.length - 1 };

            case 'ArrowRight':
                if (row.isFolder && !row.open) return { type: 'expand', index: i };
                if (row.isFolder && row.open && i + 1 < rows.length && rows[i + 1].depth > row.depth) return { type: 'select', index: i + 1 };
                return none;

            case 'ArrowLeft':
                if (row.isFolder && row.open) return { type: 'collapse', index: i };
                for (var j = i - 1; j >= 0; j--) {
                    if (rows[j].depth < row.depth) return { type: 'select', index: j };
                }
                return none;

            case 'Enter':
            case ' ':
                return { type: row.isFolder ? 'toggle' : 'open', index: i };

            default: return none;
        }
    }

    var api = {
        normalizePath: normalizePath, isInside: isInside, relativeSegments: relativeSegments, folderName: folderName,
        normalizeState: normalizeState, rememberFolder: rememberFolder, groupByDepth: groupByDepth, keyAction: keyAction,
        WIDTH: WIDTH, MAX_RECENT: MAX_RECENT
    };

    if (typeof module !== 'undefined' && module.exports) module.exports = api;
    if (typeof window === 'undefined' || typeof document === 'undefined') return;

    // ======================= Interfaz =======================

    var QC = window.QC;
    var host = QC && QC.root;
    var layout = host && host.querySelector('[data-role="layout"]');
    var panel = host && host.querySelector('[data-role="explorer"]');

    if (!QC || !QC.editor || !layout || !panel) return;

    function $(role) { return host.querySelector('[data-role="' + role + '"]'); }

    var el = QC.el;
    var bodyBox = $('x-body');
    var folderBar = $('x-folder');
    var folderNameBox = $('x-folder-name');
    var toggleButton = $('toggle-explorer');
    var splitter = $('x-splitter');

    var STORAGE_KEY = 'qc-explorer';
    var state = normalizeState(QC.readJson(STORAGE_KEY, null));

    var nodes = new Map();       // clave de ruta → registro del nodo (carpetas y archivos ya dibujados)
    var rootRecords = [];        // hijos directos de la carpeta abierta
    var selected = null;         // registro con el foco del teclado
    var tabInfo = { activePath: '', open: [], dirty: [] };
    var loadToken = 0;           // descarta respuestas viejas (cambió la carpeta o se actualizó mientras cargaba)
    var treeFolder = null;       // carpeta de la que se construyó el árbol actual (null = ninguno)

    function save() {
        QC.writeJson(STORAGE_KEY, state);
    }

    // La lista de carpetas desplegadas se mantiene de a una (no se recalcula desde el árbol): con el panel oculto o mientras
    // se reconstruye el árbol, el árbol está vacío y recalcularla borraría lo que hay que restaurar.
    function rememberExpanded(path, open) {
        var k = key(path);
        state.expanded = state.expanded.filter(function (p) { return key(p) !== k; });
        if (open) state.expanded.push(normalizePath(path));
    }

    // ---------- mostrar / ocultar (Ctrl+K) ----------

    function applyPanel() {
        layout.classList.toggle('qc-explorer-open', state.open);
        layout.style.setProperty('--qc-explorer-w', state.width + 'px');
        toggleButton.title = (state.open ? 'Ocultar' : 'Mostrar') + ' el explorador de archivos (Ctrl+K)';
        toggleButton.classList.toggle('active', state.open);
    }

    function setOpen(open) {
        state.open = open;
        applyPanel();
        save();

        if (!open) return;

        // Hasta que se muestra por primera vez no se le pide nada al servidor.
        if (treeFolder !== state.folder) loadRoot();
        else reveal(tabInfo.activePath);
    }

    document.addEventListener('keydown', function (event) {
        if (!(event.ctrlKey || event.metaKey) || event.altKey || event.shiftKey) return;
        if (event.key.toLowerCase() !== 'k') return;
        if (document.body.classList.contains('modal-open')) return;   // con un diálogo abierto no se toca el panel

        event.preventDefault();   // en el navegador Ctrl+K enfoca la barra de búsqueda
        setOpen(!state.open);
    }, true);

    toggleButton.addEventListener('click', function () { setOpen(!state.open); });
    $('x-hide').addEventListener('click', function () { setOpen(false); });

    // ---------- ancho ----------

    splitter.addEventListener('mousedown', function (event) {
        event.preventDefault();
        var startX = event.clientX, startWidth = state.width;

        function move(e) {
            // El panel está a la derecha: arrastrar hacia la izquierda lo ensancha.
            state.width = clampWidth(startWidth - (e.clientX - startX));
            layout.style.setProperty('--qc-explorer-w', state.width + 'px');
        }

        function up() {
            document.removeEventListener('mousemove', move);
            document.removeEventListener('mouseup', up);
            save();
        }

        document.addEventListener('mousemove', move);
        document.addEventListener('mouseup', up);
    });

    splitter.addEventListener('dblclick', function () { state.width = WIDTH.def; applyPanel(); save(); });

    // ---------- carpeta abierta ----------

    function clearTree() {
        nodes.clear();
        rootRecords = [];
        selected = null;
        bodyBox.textContent = '';
    }

    function renderFolderBar() {
        folderBar.hidden = !state.folder;
        folderNameBox.textContent = state.folder ? folderName(state.folder) : '';
        folderNameBox.title = state.folder;
    }

    function openFolder(path) {
        state.folder = normalizePath(path);
        state.expanded = [];
        state.scroll = 0;
        state.recent = rememberFolder(state.recent, state.folder);
        state.open = true;
        applyPanel();
        save();
        return loadRoot();
    }

    function closeFolder() {
        loadToken++;
        state.folder = '';
        state.expanded = [];
        state.scroll = 0;
        treeFolder = null;
        save();
        clearTree();
        renderFolderBar();
        renderEmpty();
    }

    function chooseFolder() {
        QC.editor.pickFolder({ title: 'Abrir carpeta', folder: state.folder }).then(function (path) {
            if (path) openFolder(path);
        });
    }

    $('x-open').addEventListener('click', chooseFolder);
    $('x-close-folder').addEventListener('click', closeFolder);
    $('x-refresh').addEventListener('click', function () { if (state.folder) loadRoot(); });
    $('x-collapse').addEventListener('click', collapseAll);

    function muted(text) { return el('div', 'qx-hint', text); }

    // Sin carpeta abierta: invitación a abrir una y las carpetas usadas antes (las recientes de acá y las agregadas en el panel izquierdo).
    function renderEmpty() {
        bodyBox.textContent = '';

        var box = el('div', 'qx-empty');
        box.appendChild(el('div', 'text-muted small mb-2', 'No hay ninguna carpeta abierta.'));

        var open = el('button', 'btn btn-primary btn-sm w-100 mb-3', '📂 Abrir carpeta...');
        open.type = 'button';
        open.addEventListener('click', chooseFolder);
        box.appendChild(open);

        var known = uniquePaths(state.recent.concat(QC.readJson('qc-roots', [])));

        if (known.length) {
            box.appendChild(el('div', 'text-muted small mb-1', 'Carpetas usadas antes'));

            known.forEach(function (path) {
                var item = el('button', 'qx-recent');
                item.type = 'button';
                item.title = path;
                item.appendChild(document.createTextNode('📁 ' + folderName(path)));
                item.appendChild(el('small', '', path));
                item.addEventListener('click', function () { openFolder(path); });
                box.appendChild(item);
            });
        }

        box.appendChild(el('div', 'text-muted small mt-3', 'Ctrl+K muestra u oculta este panel.'));
        bodyBox.appendChild(box);
    }

    function renderLoadError(error) {
        bodyBox.textContent = '';

        var box = el('div', 'qx-empty');
        box.appendChild(el('div', 'text-danger small mb-2', error.message));

        var retry = el('button', 'btn btn-outline-secondary btn-sm me-2', 'Reintentar');
        retry.type = 'button';
        retry.addEventListener('click', loadRoot);

        var close = el('button', 'btn btn-outline-secondary btn-sm', 'Cerrar carpeta');
        close.type = 'button';
        close.addEventListener('click', closeFolder);

        box.appendChild(retry);
        box.appendChild(close);
        bodyBox.appendChild(box);
    }

    // Construye el árbol de la carpeta abierta desde cero (al abrirla, al actualizar y al recargar la página).
    function loadRoot() {
        var token = ++loadToken;
        clearTree();
        renderFolderBar();

        if (!state.folder) {
            treeFolder = null;
            renderEmpty();
            return Promise.resolve();
        }

        bodyBox.appendChild(muted('Cargando...'));

        return QC.get('Browse', { path: state.folder }).then(function (listing) {
            if (token !== loadToken) return;

            state.folder = normalizePath(listing.path || state.folder);
            treeFolder = state.folder;
            renderFolderBar();

            bodyBox.textContent = '';
            rootRecords = renderListing(bodyBox, listing, 0, null);

            return restoreExpanded(token).then(function () {
                if (token !== loadToken) return;
                bodyBox.scrollTop = state.scroll;
                return reveal(tabInfo.activePath);
            });
        }).catch(function (error) {
            if (token !== loadToken) return;

            treeFolder = null;
            // Una carpeta que ya no existe sale de la lista de recientes; el resto de los fallos (permiso...) se pueden reintentar.
            if (error.code === 'not_found') {
                state.recent = state.recent.filter(function (r) { return key(r) !== key(state.folder); });
                save();
            }

            renderLoadError(error);
        });
    }

    // Vuelve a desplegar las carpetas que estaban abiertas, un nivel a la vez (los hijos de un nivel solo existen cuando se cargó el anterior).
    function restoreExpanded(token) {
        return groupByDepth(state.folder, state.expanded).reduce(function (chain, level) {
            return chain.then(function () {
                if (token !== loadToken) return null;
                return Promise.all(level.map(function (path) { return expand(nodes.get(key(path))); }));
            });
        }, Promise.resolve());
    }

    // ---------- árbol ----------

    function renderListing(container, listing, depth, parent) {
        var records = listing.entries.map(function (entry) {
            var record = buildNode(entry, depth, parent);
            container.appendChild(record.wrap);
            return record;
        });

        var indent = (6 + depth * 14 + 16) + 'px';

        if (!listing.entries.length) {
            var empty = muted('(sin subcarpetas ni archivos .sql)');
            empty.style.paddingLeft = indent;
            container.appendChild(empty);
        }

        if (listing.truncated) {
            var more = muted('(se muestran los primeros 2000 elementos)');
            more.style.paddingLeft = indent;
            container.appendChild(more);
        }

        return records;
    }

    function buildNode(entry, depth, parent) {
        var wrap = el('div', 'qx-node');
        var row = el('div', 'qx-row ' + (entry.isFolder ? 'qx-folder-row' : 'qx-file-row'));
        row.style.paddingLeft = (6 + depth * 14) + 'px';
        row.title = entry.path;
        row.setAttribute('role', 'treeitem');

        var twisty = el('span', 'qx-twisty', entry.isFolder ? '▸' : '');
        var icon = el('span', 'qx-icon', entry.isFolder ? '📁' : '📄');
        var label = el('span', 'qx-label', entry.name);
        var dot = el('span', 'qx-dot', '');

        [twisty, icon, label, dot].forEach(function (part) { row.appendChild(part); });
        wrap.appendChild(row);

        var kids = null;

        if (entry.isFolder) {
            kids = el('div', 'qx-children');
            kids.hidden = true;
            wrap.appendChild(kids);
            row.setAttribute('aria-expanded', 'false');
        }

        var record = {
            entry: entry, depth: depth, parent: parent, wrap: wrap, row: row, twisty: twisty, icon: icon, dot: dot, kids: kids,
            open: false, loaded: false, loading: null, childRecords: []
        };

        row.addEventListener('click', function () {
            bodyBox.focus({ preventScroll: true });   // para que las flechas funcionen después de un click
            choose(record);
        });

        nodes.set(key(entry.path), record);
        mark(record);

        return record;
    }

    // Click / Enter: una carpeta se despliega o se pliega; un archivo se abre en el editor.
    function choose(record) {
        select(record);

        if (record.entry.isFolder) {
            if (record.open) collapse(record);
            else expand(record);
        } else {
            QC.editor.openFile(record.entry.path);
        }
    }

    function select(record) {
        if (selected) selected.row.classList.remove('qx-selected');
        selected = record;

        if (record) {
            record.row.classList.add('qx-selected');
            record.row.scrollIntoView({ block: 'nearest' });
        }
    }

    function setOpenVisual(record, open) {
        record.open = open;
        rememberExpanded(record.entry.path, open);
        record.twisty.textContent = open ? '▾' : '▸';
        record.icon.textContent = open ? '📂' : '📁';
        record.kids.hidden = !open;
        record.row.setAttribute('aria-expanded', open ? 'true' : 'false');
    }

    function collapse(record) {
        setOpenVisual(record, false);
        save();
    }

    // Despliega una carpeta (la lista de sus hijos se pide la primera vez). Resuelve con true si quedó desplegada.
    function expand(record) {
        if (!record || !record.entry.isFolder) return Promise.resolve(false);

        setOpenVisual(record, true);

        if (record.loaded) { save(); return Promise.resolve(true); }
        if (record.loading) return record.loading;

        var token = loadToken;
        record.kids.textContent = '';
        record.kids.appendChild(muted('Cargando...'));

        record.loading = QC.get('Browse', { path: record.entry.path }).then(function (listing) {
            if (token !== loadToken) return false;

            record.kids.textContent = '';
            record.childRecords = renderListing(record.kids, listing, record.depth + 1, record);
            record.loaded = true;
            return true;
        }).catch(function (error) {
            if (token !== loadToken) return false;

            record.kids.textContent = '';
            setOpenVisual(record, false);
            QC.toast('No se pudo abrir la carpeta "' + record.entry.name + '": ' + error.message, 'danger');
            return false;
        }).then(function (result) {
            record.loading = null;
            save();
            return result;
        });

        return record.loading;
    }

    function collapseAll() {
        nodes.forEach(function (record) { if (record.entry.isFolder && record.open) setOpenVisual(record, false); });
        save();
    }

    // ---------- archivo activo, abiertos y con cambios (desde las pestañas del editor) ----------

    function mark(record) {
        if (record.entry.isFolder) return;

        var k = key(record.entry.path);
        var isActive = !!tabInfo.activePath && key(tabInfo.activePath) === k;
        var isDirty = tabInfo.dirty.some(function (p) { return key(p) === k; });
        var isOpen = isDirty || tabInfo.open.some(function (p) { return key(p) === k; });

        record.row.classList.toggle('qx-active', isActive);
        record.row.classList.toggle('qx-opened', isOpen);
        record.dot.textContent = isDirty ? '●' : '';
        record.dot.title = isDirty ? 'Tiene cambios sin guardar' : '';
    }

    var lastTabsKey = '';

    QC.editor.onTabsChanged(function (info) {
        var tabsKey = info.activePath + '|' + info.open.join(';') + '|' + info.dirty.join(';');
        var activeChanged = key(info.activePath) !== key(tabInfo.activePath);

        tabInfo = info;

        // Se llama en cada tecla: solo se toca el DOM si cambió el archivo activo, los abiertos o los modificados.
        if (tabsKey === lastTabsKey) return;
        lastTabsKey = tabsKey;

        nodes.forEach(mark);

        if (activeChanged && state.open) reveal(info.activePath);
    });

    // Despliega las carpetas que llevan al archivo y lo deja a la vista (si está dentro de la carpeta abierta).
    function reveal(path) {
        if (!state.open || !state.folder || treeFolder !== state.folder) return Promise.resolve(false);

        var segments = relativeSegments(state.folder, path);
        if (!segments) return Promise.resolve(false);

        var token = loadToken;

        function step(index, records) {
            var wanted = segments[index].toLowerCase();
            var record = records.filter(function (r) { return r.entry.name.toLowerCase() === wanted; })[0];

            if (!record || token !== loadToken) return Promise.resolve(false);

            if (index === segments.length - 1) {
                select(record);
                return Promise.resolve(true);
            }

            return expand(record).then(function (done) { return done ? step(index + 1, record.childRecords) : false; });
        }

        return step(0, rootRecords);
    }

    // ---------- teclado dentro del árbol ----------

    function visibleRecords() {
        var out = [];

        (function walk(records) {
            records.forEach(function (record) {
                out.push(record);
                if (record.entry.isFolder && record.open && record.loaded) walk(record.childRecords);
            });
        })(rootRecords);

        return out;
    }

    var NAV_KEYS = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'Enter', ' '];

    // Aplica una tecla de navegación; devuelve false si no hacía nada.
    function handleKey(keyName) {
        var records = visibleRecords();
        var action = keyAction(
            records.map(function (r) { return { isFolder: r.entry.isFolder, open: r.open, depth: r.depth }; }),
            records.indexOf(selected),
            keyName);

        if (action.type === 'none') return false;

        var record = records[action.index];

        if (action.type === 'select') select(record);
        else if (action.type === 'expand') expand(record);
        else if (action.type === 'collapse') collapse(record);
        else choose(record);   // 'toggle' y 'open' hacen lo mismo que un click

        return true;
    }

    bodyBox.addEventListener('keydown', function (event) {
        if (event.ctrlKey || event.metaKey || event.altKey) return;

        if (event.key === 'Escape') { QC.editor.focus(); return; }
        if (NAV_KEYS.indexOf(event.key) < 0) return;

        // Si una carpeta todavía se está cargando (se acaba de desplegar), la tecla espera: así "→ ↓" cae en su primer hijo y no en el siguiente de afuera.
        var loading = visibleRecords().filter(function (r) { return r.loading; })[0];

        if (loading) {
            event.preventDefault();
            var keyName = event.key;
            loading.loading.then(function () { handleKey(keyName); });
            return;
        }

        if (handleKey(event.key)) event.preventDefault();
    });

    var scrollTimer = null;
    bodyBox.addEventListener('scroll', function () {
        clearTimeout(scrollTimer);
        scrollTimer = setTimeout(function () { state.scroll = bodyBox.scrollTop; QC.writeJson(STORAGE_KEY, state); }, 250);
    });

    // ---------- arranque ----------

    applyPanel();
    renderFolderBar();

    if (state.open) loadRoot();
    else if (!state.folder) renderEmpty();
})(this);
