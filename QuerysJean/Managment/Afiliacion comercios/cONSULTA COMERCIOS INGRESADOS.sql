--172.26.61.204,4433
select * from nts_tarjcred.dbo.tc_comercios where co_comercio ='255363'order by co_fchapertura desc
select * from nts_tarjcred.dbo.tc_comercio_empresa where ce_comercio = 255363--comercio nuevo ingresado
select * from nts_tarjcred.dbo.tc_comercios_alternos  where ca_comercio = 255363
select * from nts_tarjcred.dbo.tc_cafiliaciones  where ca_comercio=255363 --order by ca_ultfchactualiza desc
select * from nts_tarjcred.dbo.tc_comercio_marca where cm_comercio=255363 --order by cm_ultfchactualiza desc
select * from nts_tarjcred.dbo.tc_tcomerdiferi where cd_comercio = 255363 --order by cd_ultfchactualiza desc

--255358

exec nts_tarjcred.dbo.sp_bprocesa_linkpagos
@pp_empresa   = 1,
@pp_tipoeje   = 'B',
@pp_batchid   = 31,
@pp_procesoid = 9012,
@pp_usuario   = 'JeanQA',
@pp_seculotpr = 2,
@pp_estacion  = 'QATEST',
@pp_fecha     = '2025-09-10',
@pp_secuc     = 0

declare 
@msgerr varchar(4000) = null, 
@rc int = 0 
exec @rc = nts_tarjcred.dbo.sp_consulta_comercios_preaprobados 
@pp_empresa   = 1, 
@pp_usuario   = 'zzPrueba', 
@pp_estacion  = 'zzPrueba', 
@pp_comercio  = 255512, 
@pp_msgerr    = @msgerr out, 
@pp_opcion    = 'MI' 
select @rc, @msgerr
 