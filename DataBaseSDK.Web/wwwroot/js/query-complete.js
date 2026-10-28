// Autocompletado de las consolas SQL (Oracle / SQL Server), al estilo de IntelliSense / SQL Developer:
//   SELECT * FROM Bg_        → tablas y vistas de todos los esquemas que empiezan (o contienen) "Bg_", más esquemas y bases
//   SELECT * FROM ventas.    → los objetos de ese esquema            (en SQL Server también  base.  →  sus esquemas)
//   SELECT c.|               → las columnas de la tabla a la que apunta el alias "c" en el FROM
//   WHERE |  /  SELECT |     → columnas de las tablas del FROM, funciones y palabras clave
// Se abre solo al escribir (2+ letras o un punto), se fuerza con Ctrl+Espacio; ↑/↓ eligen, Enter/Tab aceptan, Esc cierra.
//
// Este archivo tiene dos partes: la lógica de análisis del texto (sin DOM, probada con `node --test`, ver js-tests/)
// y la interfaz (la ventanita), que solo se arma en el navegador.
(function (root) {
    'use strict';

    // ======================= Lógica pura (sin DOM) =======================

    var IDENT = '(?:\\[[^\\]]*\\]|"[^"]*"|[A-Za-z_@#][\\w$#]*)';
    var IDENT_RE = new RegExp(IDENT, 'g');
    var TOKEN_RE = new RegExp(IDENT + '(?:\\.' + IDENT + ')*|[(),;]|\\S', 'g');
    var TAIL_RE = new RegExp('((?:' + IDENT + '\\.)*)((?:\\[|")?[\\w$#]*)$');
    var CLAUSE_RE = /\b(SELECT|FROM|JOIN|WHERE|ON|AND|OR|HAVING|SET|BY|UNION|VALUES|INTO|WITH|CASE|WHEN|THEN|ELSE)\b/gi;
    var LITERAL = '\u0000';

    // Palabras que no pueden ser el alias de una tabla (ni su nombre).
    var NOT_AN_ALIAS = toSet('WHERE GROUP ORDER HAVING UNION INTERSECT EXCEPT MINUS JOIN INNER LEFT RIGHT FULL CROSS OUTER NATURAL ON USING SELECT SET FETCH OFFSET FOR LIMIT WITH AS AND OR WHEN THEN ELSE END NOLOCK WINDOW');
    var COLUMN_CLAUSES = toSet('SELECT WHERE ON AND OR HAVING BY SET CASE WHEN THEN ELSE');

    var RESERVED = {
        sqlserver: toSet('ADD ALL ALTER AND ANY AS ASC BEGIN BETWEEN BY CASE CHECK COLUMN CONSTRAINT CREATE CROSS CURRENT DATABASE DEFAULT DELETE DESC DISTINCT DROP ELSE END EXEC EXISTS FOREIGN FROM FULL GROUP HAVING IN INDEX INNER INSERT INTO IS JOIN KEY LEFT LIKE NOT NULL OF ON OR ORDER OUTER PRIMARY PROCEDURE RIGHT SELECT SET TABLE TOP TRUNCATE UNION UNIQUE UPDATE USER VALUES VIEW WHERE WITH'),
        oracle: toSet('ACCESS ADD ALL ALTER AND ANY AS ASC AUDIT BETWEEN BY CHAR CHECK CLUSTER COLUMN COMMENT COMPRESS CONNECT CREATE CURRENT DATE DECIMAL DEFAULT DELETE DESC DISTINCT DROP ELSE EXCLUSIVE EXISTS FILE FLOAT FOR FROM GRANT GROUP HAVING IDENTIFIED IMMEDIATE IN INCREMENT INDEX INITIAL INSERT INTEGER INTERSECT INTO IS LEVEL LIKE LOCK LONG MINUS MODE MODIFY NOT NOWAIT NULL NUMBER OF OFFLINE ON ONLINE OPTION OR ORDER PCTFREE PRIOR PUBLIC RAW RENAME RESOURCE REVOKE ROW ROWID ROWNUM ROWS SELECT SESSION SET SHARE SIZE SMALLINT START SYNONYM SYSDATE TABLE THEN TO TRIGGER UID UNION UNIQUE UPDATE USER VALIDATE VALUES VARCHAR VARCHAR2 VIEW WHERE WITH')
    };

    var KEYWORDS = {
        common: ['SELECT', 'DISTINCT', 'FROM', 'WHERE', 'GROUP BY', 'ORDER BY', 'HAVING', 'JOIN', 'INNER JOIN', 'LEFT JOIN', 'RIGHT JOIN',
            'FULL JOIN', 'CROSS JOIN', 'ON', 'AS', 'AND', 'OR', 'NOT', 'IN', 'EXISTS', 'BETWEEN', 'LIKE', 'IS NULL', 'IS NOT NULL', 'UNION',
            'UNION ALL', 'INTERSECT', 'CASE', 'WHEN', 'THEN', 'ELSE', 'END', 'ASC', 'DESC', 'WITH', 'COUNT(*)', 'COUNT(', 'SUM(', 'AVG(',
            'MIN(', 'MAX(', 'COALESCE(', 'NULLIF(', 'CAST(', 'UPPER(', 'LOWER(', 'TRIM(', 'REPLACE(', 'ROUND(', 'ABS(', 'OVER(', 'PARTITION BY',
            'ROW_NUMBER() OVER(', 'RANK() OVER(', 'LIMIT'],
        sqlserver: ['TOP', 'GETDATE()', 'SYSDATETIME()', 'ISNULL(', 'DATEADD(', 'DATEDIFF(', 'DATEPART(', 'DATENAME(', 'EOMONTH(', 'LEN(', 'CHARINDEX(',
            'SUBSTRING(', 'LTRIM(', 'RTRIM(', 'CONVERT(', 'FORMAT(', 'STRING_AGG(', 'CONCAT(', 'IIF(', 'TRY_CAST(', 'TRY_CONVERT(', 'NEWID()',
            'OFFSET', 'FETCH NEXT', 'ROWS ONLY', 'WITH (NOLOCK)', 'EXCEPT', 'OUTER APPLY', 'CROSS APPLY', 'STUFF(', 'ISNUMERIC(', 'DISTINCT TOP'],
        oracle: ['ROWNUM', 'SYSDATE', 'SYSTIMESTAMP', 'CURRENT_DATE', 'NVL(', 'NVL2(', 'DECODE(', 'TO_CHAR(', 'TO_DATE(', 'TO_NUMBER(', 'TRUNC(',
            'ADD_MONTHS(', 'MONTHS_BETWEEN(', 'LAST_DAY(', 'SUBSTR(', 'INSTR(', 'LENGTH(', 'LISTAGG(', 'FETCH FIRST', 'ROWS ONLY', 'MINUS',
            'CONNECT BY', 'START WITH', 'PRIOR', 'DUAL', 'REGEXP_LIKE(', 'REGEXP_SUBSTR(', 'LPAD(', 'RPAD(', 'INITCAP(', 'FOR UPDATE']
    };

    function toSet(words) {
        var set = {};
        words.split(/\s+/).forEach(function (w) { if (w) set[w] = true; });
        return set;
    }

    function stripQuotes(part) {
        if (part.charAt(0) === '[' && part.charAt(part.length - 1) === ']') return part.slice(1, -1).replace(/\]\]/g, ']');
        if (part.charAt(0) === '"' && part.charAt(part.length - 1) === '"') return part.slice(1, -1).replace(/""/g, '"');
        return part;
    }

    // "dbo.[Mi Tabla]" → ["dbo", "Mi Tabla"]
    function splitChain(chain) {
        return (chain.match(IDENT_RE) || []).map(stripQuotes);
    }

    // Misma longitud que el texto: comentarios y textos entre comillas simples quedan como \u0000 (así los índices coinciden).
    function maskSql(text) {
        var out = text.split('');
        var n = text.length, i = 0;

        while (i < n) {
            var c = text.charAt(i), d = text.charAt(i + 1);

            if (c === '-' && d === '-') {
                while (i < n && text.charAt(i) !== '\n') { out[i] = LITERAL; i++; }
            } else if (c === '/' && d === '*') {
                var end = text.indexOf('*/', i + 2);
                end = end < 0 ? n : end + 2;
                for (; i < end; i++) out[i] = LITERAL;
            } else if (c === "'") {
                out[i] = LITERAL;
                i++;
                while (i < n) {
                    out[i] = LITERAL;
                    if (text.charAt(i) === "'") {
                        if (text.charAt(i + 1) === "'") { out[i + 1] = LITERAL; i += 2; continue; }
                        i++;
                        break;
                    }
                    i++;
                }
            } else {
                i++;
            }
        }

        return out.join('');
    }

    function tokenize(masked) {
        return (masked.replace(/\u0000/g, ' ').match(TOKEN_RE) || []).map(function (v) {
            return { v: v, word: /^(\[|"|[A-Za-z_@#])/.test(v) };
        });
    }

    // Tablas del FROM / JOIN de una sentencia, con su alias: [{ parts: ['dbo','Clientes'], alias: 'c' }]
    function parseTables(statement) {
        var tokens = tokenize(maskSql(statement));
        var tables = [];
        var expect = false, inFrom = false;

        for (var i = 0; i < tokens.length; i++) {
            var tk = tokens[i];
            var up = tk.v.toUpperCase();

            if (tk.word && (up === 'FROM' || up === 'JOIN')) { expect = true; inFrom = true; continue; }

            if (tk.word && (up === 'WHERE' || up === 'GROUP' || up === 'ORDER' || up === 'HAVING' || up === 'UNION' || up === 'ON' ||
                up === 'SELECT' || up === 'SET' || up === 'INTERSECT' || up === 'EXCEPT' || up === 'MINUS')) {
                inFrom = false; expect = false; continue;
            }

            if (tk.v === ',' && inFrom) { expect = true; continue; }

            if (!expect) continue;

            if (tk.v === '(') {
                // Tabla derivada (subconsulta): se salta; sus columnas no se conocen.
                var depth = 1;
                i++;
                while (i < tokens.length && depth > 0) {
                    if (tokens[i].v === '(') depth++;
                    else if (tokens[i].v === ')') depth--;
                    i++;
                }
                i--;
                expect = false;
                continue;
            }

            if (tk.word && !NOT_AN_ALIAS[up]) {
                var entry = { parts: splitChain(tk.v), alias: null };
                var j = i + 1;

                if (tokens[j] && tokens[j].word && tokens[j].v.toUpperCase() === 'AS') j++;

                if (tokens[j] && tokens[j].word && !NOT_AN_ALIAS[tokens[j].v.toUpperCase()] && tokens[j].v.indexOf('.') < 0) {
                    entry.alias = stripQuotes(tokens[j].v);
                    i = j;
                }

                tables.push(entry);
            }

            expect = false;
        }

        return tables;
    }

    // Qué hay que sugerir en la posición del cursor.
    // → { mode: 'none'|'object'|'column'|'qualified'|'keyword', prefix, replaceStart, openQuote, qualifiers, tables }
    function analyze(text, caret) {
        var masked = maskSql(text);
        var none = { mode: 'none', prefix: '', replaceStart: caret, openQuote: false, qualifiers: [], tables: [] };

        // Dentro de un comentario o de un texto entre comillas no se sugiere nada.
        if (caret > 0 && masked.charAt(caret - 1) === LITERAL) return none;

        var stmtStart = masked.lastIndexOf(';', caret - 1) + 1;
        var stmtEnd = masked.indexOf(';', caret);
        if (stmtEnd < 0) stmtEnd = masked.length;

        var before = masked.slice(stmtStart, caret);
        var match = TAIL_RE.exec(before.slice(-300));
        if (!match) return none;

        var partial = match[2];
        var openQuote = partial.charAt(0) === '[' || partial.charAt(0) === '"';
        var prefix = openQuote ? partial.slice(1) : partial;
        var qualifiers = splitChain(match[1].replace(/\.$/, ''));
        var wordStart = caret - match[0].length;

        // Las tablas se leen SIN lo que se está tecleando: "FROM ventas." es un esquema a medio escribir, no una tabla "ventas".
        var statement = text.slice(stmtStart, wordStart) + text.slice(caret, stmtEnd);

        var result = {
            mode: 'keyword', prefix: prefix, replaceStart: caret - partial.length, openQuote: openQuote,
            qualifiers: qualifiers, tables: parseTables(statement)
        };

        if (qualifiers.length) { result.mode = 'qualified'; return result; }

        // Última palabra clave de la sentencia antes de lo que se está escribiendo.
        var head = masked.slice(stmtStart, wordStart);
        var last = null, m;
        CLAUSE_RE.lastIndex = 0;
        while ((m = CLAUSE_RE.exec(head)) !== null) last = m;

        if (!last) return result;

        var clause = last[1].toUpperCase();

        if (clause === 'FROM' || clause === 'JOIN') {
            // Solo se espera una tabla justo después de FROM/JOIN o de una coma ("FROM a, b").
            var segment = tokenize(head.slice(last.index + last[0].length));
            if (segment.length === 0 || segment[segment.length - 1].v === ',') result.mode = 'object';
        } else if (COLUMN_CLAUSES[clause]) {
            result.mode = 'column';
        }

        return result;
    }

    function quoteName(engine, name) {
        if (engine === 'oracle') {
            return /^[A-Z][A-Z0-9_$#]*$/.test(name) && !RESERVED.oracle[name] ? name : '"' + name.replace(/"/g, '""') + '"';
        }

        return /^[A-Za-z_@#][\w$#]*$/.test(name) && !RESERVED.sqlserver[name.toUpperCase()] ? name : '[' + name.replace(/]/g, ']]') + ']';
    }

    // Texto a insertar para un objeto: sin esquema si es el esquema por defecto (o PUBLIC en Oracle), con esquema si no.
    function objectInsert(engine, schema, name, defaultSchemas) {
        var isDefault = (defaultSchemas || []).some(function (s) { return s.toUpperCase() === schema.toUpperCase(); });
        var isPublic = engine === 'oracle' && schema.toUpperCase() === 'PUBLIC';

        return isDefault || isPublic ? quoteName(engine, name) : quoteName(engine, schema) + '.' + quoteName(engine, name);
    }

    function keywordItems(engine, prefix) {
        var up = prefix.toUpperCase();
        var lower = prefix !== '' && prefix === prefix.toLowerCase();

        return KEYWORDS.common.concat(KEYWORDS[engine] || [])
            .filter(function (k, i, all) { return all.indexOf(k) === i && k.toUpperCase().indexOf(up) === 0; })
            .map(function (k) { return { label: k, insert: lower ? k.toLowerCase() : k, kind: 'Keyword', detail: '', weight: 7 }; });
    }

    // Lo que EMPIEZA con el texto primero, después lo que lo contiene; dentro de cada grupo, por tipo y nombre.
    function rank(items, prefix) {
        var p = prefix.toUpperCase();

        function score(label) {
            var up = label.toUpperCase();
            if (p === '') return 1;
            if (up === p) return 0;
            if (up.indexOf(p) === 0) return 1;
            return up.indexOf(p) > 0 ? 2 : -1;
        }

        return items
            .map(function (item) { return { item: item, score: score(item.label) }; })
            .filter(function (x) { return x.score >= 0; })
            .sort(function (a, b) {
                return a.score - b.score || (a.item.weight || 0) - (b.item.weight || 0) || a.item.label.localeCompare(b.item.label);
            })
            .map(function (x) { return x.item; });
    }

    var api = {
        maskSql: maskSql, tokenize: tokenize, parseTables: parseTables, analyze: analyze, splitChain: splitChain,
        quoteName: quoteName, objectInsert: objectInsert, keywordItems: keywordItems, rank: rank, KEYWORDS: KEYWORDS
    };

    if (typeof module !== 'undefined' && module.exports) module.exports = api;
    if (typeof window === 'undefined' || typeof document === 'undefined') return;

    // ======================= Interfaz (solo en el navegador) =======================

    window.QCComplete = api;

    var KIND_WEIGHT = { Table: 1, View: 2, Synonym: 3, Schema: 4, Database: 5, Function: 6, Column: 0 };
    var KIND_ICON = { Table: '▦', View: '👁', Synonym: '↪', Schema: '🗂', Database: '🛢', Function: 'ƒ', Procedure: '⚙', Package: '📦', Sequence: '#', Type: '𝑻', Column: '▫', Keyword: '⌨' };
    var KIND_LABEL = { Table: 'tabla', View: 'vista', Synonym: 'sinónimo', Function: 'función', Procedure: 'procedimiento', Package: 'paquete', Sequence: 'secuencia', Type: 'tipo' };
    var FROM_KINDS = { Table: true, View: true, Synonym: true, Function: true };
    var MAX_VISIBLE = 60;

    function init() {
        var QC = window.QC;
        var rootEl = QC && QC.root;
        if (!rootEl) return;

        var ta = rootEl.querySelector('[data-role="sql"]');
        var aliasSel = rootEl.querySelector('[data-role="alias"]');
        var dbSel = rootEl.querySelector('[data-role="database"]');
        var schemaSel = rootEl.querySelector('[data-role="schema"]');
        var host = rootEl.querySelector('.qc-editor-host');
        if (!ta || !aliasSel || !host) return;

        var engine = QC.kind;
        var CACHE_MS = 5 * 60 * 1000;
        var cache = {};
        var lastSearch = null;

        function alias() { return aliasSel.value; }
        function database() { return dbSel ? dbSel.value : ''; }

        // Lo que se pidió una vez no se vuelve a pedir en 5 minutos; un fallo no se cachea.
        function cached(key, load) {
            var hit = cache[key];
            if (hit && Date.now() - hit.at < CACHE_MS) return hit.promise;

            var promise = load();
            cache[key] = { at: Date.now(), promise: promise };
            promise.catch(function () { delete cache[key]; });
            return promise;
        }

        function resetCache() { cache = {}; lastSearch = null; }
        aliasSel.addEventListener('change', resetCache);
        if (dbSel) dbSel.addEventListener('change', function () { cache = {}; lastSearch = null; });
        if (schemaSel) schemaSel.addEventListener('change', function () { cache = {}; lastSearch = null; });

        function guard(promise) { return promise.catch(function () { return null; }); }   // el autocompletado nunca debe romper el editor

        // ----- consultas al servidor (las mismas del explorador) -----

        function listDatabases() {
            return cached('db|' + alias(), function () { return QC.get('Databases', { alias: alias() }); });
        }

        function listSchemas(dbName) {
            var db = dbName === undefined ? database() : dbName;
            return cached('sch|' + alias() + '|' + db, function () { return QC.get('Schemas', { alias: alias(), database: db }); });
        }

        function listObjects(db, schema, kind) {
            return cached('obj|' + alias() + '|' + db + '|' + schema + '|' + kind, function () {
                return QC.get('Objects', { alias: alias(), database: db, schema: schema, kind: kind })
                    .then(function (page) { return page.items.map(function (i) { return i.name; }); });
            });
        }

        function listColumns(ref) {
            return cached('col|' + alias() + '|' + ref.database + '|' + ref.schema + '|' + ref.name, function () {
                return QC.get('Group', { alias: alias(), database: ref.database, schema: ref.schema, kind: 'Table', name: ref.name, group: 'Columns' });
            });
        }

        // Busca objetos por nombre; si ya se buscó algo más corto (y no se cortó), se filtra en memoria sin volver al servidor.
        function search(prefix) {
            var key = alias() + '|' + database();
            var lower = prefix.toLowerCase();

            if (lastSearch && lastSearch.key === key && !lastSearch.truncated && lower.indexOf(lastSearch.prefix) === 0) {
                return Promise.resolve({
                    items: lastSearch.items.filter(function (o) { return o.name.toLowerCase().indexOf(lower) >= 0; }),
                    truncated: false
                });
            }

            return QC.get('Search', { alias: alias(), database: database(), filter: prefix }).then(function (page) {
                lastSearch = { key: key, prefix: lower, items: page.items, truncated: page.truncated };
                return page;
            });
        }

        function defaultSchemas() {
            return guard(listSchemas()).then(function (schemas) {
                var names = (schemas || []).filter(function (s) { return s.isCurrent; }).map(function (s) { return s.name; });
                if (schemaSel && schemaSel.value) names.unshift(schemaSel.value);
                return names;
            });
        }

        // ----- armado de sugerencias -----

        function objectItems(objects, defaults) {
            return objects.map(function (o) {
                return {
                    label: o.name, kind: o.kind, weight: KIND_WEIGHT[o.kind] || 5,
                    insert: objectInsert(engine, o.schema, o.name, defaults),
                    detail: o.schema + ' · ' + (KIND_LABEL[o.kind] || o.kind)
                };
            });
        }

        function schemaItems(schemas, opts) {
            return schemas.map(function (s) {
                return { label: s.name, kind: 'Schema', weight: KIND_WEIGHT.Schema, insert: quoteName(engine, s.name) + '.', continueDot: true,
                    detail: (opts && opts.detail) || 'esquema' };
            });
        }

        function inSchema(db, schema) {
            return Promise.all(['Table', 'View', 'Synonym'].map(function (kind) {
                return guard(listObjects(db, schema, kind)).then(function (names) { return (names || []).map(function (n) { return { name: n, kind: kind }; }); });
            })).then(function (groups) {
                return [].concat.apply([], groups).map(function (o) {
                    return { label: o.name, kind: o.kind, weight: KIND_WEIGHT[o.kind], insert: quoteName(engine, o.name), detail: schema + ' · ' + KIND_LABEL[o.kind] };
                });
            });
        }

        function columnItems(columns, owner) {
            return (columns || []).map(function (c) {
                return { label: c.name, kind: 'Column', weight: 0, insert: quoteName(engine, c.name), detail: (c.detail || '') + (owner ? '  ·  ' + owner : '') };
            });
        }

        // Dónde vive una tabla del FROM: con esquema explícito, o buscándola por nombre (primero en el esquema por defecto).
        function resolveTable(parts) {
            if (engine === 'sqlserver' && parts.length >= 3) return Promise.resolve({ database: parts[0], schema: parts[1], name: parts[2] });
            if (parts.length >= 2) return Promise.resolve({ database: database(), schema: parts[parts.length - 2], name: parts[parts.length - 1] });

            var name = parts[0];

            return Promise.all([guard(search(name)), defaultSchemas()]).then(function (r) {
                var found = ((r[0] && r[0].items) || []).filter(function (o) {
                    return o.name.toLowerCase() === name.toLowerCase() && FROM_KINDS[o.kind];
                });
                if (!found.length) return null;

                var defaults = r[1].map(function (s) { return s.toUpperCase(); });
                found.sort(function (a, b) {
                    var ia = defaults.indexOf(a.schema.toUpperCase()), ib = defaults.indexOf(b.schema.toUpperCase());
                    return (ia < 0 ? 99 : ia) - (ib < 0 ? 99 : ib);
                });

                return { database: database(), schema: found[0].schema, name: found[0].name };
            });
        }

        function columnsOfTables(entries) {
            return Promise.all(entries.slice(0, 6).map(function (entry) {
                var owner = entry.alias || entry.parts[entry.parts.length - 1];

                return resolveTable(entry.parts)
                    .then(function (ref) { return ref ? guard(listColumns(ref)) : null; })
                    .then(function (columns) { return columnItems(columns, owner); });
            })).then(function (groups) { return [].concat.apply([], groups); });
        }

        function qualified(ctx) {
            var q = ctx.qualifiers;
            var last = function (t) { return t.parts[t.parts.length - 1].toUpperCase(); };
            var is = function (a, b) { return a.toUpperCase() === b.toUpperCase(); };

            if (q.length === 1) {
                // 1) alias (o nombre) de una tabla del FROM → sus columnas
                var entry = ctx.tables.filter(function (t) { return (t.alias && is(t.alias, q[0])) || last(t) === q[0].toUpperCase(); })[0];
                if (entry) return columnsOfTables([entry]);

                return Promise.all([guard(listSchemas()), engine === 'sqlserver' ? guard(listDatabases()) : Promise.resolve([])]).then(function (r) {
                    var schema = (r[0] || []).filter(function (s) { return is(s.name, q[0]); })[0];
                    if (schema) return inSchema(database(), schema.name);                           // 2) esquema → sus objetos

                    var db = (r[1] || []).filter(function (d) { return is(d.name, q[0]); })[0];
                    if (db) {                                                                        // 3) base → sus esquemas
                        return guard(listSchemas(db.name)).then(function (schemas) { return schemaItems(schemas || [], { detail: 'esquema de ' + db.name }); });
                    }

                    // 4) nombre de tabla sin esquema → sus columnas
                    return resolveTable([q[0]]).then(function (ref) { return ref ? guard(listColumns(ref)).then(function (c) { return columnItems(c, q[0]); }) : []; });
                });
            }

            if (q.length === 2) {
                var dbFirst = engine === 'sqlserver' ? guard(listDatabases()) : Promise.resolve([]);

                return dbFirst.then(function (dbs) {
                    var db = (dbs || []).filter(function (d) { return is(d.name, q[0]); })[0];
                    if (db) return inSchema(db.name, q[1]);                                         // base.esquema. → objetos
                    return guard(listColumns({ database: database(), schema: q[0], name: q[1] })).then(function (c) { return columnItems(c, q[1]); });   // esquema.tabla. → columnas
                });
            }

            return guard(listColumns({ database: q[0], schema: q[1], name: q[2] })).then(function (c) { return columnItems(c, q[2]); });
        }

        function gather(ctx) {
            if (ctx.mode === 'keyword') return Promise.resolve(keywordItems(engine, ctx.prefix));
            if (ctx.mode === 'qualified') return qualified(ctx);

            if (ctx.mode === 'column') {
                var words = ctx.prefix.length >= 2 ? keywordItems(engine, ctx.prefix) : [];
                if (!ctx.tables.length) return Promise.resolve(words);
                return columnsOfTables(ctx.tables).then(function (columns) { return columns.concat(words); });
            }

            // 'object': después de FROM / JOIN
            var objects = ctx.prefix ? guard(search(ctx.prefix)) : Promise.resolve(null);

            return Promise.all([objects, defaultSchemas(), guard(listSchemas()), engine === 'sqlserver' ? guard(listDatabases()) : Promise.resolve([])]).then(function (r) {
                var found = ((r[0] && r[0].items) || []).filter(function (o) { return FROM_KINDS[o.kind]; });
                var items = objectItems(found, r[1]);
                var p = ctx.prefix.toUpperCase();

                items = items.concat(schemaItems((r[2] || []).filter(function (s) { return s.name.toUpperCase().indexOf(p) === 0; })));
                items = items.concat((r[3] || []).filter(function (d) { return d.name.toUpperCase().indexOf(p) === 0; }).map(function (d) {
                    return { label: d.name, kind: 'Database', weight: KIND_WEIGHT.Database, insert: quoteName(engine, d.name) + '.', continueDot: true, detail: 'base de datos' };
                }));

                return items;
            });
        }

        // ----- ventanita -----

        var popup = document.createElement('div');
        popup.className = 'qc-complete';
        popup.hidden = true;
        host.appendChild(popup);

        var state = { items: [], index: 0, ctx: null, visible: false };
        var seq = 0, timer = null, suppress = false;

        function hide() {
            seq++;
            clearTimeout(timer);
            state.visible = false;
            popup.hidden = true;
        }

        var measure = document.createElement('canvas').getContext('2d');

        // Posición en pantalla de un índice del texto: la fuente es monoespaciada, el alto de línea fijo y no hay ajuste de línea.
        function caretPoint(index) {
            var cs = getComputedStyle(ta);
            measure.font = cs.fontSize + ' ' + cs.fontFamily;
            var charWidth = measure.measureText('M').width;
            var lineHeight = parseFloat(cs.lineHeight) || 20;
            var lines = ta.value.slice(0, index).split('\n');
            var column = 0;

            lines[lines.length - 1].split('').forEach(function (ch) { column += ch === '\t' ? 4 - (column % 4) : 1; });

            return {
                x: ta.offsetLeft + parseFloat(cs.paddingLeft) + column * charWidth - ta.scrollLeft,
                y: ta.offsetTop + parseFloat(cs.paddingTop) + lines.length * lineHeight - ta.scrollTop
            };
        }

        function highlight() {
            Array.prototype.forEach.call(popup.children, function (child, i) {
                child.classList.toggle('qc-complete-active', i === state.index);
                if (i === state.index) child.scrollIntoView({ block: 'nearest' });
            });
        }

        function render(ctx, items) {
            popup.innerHTML = '';

            items.slice(0, MAX_VISIBLE).forEach(function (item, i) {
                var row = document.createElement('div');
                row.className = 'qc-complete-item';

                var icon = document.createElement('span');
                icon.className = 'qc-complete-icon';
                icon.textContent = KIND_ICON[item.kind] || '';
                row.appendChild(icon);

                // El tramo que coincide con lo escrito va en negrita.
                var label = document.createElement('span');
                label.className = 'qc-complete-label';
                var at = ctx.prefix ? item.label.toUpperCase().indexOf(ctx.prefix.toUpperCase()) : -1;

                if (at >= 0) {
                    label.appendChild(document.createTextNode(item.label.slice(0, at)));
                    var strong = document.createElement('b');
                    strong.textContent = item.label.slice(at, at + ctx.prefix.length);
                    label.appendChild(strong);
                    label.appendChild(document.createTextNode(item.label.slice(at + ctx.prefix.length)));
                } else {
                    label.textContent = item.label;
                }

                row.appendChild(label);

                if (item.detail) {
                    var detail = document.createElement('span');
                    detail.className = 'qc-complete-detail';
                    detail.textContent = item.detail;
                    row.appendChild(detail);
                }

                // mousedown (no click) y sin quitarle el foco al editor.
                row.addEventListener('mousedown', function (event) { event.preventDefault(); accept(items[i]); });
                row.addEventListener('mousemove', function () { state.index = i; highlight(); });
                popup.appendChild(row);
            });

            if (items.length > MAX_VISIBLE) {
                var more = document.createElement('div');
                more.className = 'qc-complete-more';
                more.textContent = '+ ' + (items.length - MAX_VISIBLE) + ' más: escribe más letras';
                popup.appendChild(more);
            }
        }

        function show(ctx, items) {
            if (!items.length) { hide(); return; }

            state.items = items.slice(0, MAX_VISIBLE);
            state.index = 0;
            state.ctx = ctx;
            state.visible = true;

            render(ctx, items);
            popup.hidden = false;

            var point = caretPoint(ctx.replaceStart);
            popup.style.left = Math.max(0, Math.min(point.x, host.clientWidth - popup.offsetWidth - 4)) + 'px';
            popup.style.top = point.y + 'px';
            highlight();
        }

        function accept(item) {
            if (!item || !state.ctx) return;

            var start = state.ctx.replaceStart;
            var end = ta.selectionStart;

            // Si se había escrito "[" o '"' y el siguiente carácter es el que cierra, se reemplaza también.
            var next = ta.value.charAt(end);
            if (state.ctx.openQuote && (next === ']' || next === '"')) end++;

            ta.focus();
            ta.setSelectionRange(start, end);
            suppress = true;
            document.execCommand('insertText', false, item.insert);   // conserva Ctrl+Z
            suppress = false;
            hide();

            // Después de un esquema o una base se sigue con el punto: se abre enseguida la lista de lo que contiene.
            if (item.continueDot) setTimeout(function () { compute(true); }, 30);
        }

        function compute(force) {
            if (ta.selectionStart !== ta.selectionEnd) { hide(); return; }

            var ctx = analyze(ta.value, ta.selectionStart);
            if (ctx.mode === 'none') { hide(); return; }

            var minimum = force || ctx.mode === 'qualified' ? 0 : 2;
            if (ctx.prefix.length < minimum) { hide(); return; }

            // Sin conexión elegida solo se pueden sugerir palabras clave.
            if (!alias() && ctx.mode !== 'keyword') ctx.mode = 'keyword';

            var mine = ++seq;

            gather(ctx).then(function (items) {
                if (mine !== seq) return;   // llegó tarde: ya se escribió otra cosa
                show(ctx, rank(items, ctx.prefix));
            }).catch(function () { if (mine === seq) hide(); });
        }

        ta.addEventListener('input', function (event) {
            if (suppress) return;

            if (event.inputType === 'insertFromPaste' || event.inputType === 'insertFromDrop' ||
                event.inputType === 'historyUndo' || event.inputType === 'historyRedo') { hide(); return; }

            clearTimeout(timer);
            timer = setTimeout(function () { compute(false); }, event.data === '.' ? 0 : 150);
        });

        // En captura, para ir antes que el Tab / Enter del editor.
        ta.addEventListener('keydown', function (event) {
            var mod = event.ctrlKey || event.metaKey;

            if (mod && event.code === 'Space') {
                event.preventDefault();
                event.stopImmediatePropagation();
                compute(true);
                return;
            }

            if (!state.visible) return;

            var handled = true;

            switch (event.key) {
                case 'ArrowDown': state.index = (state.index + 1) % state.items.length; highlight(); break;
                case 'ArrowUp': state.index = (state.index - 1 + state.items.length) % state.items.length; highlight(); break;
                case 'PageDown': state.index = Math.min(state.index + 8, state.items.length - 1); highlight(); break;
                case 'PageUp': state.index = Math.max(state.index - 8, 0); highlight(); break;
                case 'Escape': hide(); break;
                case 'Enter':
                case 'Tab':
                    if (mod || event.shiftKey || event.altKey) { hide(); handled = false; }   // Ctrl+Enter sigue siendo "ejecutar"
                    else accept(state.items[state.index]);
                    break;
                case 'ArrowLeft': case 'ArrowRight': case 'Home': case 'End':
                    hide();
                    handled = false;
                    break;
                default:
                    handled = false;
            }

            if (handled) {
                event.preventDefault();
                event.stopImmediatePropagation();
            }
        }, true);

        ta.addEventListener('blur', function () { setTimeout(hide, 150); });
        ta.addEventListener('scroll', hide);
        ta.addEventListener('click', hide);

        QC.complete = { refresh: resetCache };
    }

    // query-editor.js crea el editor y query-console.js la conexión: se arranca cuando el resto de la página ya está lista.
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})(this);
