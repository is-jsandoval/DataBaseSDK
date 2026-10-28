--172.26.61.204
--monitran
--monitran


---------------------------------------------------------------------------------------------------
--Insertar comercio link de pagos
---------------------------------------------------------------------------------------------------

insert into [BG_TComercios].[Pagos].[lp_comercios] 
(co_comercio_syscard,co_ruc,co_giro,co_terminal,co_mid,co_descripcion,co_ciudad,co_pais,co_estado,co_fch_creacion,co_fch_modificacion,co_usr_modificacion,co_estacion_modificacion,co_id_representante,co_url)  
Values
('987721','0903603348001','5732','1S000031','1000080011','COMERCIO ELIMINAR LINK','GYE','ECU','A','2026-02-02 08:31:48.310','2026-02-02 08:31:48.310','TestQA','QAEstation','0954121646',NULL)

select co_estado,co_usr_modificacion,co_fch_modificacion,* from [BG_TComercios].[Pagos].[lp_comercios] order by co_fch_creacion desc 


update [BG_TComercios].[Pagos].[lp_comercios] set co_estado='A' where co_comercio_syscard='987721'
select * from [BG_TComercios].[Pagos].[lp_comercios] order by co_comercio_syscard desc 
----------------------------------------------------------------------------------------------------
--consulta comercios link de pagos
--COMERCIO DE SELE 
select top(100) * from nts_tarjcred.dbo.tc_comercio_empresa where ce_comercio='183845' 

  select * FROM [BG_TComercios].[Pagos].[lp_comercios] where co_terminal ='BS000003' and co_comercio_syscard='250006'
  select * from nts_tarjcred.dbo.tc_comercios where co_ruc='0918442435001'
  select * from BG_TComercios.Pagos.lp_comercios where co_ruc='0918442435001'
  update BG_TComercios.Pagos.lp_comercios set co_terminal='BS000031'where co_ruc='0920134921001'
  select * from nts_tarjcred.dbo.tc_comercios_alternos  where ca_codalterno='6000000002'
  select * from nts_tarjcred.dbo.tc_comercios  where co_comercio='231977'
  select * from nts_tarjcred.dbo.tc_comercios  where co_comercio='254623'
  select * FROM [BG_TComercios].[Pagos].[lp_comercios] where co_mid ='6000000002' 
  select top(100) ta_iva,ta_estado,* from nts_tarjcred..tc_atrnautoriza where ta_origenest='6000000002' order by ta_fchsis desc 
  select top(100) la_iva,la_estado,* from [nts_tarjcred].[dbo].[tc_atrnautorizalog] where la_origenest='6000000002' order by la_fchsis desc 

  select top(100) la_estado,la_identifica,la_origenest,* from nts_tarjcred..tc_atrnautorizalog where la_comercio='255357' order by la_fchsis desc
  select * from nts_tarjcred.dbo.tc_comercios_alternos where ca_comercio='255357'

  select * from nts_tarjcred.dbo.tc_empresa
  
  exec nts_tarjcred.dbo.sp_bprocesa_linkpagos
@pp_empresa   = 1,
@pp_tipoeje   = 'B',
@pp_batchid   = 31,
@pp_procesoid = 9012,
@pp_usuario   = 'apl_sysbatch',
@pp_seculotpr = 2,
@pp_estacion  = 'GYEWLOADAPP01',
@pp_fecha     = '2025-10-13',
@pp_secuc     = 0


select * from nts_tarjcred..tc_transacciones where tn_tarjeta = '4540630099990478'

select * from nts_intercambio..ic_outincf10 where f10_bin + f10_accountnum = '4540630099990478' and f10_comercio = 255357 

  --update [BG_TComercios].[Pagos].[lp_comercios] set co_estado='C' where co_comercio_syscard='231977'
  select distinct co_estado FROM [BG_TComercios].[Pagos].[lp_comercios] 
--insert into [BG_TComercios].[Pagos].[lp_comercios] (co_comercio_syscard,co_ruc,co_giro,co_terminal,co_mid,co_descripcion,co_ciudad,co_pais,co_estado,co_fch_creacion,co_fch_modificacion,co_usr_modificacion,co_estacion_modificacion)   Values('107487','1204116998001','5732','BS000010','1000080010','ELECTRODOMESTICOS DIGITAL MARTy','GYE','ECU','A','2025-05-22 10:31:48.310','2025-05-22 10:31:48.310','TestQA','zzsystem')

select * from nts_tarjcred..tc_comercios 
inner join nts_tarjcred..tc_comercios_alternos on co_comercio = ca_comercio
where co_linkPagos = 'S'

select * from nts_tarjcred..tc_comercios_alternos where ca_comercio = 232002

select * from [BG_TComercios].[Pagos].[lp_comercios] 
--insert [BG_TComercios].[Pagos].[lp_limites] Values('107487','150.50')

Select * from  [BG_TComercios].[Pagos].[lp_limites]


--Update [BG_TComercios].[Pagos].[lp_comercios] set co_estado='A' where co_comercio_syscard='79889'

  /****** Script para el comando SelectTopNRows de SSMS  ******/
SELECT *  FROM BG_TComercios.Pagos.lp_links where ln_ruc='0930247994001'order by ln_fecha desc

    /****** Script para el comando SelectTopNRows de SSMS  ******/
SELECT *
  FROM tcom_LinkPagos.dbo.[lp_links_logs] order by lg_fecha desc
SELECT lt_comercio,lt_monto_maximo,*
  FROM [BG_TComercios].[Pagos].[lp_comercios]
  left join  [BG_TComercios].[Pagos].[lp_limites] on co_comercio_syscard=lt_comercio
  where co_ruc='1204116998001'

  select * from [BG_TComercios].[Pagos].[lp_comercios]
    select * from [BG_TComercios].[Pagos].[lp_links] order by ln_fecha desc
	  SELECT  top 1*        FROM [BG_TComercios].[Pagos].[lp_links]		where ln_estado = 'A'
  select * from [BG_TComercios].[Pagos].[lp_links_logs]
 select * from [BG_TComercios].[Pagos].[lp_limites] 

alter table [BG_TComercios].[Pagos].[lp_links_logs]
alter column lg_correoComercio varchar(50)


--update [BG_TComercios].[Pagos].[lp_links] set ln_fecha='2025-05-05 10:17:06.180' where ln_audit_number='698443'
--update [BG_TComercios].[Pagos].[lp_links_logs] set lg_fecha='2025-05-05 10:17:06.180' where lg_audit_number='698443'



--172.26.61.204 server
--monitran       user
--monitran       pass
select * from [BG_TComercios].[Pagos].[lp_links_logs] where lg_ruc='1204116998001' order by lg_fecha desc--Tabla logs de links generados
select * from [BG_TComercios].[Pagos].[lp_links] where ln_ruc='1204116998001' order by ln_fecha desc--Tabla registro de links generados
select * from [BG_TComercios].[Pagos].[lp_comercios] where  co_ruc='0930247994001'--Tablas Comercios registrados
select * from [BG_TComercios].[Pagos].[lp_limites] --Limites para los --Limites motos para generar links para comercios 
select * from nts_tarjcred.dbo.tc_comercios where co_ruc='0921288684001'--Tabla en syscard para consultar los comercios

/*
Insertar registros comercios para link

insert [BG_TComercios].[Pagos].[lp_limites] Values('107487','150.50')

insert into [BG_TComercios].[Pagos].[lp_comercios] 
(co_comercio_syscard,co_ruc,co_giro,co_terminal,co_mid,
co_descripcion,co_ciudad,co_pais,co_estado,co_fch_creacion
,co_fch_modificacion,co_usr_modificacion,co_estacion_modificacion)  
Values('1000008','0930247994001','5732','1S000010','1000080010','Test1 JC','GYE','ECU','A','2025-05-22 10:31:48.310','2025-05-22 10:31:48.310','TestQA','zzsystem')
*/

select * from [BG_TComercios].[Pagos].[lp_links_logs] order by lg_fecha desc

 


 
