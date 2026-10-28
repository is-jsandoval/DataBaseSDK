select co_comercio,co_ruc,co_descripcion,'MonLim',co_categoria,ca_codalterno,* 
from nts_tarjcred.dbo.tc_comercios left join  nts_tarjcred.dbo.tc_comercios_alternos on co_comercio=ca_comercio
where co_comercio='258459'

exec [Pagos].[sp_crearComercio]
    @codigoSyscard =258459,
    @ruc ='0954121646001',
	@nombreComercio ='Jean Sandoval Quimis',
	@montoLimite = 8000,
	@mid ='78884561',
	@codigoGiro =8062,
	@ciudad ='GYE',
	@pais ='ECU',
	@identificacionRepresentante ='0954121646001',
	@usuarioModificacion ='TestQAJean',
	@estacionModificacion ='TestQAJean',
	@codigoTerminal ='700100001'

select * from BG_TComercios.pagos.lp_actividades where ac_giro='8062'
update BG_TComercios.pagos.lp_actividades set ac_monto_maximo=1111 where ac_giro='8062' and ac_codigo_actividad='O841211'
update BG_TComercios.pagos.lp_actividades set ac_monto_maximo=2222 where ac_giro='8062' and ac_codigo_actividad='Q861001'
update BG_TComercios.pagos.lp_actividades set ac_monto_maximo=3333 where ac_giro='8062' and ac_codigo_actividad='Q861003'
update BG_TComercios.pagos.lp_actividades set ac_monto_maximo=4444 where ac_giro='8062' and ac_codigo_actividad='Q861005'

select co_metodoafuente,co_metodoaimpuesto, * from nts_tarjcred.dbo.tc_comercios where co_ruc='0954121646001'
select * from BG_TComercios.Pagos.lp_comercios where co_ruc='0954121646001'





select * from BG_TComercios.Pagos.lp_limites where lt_comercio ='258461'
select * from nts_tarjcred.dbo.tc_comercios where co_comercio='258461'





update BG_TComercios.Pagos.lp_limites set lt_monto_maximo='11111' where lt_comercio ='258461'