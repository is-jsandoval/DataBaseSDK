--cedula prueba sin cuenta 
select co_comercio from nts_tarjcred.dbo.tc_comercios where co_ruc like '1104476047%'
--consultar cuenta comercio
select * from nts_tarjcred.dbo.tc_comercio_empresa where 
ce_comercio in 
(select co_comercio from nts_tarjcred.dbo.tc_comercios where co_estado='A' and  co_ruc like '1104476047%')








update nts_tarjcred.dbo.tc_comercio_empresa
set ce_institucionbanco='15' , ce_cuentacomercio='56565458'
where 
ce_comercio in 
(select co_comercio from nts_tarjcred.dbo.tc_comercios where co_estado='A' and  co_ruc like '1104476047%')