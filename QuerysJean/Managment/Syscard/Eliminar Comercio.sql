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
select 'Datos Adicionales de Syscard',* from nts_tarjcred.dbo.tc_comercio_empresa where ce_comercio in (select co_comercio from #comerciosLista)
select 'Limites',* from BG_TComercios.Pagos.lp_limites where lt_comercio in (select co_comercio from #comerciosLista)



-------------------------------------------------------------------------------------------------------para resetear los comercios sin link de pagos 
update nts_tarjcred.dbo.tc_comercios set co_linkPagos =null where co_ruc =@ruc
-----------------------------------------------------------------------------------------------------------------------------

delete BG_TComercios.Pagos.lp_links_logs where lg_ruc = @ruc
delete BG_TComercios.Pagos.lp_links where ln_ruc = @ruc
delete BG_TComercios.Pagos.lp_consumos_link_estatico where ce_codigo_link_estatico in (select le_codigoLink from BG_TComercios.Pagos.lp_comercios_link_estaticos where le_ruc = '0954121646001')
delete BG_TComercios.Pagos.lp_comercios_link_estaticos where le_ruc = @ruc
delete BG_TComercios.Pagos.lp_limites where lt_comercio  in (select co_comercio_syscard from BG_TComercios.Pagos.lp_comercios where co_ruc =@ruc)
delete BG_TComercios.Pagos.lp_comercios where co_ruc = @ruc


delete  nts_clientes.dbo.cl_telefonos where tf_codigo in (select co_comercio from #comerciosLista)
Delete nts_tarjcred.dbo.tc_cfechapagos where fp_comercio in (select co_comercio from #comerciosLista)
Delete nts_tarjcred.dbo.tc_comercios_alternos where ca_comercio in (select co_comercio from #comerciosLista)
Delete nts_tarjcred.dbo.tc_comercio_marca where cm_comercio in (select co_comercio from #comerciosLista)
Delete nts_tarjcred.dbo.tc_cafiliaciones Where ca_comercio in (select co_comercio from #comerciosLista)
Delete nts_tarjcred.dbo.tc_cliquidadepositados Where rd_comercio in (select co_comercio from #comerciosLista)
Delete nts_tarjcred.dbo.tc_cliquidahistoria Where lh_comercio in (select co_comercio from #comerciosLista)
Delete nts_tarjcred.dbo.tc_cliquidadiaria Where ld_comercio in (select co_comercio from #comerciosLista)
Delete nts_tarjcred.dbo.tc_tcomerdiferi Where cd_comercio in (select co_comercio from #comerciosLista)
Delete nts_tarjcred.dbo.tc_comercioslimitepiso Where lp_comercio in (select co_comercio from #comerciosLista)
Delete nts_tarjcred.dbo.tc_tdiferidos Where di_comercio in (select co_comercio from #comerciosLista)
DELETE nts_tarjcred.dbo.tc_transacciones_hist where th_comercio in (select co_comercio from #comerciosLista)
DELETE nts_tarjcred.dbo.tc_transacciones where tn_comercio in (select co_comercio from #comerciosLista)
Delete nts_tarjcred.dbo.tc_comercio_empresa where ce_comercio in (select co_comercio from #comerciosLista)
Delete nts_tarjcred.dbo.tc_comercios where co_comercio in (select co_comercio from #comerciosLista)


