--172.26.61.204,4433
--Ejecutar todo el script primero hacer el select
If OBJECT_ID (N'tempdb.dbo.#comerciosLista', N'U') Is Not Null
DROP TABLE #comerciosLista
-----------------------------------------------------------------------
declare @ruc varchar(13);
set @ruc='0954121646001';--cambiar por numero de ruc e
select co_comercio into #comerciosLista from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc 

--select * from nts_tarjcred.dbo.tc_cafiliaciones Where ca_comercioZ
select 'Syscard',co_linkPagos,co_validadoBot,co_fchValidaBot,* from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc--hacer primero el select!!!!!!!!!
select 'Link de pagos',* from BG_TComercios.Pagos.lp_comercios where co_ruc=@ruc --hacer primero el select!!!!!!!!!




88
-------------------------------------------------------------------------------------------------------para resetear los comercios sin link de pagos 
update nts_tarjcred.dbo.tc_comercios set co_linkPagos =null where co_comercio=@comercio
-----------------------------------------------------------------------------------------------------------------------------

declare @comercio varchar(13);
set @comercio='259463';



delete from BG_TComercios.Pagos.lp_limites where lt_comercio  = @comercio
delete from BG_TComercios.Pagos.lp_links where ln_ruc = (select co_ruc from BG_TComercios.Pagos.lp_comercios where co_comercio_syscard=@comercio)
delete from BG_TComercios.Pagos.lp_links_logs where lg_ruc = (select co_ruc from BG_TComercios.Pagos.lp_comercios where co_comercio_syscard=@comercio)
delete from BG_TComercios.Pagos.lp_comercios where co_comercio_syscard = @comercio


delete  nts_clientes.dbo.cl_telefonos where tf_codigo = @comercio
Delete nts_tarjcred.dbo.tc_cfechapagos where fp_comercio = @comercio
Delete nts_tarjcred.dbo.tc_comercios_alternos where ca_comercio = @comercio
Delete nts_tarjcred.dbo.tc_comercio_marca where cm_comercio = @comercio
Delete nts_tarjcred.dbo.tc_cafiliaciones Where ca_comercio = @comercio
Delete nts_tarjcred.dbo.tc_cliquidadepositados Where rd_comercio = @comercio
Delete nts_tarjcred.dbo.tc_cliquidahistoria Where lh_comercio = @comercio
Delete nts_tarjcred.dbo.tc_cliquidadiaria Where ld_comercio = @comercio
Delete nts_tarjcred.dbo.tc_tcomerdiferi Where cd_comercio = @comercio
Delete nts_tarjcred.dbo.tc_comercioslimitepiso Where lp_comercio = @comercio
Delete nts_tarjcred.dbo.tc_tdiferidos Where di_comercio = @comercio
DELETE nts_tarjcred.dbo.tc_transacciones_hist where th_comercio = @comercio
DELETE nts_tarjcred.dbo.tc_transacciones where tn_comercio = @comercio
Delete nts_tarjcred.dbo.tc_comercio_empresa where ce_comercio = @comercio
Delete nts_tarjcred.dbo.tc_comercios where co_comercio = @comercio
