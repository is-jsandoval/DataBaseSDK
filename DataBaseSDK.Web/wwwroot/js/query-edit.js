// Edición de filas desde la grilla de resultados (consolas Oracle / SQL Server).
//
// Flujo: doble clic en una celda → se edita → queda marcada en naranja (nada se guarda todavía) → "Revisar y confirmar…"
// muestra antes → después de cada celda → "Probar" (hace todo y revierte) o "Confirmar y guardar".
// El navegador NO decide qué se puede editar: el servidor analiza el SELECT, lee la estructura de la tabla y entrega en
// data-edit las columnas editables y la clave primaria. Al guardar, el servidor vuelve a validarlo todo, verifica que cada fila
// siga como la viste y la actualiza por clave primaria dentro de una transacción (ver RowEditorBase.cs).
//
// Este archivo tiene dos partes: la lógica de validación y armado del pedido (sin DOM, probada con `node --test`, ver js-tests/)
// y la interfaz, que solo se arma en el navegador.
(function (root) {
    'use strict';

    // ======================= Lógica pura (sin DOM) =======================

    var MAX_CELL_CHARS = 4000;

    var PATTERNS = {
        Integer: /^[+-]?\d+$/,
        Decimal: /^[+-]?(\d+\.?\d*|\.\d+)([eE][+-]?\d+)?$/,
        DateTime: /^\d{4}-\d{2}-\d{2}([ T]\d{2}:\d{2}(:\d{2}(\.\d{1,7})?)?)?$/,
        Bool: /^(true|false|1|0)$/i,
        Guid: /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
    };

    var HINTS = {
        Integer: 'un número entero, ej. 125',
        Decimal: 'un número con punto decimal, ej. 1250.50',
        DateTime: 'una fecha: 2026-10-26 o 2026-10-26 14:30:00',
        Bool: 'true, false, 1 o 0',
        Guid: 'un GUID, ej. 3f2504e0-4f89-11d3-9a0c-0305e82c3301'
    };

    // El texto de la grilla que salió recortado (4000 caracteres + "…"): editarlo borraría el resto del valor.
    function isTruncated(text) {
        return typeof text === 'string' && text.length === MAX_CELL_CHARS + 1 && text.charAt(text.length - 1) === '…';
    }

    // null si el texto sirve para esa columna; si no, el motivo. El servidor repite esta validación (acá es solo para avisar al instante).
    function validateInput(column, text) {
        if (text === null) return column.nullable ? null : 'La columna ' + column.name + ' no admite NULL.';
        var pattern = PATTERNS[column.category];
        if (!pattern) return null;
        return pattern.test(text.trim()) ? null : 'Se esperaba ' + HINTS[column.category] + '.';
    }

    function findColumn(info, name) {
        var lower = String(name).toLowerCase();
        for (var i = 0; i < info.columns.length; i++) {
            if (info.columns[i].name.toLowerCase() === lower) return i;
        }
        return -1;
    }

    // "ID = 5, SUC = 'A'": cómo se nombra una fila en el diálogo.
    function keyText(key) {
        return Object.keys(key).map(function (name) { return name + ' = ' + key[name]; }).join(', ');
    }

    function describeValue(value) {
        if (value === null) return '(null)';
        if (value === '') return '(vacío)';
        return value.length > 120 ? value.slice(0, 120) + '…' : value;
    }

    // rows: [{ key: {PK: valor}, cells: { columna: { original, value } } }] → cuerpo del POST ApplyEdits (solo celdas realmente cambiadas).
    function buildRequest(info, rows, dryRun) {
        var changes = [];

        rows.forEach(function (row) {
            var set = {};
            var any = false;

            Object.keys(row.cells).forEach(function (name) {
                var cell = row.cells[name];
                if (cell.original === cell.value) return;
                set[name] = { original: cell.original, value: cell.value };
                any = true;
            });

            if (any) changes.push({ primaryKey: row.key, set: set });
        });

        return { alias: info.alias, database: info.database || null, schema: info.schema, table: info.table, dryRun: !!dryRun, changes: changes };
    }

    var api = {
        isTruncated: isTruncated, validateInput: validateInput, findColumn: findColumn,
        keyText: keyText, describeValue: describeValue, buildRequest: buildRequest
    };

    if (typeof module !== 'undefined' && module.exports) module.exports = api;
    if (typeof window === 'undefined' || typeof document === 'undefined') return;

    // ======================= Interfaz =======================

    var grid = document.querySelector('[data-role="results-grid"][data-edit]');
    if (!grid || !window.QC) return;

    var info;
    try { info = JSON.parse(grid.dataset.edit); } catch (e) { return; }

    var QC = window.QC;
    var pkIndexes = info.primaryKey.map(function (name) { return findColumn(info, name); });

    // Estado: por fila (<tr>) → { columna → { original, value } }. "original" es lo que el servidor devolvió, no lo que se ve.
    var pending = new Map();
    var editing = null;

    function $(role) { return document.querySelector('[data-role="' + role + '"]'); }

    function columnOf(td) { return info.columns[td.cellIndex - 1]; }

    // Valor tal como lo devolvió la consulta (null = NULL).
    function readOriginal(td) {
        if (td.dataset.origNull === '1') return null;
        if (td.dataset.orig !== undefined) return td.dataset.orig;
        return td.classList.contains('qc-null') ? null : td.textContent;
    }

    function rememberOriginal(td) {
        if (td.dataset.orig !== undefined || td.dataset.origNull === '1') return;
        var value = readOriginal(td);
        if (value === null) td.dataset.origNull = '1';
        else td.dataset.orig = value;
    }

    function paint(td, value) {
        td.textContent = value === null ? '(null)' : value;
        td.title = value === null ? '' : value;
        td.classList.toggle('qc-null', value === null);
        td.classList.toggle('text-muted', value === null);
        td.classList.toggle('fst-italic', value === null);
    }

    function rowKey(tr) {
        var key = {};
        pkIndexes.forEach(function (index) {
            var td = tr.cells[index + 1];
            key[info.columns[index].name] = readOriginal(td);
        });
        return key;
    }

    function markEditable() {
        var rows = grid.querySelectorAll('tbody tr');

        rows.forEach(function (tr) {
            info.columns.forEach(function (column, index) {
                var td = tr.cells[index + 1];
                if (!td || !column.editable) return;
                if (isTruncated(td.textContent)) return;     // recortado en pantalla: no se edita
                td.classList.add('qc-editable', 'qc-cell');
            });
        });
    }

    // ---------- edición de una celda ----------

    function closeEditor(accept) {
        if (!editing) return;
        var state = editing;
        editing = null;

        var td = state.td;
        var value = state.value;

        if (accept) {
            var problem = validateInput(state.column, value);

            if (problem) {
                QC.toast(problem, 'warning');
                editing = state;
                state.input.focus();
                return;
            }

            record(td, value);
        } else {
            repaint(td);
        }
    }

    // Vuelve a dibujar la celda según el estado (pendiente o no).
    function repaint(td) {
        td.classList.remove('qc-editing');
        var original = readOriginal(td);
        var cell = cellState(td);
        paint(td, cell ? cell.value : original);
        td.classList.toggle('qc-changed', !!cell);
        td.title = cell ? 'Antes: ' + describeValue(cell.original) : (original === null ? '' : original);
    }

    function cellState(td) {
        var cells = pending.get(td.parentNode);
        return cells ? cells[columnOf(td).name] || null : null;
    }

    function record(td, value) {
        var tr = td.parentNode;
        var name = columnOf(td).name;
        var original = readOriginal(td);
        var cells = pending.get(tr) || {};

        if (value === original) {
            delete cells[name];
        } else {
            cells[name] = { original: original, value: value };
        }

        if (Object.keys(cells).length) pending.set(tr, cells);
        else pending.delete(tr);

        repaint(td);
        refreshBar();
    }

    function openEditor(td) {
        if (editing) closeEditor(true);
        if (editing) return;                          // el valor anterior no era válido: sigue abierto

        var column = columnOf(td);
        rememberOriginal(td);

        var cell = cellState(td);
        var current = cell ? cell.value : readOriginal(td);

        var box = document.createElement('div');
        box.className = 'qc-cell-editor';

        var input = document.createElement('input');
        input.type = 'text';
        input.value = current === null ? '' : current;
        input.placeholder = current === null ? '(null)' : '';
        input.spellcheck = false;
        input.autocomplete = 'off';
        input.title = column.name + ' · ' + column.category + (column.nullable ? ' · admite NULL' : ' · NOT NULL');

        var nullButton = document.createElement('button');
        nullButton.type = 'button';
        nullButton.className = 'btn btn-outline-secondary';
        nullButton.textContent = '∅';
        nullButton.title = column.nullable ? 'Poner NULL' : 'Esta columna no admite NULL';
        nullButton.disabled = !column.nullable;

        var okButton = document.createElement('button');
        okButton.type = 'button';
        okButton.className = 'btn btn-success';
        okButton.textContent = '✓';
        okButton.title = 'Aceptar (Enter)';

        var cancelButton = document.createElement('button');
        cancelButton.type = 'button';
        cancelButton.className = 'btn btn-outline-secondary';
        cancelButton.textContent = '✕';
        cancelButton.title = 'Cancelar (Esc)';

        [input, nullButton, okButton, cancelButton].forEach(function (node) { box.appendChild(node); });

        // Los botones no le quitan el foco al campo: así el "salir de la celda = aceptar" no se adelanta a "Cancelar" ni a "NULL".
        [nullButton, okButton, cancelButton].forEach(function (button) {
            button.addEventListener('mousedown', function (event) { event.preventDefault(); });
        });

        td.textContent = '';
        td.appendChild(box);
        td.classList.add('qc-editing');

        var state = { td: td, column: column, input: input, value: current };
        editing = state;

        input.addEventListener('input', function () { state.value = input.value; });
        input.addEventListener('keydown', function (event) {
            if (event.key === 'Enter') { event.preventDefault(); closeEditor(true); }
            else if (event.key === 'Escape') { event.preventDefault(); closeEditor(false); }
        });

        // Al salir de la celda se acepta (como en una grilla de escritorio), salvo que el foco pase a uno de sus botones.
        input.addEventListener('blur', function (event) {
            if (event.relatedTarget && box.contains(event.relatedTarget)) return;
            if (editing === state) closeEditor(true);
        });

        nullButton.addEventListener('click', function () { state.value = null; closeEditor(true); });
        okButton.addEventListener('click', function () { closeEditor(true); });
        cancelButton.addEventListener('click', function () { closeEditor(false); });

        input.focus();
        input.select();
    }

    grid.addEventListener('dblclick', function (event) {
        var td = event.target.closest ? event.target.closest('td.qc-editable') : null;
        if (!td || td.classList.contains('qc-editing')) return;
        event.preventDefault();
        openEditor(td);
    });

    // ---------- barra de cambios pendientes ----------

    var bar = $('edit-bar');
    var barCount = $('edit-count');

    function pendingRows() {
        var rows = [];
        pending.forEach(function (cells, tr) { rows.push({ tr: tr, key: rowKey(tr), cells: cells }); });
        return rows;
    }

    function pendingCellCount() {
        var count = 0;
        pending.forEach(function (cells) { count += Object.keys(cells).length; });
        return count;
    }

    function refreshBar() {
        var cellCount = pendingCellCount();
        bar.hidden = cellCount === 0;
        barCount.textContent = cellCount + ' cambio(s) sin guardar en ' + pending.size + ' fila(s)';
    }

    function discardAll() {
        var cells = [];
        pending.forEach(function (_, tr) {
            Array.prototype.forEach.call(tr.querySelectorAll('td.qc-changed'), function (td) { cells.push(td); });
        });

        pending.clear();
        cells.forEach(repaint);
        refreshBar();
    }

    $('edit-discard').addEventListener('click', function () {
        if (pendingCellCount() && !confirm('¿Descartar los ' + pendingCellCount() + ' cambio(s) sin guardar?')) return;
        discardAll();
    });

    window.addEventListener('beforeunload', function (event) {
        if (!pendingCellCount()) return;
        event.preventDefault();
        event.returnValue = '';
    });

    // ---------- diálogo de confirmación ----------

    var modalEl = $('edit-modal');
    var modal = new bootstrap.Modal(modalEl);
    var ack = $('edit-modal-ack');
    var applyButton = $('edit-modal-apply');
    var testButton = $('edit-modal-test');
    var msg = $('edit-modal-msg');
    var busy = false;

    function showMessage(text, type) {
        msg.hidden = false;
        msg.className = 'alert mt-2 mb-0 py-2 small alert-' + type;
        msg.textContent = text;
    }

    function syncButtons() {
        applyButton.disabled = busy || !ack.checked || pendingCellCount() === 0;
        testButton.disabled = busy || pendingCellCount() === 0;
    }

    ack.addEventListener('change', syncButtons);

    function renderDiff() {
        var diff = $('edit-modal-diff');
        diff.textContent = '';

        var table = document.createElement('table');
        var head = table.createTHead().insertRow();
        ['Fila (clave primaria)', 'Columna', 'Antes', 'Después'].forEach(function (title) {
            var th = document.createElement('th');
            th.textContent = title;
            head.appendChild(th);
        });

        var body = table.createTBody();

        pendingRows().forEach(function (row) {
            var names = Object.keys(row.cells);

            names.forEach(function (name, index) {
                var tr = body.insertRow();
                var keyCell = tr.insertCell();
                keyCell.className = 'qc-diff-key';
                keyCell.textContent = index === 0 ? keyText(row.key) : '';

                tr.insertCell().textContent = name;

                var from = tr.insertCell();
                from.className = 'qc-from';
                from.textContent = describeValue(row.cells[name].original);

                var to = tr.insertCell();
                to.className = 'qc-to';
                to.textContent = describeValue(row.cells[name].value);
            });
        });

        diff.appendChild(table);
    }

    $('edit-review').addEventListener('click', function () {
        if (editing) closeEditor(true);
        if (editing || !pendingCellCount()) return;

        $('edit-modal-table').textContent = info.schema + '.' + info.table;
        $('edit-modal-alias').textContent = info.alias;
        $('edit-modal-alias2').textContent = info.alias;
        ack.checked = false;
        msg.hidden = true;
        busy = false;
        renderDiff();
        syncButtons();
        modal.show();
    });

    function send(dryRun) {
        var rows = pendingRows().map(function (row) { return { key: row.key, cells: row.cells }; });
        busy = true;
        syncButtons();
        showMessage(dryRun ? 'Probando (no se guarda nada)...' : 'Guardando...', 'secondary');

        return QC.post('ApplyEdits', buildRequest(info, rows, dryRun)).then(function (result) {
            busy = false;
            return result;
        }, function (error) {
            busy = false;
            syncButtons();
            throw error;
        });
    }

    testButton.addEventListener('click', function () {
        send(true).then(function (result) {
            showMessage('Prueba correcta: se podrían guardar ' + result.rowsAffected + ' fila(s). No se guardó nada.', 'success');
            syncButtons();
        }).catch(function (error) { showMessage(explain(error), 'danger'); });
    });

    applyButton.addEventListener('click', function () {
        send(false).then(function (result) {
            // Lo guardado pasa a ser el nuevo "original": la grilla queda al día sin volver a consultar.
            var saved = [];

            pending.forEach(function (cells, tr) {
                Object.keys(cells).forEach(function (name) {
                    var td = tr.cells[findColumn(info, name) + 1];
                    var value = cells[name].value;

                    if (value === null) { td.dataset.origNull = '1'; delete td.dataset.orig; }
                    else { td.dataset.orig = value; delete td.dataset.origNull; }

                    saved.push(td);
                });
            });

            pending.clear();

            saved.forEach(function (td) {
                repaint(td);
                td.classList.add('qc-saved');
            });

            refreshBar();
            modal.hide();
            QC.toast(result.rowsAffected + ' fila(s) actualizada(s) en ' + info.table + '. Quedó registro en .local\\data-edits.log.', 'success');
        }).catch(function (error) { showMessage(explain(error), 'danger'); });
    });

    function explain(error) {
        var text = error.message;

        switch (error.code) {
            case 'write_not_allowed':
                return text + '\n\nActiva "Permitir editar datos" en Editar conexión.';
            default:
                return text;
        }
    }

    markEditable();
    refreshBar();
})(this);
