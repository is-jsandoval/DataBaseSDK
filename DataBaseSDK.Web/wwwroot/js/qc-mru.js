// Orden de uso reciente de las pestañas (para Ctrl+Tab, como en SSMS). Lógica pura, sin DOM: probada con `node --test`
// (js-tests/query-mru.test.js) y expuesta en el navegador como window.QC.mru para query-editor.js.
//
// La lista va de la pestaña usada más recientemente a la más antigua: el primer elemento es la pestaña activa.
(function (root) {
    'use strict';

    // Pone `id` al frente (sin repetirlo).
    function touch(list, id) {
        return [id].concat(list.filter(function (x) { return x !== id; }));
    }

    function remove(list, id) {
        return list.filter(function (x) { return x !== id; });
    }

    // Lo guardado (se conserva entre recargas: cada ejecución de una consulta recarga la página) puede traer pestañas que ya no
    // existen o no traer algunas. Queda EXACTAMENTE `ids`, en el orden guardado (las que faltaban, al final), con la activa al frente.
    function restore(saved, ids, activeId) {
        var ordered = [];

        (Array.isArray(saved) ? saved : []).concat(ids).forEach(function (id) {
            if (ids.indexOf(id) >= 0 && ordered.indexOf(id) < 0) ordered.push(id);
        });

        return ordered.indexOf(activeId) >= 0 ? touch(ordered, activeId) : ordered;
    }

    // Dónde arranca el selector: Ctrl+Tab cae en la pestaña ANTERIOR (la 2.ª de la lista); Ctrl+Mayús+Tab, en la más antigua.
    // -1 si no hay a dónde ir (una sola pestaña).
    function startIndex(count, direction) {
        if (count < 2) return -1;
        return direction < 0 ? count - 1 : 1;
    }

    // Cada Tab pulsado con Ctrl mantenido avanza una posición (hacia atrás con Mayús), dando la vuelta.
    function step(count, index, direction) {
        if (count < 1) return -1;
        return (index + (direction < 0 ? -1 : 1) + count) % count;
    }

    var api = { touch: touch, remove: remove, restore: restore, startIndex: startIndex, step: step };

    if (typeof module !== 'undefined' && module.exports) module.exports = api;
    if (typeof window !== 'undefined' && window.QC) window.QC.mru = api;
})(this);
