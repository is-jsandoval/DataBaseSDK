// Pruebas del orden de uso reciente de pestañas (Ctrl+Tab). Se corren con:  node --test DataBaseSDK.Web/js-tests/query-mru.test.js (pasar el archivo, no la carpeta)
'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const M = require('../wwwroot/js/qc-mru.js');

test('touch: la usada ahora va al frente, sin repetirse y sin tocar la lista original', () => {
    const list = ['a', 'b', 'c'];

    assert.deepEqual(M.touch(list, 'c'), ['c', 'a', 'b']);
    assert.deepEqual(M.touch(list, 'a'), ['a', 'b', 'c']);
    assert.deepEqual(M.touch(list, 'nueva'), ['nueva', 'a', 'b', 'c']);
    assert.deepEqual(list, ['a', 'b', 'c']);
});

test('touch: alternar entre dos pestañas hace que Ctrl+Tab siempre caiga en la otra', () => {
    let list = ['a', 'b', 'c'];                // activa: a, anterior: b

    list = M.touch(list, list[M.startIndex(list.length, 1)]);
    assert.deepEqual(list.slice(0, 2), ['b', 'a']);   // ahora activa b, anterior a

    list = M.touch(list, list[M.startIndex(list.length, 1)]);
    assert.deepEqual(list.slice(0, 2), ['a', 'b']);   // vuelve a a
});

test('remove: quita la pestaña cerrada y respeta el resto del orden', () => {
    assert.deepEqual(M.remove(['a', 'b', 'c'], 'b'), ['a', 'c']);
    assert.deepEqual(M.remove(['a', 'b', 'c'], 'z'), ['a', 'b', 'c']);
});

test('restore: queda exactamente el conjunto actual de pestañas, con la activa al frente', () => {
    // Lo guardado trae una pestaña que ya no existe ("x") y no conoce a "d".
    assert.deepEqual(M.restore(['b', 'x', 'a', 'c'], ['a', 'b', 'c', 'd'], 'b'), ['b', 'a', 'c', 'd']);
    // La activa va al frente aunque lo guardado la tuviera en otro lugar.
    assert.deepEqual(M.restore(['b', 'a', 'c'], ['a', 'b', 'c'], 'c'), ['c', 'b', 'a']);
});

test('restore: sin nada guardado (o dañado) usa el orden de las pestañas, sin repetir ids', () => {
    assert.deepEqual(M.restore(null, ['a', 'b'], 'a'), ['a', 'b']);
    assert.deepEqual(M.restore('basura', ['a', 'b'], 'b'), ['b', 'a']);
    assert.deepEqual(M.restore(['a', 'a', 'b', 'b'], ['a', 'b'], 'a'), ['a', 'b']);
    assert.deepEqual(M.restore(['a'], ['a'], 'zzz'), ['a']);   // una activa desconocida no inventa pestañas
    assert.deepEqual(M.restore([], [], null), []);
});

test('startIndex: Ctrl+Tab arranca en la anterior, Ctrl+Mayús+Tab en la más antigua, y con una sola pestaña no hay a dónde ir', () => {
    assert.equal(M.startIndex(3, 1), 1);
    assert.equal(M.startIndex(2, 1), 1);
    assert.equal(M.startIndex(4, -1), 3);
    assert.equal(M.startIndex(2, -1), 1);
    assert.equal(M.startIndex(1, 1), -1);
    assert.equal(M.startIndex(0, 1), -1);
});

test('step: Tab avanza y Mayús+Tab retrocede, dando la vuelta', () => {
    assert.equal(M.step(4, 1, 1), 2);
    assert.equal(M.step(4, 3, 1), 0);
    assert.equal(M.step(4, 0, -1), 3);
    assert.equal(M.step(4, 2, -1), 1);
    assert.equal(M.step(1, 0, 1), 0);
    assert.equal(M.step(0, 0, 1), -1);
});
