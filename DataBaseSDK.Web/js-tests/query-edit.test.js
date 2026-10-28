// Pruebas de la lógica de edición de la grilla (sin DOM). Se corren con:  node --test DataBaseSDK.Web/js-tests/query-edit.test.js (pasar el archivo, no la carpeta)
'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const E = require('../wwwroot/js/query-edit.js');

const info = {
    alias: 'dev', database: 'ventas', schema: 'dbo', table: 'Clientes', primaryKey: ['ID'],
    columns: [
        { name: 'ID', editable: false, nullable: false, category: 'Integer' },
        { name: 'NOMBRE', editable: true, nullable: true, category: 'String' },
        { name: 'MONTO', editable: true, nullable: false, category: 'Decimal' },
        { name: 'CREADO', editable: true, nullable: true, category: 'DateTime' }
    ]
};

const col = (name) => info.columns.find((c) => c.name === name);

test('validateInput: números, fechas, booleanos y GUID', () => {
    assert.equal(E.validateInput(col('MONTO'), '1250.50'), null);
    assert.equal(E.validateInput(col('MONTO'), ' -3 '), null);
    assert.equal(E.validateInput(col('MONTO'), '1e3'), null);
    assert.match(E.validateInput(col('MONTO'), '1,5'), /número con punto decimal/);
    assert.match(E.validateInput(col('MONTO'), 'mucho'), /Se esperaba/);

    assert.equal(E.validateInput({ name: 'N', nullable: true, category: 'Integer' }, '42'), null);
    assert.match(E.validateInput({ name: 'N', nullable: true, category: 'Integer' }, '4.2'), /entero/);

    assert.equal(E.validateInput(col('CREADO'), '2026-10-26'), null);
    assert.equal(E.validateInput(col('CREADO'), '2026-10-26 14:30:15'), null);
    assert.equal(E.validateInput(col('CREADO'), '2026-10-26T14:30'), null);
    assert.match(E.validateInput(col('CREADO'), '26/10/2026'), /fecha/);

    const bool = { name: 'B', nullable: true, category: 'Bool' };
    assert.equal(E.validateInput(bool, 'TRUE'), null);
    assert.equal(E.validateInput(bool, '0'), null);
    assert.match(E.validateInput(bool, 'si'), /true, false/);

    const guid = { name: 'G', nullable: true, category: 'Guid' };
    assert.equal(E.validateInput(guid, '3f2504e0-4f89-11d3-9a0c-0305e82c3301'), null);
    assert.match(E.validateInput(guid, 'abc'), /GUID/);
});

test('validateInput: los textos aceptan cualquier cosa, NULL solo si la columna lo admite', () => {
    assert.equal(E.validateInput(col('NOMBRE'), "O'Brien; DROP TABLE x"), null);
    assert.equal(E.validateInput(col('NOMBRE'), ''), null);
    assert.equal(E.validateInput(col('NOMBRE'), null), null);
    assert.match(E.validateInput(col('MONTO'), null), /no admite NULL/);
});

test('isTruncated: solo el texto recortado por el servidor (4000 caracteres + …)', () => {
    assert.equal(E.isTruncated('x'.repeat(4000) + '…'), true);
    assert.equal(E.isTruncated('x'.repeat(4000)), false);
    assert.equal(E.isTruncated('corto…'), false);
    assert.equal(E.isTruncated(null), false);
});

test('findColumn: sin distinguir mayúsculas', () => {
    assert.equal(E.findColumn(info, 'nombre'), 1);
    assert.equal(E.findColumn(info, 'ID'), 0);
    assert.equal(E.findColumn(info, 'otra'), -1);
});

test('keyText y describeValue', () => {
    assert.equal(E.keyText({ ID: '7', SUC: 'A' }), 'ID = 7, SUC = A');
    assert.equal(E.describeValue(null), '(null)');
    assert.equal(E.describeValue(''), '(vacío)');
    assert.equal(E.describeValue('hola'), 'hola');
    assert.equal(E.describeValue('x'.repeat(300)).length, 121);
});

test('buildRequest: solo viajan las celdas realmente cambiadas, con su valor original', () => {
    const rows = [
        { key: { ID: '7' }, cells: { NOMBRE: { original: 'Ana', value: 'Ana María' }, MONTO: { original: '10', value: '10' } } },
        { key: { ID: '8' }, cells: { MONTO: { original: '5.00', value: '6.00' }, CREADO: { original: null, value: '2026-10-26' } } },
        { key: { ID: '9' }, cells: { NOMBRE: { original: 'x', value: 'x' } } }   // sin cambios reales: no viaja
    ];

    const body = E.buildRequest(info, rows, false);

    assert.equal(body.alias, 'dev');
    assert.equal(body.database, 'ventas');
    assert.equal(body.schema, 'dbo');
    assert.equal(body.table, 'Clientes');
    assert.equal(body.dryRun, false);
    assert.equal(body.changes.length, 2);
    assert.deepEqual(body.changes[0], { primaryKey: { ID: '7' }, set: { NOMBRE: { original: 'Ana', value: 'Ana María' } } });
    assert.deepEqual(Object.keys(body.changes[1].set), ['MONTO', 'CREADO']);
    assert.equal(body.changes[1].set.CREADO.original, null);
});

test('buildRequest: el modo de prueba y la base vacía', () => {
    const body = E.buildRequest({ ...info, database: '' }, [{ key: { ID: '1' }, cells: { NOMBRE: { original: 'a', value: null } } }], true);

    assert.equal(body.dryRun, true);
    assert.equal(body.database, null);
    assert.equal(body.changes[0].set.NOMBRE.value, null);
});
