// Pruebas de la sesión versionada (db-workspace.json) y la caché de resultados por pestaña. Se corren con:
//   node --test DataBaseSDK.Web/js-tests/qc-workspace.test.js (pasar el archivo, no la carpeta)
'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const W = require('../wwwroot/js/qc-workspace.js');

test('snapshot: solo archivos con ruta que siguen en disco, sin repetir, cada uno con su conexión, y el activo si está entre ellos', () => {
    const tabs = [
        { path: 'C:\\repo\\a.sql', alias: 'ORA', schema: 'BG_TP' },
        { path: null, alias: 'X' },                  // consulta sin guardar
        { path: 'C:\\repo\\B.sql', missing: true },  // ya no existe en disco
        { path: 'c:\\REPO\\A.SQL', alias: 'OTRA' }   // la misma que la primera
    ];

    assert.deepEqual(W.snapshot(tabs, 'C:\\repo\\a.sql'), {
        files: [{ path: 'C:\\repo\\a.sql', alias: 'ORA', database: null, schema: 'BG_TP' }],
        activeFile: 'C:\\repo\\a.sql'
    });
    assert.equal(W.snapshot(tabs, null).activeFile, null);
    assert.equal(W.snapshot(tabs, 'C:\\repo\\B.sql').activeFile, null);
});

test('signature: no distingue mayúsculas en rutas y cambia con el orden, el activo o la conexión de un archivo', () => {
    const base = { files: [{ path: 'C:\\x.sql', alias: 'A' }, { path: 'C:\\y.sql' }], activeFile: 'C:\\x.sql' };
    const a = W.signature(base);

    assert.equal(a, W.signature({ files: [{ path: 'c:\\X.sql', alias: 'A' }, { path: 'C:\\Y.SQL' }], activeFile: 'c:\\x.SQL' }));
    assert.notEqual(a, W.signature({ files: [base.files[1], base.files[0]], activeFile: 'C:\\x.sql' }));
    assert.notEqual(a, W.signature(Object.assign({}, base, { activeFile: 'C:\\y.sql' })));
    assert.notEqual(a, W.signature({ files: [{ path: 'C:\\x.sql', alias: 'B' }, { path: 'C:\\y.sql' }], activeFile: 'C:\\x.sql' }));
});

test('reconcile: abre lo que trae el repo, da su conexión a las que ya estaban y cierra solo pestañas limpias que el repo no tiene', () => {
    const tabs = [
        { path: 'C:\\r\\keep.sql' },
        { path: 'C:\\r\\clean.sql' },
        { path: 'C:\\r\\dirty.sql', dirty: true },
        { path: 'C:\\r\\gone.sql', missing: true },
        { path: null, dirty: true }
    ];
    const keep = { path: 'c:\\R\\KEEP.sql', alias: 'SQL-1', database: 'Ventas' };
    const fresh = { path: 'C:\\r\\new.sql', alias: 'SQL-2' };

    const plan = W.reconcile(tabs, { files: [keep, fresh], activeFile: 'C:\\r\\new.sql' });

    assert.deepEqual(plan.open, [fresh]);
    assert.deepEqual(plan.contexts, [keep]);
    assert.deepEqual(plan.close, ['C:\\r\\clean.sql']);   // nunca trabajo sin guardar ni pestañas que conservan la caché
    assert.equal(plan.activeFile, 'C:\\r\\new.sql');
});

test('reconcile: sin datos del repo no abre nada', () => {
    assert.deepEqual(W.reconcile([], null), { open: [], close: [], contexts: [], activeFile: null });
});

test('putResult: la pestaña que ejecuta pasa al final, reemplaza su resultado anterior y se respeta el tope', () => {
    let cache = null;
    cache = W.putResult(cache, 't1', { html: 'a' }, 2);
    cache = W.putResult(cache, 't2', { html: 'b' }, 2);
    cache = W.putResult(cache, 't1', { html: 'c' }, 2);

    assert.deepEqual(cache.order, ['t2', 't1']);
    assert.equal(cache.items.t1.html, 'c');

    cache = W.putResult(cache, 't3', { html: 'd' }, 2);
    assert.deepEqual(cache.order, ['t1', 't3']);
    assert.equal(cache.items.t2, undefined);
});

test('dropOldest: libera espacio sin tocar la recién guardada; removeResults olvida las pestañas cerradas', () => {
    const cache = W.putResult(W.putResult(null, 'viejo', { html: 'a' }), 'nuevo', { html: 'b' });

    assert.deepEqual(W.dropOldest(cache, 'nuevo').order, ['nuevo']);
    assert.equal(W.dropOldest(W.putResult(null, 'solo', {}), 'solo'), null);   // nada más que sacar: no entra
    assert.deepEqual(W.removeResults(cache, ['nuevo', 'otra']).order, ['nuevo']);
});

test('normalizeCache: lo guardado dañado vuelve vacío y sin entradas huérfanas', () => {
    assert.deepEqual(W.normalizeCache('basura'), { order: [], items: {} });
    assert.deepEqual(W.normalizeCache({ order: ['a', 'b'], items: { a: { html: 'x' } } }), { order: ['a'], items: { a: { html: 'x' } } });
});
