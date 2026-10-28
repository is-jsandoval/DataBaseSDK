select NumeroDocumento,FechaMovimiento,* from BG_AfiliacionComercios.CREDITO_FACTURACION.ABONO_CREDIPOS_2026
where identificacion='0903603348' 


---insertar registros
INSERT INTO BG_AfiliacionComercios.CREDITO_FACTURACION.ABONO_CREDIPOS_2026
(MovimientoId, Identificacion, ClienteId, CapitalProgramado, FechaConciliacion, FechaVencimiento, NumeroDocumento, Estado, ProductoRelacionado, FechaCancelacion, CapitalPagado, InteresProgramado, InteresPagado, PlazoDias, TipoCuenta, NumeroCuenta, IndicadorDocumento, FechaMovimiento, IndiceMovimiento, FormaPago, CapitalMovimiento, InteresMovimiento, FechaCreacion, UsuarioCreacion
)
SELECT MovimientoId + 1000,Identificacion, ClienteId, CapitalProgramado, FechaConciliacion, FechaVencimiento, '109223' AS NumeroDocumento, Estado, ProductoRelacionado, FechaCancelacion, CapitalPagado, InteresProgramado, InteresPagado, PlazoDias, TipoCuenta, NumeroCuenta, IndicadorDocumento, FechaMovimiento, IndiceMovimiento, FormaPago, CapitalMovimiento, InteresMovimiento, FechaCreacion, UsuarioCreacion
FROM BG_AfiliacionComercios.CREDITO_FACTURACION.ABONO_CREDIPOS_2026
WHERE Identificacion = '0903603348';



select * from BG_AfiliacionComercios.CREDITO_FACTURACION.EXPEDIENTE_CREDIPOS

select * from BG_AfiliacionComercios.CREDITO_FACTURACION.ABONO_CREDIPOS_2026
where identificacion='0903603348' and NumeroDocumento='109148'




--update BG_AfiliacionComercios.CREDITO_FACTURACION.ABONO_CREDIPOS_2026
--set FechaMovimiento=getdate()-3
--where identificacion='0903603348' and MovimientoId='1321'