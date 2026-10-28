--172.26.61.204,4433
--Ejecutar todo el script primero hacer el select
declare @ruc varchar(13);
set @ruc='0903603348001';--cambiar por numero de ruc 

--select * from nts_tarjcred.dbo.tc_cafiliaciones Where ca_comercio
select co_linkPagos,co_cedulagerente,* from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc--hacer primero el select!!!!!!!!!
select 'Registros a Eliminar en link de pagos',* from BG_TComercios.Pagos.lp_comercios where co_ruc=@ruc--hacer primero el select!!!!!!!!!

--update nts_tarjcred.dbo.tc_comercios set co_linkPagos = 'N' where co_comercio ='258560'
--update nts_tarjcred.dbo.tc_comercios set co_cedulagerente = '0954121646' where co_comercio ='258560'


select cd_valorfijo,* from nts_tarjcred.dbo.tc_tcomerdiferi where cd_comercio=(select co_comercio from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc and co_linkPagos='S')





select cd_valorfijo,* from nts_tarjcred.dbo.tc_tcomerdiferi where cd_comercio in (select co_comercio from nts_tarjcred.dbo.tc_comercios where  co_linkPagos='S')
select distinct cd_valorfijo from nts_tarjcred.dbo.tc_tcomerdiferi where cd_comercio in (select co_comercio from nts_tarjcred.dbo.tc_comercios where  co_linkPagos='S')