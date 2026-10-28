--credipos

select * from BG_AfiliacionComercios.CREDITO_FACTURACION.ABONO_CREDIPOS_2026
where Identificacion='0903603348' 


--ingualar el abono
update BG_AfiliacionComercios.CREDITO_FACTURACION.ABONO_CREDIPOS_2026
set CapitalPagado=CapitalProgramado,InteresPagado=InteresProgramado
where Identificacion='0903603348' and MovimientoId='1321'

--restar el abono
update BG_AfiliacionComercios.CREDITO_FACTURACION.ABONO_CREDIPOS_2026
set CapitalPagado=CapitalProgramado-10,InteresPagado=InteresProgramado-10
where Identificacion='0903603348' and MovimientoId='1321'






INSERT INTO BG_AfiliacionComercios.CREDITO_FACTURACION.ABONO_CREDIPOS_2026
(
    MovimientoId,
    Identificacion,
    ClienteId,
    CapitalProgramado,
    FechaConciliacion,
    FechaVencimiento,
    NumeroDocumento,
    Estado,
    ProductoRelacionado,
    FechaCancelacion,
    CapitalPagado,
    InteresProgramado,
    InteresPagado,
    PlazoDias,
    TipoCuenta,
    NumeroCuenta,
    IndicadorDocumento,
    FechaMovimiento,
    IndiceMovimiento,
    FormaPago,
    CapitalMovimiento,
    InteresMovimiento,
    FechaCreacion,
    UsuarioCreacion
)
SELECT
    MAX_ID + ROW_NUMBER() OVER (ORDER BY MovimientoId) AS MovimientoId,
    '0903603348' AS Identificacion,
    '2135940' AS ClienteId,
    CapitalProgramado,
    FechaConciliacion,
    FechaVencimiento,
    '109148' AS NumeroDocumento,
    Estado,
    ProductoRelacionado,
    FechaCancelacion,
    CapitalPagado,
    InteresProgramado,
    InteresPagado,
    PlazoDias,
    TipoCuenta,
    NumeroCuenta,
    IndicadorDocumento,
    FechaMovimiento,
    IndiceMovimiento,
    FormaPago,
    CapitalMovimiento,
    InteresMovimiento,
    FechaCreacion,
    UsuarioCreacion
FROM
(
    SELECT
        A.*,
        (
            SELECT ISNULL(MAX(MovimientoId), 0)
            FROM BG_AfiliacionComercios.CREDITO_FACTURACION.ABONO_CREDIPOS_2026
        ) AS MAX_ID
    FROM BG_AfiliacionComercios.CREDITO_FACTURACION.ABONO_CREDIPOS_2026 A
    WHERE Identificacion = '0951914316'
) A;
