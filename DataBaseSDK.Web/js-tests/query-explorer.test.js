// Pruebas de la lógica del explorador de archivos (sin DOM). Se corren con:  node --test DataBaseSDK.Web/js-tests/query-explorer.test.js (pasar el archivo, no la carpeta)
'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const X = require('../wwwroot/js/query-explorer.js');

test('normalizePath: separador de Windows, sin barras al final (salvo la raíz de una unidad)', () => {
    assert.equal(X.normalizePath('C:/scripts/ventas/'), 'C:\\scripts\\ventas');
    assert.equal(X.normalizePath('C:\\scripts\\\\'), 'C:\\scripts');
    assert.equal(X.normalizePath('C:\\'), 'C:\\');
    assert.equal(X.normalizePath('\\\\servidor\\compartida\\'), '\\\\servidor\\compartida');
    assert.equal(X.normalizePath(''), '');
    assert.equal(X.normalizePath(null), '');
});

test('isInside: dentro a cualquier profundidad, sin distinguir mayúsculas, nunca la propia carpeta ni una "vecina" con el mismo prefijo', () => {
    assert.equal(X.isInside('C:\\scripts', 'C:\\scripts\\a.sql'), true);
    assert.equal(X.isInside('C:\\scripts', 'c:\\SCRIPTS\\ventas\\2026\\a.sql'), true);
    assert.equal(X.isInside('C:\\scripts\\', 'C:/scripts/a.sql'), true);
    assert.equal(X.isInside('C:\\scripts', 'C:\\scripts'), false);
    assert.equal(X.isInside('C:\\scripts', 'C:\\scripts2\\a.sql'), false);       // "scripts2" no es "scripts"
    assert.equal(X.isInside('C:\\scripts', 'D:\\scripts\\a.sql'), false);
    assert.equal(X.isInside('C:\\', 'C:\\a.sql'), true);                          // raíz de una unidad
    assert.equal(X.isInside('', 'C:\\a.sql'), false);
    assert.equal(X.isInside('C:\\scripts', ''), false);
});

test('relativeSegments: las partes por debajo de la carpeta, con su texto original', () => {
    assert.deepEqual(X.relativeSegments('C:\\scripts', 'C:\\Scripts\\Ventas\\2026\\Reporte.sql'), ['Ventas', '2026', 'Reporte.sql']);
    assert.deepEqual(X.relativeSegments('C:\\scripts', 'C:\\scripts\\a.sql'), ['a.sql']);
    assert.deepEqual(X.relativeSegments('C:\\', 'C:\\a.sql'), ['a.sql']);
    assert.equal(X.relativeSegments('C:\\scripts', 'C:\\otro\\a.sql'), null);
    assert.equal(X.relativeSegments('C:\\scripts', 'C:\\scripts'), null);
});

test('folderName: el último segmento, o la unidad entera', () => {
    assert.equal(X.folderName('C:\\Automations\\DataBaseSDK'), 'DataBaseSDK');
    assert.equal(X.folderName('C:\\Automations\\DataBaseSDK\\'), 'DataBaseSDK');
    assert.equal(X.folderName('C:\\'), 'C:\\');
});

test('normalizeState: lo dañado o viejo vuelve a los valores por defecto', () => {
    const empty = X.normalizeState(null);
    assert.deepEqual(empty, { open: false, width: X.WIDTH.def, folder: '', expanded: [], recent: [], scroll: 0 });
    assert.deepEqual(X.normalizeState('basura'), empty);
    assert.deepEqual(X.normalizeState([1, 2]), empty);

    const messy = X.normalizeState({ open: 1, width: 'ancho', folder: 5, expanded: 'no-es-lista', recent: [null, 7], scroll: -30 });
    assert.equal(messy.open, true);
    assert.equal(messy.width, X.WIDTH.def);
    assert.equal(messy.folder, '');
    assert.deepEqual(messy.expanded, []);
    assert.deepEqual(messy.recent, []);
    assert.equal(messy.scroll, 0);
});

test('normalizeState: ancho dentro de los límites, rutas normalizadas y sin repetir, recientes con tope', () => {
    assert.equal(X.normalizeState({ width: 5 }).width, X.WIDTH.min);
    assert.equal(X.normalizeState({ width: 99999 }).width, X.WIDTH.max);
    assert.equal(X.normalizeState({ width: 410 }).width, 410);

    const state = X.normalizeState({
        folder: 'C:/scripts/',
        expanded: ['C:\\scripts\\a', 'c:\\SCRIPTS\\A', '', 'C:\\scripts\\b\\'],
        recent: Array.from({ length: 20 }, (_, i) => 'C:\\carpeta' + i)
    });

    assert.equal(state.folder, 'C:\\scripts');
    assert.deepEqual(state.expanded, ['C:\\scripts\\a', 'C:\\scripts\\b']);
    assert.equal(state.recent.length, X.MAX_RECENT);
    assert.equal(state.recent[0], 'C:\\carpeta0');
});

test('rememberFolder: la más reciente primero, sin repetir (aunque cambien las mayúsculas) y con tope', () => {
    let recent = [];
    recent = X.rememberFolder(recent, 'C:\\a');
    recent = X.rememberFolder(recent, 'C:\\b');
    recent = X.rememberFolder(recent, 'c:\\A\\');
    assert.deepEqual(recent, ['c:\\A', 'C:\\b']);

    for (let i = 0; i < 20; i++) recent = X.rememberFolder(recent, 'C:\\x' + i);
    assert.equal(recent.length, X.MAX_RECENT);
    assert.equal(recent[0], 'C:\\x19');
});

test('groupByDepth: carpetas desplegadas por nivel (1 = hijas de la carpeta abierta); ignora las de afuera', () => {
    const levels = X.groupByDepth('C:\\s', [
        'C:\\s\\a\\b', 'C:\\s\\a', 'C:\\s\\z', 'C:\\s\\a\\b\\c', 'D:\\otra\\x', 'C:\\s'
    ]);

    assert.deepEqual(levels, [['C:\\s\\a', 'C:\\s\\z'], ['C:\\s\\a\\b'], ['C:\\s\\a\\b\\c']]);
    assert.deepEqual(X.groupByDepth('C:\\s', []), []);
    // Un nivel sin padre en la lista no deja huecos.
    assert.deepEqual(X.groupByDepth('C:\\s', ['C:\\s\\a\\b\\c']), [['C:\\s\\a\\b\\c']]);
});

// Árbol de ejemplo (en orden visible):
//   0 ventas/ (abierta)  1   2026/ (cerrada)  2   reporte.sql  2   notas.sql ...
const rows = [
    { isFolder: true, open: true, depth: 0 },    // 0 ventas
    { isFolder: true, open: false, depth: 1 },   // 1   2026
    { isFolder: false, open: false, depth: 1 },  // 2   reporte.sql
    { isFolder: true, open: false, depth: 0 },   // 3 compras
    { isFolder: false, open: false, depth: 0 }   // 4 suelto.sql
];

test('keyAction: flechas arriba / abajo, Home y End (con topes)', () => {
    assert.deepEqual(X.keyAction(rows, 1, 'ArrowDown'), { type: 'select', index: 2 });
    assert.deepEqual(X.keyAction(rows, 1, 'ArrowUp'), { type: 'select', index: 0 });
    assert.deepEqual(X.keyAction(rows, 0, 'ArrowUp'), { type: 'select', index: 0 });
    assert.deepEqual(X.keyAction(rows, 4, 'ArrowDown'), { type: 'select', index: 4 });
    assert.deepEqual(X.keyAction(rows, 2, 'Home'), { type: 'select', index: 0 });
    assert.deepEqual(X.keyAction(rows, 2, 'End'), { type: 'select', index: 4 });
});

test('keyAction: sin nada seleccionado, abajo / Home eligen la primera y arriba / End la última', () => {
    assert.deepEqual(X.keyAction(rows, -1, 'ArrowDown'), { type: 'select', index: 0 });
    assert.deepEqual(X.keyAction(rows, -1, 'Home'), { type: 'select', index: 0 });
    assert.deepEqual(X.keyAction(rows, -1, 'ArrowUp'), { type: 'select', index: 4 });
    assert.deepEqual(X.keyAction(rows, -1, 'End'), { type: 'select', index: 4 });
    assert.equal(X.keyAction(rows, -1, 'Enter').type, 'none');
});

test('keyAction: flecha derecha despliega una carpeta cerrada, o baja al primer hijo si ya está abierta', () => {
    assert.deepEqual(X.keyAction(rows, 1, 'ArrowRight'), { type: 'expand', index: 1 });
    assert.deepEqual(X.keyAction(rows, 0, 'ArrowRight'), { type: 'select', index: 1 });
    assert.equal(X.keyAction(rows, 2, 'ArrowRight').type, 'none');                          // un archivo no se despliega
    assert.equal(X.keyAction([{ isFolder: true, open: true, depth: 0 }], 0, 'ArrowRight').type, 'none');   // abierta pero vacía
});

test('keyAction: flecha izquierda pliega una carpeta abierta, o sube a la carpeta que contiene', () => {
    assert.deepEqual(X.keyAction(rows, 0, 'ArrowLeft'), { type: 'collapse', index: 0 });
    assert.deepEqual(X.keyAction(rows, 2, 'ArrowLeft'), { type: 'select', index: 0 });      // reporte.sql → ventas
    assert.deepEqual(X.keyAction(rows, 1, 'ArrowLeft'), { type: 'select', index: 0 });      // 2026 (cerrada) → ventas
    assert.equal(X.keyAction(rows, 4, 'ArrowLeft').type, 'none');                           // suelto.sql no tiene carpeta que lo contenga
    assert.equal(X.keyAction(rows, 3, 'ArrowLeft').type, 'none');                           // compras (cerrada) está en la raíz
});

test('keyAction: Enter y Espacio abren un archivo o alternan una carpeta; otras teclas no hacen nada', () => {
    assert.deepEqual(X.keyAction(rows, 2, 'Enter'), { type: 'open', index: 2 });
    assert.deepEqual(X.keyAction(rows, 4, ' '), { type: 'open', index: 4 });
    assert.deepEqual(X.keyAction(rows, 3, 'Enter'), { type: 'toggle', index: 3 });
    assert.deepEqual(X.keyAction(rows, 0, ' '), { type: 'toggle', index: 0 });
    assert.equal(X.keyAction(rows, 2, 'a').type, 'none');
    assert.equal(X.keyAction([], 0, 'ArrowDown').type, 'none');
});
