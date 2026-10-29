// Consola de consultas (Oracle / SQL Server), al estilo de SSMS / SQL Developer:
// explorador en árbol (conexión → bases → esquemas → tablas, vistas, procedimientos... → columnas),
// conexiones que se agregan desde la pantalla, historial local y Ctrl+Enter.
// Todo el estado persistente del navegador vive en localStorage; las conexiones, en el servidor (cifradas).
(function () {
    var root = document.querySelector('[data-query-console]');
    if (!root) return;

    var kind = root.dataset.kind;                       // 'oracle' | 'sqlserver'
    var baseUrl = root.dataset.baseUrl;
    var kinds = JSON.parse(root.dataset.kinds || '[]'); // tipos de objeto del motor, en orden
    var hasDatabases = root.dataset.hasDatabases === '1';

    function $(role) { return root.querySelector('[data-role="' + role + '"]'); }

    var form = $('form');
    var aliasSel = $('alias');
    var dbSel = $('database');       // solo SQL Server
    var schemaSel = $('schema');     // solo Oracle
    var filter = $('filter');
    var treeBox = $('tree');
    var resultsBox = $('results');
    var detailBox = $('detail');
    var history = $('history');
    var runButton = $('run');
    var descriptionBox = $('description');
    var showSystem = $('show-system');

    var KIND_LABELS = { Table: 'Tablas', View: 'Vistas', Procedure: 'Procedimientos', Function: 'Funciones', Package: 'Paquetes', Sequence: 'Secuencias', Synonym: 'Sinónimos', Type: 'Tipos' };
    var KIND_ICONS = { Table: '▦', View: '👁', Procedure: '⚙', Function: 'ƒ', Package: '📦', Sequence: '#', Synonym: '↪', Type: '𝑻' };
    var KIND_SINGULAR = { Table: 'Tabla', View: 'Vista', Procedure: 'Procedimiento', Function: 'Función', Package: 'Paquete', Sequence: 'Secuencia', Synonym: 'Sinónimo', Type: 'Tipo' };
    var GROUPS = { Table: ['Columns', 'Indexes', 'ForeignKeys', 'Triggers'], View: ['Columns'], Procedure: ['Parameters'], Function: ['Parameters'], Package: ['Members'] };
    var GROUP_LABELS = { Columns: 'Columnas', Indexes: 'Índices', ForeignKeys: 'Claves foráneas', Triggers: 'Triggers', Parameters: 'Parámetros', Members: 'Miembros' };
    var QUERYABLE = { Table: true, View: true, Synonym: true };

    // ---------- utilidades (qc-common.js) ----------

    var QC = window.QC;
    var el = QC.el, readJson = QC.readJson, writeJson = QC.writeJson, get = QC.get, post = QC.post, muted = QC.muted, node = QC.node;

    // ---------- contexto (conexión / base / esquema) ----------

    function currentAlias() { return aliasSel.value; }
    function currentDatabase() { return dbSel ? dbSel.value : ''; }

    function ensureOption(select, value) {
        if (!select || !value) return;
        var exists = Array.prototype.some.call(select.options, function (o) { return o.value === value; });
        if (!exists) {
            var option = document.createElement('option');
            option.value = value;
            option.textContent = value;
            select.appendChild(option);
        }
    }

    function setAlias(alias) {
        if (aliasSel.value === alias) return;
        aliasSel.value = alias;
        onAliasChanged();
    }

    function setDatabase(name) {
        if (!dbSel) return;
        ensureOption(dbSel, name);
        dbSel.value = name || '';
        contextChanged();
    }

    function setSchema(name) {
        if (!schemaSel) return;
        ensureOption(schemaSel, name);
        schemaSel.value = name || '';
        contextChanged();
    }

    var optionsLoadedFor = null;

    // Las listas de bases/esquemas se piden al abrir el selector (no al cargar la página: eso abriría una conexión sin que lo pidas).
    function ensureContextOptions() {
        var alias = currentAlias();
        var select = dbSel || schemaSel;
        if (!alias || !select || optionsLoadedFor === alias) return;
        optionsLoadedFor = alias;

        var request = dbSel ? get('Databases', { alias: alias }) : get('Schemas', { alias: alias });
        request.then(function (items) {
            var keep = select.value;
            select.innerHTML = '';
            var first = document.createElement('option');
            first.value = '';
            first.textContent = dbSel ? '(predeterminada)' : '(el del usuario)';
            select.appendChild(first);
            items.forEach(function (item) {
                var option = document.createElement('option');
                option.value = item.name;
                option.textContent = item.name + (item.isSystem ? ' (sistema)' : '');
                select.appendChild(option);
            });
            ensureOption(select, keep);
            select.value = keep || '';
        }).catch(function () { optionsLoadedFor = null; /* el detalle del error ya sale al expandir el árbol */ });
    }

    [dbSel, schemaSel].forEach(function (select) {
        if (!select) return;
        select.addEventListener('focus', ensureContextOptions);
        select.addEventListener('mousedown', ensureContextOptions);
    });

    function onAliasChanged() {
        optionsLoadedFor = null;
        if (dbSel) dbSel.value = '';
        if (schemaSel) schemaSel.value = '';
        updateDescription();
        contextChanged();
    }

    // ---------- contexto por pestaña (como cada ventana de consulta de SSMS) ----------
    // Cada pestaña del editor tiene SU conexión, base y esquema: al cambiar de pestaña los selectores muestran los de
    // ella, y lo que el usuario elige aquí solo cambia la pestaña activa. Una pestaña sin contexto hereda el actual.

    var applying = false;    // mientras se aplica el contexto de una pestaña, los cambios no son "del usuario"
    var lastActiveId = null;

    function currentContext() {
        return { alias: aliasSel.value, database: dbSel ? dbSel.value : null, schema: schemaSel ? schemaSel.value : null };
    }

    function contextChanged() {
        if (applying) return;
        if (QC.editor && QC.editor.setActiveContext) QC.editor.setActiveContext(currentContext());
        rememberContext();
    }

    function applyContext(ctx) {
        var known = Array.prototype.some.call(aliasSel.options, function (o) { return o.value === ctx.alias; });
        if (!known) return false;   // la conexión no existe en esta máquina: se queda la actual

        applying = true;
        try {
            if (aliasSel.value !== ctx.alias) {
                aliasSel.value = ctx.alias;
                optionsLoadedFor = null;
                updateDescription();
            }
            setDatabase(ctx.database || '');
            setSchema(ctx.schema || '');
        } finally {
            applying = false;
        }

        rememberContext();
        return true;
    }

    function followActiveTab(info) {
        if (info.activeId === lastActiveId) return;
        lastActiveId = info.activeId;

        var ctx = QC.editor.activeContext();
        if (!ctx || !applyContext(ctx)) QC.editor.setActiveContext(currentContext());
    }

    // La conexión elegida (y su base o esquema) también se recuerda para la consola en db-workspace.json: es con la que
    // arrancan las pestañas nuevas al volver de la otra consola u otra computadora. Si falla no pasa nada: es una comodidad.
    function rememberContext() {
        if (!aliasSel.value) return;
        post('SaveWorkspace', {
            context: true,
            alias: aliasSel.value,
            database: dbSel ? dbSel.value : null,
            schema: schemaSel ? schemaSel.value : null
        }).catch(function () { });
    }

    [dbSel, schemaSel].forEach(function (select) {
        if (select) select.addEventListener('change', contextChanged);
    });

    var connectionsCache = [];

    function updateDescription() {
        var current = connectionsCache.filter(function (c) { return c.alias === aliasSel.value; })[0];
        descriptionBox.textContent = current ? current.description : '';

        // Aviso rojo cuando la conexión elegida permite editar filas desde la grilla.
        var badge = $('write-badge');
        if (badge) badge.hidden = !(current && current.allowWrite);
    }

    aliasSel.addEventListener('change', onAliasChanged);

    // ---------- nombres y plantillas ----------

    function oraName(name) {
        return /^[A-Z][A-Z0-9_$#]*$/.test(name) ? name : '"' + name.replace(/"/g, '""') + '"';
    }

    function qualified(obj) {
        if (kind === 'sqlserver') {
            return '[' + obj.schema.replace(/]/g, ']]') + '].[' + obj.name.replace(/]/g, ']]') + ']';
        }
        return oraName(obj.schema) + '.' + oraName(obj.name);
    }

    function selectTemplate(obj) {
        return kind === 'sqlserver'
            ? 'SELECT TOP 100 * FROM ' + qualified(obj)
            : 'SELECT * FROM ' + qualified(obj) + ' WHERE ROWNUM <= 100';
    }

    function runSelect(obj) {
        applyObjectContext(obj);
        QC.editor.showQuery(selectTemplate(obj));
        form.requestSubmit(runButton);
    }

    function applyObjectContext(obj) {
        setAlias(obj.alias);
        if (dbSel) setDatabase(obj.database);
        if (schemaSel) setSchema(obj.schema);
    }

    function insertAtCursor(text) { QC.editor.insertAtCursor(text); }

    // ---------- árbol ----------

    function objectNode(base, name) {
        var obj = { alias: base.alias, database: base.database, schema: base.schema, kind: base.kind, name: name };
        var groups = GROUPS[obj.kind] || [];

        return node({
            icon: KIND_ICONS[obj.kind],
            label: name,
            expandable: groups.length > 0,
            onSelect: function () { showDetail(obj); },
            onDblClick: QUERYABLE[obj.kind] ? function () { runSelect(obj); } : null,
            load: function () {
                return Promise.resolve(groups.map(function (group) {
                    return node({
                        label: GROUP_LABELS[group],
                        expandable: true,
                        load: function () {
                            return get('Group', { alias: obj.alias, database: obj.database, schema: obj.schema, kind: obj.kind, name: obj.name, group: group })
                                .then(function (items) {
                                    return items.map(function (item) {
                                        return node({
                                            label: item.name,
                                            detail: item.detail,
                                            onDblClick: group === 'Columns' ? function () { insertAtCursor(item.name); } : null
                                        });
                                    });
                                });
                        }
                    });
                }));
            }
        });
    }

    function kindFolder(base, kindName) {
        return node({
            icon: '📁',
            label: KIND_LABELS[kindName] || kindName,
            expandable: true,
            load: function () {
                return get('Objects', { alias: base.alias, database: base.database, schema: base.schema, kind: kindName }).then(function (page) {
                    var nodes = page.items.map(function (item) {
                        return objectNode({ alias: base.alias, database: base.database, schema: base.schema, kind: kindName }, item.name);
                    });
                    if (page.truncated) nodes.push(muted('(hay más: se muestran los primeros ' + page.items.length + '; usa el buscador)'));
                    return nodes;
                });
            }
        });
    }

    function schemaNode(alias, database, schema) {
        return node({
            icon: '🗂',
            label: schema.name,
            detail: schema.isCurrent ? '(actual)' : (schema.isSystem ? 'sistema' : ''),
            expandable: true,
            onSelect: function () {
                setAlias(alias);
                if (dbSel) setDatabase(database);
                if (schemaSel) setSchema(schema.name);
            },
            load: function () {
                return Promise.resolve(kinds.map(function (kindName) {
                    return kindFolder({ alias: alias, database: database, schema: schema.name }, kindName);
                }));
            }
        });
    }

    function databaseNode(alias, database) {
        return node({
            icon: '🛢',
            label: database.name,
            expandable: true,
            onSelect: function () { setAlias(alias); setDatabase(database.name); },
            load: function () {
                return get('Schemas', { alias: alias, database: database.name }).then(function (schemas) {
                    return schemas.map(function (schema) { return schemaNode(alias, database.name, schema); });
                });
            }
        });
    }

    function connectionNode(connection) {
        var actions = [];

        if (connection.editable) {
            actions.push({ text: '✎', title: 'Editar la conexión', run: function () { openModal(connection.alias); } });
            actions.push({ text: '🗑', title: 'Borrar la conexión', run: function () { deleteConnection(connection.alias); } });
        }

        return node({
            icon: '🖥',
            label: connection.alias,
            badge: connection.origin === 'env' ? '.env' : '',
            detail: connection.description,
            expandable: true,
            actions: actions,
            onSelect: function () { setAlias(connection.alias); if (dbSel) setDatabase(''); if (schemaSel) setSchema(''); },
            load: function () {
                if (hasDatabases) {
                    return get('Databases', { alias: connection.alias }).then(function (databases) {
                        var user = databases.filter(function (d) { return !d.isSystem; });
                        var system = databases.filter(function (d) { return d.isSystem; });
                        var nodes = user.map(function (d) { return databaseNode(connection.alias, d); });

                        if (system.length) {
                            nodes.push(node({
                                icon: '📁',
                                label: 'Bases del sistema',
                                expandable: true,
                                load: function () { return Promise.resolve(system.map(function (d) { return databaseNode(connection.alias, d); })); }
                            }));
                        }

                        return nodes;
                    });
                }

                return get('Schemas', { alias: connection.alias, system: showSystem && showSystem.checked ? 'true' : 'false' }).then(function (schemas) {
                    return schemas.map(function (schema) { return schemaNode(connection.alias, '', schema); });
                });
            }
        });
    }

    function renderTree(connections) {
        connectionsCache = connections;
        treeBox.innerHTML = '';
        QC.clearSelection();

        if (!connections.length) {
            var empty = el('div', 'text-muted');
            empty.textContent = 'No hay conexiones. Usa "＋ Conexión" para agregar una' + (kind === 'oracle' ? ' (o define ORACLE_CONN_<NOMBRE> en el .env).' : ' (o define SQLSERVER_CONN_<NOMBRE> en el .env).');
            treeBox.appendChild(empty);
            return;
        }

        connections.forEach(function (connection) { treeBox.appendChild(connectionNode(connection)); });
    }

    // Recarga la lista de conexiones (select del editor + árbol) sin recargar la página.
    function refreshConnections(selectAlias) {
        return get('Connections').then(function (connections) {
            var keep = selectAlias || aliasSel.value;

            aliasSel.innerHTML = '';
            connections.forEach(function (connection) {
                var option = document.createElement('option');
                option.value = connection.alias;
                option.textContent = connection.alias;
                aliasSel.appendChild(option);
            });

            var exists = connections.some(function (c) { return c.alias === keep; });
            var changed = aliasSel.value !== (exists ? keep : (connections[0] ? connections[0].alias : ''));
            aliasSel.value = exists ? keep : (connections[0] ? connections[0].alias : '');
            if (changed) onAliasChanged();

            renderTree(connections);
            updateDescription();
        }).catch(function (error) {
            treeBox.innerHTML = '';
            treeBox.appendChild(el('div', 'text-danger', error.message));
        });
    }

    $('refresh').addEventListener('click', function () { optionsLoadedFor = null; refreshConnections(); });
    if (showSystem) showSystem.addEventListener('change', function () { refreshConnections(); });

    // ---------- panel de detalle ----------

    function showDetail(obj) {
        applyObjectContext(obj);

        detailBox.innerHTML = '';
        detailBox.appendChild(el('div', 'fw-semibold', (KIND_ICONS[obj.kind] || '') + ' ' + obj.schema + '.' + obj.name));
        detailBox.appendChild(el('div', 'text-muted', (KIND_SINGULAR[obj.kind] || obj.kind) + (obj.database ? ' · base ' + obj.database : '')));

        var buttons = el('div', 'd-flex gap-1 flex-wrap my-1');

        if (QUERYABLE[obj.kind]) {
            var select = el('button', 'btn btn-primary btn-sm py-0', '▶ SELECT 100');
            select.type = 'button';
            select.addEventListener('click', function () { runSelect(obj); });
            buttons.appendChild(select);
        }

        var definition = el('button', 'btn btn-outline-secondary btn-sm py-0', '📄 Definición');
        definition.type = 'button';
        buttons.appendChild(definition);

        var copy = el('button', 'btn btn-outline-secondary btn-sm py-0', 'Copiar nombre');
        copy.type = 'button';
        copy.addEventListener('click', function () {
            navigator.clipboard.writeText(qualified(obj)).then(function () { copy.textContent = '✓ Copiado'; setTimeout(function () { copy.textContent = 'Copiar nombre'; }, 1200); });
        });
        buttons.appendChild(copy);
        detailBox.appendChild(buttons);

        var output = el('div');
        detailBox.appendChild(output);

        definition.addEventListener('click', function () {
            output.innerHTML = '';
            output.appendChild(muted('Cargando...'));

            get('Definition', { alias: obj.alias, database: obj.database, schema: obj.schema, kind: obj.kind, name: obj.name }).then(function (result) {
                output.innerHTML = '';

                if (!result.text) {
                    output.appendChild(muted(obj.kind === 'Table' && kind === 'sqlserver'
                        ? 'SQL Server no guarda el DDL de una tabla: expande el nodo para ver sus columnas, índices y claves.'
                        : 'No hay definición disponible (está cifrada o no tienes permiso para verla).'));
                    return;
                }

                var pre = el('pre', 'qc-definition border rounded p-2 mb-1', result.text);
                output.appendChild(pre);

                var copyText = el('button', 'btn btn-outline-secondary btn-sm py-0', 'Copiar definición');
                copyText.type = 'button';
                copyText.addEventListener('click', function () { navigator.clipboard.writeText(result.text); });
                output.appendChild(copyText);
            }).catch(function (error) {
                output.innerHTML = '';
                output.appendChild(el('div', 'text-danger', error.message));
            });
        });
    }

    // ---------- búsqueda por nombre ----------

    var filterTimer = null;

    filter.addEventListener('input', function () {
        clearTimeout(filterTimer);
        filterTimer = setTimeout(search, 350);
    });

    function search() {
        var term = filter.value.trim();

        if (term.length < 2) {
            resultsBox.hidden = true;
            treeBox.hidden = false;
            return;
        }

        if (!currentAlias()) return;

        treeBox.hidden = true;
        resultsBox.hidden = false;
        resultsBox.innerHTML = '';
        resultsBox.appendChild(muted('Buscando...'));

        get('Search', { alias: currentAlias(), database: currentDatabase(), filter: term }).then(function (page) {
            resultsBox.innerHTML = '';

            if (!page.items.length) {
                resultsBox.appendChild(muted('Sin coincidencias.'));
                return;
            }

            page.items.forEach(function (item) {
                var obj = { alias: currentAlias(), database: currentDatabase(), schema: item.schema, kind: item.kind, name: item.name };
                var row = node({
                    icon: KIND_ICONS[item.kind],
                    label: item.schema + '.' + item.name,
                    detail: KIND_SINGULAR[item.kind],
                    onSelect: function () { showDetail(obj); },
                    onDblClick: QUERYABLE[item.kind] ? function () { runSelect(obj); } : null
                });
                resultsBox.appendChild(row);
            });

            if (page.truncated) resultsBox.appendChild(muted('(hay más coincidencias: afina la búsqueda)'));
        }).catch(function (error) {
            resultsBox.innerHTML = '';
            resultsBox.appendChild(el('div', 'text-danger', error.message));
        });
    }

    // ---------- diálogo de conexión ----------

    var modalEl = $('modal');
    var modal = new bootstrap.Modal(modalEl);
    var fields = {};
    modalEl.querySelectorAll('[data-field]').forEach(function (field) { fields[field.dataset.field] = field; });
    var modalMsg = $('modal-msg');
    var editingAlias = null;

    function groupEls(name) { return modalEl.querySelectorAll('[data-group="' + name + '"]'); }

    function syncModal() {
        if (kind === 'oracle') {
            var mode = fields.oracleMode.value;
            groupEls('host').forEach(function (g) { g.hidden = mode === 'Tns'; });
            $('modal').querySelector('[data-role="oracle-target-label"]').textContent =
                mode === 'Tns' ? 'Alias de tnsnames.ora' : (mode === 'Sid' ? 'SID' : 'Nombre del servicio');
        } else {
            groupEls('credentials').forEach(function (g) { g.hidden = fields.auth.value === 'windows'; });
        }
    }

    Object.keys(fields).forEach(function (name) {
        if (name === 'oracleMode' || name === 'auth') fields[name].addEventListener('change', syncModal);
    });

    function showModalMessage(text, ok) {
        modalMsg.hidden = false;
        modalMsg.className = 'alert mt-2 mb-0 py-2 small ' + (ok ? 'alert-success' : 'alert-danger');
        modalMsg.textContent = text;
    }

    function resetModal() {
        Object.keys(fields).forEach(function (name) {
            var field = fields[name];
            if (field.type === 'checkbox') field.checked = field.dataset.field !== 'allowWrite';   // "confiar en el certificado" arranca prendido; "permitir editar datos", apagado
            else if (field.tagName === 'SELECT') field.selectedIndex = 0;
            else field.value = '';
        });
        fields.alias.disabled = false;
        modalMsg.hidden = true;
        $('password-hint').hidden = true;
    }

    function openModal(alias) {
        resetModal();
        editingAlias = alias || null;
        $('modal-title').textContent = alias ? 'Editar conexión ' + alias : 'Nueva conexión ' + (kind === 'oracle' ? 'Oracle' : 'SQL Server');

        if (!alias) {
            syncModal();
            modal.show();
            return;
        }

        get('Connection', { alias: alias }).then(function (c) {
            fields.alias.value = c.alias;
            fields.alias.disabled = true;
            fields.host.value = c.host || '';
            fields.port.value = c.port || '';
            fields.user.value = c.user || '';
            fields.allowWrite.checked = !!c.allowWrite;
            if (kind === 'oracle') {
                fields.oracleMode.value = c.oracleMode || 'ServiceName';
                fields.oracleTarget.value = c.oracleTarget || '';
            } else {
                fields.database.value = c.database || '';
                fields.auth.value = c.windowsAuth ? 'windows' : 'sql';
                fields.trust.checked = c.trustServerCertificate;
            }
            $('password-hint').hidden = !c.hasPassword;
            syncModal();
            modal.show();
        }).catch(function (error) { alert(error.message); });
    }

    function collect() {
        var data = {
            alias: fields.alias.value.trim(),
            isEdit: !!editingAlias,
            host: fields.host.value.trim(),
            port: fields.port.value ? parseInt(fields.port.value, 10) : null,
            user: fields.user.value.trim(),
            password: fields.password.value,
            allowWrite: fields.allowWrite.checked
        };

        if (kind === 'oracle') {
            data.oracleMode = fields.oracleMode.value;
            data.oracleTarget = fields.oracleTarget.value.trim();
        } else {
            data.database = fields.database.value.trim();
            data.windowsAuth = fields.auth.value === 'windows';
            data.trustServerCertificate = fields.trust.checked;
        }

        return data;
    }

    $('new-connection').addEventListener('click', function () { openModal(null); });

    $('modal-test').addEventListener('click', function () {
        showModalMessage('Probando la conexión...', true);
        post('TestConnection', collect()).then(function (result) {
            showModalMessage(result.message, result.ok);
        }).catch(function (error) { showModalMessage(error.message, false); });
    });

    $('modal-save').addEventListener('click', function () {
        var data = collect();

        if (data.allowWrite && !confirm('Vas a permitir editar filas con la conexión "' + data.alias + '".\n\n' +
            'Los cambios se hacen sobre datos reales (con vista previa, confirmación y bitácora). ¿Seguro que no es producción y que el usuario tiene el permiso mínimo necesario?')) return;

        post('SaveConnection', data).then(function () {
            modal.hide();
            return refreshConnections(data.alias);
        }).catch(function (error) { showModalMessage(error.message, false); });
    });

    function deleteConnection(alias) {
        if (!confirm('¿Borrar la conexión "' + alias + '"? Se elimina de este equipo (no toca la base de datos).')) return;

        post('DeleteConnection', { alias: alias }).then(function () { return refreshConnections(); })
            .catch(function (error) { alert(error.message); });
    }

    // ---------- historial ----------

    var historyKey = 'qc-history-' + kind;

    function renderHistory() {
        var items = readJson(historyKey, []);
        history.innerHTML = '<option value="">Historial...</option>';
        items.forEach(function (item, index) {
            var option = document.createElement('option');
            option.value = String(index);
            option.textContent = item.replace(/s+/g, ' ').slice(0, 70);
            option.title = item;
            history.appendChild(option);
        });
    }

    history.addEventListener('change', function () {
        if (history.value === '') return;
        var items = readJson(historyKey, []);
        if (items[Number(history.value)]) QC.editor.showQuery(items[Number(history.value)]);
        history.value = '';
        QC.editor.focus();
    });

    // El editor (query-editor.js) llena el campo oculto "Sql" en su propio submit (capturando primero): acá se recuerda lo que se ejecuta.
    form.addEventListener('submit', function () {
        var text = QC.editor.getRunText().trim();
        if (!text) return;
        var items = readJson(historyKey, []).filter(function (item) { return item !== text; });
        items.unshift(text);
        writeJson(historyKey, items.slice(0, 25));
    });

    renderHistory();

    // ---------- arranque ----------

    // Recién con la lista de conexiones cargada se puede aplicar la de la pestaña activa (y seguir los cambios de pestaña).
    refreshConnections(root.dataset.selectedAlias).then(function () {
        if (QC.editor && QC.editor.onTabsChanged) QC.editor.onTabsChanged(followActiveTab);
    });
})();
