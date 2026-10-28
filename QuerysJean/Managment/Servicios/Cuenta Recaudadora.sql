--204
select * from nts_tarjcred.dbo.tc_comercios where co_ruc='0903603348001'
select * from nts_tarjcred.dbo.tc_comercio_empresa where ce_comercio in (select co_comercio from nts_tarjcred.dbo.tc_comercios where co_ruc='0954121646001')

update  nts_tarjcred.dbo.tc_comercio_empresa set ce_cuentacomercio= '29579665', ce_institucionbanco='17' 
where ce_comercio=(select co_comercio from nts_tarjcred.dbo.tc_comercios where co_ruc='0954121646001')--guayaquil

update  nts_tarjcred.dbo.tc_comercio_empresa set ce_cuentacomercio= '29579777', ce_institucionbanco='13'
where ce_comercio in (select co_comercio from nts_tarjcred.dbo.tc_comercios where co_ruc='0954121646001')