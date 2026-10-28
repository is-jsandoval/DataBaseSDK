--172.26.61.204,4433
--Ejecutar todo el script primero hacer el select
declare @ruc varchar(13);
set @ruc='0954121646001';--cambiar por numero de ruc 

--select * from nts_tarjcred.dbo.tc_cafiliaciones Where ca_comercio
select co_linkPagos,co_cedulagerente,* from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc--hacer primero el select!!!!!!!!!
select 'Registros a Eliminar en link de pagos',* from BG_TComercios.Pagos.lp_comercios where co_ruc=@ruc--hacer primero el select!!!!!!!!!

update nts_tarjcred.dbo.tc_comercios set co_linkPagos= null where co_comercio ='258678'
update nts_tarjcred.dbo.tc_comercios set co_cedulagerente = '0954121646' where co_comercio ='258560'

-----------------------------------------------------------------------------------------------------------------------------

delete from BG_TComercios.Pagos.lp_limites where lt_comercio  = (select co_comercio_syscard from BG_TComercios.Pagos.lp_comercios where co_ruc =@ruc)
delete from BG_TComercios.Pagos.lp_links_logs where lg_ruc = (select co_comercio_syscard from BG_TComercios.Pagos.lp_comercios where co_ruc =@ruc)
delete from BG_TComercios.Pagos.lp_links where ln_ruc = (select co_comercio_syscard from BG_TComercios.Pagos.lp_comercios where co_ruc =@ruc)
delete from BG_TComercios.Pagos.lp_comercios where co_ruc = @ruc
delete from BG_TComercios.Pagos.lp_links where ln_ruc = (select co_comercio_syscard from BG_TComercios.Pagos.lp_comercios where co_ruc =@ruc)
delete from BG_TComercios.Pagos.lp_links_logs where lg_ruc = (select co_comercio_syscard from BG_TComercios.Pagos.lp_comercios where co_ruc =@ruc)


Delete nts_tarjcred.dbo.tc_cfechapagos where fp_comercio in (select co_comercio from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc and co_linkPagos is not NULL)
Delete nts_tarjcred.dbo.tc_comercios_alternos where ca_comercio in (select co_comercio from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc and co_linkPagos is not NULL)
Delete nts_tarjcred.dbo.tc_comercio_marca where cm_comercio in (select co_comercio from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc and co_linkPagos is not NULL)
Delete nts_tarjcred.dbo.tc_cafiliaciones Where ca_comercio in (select co_comercio from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc and co_linkPagos is not NULL)
Delete nts_tarjcred.dbo.tc_cliquidadepositados Where rd_comercio in (select co_comercio from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc and co_linkPagos is not NULL)
Delete nts_tarjcred.dbo.tc_cliquidahistoria Where lh_comercio in (select co_comercio from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc and co_linkPagos is not NULL)
Delete nts_tarjcred.dbo.tc_cliquidadiaria Where ld_comercio in (select co_comercio from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc and co_linkPagos is not NULL)
Delete nts_tarjcred.dbo.tc_tcomerdiferi Where cd_comercio in (select co_comercio from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc and co_linkPagos is not NULL)
Delete nts_tarjcred.dbo.tc_comercioslimitepiso Where lp_comercio in (select co_comercio from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc and co_linkPagos is not NULL)
Delete nts_tarjcred.dbo.tc_tdiferidos Where di_comercio in (select co_comercio from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc and co_linkPagos is not NULL)
DELETE nts_tarjcred.dbo.tc_transacciones_hist where th_comercio in (select co_comercio from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc and co_linkPagos is not NULL)
DELETE nts_tarjcred.dbo.tc_transacciones where tn_comercio in (select co_comercio from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc and co_linkPagos is not NULL)
Delete nts_tarjcred.dbo.tc_comercio_empresa where ce_comercio in (select co_comercio from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc and co_linkPagos is not NULL)
Delete nts_tarjcred.dbo.tc_comercios where co_comercio in (select co_comercio from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc and co_linkPagos is not NULL)
