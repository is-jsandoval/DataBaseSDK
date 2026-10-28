select co_metodoaimpuesto,co_metodoaimpuesto,* from nts_tarjcred.dbo.tc_comercios where co_ruc='0903603348001'

sp_helptext 'sp_consulta_comision_retencion_comercios'
 
--por comercio
EXEC dbo.sp_consulta_comision_retencion_comercios
@pp_comercio = 259453
 
--por ruc
EXEC dbo.sp_consulta_comision_retencion_comercios
@pp_ruc = '0952482099001'
 
EXEC dbo.sp_consulta_comision_retencion_comercios