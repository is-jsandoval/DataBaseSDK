--IP Servidor: 172.26.60.105,4433
--Usuario: usr_bizagi
--Clave: Bg123456 

-----------------------------------------------
--[INICIO] BLOQUE REVISIÓN COMPLETA CUENTAS 3.0
-----------------------------------------------
use BG_NEO

declare @cedula varchar(10)='1708255631' --Aquí cambiar la cédula y ejecutar todo el bloque

--Activos, Pasivos, Patrimonio
select * from [dbo].[BG_EXP_SolicitudProducto] where numIdentificacion = @cedula
select * from [BG_NEO].DBO.BG_PER_Personas where numIdentificacion = @cedula
select * from BG_INGRESOS_PERSONAS_NATURALES where  IDENTIFICACION=@cedula
--sueldoMensual
select * from BG_PER_IngresosGastos where idpersonanatural in (select idPersona from bg_per_personas where numidentificacion=@cedula)
select * from BG_EXP_Expediente where idpersona in (select idPersona from bg_per_personas where numidentificacion=@cedula)
select * from BG_EXP_Expedientedetalle where idexpediente in (select idexpediente from BG_EXP_Expediente where idpersona in (select idPersona from bg_per_personas where numidentificacion=@cedula))
select * from BG_PER_Ubicaciones where idpersona in (select idPersona from bg_per_personas where numidentificacion=@cedula) --consultar, hay dos tipos detalle direccion
select * from BG_EXP_ExpedienteSolicitudProductoPasivos where idexpediente in (select idexpediente from BG_EXP_Expediente where idpersona in (select idPersona from bg_per_personas where numidentificacion=@cedula))
SELECT * from BG_EXP_ColaProcesosMigracionCuentas where idexpediente in (select idexpediente from BG_EXP_Expediente where idpersona in (select idPersona from bg_per_personas where numidentificacion=@cedula))
--------------------------------------------
--[FIN] BLOQUE REVISIÓN COMPLETA CUENTAS 3.0
--------------------------------------------


--------------------------
--** CONSULTAS VARIAS **--
--------------------------

--select * from [dbo].[BG_PER_ClientesPersonasNaturales]

--select * from [dbo].[BG_EXP_ExpedienteSolicitud]

--Productos
/*
SELECT fechaIngreso, * FROM [dbo].[BG_PRO_Productos] where idCodigo = 2358
SELECT fechaIngreso, * FROM [dbo].[BG_PRO_Productos] where idCodigo = 4
SELECT fechaIngreso, * FROM [dbo].[BG_PRO_Productos] where UPPER(strDefinicion) like '%MICRO%'
SELECT fechaIngreso, * FROM [dbo].[BG_PRO_Productos] where UPPER(strDefinicion) like '%AHORRO%' --175
SELECT fechaIngreso, * FROM [dbo].[BG_PRO_Productos] where UPPER(strDefinicion) like '%TARJETA%' --2049 TC


select idExpediente, idProducto, fechaIngreso, idCatDictamen, idDictamen, motivoDictamen, productoSolicitado, estado, numAliasTarjetaCredito, numSolicitudSyscard, estadoSolicitudTarjetaCredito 
from BG_EXP_Expedientedetalle where idExpediente = 20986011

select idExpediente, idProducto, fechaIngreso, idCatDictamen, idDictamen, motivoDictamen, productoSolicitado, estado, numAliasTarjetaCredito, numSolicitudSyscard, estadoSolicitudTarjetaCredito 
from BG_EXP_Expedientedetalle where idExpediente in (select idExpediente from BG_EXP_Expediente where idCatEstadoEtapa = 104 and idEstadoEtapa = 3076 and idEtapa = 3119) and idProducto = 6 and fechaIngreso > '20240101'

--idExpediente = 20986011 and 
--upper(strNombre) like '%MICRO%'

select idCodigo, strvalor, * from [dbo].[BG_ADM_CatalogosDetalles] where upper(strvalor) like '%REGION%' order by fechaingreso desc 
-------------------------------------------------------------------------------------------
select * from [BG_NEO].DBO.BG_PER_Personas p where p.idCatDetSegmentoEstrategico = 11920 --Aquí veo todos los clientes que son segmento PERSONAS
*/
/*

select * from BG_EXP_Expediente where idexpediente in (
	'20981802',
	'20981803',
	'20982207',
	'20982210',
	'20982212')




User	  Contraseña
TestQA14  0401057542
TestQA15  0905302477
TestQA16  0931132211
TestQA17  1804088738



select * from [BG_NEO].DBO.BG_PER_Personas where numIdentificacion in ('0100005982','0100049493', '0100057645')

select * from BG_INGRESOS_PERSONAS_NATURALES where region = 'SUR' 

IDENTIFICACION='0932047962'

select  * from [dbo].[BG_ADM_Catalogos] where idcodigo in (4, 5, 11, 22, 39, 190)

select idCodigo, strvalor, * from [dbo].[BG_ADM_CatalogosDetalles] where idCatalogo in (190) --los segmentos estrategicos

2108	Avanti
2109	Evolution
11920	Personas


select idCodigo, strvalor, * from [dbo].[BG_ADM_CatalogosDetalles] where upper(strvalor) like '%crédito no recomendado%' --los segmentos estrategicos 3116 6
select idCodigo, strvalor, * from [dbo].[BG_ADM_CatalogosDetalles] where idcodigo = 3116
select idCodigo, strvalor, * from [dbo].[BG_ADM_CatalogosDetalles] where idCatalogo in (41) --los segmentos estrategicos 11920
select idCodigo, strvalor, * from [dbo].[BG_ADM_CatalogosDetalles] where idCatalogo in (92)

select idCodigo, strvalor, * from [dbo].[BG_ADM_CatalogosDetalles] where idCatalogo in (110) --3116 Análisis de Crédito (etapa)
select idCodigo, strvalor, * from [dbo].[BG_ADM_CatalogosDetalles] where idCatalogo in (100) --9667 Credito no Recomendado (estado)

select * from [dbo].[BG_ADM_CatalogosDetalles] where idCodigo in (0)
select top 100  * from [dbo].[BG_PRO_ParametrosGenerales] where upper(nombreCampo) like '%SEGURO%%CONYU%'

select top 100  * from [dbo].[BG_PRO_ParametrosGenerales] where idtipoParametro in (116,1986,117)



idCodigo

fondos 
1806

190
 
11920 

0103718037
0100027713
0100006576 --persona 

--ingreso 999 patrimonio 9000

--Países vetados
select strvalor, strvalor2
from bg_adm_catalogosdetalles
where idcatalogo= 135 and strvalor2 = 'X'

select top 100 * from [dbo].[BG_PRO_ParametrosGenerales] where upper(nombreCampo) like '%CONDICIONADO%' 

select * from BG_PRO_ParametrosGenerales where idProducto IN (2372,2813) AND nombreFisico in ('cantidadExpedientesRechazadosRiesgos','tiempoValidaExpedientesRechazadosRiesgos')





select idcodigo, sTRNombre, codProductoRelacionado, aplicaDesgravamen, codProductoImpresion, terminalFicticioHost, codProductoHostFE from [dbo].[BG_PRO_Productos] where idcodigo in (2372,
2813,
2120,
2321)


*/