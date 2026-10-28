--172.26.60.134
--inq_kmoreira
SELECT * FROM [BG_MasterData].[Empresas].BG_TB_Administradores_Representantes where RUC_COMPANIA='1793095232001'  order by FechaCarga desc
SELECT * FROM [BG_MasterData].[Empresas].BG_TB_Administradores_Representantes where IDENTIFICACION='1712447406' and NombreRepresentante='COBO CORONEL JORGE LUIS'
SELECT * FROM [BG_MasterData].[Empresas].BG_TB_Administradores_Representantes where IDENTIFICACION='1311722423' 
SELECT * FROM [BG_MasterData].[Empresas].BG_TB_Administradores_Representantes where RUC_COMPANIA='1793095232001' and  IDENTIFICACION='1712447406' 
update [BG_MasterData].[Empresas].BG_TB_Administradores_Representantes set IDENTIFICACION='1311722423' where RUC_COMPANIA='1793095232001' and  IDENTIFICACION='1712447406' 


SELECT IDENTIFICACION FROM [BG_MasterData].[Empresas].BG_TB_Administradores_Representantes where RUC_COMPANIA='0190361357001' 

--ofac 1311722423
--------Insertar representante--------------------------------------------------------------------------
declare @representante varchar(30),@ruc varchar(13),@identi varchar(10)
set @representante ='ARELLANO TREVIÑO JOSE MIGUEL'
set @ruc ='0940339799001'
set @identi ='0940339799'https://172.26.60.45/comercios/link-pago/administrador/link-pago

insert into[BG_MasterData].[Empresas].BG_TB_Administradores_Representantes values (
@ruc,@identi,'ECUADOR','GERENTE GENERAL','1998-02-14 00:00:00.000',NULL,'1998-07-10 00:00:00.000',NULL,'673','RL','1','202508','2026-01-01 14:55:50.683',NULL,NULL,@representante
)
----------------------------------------------------------------------------------
select * from [BG_MasterData].[Empresas].BG_TB_Administradores_Representantes where IDENTIFICACION='1703800225' 
update [BG_MasterData].[Empresas].BG_TB_Administradores_Representantes set RUC_COMPANIA='0000000296012' where IDENTIFICACION='0954121646' 
SELECT * FROM [BG_MasterData].[Empresas].[BG_TB_Accionistas] order by FechaCarga desc
SELECT * FROM [BG_MasterData].[Empresas].BG_TB_Gr_CIIU where CIIU6_DESC like '%nformáti%'
SELECT * FROM [BG_MasterData].[Empresas].BG_TB_Directorio_Empresarial 
SELECT * FROM [BG_MasterData].[Empresas].BG_TB_Empresa_Lider_Grupo_Economico 
SELECT * FROM [BG_MasterData].[Empresas].BG_TB_Scoring_Empresarial
SELECT TIPO_CONTRIBUYENTE,* FROM [BG_MasterData].[Empresas].BG_TB_Fact_Empresas where NUMERO_RUC='0954121646001'
SELECT CODIGO_CIIU,* FROM [BG_MasterData].[Empresas].BG_TB_Fact_Empresas where NUMERO_RUC='1722458559001'
SELECT CODIGO_CIIU,* FROM [BG_MasterData].[Empresas].BG_TB_Fact_Empresas where NUMERO_RUC='1722458559001'
SELECT * FROM [BG_MasterData].[Empresas].BG_TB_Scoring_Empresarial
update [BG_MasterData].[Empresas].BG_TB_Fact_Empresas set CODIGO_CIIU ='J620210'where NUMERO_RUC='1722458559001'

SELECT CODIGO_CIIU,* FROM [BG_MasterData].[Empresas].BG_TB_Fact_Empresas where TIPO_CONTRIBUYENTE='SOCIEDADES'
---cedula OFAC  1311722423
 
insert into [BG_MasterData].[Empresas].BG_TB_Fact_Empresas 
(ID,NUMERO_RUC,RAZON_SOCIAL,ESTADO_CONTRIBUYENTE,CLASE_CONTRIBUYENTE,OBLIGADO,TIPO_CONTRIBUYENTE,CODIGO_CIIU,FECHA_INICIO_ACT,PROVINCIA,CANTON,PARROQUIA,NUM_ESTABLECIMIENTOS_ABI,NUM_ESTABLECIMIENTOS_CER,CANT_EMPLEADOS,TOT_NOMINA_EMPL,VENTAS,AÑO_VENTAS,SEGMENTO_VENTAS,ANTIGUEDAD_EMPRESA,METODOLOGIA_BG,IMPACTO_SECTOR,PROFESIONALES,NUM_ACCIONISTAS,TOT_CAPITAL_SUS,TRES_BALANCES,TRES_107,VEMNTAS_INFERIDAS,AÑO_VTAS_INF,REGION,FANTASIA_COMERCIAL,FechaCarga,GUID_EMPRESA,FechaActualizacion,EstadoActualizacion)
Values
(
'0930482104',
'0930482104001',
'ZAMBRANO CORTEZ KEVIN ANDRES',
'ACTIVO',
'GEN',
'N',
'PERSONAS NATURALES',
'J620210',
'2018-12-13',
'GUAYAS',
'GUAYAQUIL',
'FEBRES CORDERO',
'0',
'1',
'0',
'0',
'0',
'0',
'MASIVO DIGITAL',
'7',
'SERVICIOS ESENCIALES',
'BAJO',
'',
'0',
'0',
'0',
'3',
'0',
'0',
'COSTA',
'-',
'2025-08-19 15:00:12.813',
NULL,
NULL,
NULL
)




(
'PERSONAS NATURALES',
'0930482104',
'0930482104001',
'ZAMBRANO CORTEZ KEVIN ANDRES',
'ACTIVO',
'GEN',
'N',
'PERSONAS NATURALES',
'J620210',
'2018-12-13',
'GUAYAS',
'GUAYAQUIL',
'FEBRES CORDERO',
'0',
'1',
'0',
'0',
'0',
'0',
'MASIVO DIGITAL',
'7',
'SERVICIOS ESENCIALES',
'BAJO',
'',
'0',
'0',
'0',
'3',
'0',
'0',
'COSTA',
'-',
'2025-08-19 15:00:12.813',
NULL,
NULL,
NULL
)