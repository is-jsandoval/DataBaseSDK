--204


select  distinct co_comercio from nts_tarjcred..tc_comercios 
inner join nts_tarjcred..tc_comercios_alternos on ca_comercio = co_comercio
inner join nts_tarjcred..tc_comercio_empresa on ca_comercio = ce_comercio
inner join nts_tarjcred..tc_comercio_marca on co_comercio = cm_comercio
inner join nts_tarjcred..tc_cafiliaciones a on a.ca_comercio = co_comercio
inner join nts_tarjcred..tc_tcomerdiferi on cd_comercio = co_comercio
where isnull(co_linkPagos,'') != 'S' 
 
 select co_comercio,co_ruc,co_mid,* from nts_tarjcred.dbo.tc_comercios where co_comercio ='255970'
  select co_comercio,co_ruc,co_mid,* from nts_tarjcred.dbo.tc_comercios where co_ruc ='0990003149001'
  select co_comercio,co_ruc,co_mid,* from nts_tarjcred.dbo.tc_comercios where co_ruc ='0990003149001'

insert nts_tarjcred.dbo.tc_comercios  select * from nts_tarjcred.dbo.tc_comercios where co_comercio ='255970' 
 
 --scrip de pruebas para inreso de cerc reaprobado
exec nts_tarjcred.dbo.sp_carga_base_preaprobados_lp_2 @pp_empresa = 1, @pp_usuario = 'QATestJC', @pp_estacion = 'TestQA', @pp_opcion = 'PA', @pp_comercio = 135574, @pp_mid = 384
 exec sp_consulta_comercios_preaprobados '1','','','MI','255865',''
 sp_helptext sp_consulta_comercios_preaprobados

select  * from nts_tarjcred.dbo.tc_comercios where co_linkPagos is null
select  * from BG_TComercios.Pagos.lp_comercios where co_ruc='0921288684001'
select co_comercio,co_ruc,co_mid,* from nts_tarjcred.dbo.tc_comercios where co_comercio ='264919'

select * from nts_tarjcred.dbo.tc_comercios_preaprobados_lp where ESTADO='P'
select  * from BG_TComercios.Pagos.lp_comercios where co_ruc='0921288684001'
select * from nts_tarjcred.dbo.tc_comercios where co_comercio='33076'


select * from nts_tarjcred.dbo.tc_comercios  where  co_estado = 'A'

select * from nts_tarjcred..tc_comercios where co_comercio = 15687
exec nts_tarjcred.dbo.sp_carga_base_preaprobados_lp_2 @pp_empresa = 1, @pp_usuario = 'zzPrueba', @pp_estacion = 'zzPrueba', @pp_opcion = 'PA', @pp_comercio = 15687, @pp_mid = 379

--consulta para metodo get preaprobados
exec sp_consulta_comercios_preaprobados  '','','','PA','','';
exec sp_consulta_comercios_preaprobados  '1','TestJC','','MI','226123','';
exec sp_consulta_comercios_preaprobados  '1','','','PA','','0992459603001';
--consulta para metodo post preaprobados
Select * from BG_TComercios.Pagos.lp_comercios where co_ruc like '09%'


  select * from  nts_tarjcred.dbo.tc_comercios with(nolock)  
    --left join nts_tarjcred.dbo.tc_comercios_alternos with(nolock) on ca_comercio = co_comercio and ca_empresa = @pp_empresa  
    inner join nts_tarjcred.dbo.tc_comercio_empresa with(nolock) on ce_comercio = co_comercio and ce_empresa = 1  
    inner join nts_monitran.dbo.vw_catalogo a with(nolock) on a.ca_nombre = 'CategoriaEstbl' and a.dc_cod = ltrim(rtrim(co_categoria))  


--Comercios ya habilitados con BGPay
--update nts_tarjcred.dbo.tc_comercios set co_linkPagos='N'where co_comercio= 226123
	SELECT TOP 1000 
    co_comercio,
    co_ruc,
    co_descripcion AS nombre_comercial,
    co_linkPagos,
    co_estado
FROM nts_tarjcred.dbo.tc_comercios WITH (NOLOCK)
WHERE co_linkPagos is null
  AND co_estado = 'A';


  --comercios activos sin link de pagos 

  SELECT 
    co_comercio,
    co_ruc,
    co_descripcion AS nombre_comercial,
    co_estado,
    co_linkPagos
FROM nts_tarjcred.dbo.tc_comercios 
WHERE co_estado = 'A'
  AND co_linkPagos='N'
ORDER BY co_comercio DESC;


--comercios preaprobados con links de pagos 
SELECT 
    ruc,
    CODIGOBG,
    NOMBRE_COMERCIAL,
    RAZON_SOCIAL,
    estado
FROM nts_tarjcred.dbo.tc_comercios_preaprobados_lp WITH (NOLOCK)
WHERE estado = 'P';

-- Comercio inexistente o inactivo
SELECT TOP 10 co_comercio, co_estado
FROM nts_tarjcred.dbo.tc_comercios WITH (NOLOCK)
WHERE co_estado <> 'A';

select * from nts_tarjcred.dbo.tc_comercios where co_linkPagos is null and co_estado='A' order by co_fchapertura desc 

exec sp_consulta_comercios_preaprobados '1','','','MI','255989','';

