// Pruebas de la lógica de autocompletado (sin DOM). Se corren con:  node --test DataBaseSDK.Web/js-tests/query-complete.test.js (pasar el archivo, no la carpeta)
'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const C = require('../wwwroot/js/query-complete.js');

// Analiza el texto con el cursor donde esté el marcador "|" (o al final).
function at(sql) {
    const caret = sql.includes('|') ? sql.indexOf('|') : sql.length;
    const text = sql.replace('|', '');
    return { ctx: C.analyze(text, caret), text, caret };
}

test('"SELECT * FROM Bg_" pide tablas y reemplaza solo lo escrito', () => {
    const { ctx, caret } = at('SELECT * FROM Bg_');

    assert.equal(ctx.mode, 'object');
    assert.equal(ctx.prefix, 'Bg_');
    assert.equal(ctx.replaceStart, caret - 3);
    assert.deepEqual(ctx.qualifiers, []);
});

test('el ejemplo del usuario tal cual: Select * from Bg_ (palabras clave en cualquier mayúscula)', () => {
    const { ctx } = at('Select * from Bg_');

    assert.equal(ctx.mode, 'object');
    assert.equal(ctx.prefix, 'Bg_');
});

test('después de JOIN y después de una coma del FROM también se esperan tablas', () => {
    assert.equal(at('SELECT * FROM Clientes c JOIN Pa').ctx.mode, 'object');
    assert.equal(at('SELECT * FROM Clientes c, Pa').ctx.mode, 'object');
    assert.equal(at('SELECT * FROM ').ctx.mode, 'object');
});

test('después de la tabla (y su alias) ya no son tablas sino palabras clave', () => {
    assert.equal(at('SELECT * FROM Clientes WH').ctx.mode, 'keyword');
    assert.equal(at('SELECT * FROM Clientes c WH').ctx.mode, 'keyword');
});

test('esquema. y base.esquema. son contextos calificados', () => {
    const one = at('SELECT * FROM ventas.').ctx;
    assert.equal(one.mode, 'qualified');
    assert.deepEqual(one.qualifiers, ['ventas']);
    assert.equal(one.prefix, '');

    const two = at('SELECT * FROM Pagos.dbo.Cli').ctx;
    assert.deepEqual(two.qualifiers, ['Pagos', 'dbo']);
    assert.equal(two.prefix, 'Cli');
});

test('alias. toma las tablas del FROM aunque esté DESPUÉS del cursor', () => {
    const { ctx } = at('SELECT c.| FROM dbo.Clientes c');

    assert.equal(ctx.mode, 'qualified');
    assert.deepEqual(ctx.qualifiers, ['c']);
    assert.deepEqual(ctx.tables, [{ parts: ['dbo', 'Clientes'], alias: 'c' }]);
});

test('lo que se está escribiendo no cuenta como una tabla del FROM', () => {
    // "ventas." es un esquema a medio escribir, no una tabla llamada "ventas".
    assert.deepEqual(at('SELECT * FROM ventas.').ctx.tables, []);
    assert.deepEqual(at('SELECT * FROM Bg_').ctx.tables, []);
    assert.deepEqual(at('SELECT * FROM dbo.Cli').ctx.tables, []);

    // Pero las tablas ya escritas sí cuentan, incluso con otra a medio escribir.
    assert.deepEqual(at('SELECT * FROM Clientes c JOIN ventas.').ctx.tables, [{ parts: ['Clientes'], alias: 'c' }]);
    assert.deepEqual(at('SELECT c.| FROM Clientes c').ctx.tables, [{ parts: ['Clientes'], alias: 'c' }]);
});

test('en SELECT / WHERE / ON / ORDER BY / GROUP BY se sugieren columnas', () => {
    const cases = [
        'SELECT sa',
        'SELECT * FROM Clientes WHERE sa',
        'SELECT * FROM A a JOIN B b ON sa',
        'SELECT * FROM Clientes ORDER BY sa',
        'SELECT * FROM Clientes GROUP BY sa',
        'SELECT * FROM Clientes WHERE x = 1 AND sa'
    ];

    for (const sql of cases) assert.equal(at(sql).ctx.mode, 'column', sql);
});

test('parseTables: alias con y sin AS, JOINs y nombres con esquema', () => {
    assert.deepEqual(C.parseTables('SELECT * FROM dbo.Clientes AS c LEFT JOIN Pagos p ON c.id = p.cid WHERE 1=1'), [
        { parts: ['dbo', 'Clientes'], alias: 'c' },
        { parts: ['Pagos'], alias: 'p' }
    ]);
});

test('parseTables: lista separada por comas', () => {
    assert.deepEqual(C.parseTables('SELECT * FROM Clientes c, Pagos p, Cuentas WHERE 1=1'), [
        { parts: ['Clientes'], alias: 'c' },
        { parts: ['Pagos'], alias: 'p' },
        { parts: ['Cuentas'], alias: null }
    ]);
});

test('parseTables: una palabra reservada nunca es un alias', () => {
    assert.equal(C.parseTables('SELECT * FROM Clientes WHERE id = 1')[0].alias, null);
    assert.equal(C.parseTables('SELECT * FROM Clientes INNER JOIN Pagos ON 1=1')[0].alias, null);
});

test('parseTables: identificadores entre corchetes o comillas, y las subconsultas se saltan', () => {
    assert.deepEqual(C.parseTables('SELECT * FROM [Mi Tabla] t WHERE t.id = 1'), [{ parts: ['Mi Tabla'], alias: 't' }]);
    assert.deepEqual(C.parseTables('SELECT * FROM "Mi Tabla" t'), [{ parts: ['Mi Tabla'], alias: 't' }]);

    // La tabla derivada no aporta columnas conocidas; la tabla real sí.
    assert.deepEqual(
        C.parseTables('SELECT * FROM (SELECT 1 AS x FROM Interna) s JOIN Clientes c ON c.id = s.x'),
        [{ parts: ['Clientes'], alias: 'c' }]);
});

test('una coma dentro de una función del ON no se confunde con otra tabla', () => {
    const tables = C.parseTables('SELECT * FROM A a JOIN B b ON COALESCE(a.x, b.y) = 1 WHERE a.z = 2');

    assert.deepEqual(tables.map(t => t.parts[0]), ['A', 'B']);
});

test('dentro de un texto entre comillas o de un comentario no se sugiere nada', () => {
    assert.equal(at("SELECT * FROM t WHERE nombre = 'abc|'").ctx.mode, 'none');
    assert.equal(at('SELECT 1 -- FROM Cli|').ctx.mode, 'none');
    assert.equal(at('SELECT /* FROM Cli| */ 1').ctx.mode, 'none');
});

test('un FROM dentro de un texto no cuenta como cláusula ni como tabla', () => {
    assert.deepEqual(C.parseTables("SELECT 'FROM Falsa' AS t FROM Real r"), [{ parts: ['Real'], alias: 'r' }]);
    assert.equal(at("SELECT 'FROM Falsa' AS t, sa").ctx.mode, 'column');
});

test('solo cuenta la sentencia actual (separadas por punto y coma)', () => {
    const { ctx } = at('SELECT * FROM Vieja v; SELECT * FROM Nueva n WHERE n.');

    assert.deepEqual(ctx.tables, [{ parts: ['Nueva'], alias: 'n' }]);
    assert.deepEqual(ctx.qualifiers, ['n']);
});

test('un corchete abierto se reemplaza completo, con la marca de comilla', () => {
    const { ctx, caret } = at('SELECT * FROM [Mi');

    assert.equal(ctx.prefix, 'Mi');
    assert.equal(ctx.openQuote, true);
    assert.equal(ctx.replaceStart, caret - 3);   // incluye el "["
});

test('quoteName: solo se entrecomilla lo que lo necesita', () => {
    assert.equal(C.quoteName('sqlserver', 'Clientes'), 'Clientes');
    assert.equal(C.quoteName('sqlserver', 'Mi Tabla'), '[Mi Tabla]');
    assert.equal(C.quoteName('sqlserver', 'order'), '[order]');
    assert.equal(C.quoteName('sqlserver', 'a]b'), '[a]]b]');
    assert.equal(C.quoteName('oracle', 'CLIENTES'), 'CLIENTES');
    assert.equal(C.quoteName('oracle', 'Clientes'), '"Clientes"');
    assert.equal(C.quoteName('oracle', 'ORDER'), '"ORDER"');
});

test('objectInsert: sin esquema si es el de por defecto o PUBLIC (Oracle); con esquema si no', () => {
    assert.equal(C.objectInsert('sqlserver', 'dbo', 'BG_X', ['dbo']), 'BG_X');
    assert.equal(C.objectInsert('sqlserver', 'ventas', 'BG_X', ['dbo']), 'ventas.BG_X');
    assert.equal(C.objectInsert('oracle', 'PUBLIC', 'DUAL', ['HR']), 'DUAL');
    assert.equal(C.objectInsert('oracle', 'HR', 'BG_X', ['HR']), 'BG_X');
    assert.equal(C.objectInsert('oracle', 'FIN', 'BG_X', ['HR']), 'FIN.BG_X');
    assert.equal(C.objectInsert('sqlserver', 'Mi Esquema', 'Mi Tabla', ['dbo']), '[Mi Esquema].[Mi Tabla]');
});

test('las palabras clave respetan cómo escribe el usuario y son del motor', () => {
    assert.ok(C.keywordItems('sqlserver', 'sel').map(i => i.insert).includes('select'));
    assert.ok(C.keywordItems('sqlserver', 'SEL').map(i => i.insert).includes('SELECT'));
    assert.ok(C.keywordItems('sqlserver', 'getd').some(i => i.label === 'GETDATE()'));
    assert.ok(C.keywordItems('oracle', 'sysd').some(i => i.label === 'SYSDATE'));
    assert.equal(C.keywordItems('oracle', 'getd').length, 0);
});

test('rank: exacto, luego lo que empieza, luego lo que contiene; lo demás se descarta', () => {
    const items = [
        { label: 'XBG_TABLA', weight: 1 }, { label: 'BG_B', weight: 1 }, { label: 'BG_', weight: 1 },
        { label: 'BG_A', weight: 1 }, { label: 'OTRA', weight: 1 }
    ];

    assert.deepEqual(C.rank(items, 'bg_').map(i => i.label), ['BG_', 'BG_A', 'BG_B', 'XBG_TABLA']);
});

test('rank: a igual coincidencia, primero tablas y luego vistas (por peso)', () => {
    const items = [{ label: 'BG_V', weight: 2 }, { label: 'BG_T', weight: 1 }];

    assert.deepEqual(C.rank(items, 'bg').map(i => i.label), ['BG_T', 'BG_V']);
});

test('maskSql conserva la longitud y tapa textos y comentarios', () => {
    const sql = "SELECT 'a''b' /* x */ FROM t -- fin";
    const masked = C.maskSql(sql);

    assert.equal(masked.length, sql.length);
    assert.equal(masked.indexOf('FROM'), sql.indexOf('FROM'));
    assert.ok(!masked.includes('fin'));
    assert.ok(!masked.includes('a\'\'b'));
});
