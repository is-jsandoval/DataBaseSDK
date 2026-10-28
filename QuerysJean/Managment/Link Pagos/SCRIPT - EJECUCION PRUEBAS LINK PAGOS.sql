use nts_tarjcred
go

----------------------------------------------------------------
-- VERIFICAMOS LA FECHA DE PROCESAMIENTO DE LAS TRANSACCIONES --
----------------------------------------------------------------
select em_fchproceso,em_fchaprocesar,em_offline,* from nts_tarjcred..tc_empresa
select * from nts_monitran.dbo.mt_param_general where  pg_id = 'RHLINKPAGOS' and pg_empresa = 1      

----------------------------------------------
-- ACTUALIZAMOS LA FECHA A LA FECHA DEL DIA --
----------------------------------------------
update nts_tarjcred..tc_empresa set em_fchaprocesar = '2026-01-29', em_fchproceso = '2026-01-29'


------------------------
-- TARJETA DE PRUEBAS --
------------------------

--tarjeta: '4540860761741581' cuenta: 761741 expira: 0427 cvv: 719
--tarjeta: '5230420092049182' cuenta: 92049  expira: 0527 cvv: 679
--tarjeta: '376653549162456' cuenta:  549162 expira: 0527 cvv: 9340
--tarjeta: '5144402438894002' cuenta: 9916308  expira: 0727 cvv: 317

-- 1.-) COMENZAR A INGRESAR AUTORIZACIONES PORTAL LINK DE PAGOS Y PAGARLAS

---------------------------------------------------
-- 2.- LIQUIDACION ANTICIPADA PARA LINK DE PAGOS --
---------------------------------------------------
exec nts_tarjcred.dbo.sp_bprocesa_linkpagos
@pp_empresa   = 1,
@pp_tipoeje   = 'B',
@pp_batchid   = 31,
@pp_procesoid = 9012,
@pp_usuario   = 'apl_sysbatch',
@pp_seculotpr = 2,
@pp_estacion  = 'GYEWLOADAPP01',
@pp_fecha     = '2026-02-20',
@pp_secuc     = 0



--------------------------------------------------------
-- VALIDAMOS QUE LA AUTORIZACION HAYA SIDO COMPENSADA --
--------------------------------------------------------
select la_estado,* from nts_tarjcred.dbo.tc_atrnautorizalog with(nolock)  order by la_Fchsis desc
select tn_fechaliquida,* from nts_tarjcred.dbo.tc_transacciones with(nolock) where tn_comercio = 0

----------------------------------------------
-- 3.- EJECUCION DEL PROCESO DE LIQUIDACION --
----------------------------------------------
exec nts_tarjcred.dbo.sp_bprocesaliquidacion_dv
@pp_empresa   = 1,
@pp_tipoeje   = 'B',
@pp_batchid   = 2,
@pp_procesoid = 236,
@pp_usuario   = 'apl_sysbatch',
@pp_seculotpr = 7,
@pp_estacion  = 'GYEWLOADAPP01',
@pp_fecha     = '2026-01-29',
@pp_secuc     = 0,
@pp_comercio = 258549

------------------------------------------------------------------
-- EJECUTAR SOLO SI SALE ERROR DE QUE LA FECHA YA FUE LIQUIDADA --
------------------------------------------------------------------
--delete from tc_cliquidahistoria where lh_empresa = 1 and lh_fechaliquida = '2026-01-30' and lh_comercio = 258549
select * from tc_cliquidahistoria where lh_empresa = 1  and lh_comercio = 258549

-------------------------------------------------
-- VALIDAR QUE SE HAYA LIQUIDADO CORRECTAMENTE --
-------------------------------------------------
-- tn_fechaliquida tiene que ser igual a la fecha de proceso de la tn_empresa
select tn_fechaliquida,* from nts_tarjcred.dbo.tc_transacciones with(nolock) where tn_comercio = 258549

------------------------------------------------
-- 4.- PROCESO GENERA CODIGOS SYSCARD LIQUIDACION --
------------------------------------------------
exec nts_tarjcred.dbo.sp_GeneraCodigoSyscardsLiqComercio
@pp_empresa   = 1,
@pp_tipoeje   = 'B',
@pp_batchid   = 9,
@pp_procesoid = 4063,
@pp_usuario   = 'apl_sysbatch',
@pp_seculotpr = 20,
@pp_estacion  = 'GYEWLOADAPP01',
@pp_fecha     = '2025-11-19',
@pp_secuc     = 0


---------------------------------------
-- 5.- LENADO DE TABLA TCR8000 (****)--
---------------------------------------
exec nts_tarjcred.dbo.sp_tcr1011_4_dv4
@empresa      = 1,
@pp_tipoeje   = 'B',
@pp_batchid   = 9,
@pp_procesoid = 131500,
@pp_usuario   = 'apl_sysbatch',
@pp_seculotpr = 21,
@pp_estacion  = 'GYEWLOADAPP01',
@fecha        = '2025-11-21',
@pp_comercio  = 255535,
@pp_secuc     = 0

----------------------------------------------------------
-- VALIDAR CAMPO NUEVO MARCA LINK PAGOS (MARCALP) (****)--
----------------------------------------------------------
select MarcaLP,* from nts_tarjcred..tcr8000 with(Nolock)

------------------------------------------------------
-- 6.- GENEREACION DE CODIGOS COMPROBANTE RETENCION --
------------------------------------------------------
exec nts_tarjcred.dbo.sp_bgenera_codigo_numerico
@pp_empresa    = 1,
@pp_tipoeje    = 'B',
@pp_batchid    = 9,
@pp_procesoid  = 102035,
@pp_usuario    = 'apl_sysbatch',
@pp_seculotpr  = 22,
@pp_estacion   = 'GYEWLOADAPP01',
@pp_fecha      = '2025-11-21',
@pp_secuc      = 0,
@opc           = 3,
@tabla         = 'L',
@tipo_ambiente = 2

-----------------------------------------
--7.- GENERACION DE CODIGOS DE FACTURA --
-----------------------------------------
exec nts_tarjcred.dbo.sp_bgenera_codigo_numerico
@pp_empresa    = 1,
@pp_tipoeje    = 'B',
@pp_batchid    = 9,
@pp_procesoid  = 102036,
@pp_usuario    = 'apl_sysbatch',
@pp_seculotpr  = 22,
@pp_estacion   = 'GYEWLOADAPP01',
@pp_fecha      = '2025-11-21',
@pp_secuc      = 0,
@opc           = 2,
@tabla         = 'L',
@tipo_ambiente = 2


-------------------------------------------------------------------------------------------------
-- ANTES DE HACER EL PASO 8 VALIDAR QUE EXISTA LLOS DIRECTORIOS EN LA RUTA SINO VA A DAR ERROR --
-------------------------------------------------------------------------------------------------
-- ABRIR RUTA Y VERIFICAR QUE EXISTA EL DIRECTORIO \\172.26.60.145\Banco de Guayaquil\AAAAMMDD\Archivos\, SI NO EXISTE CREARLO

--------------------------------------------------------------------
--8.- GENERACION DE ARCHIVO DE TEXTO LIQUIDACION COMERCIOS (*****)--
--------------------------------------------------------------------
exec nts_tarjcred.dbo.sp_tcr8000
@pp_empresa   = 1,
@pp_tipoeje   = 'B',
@pp_batchid   = 9,
@pp_procesoid = 131501,
@pp_usuario   = 'apl_sysbatch',
@pp_seculotpr = 22,
@pp_estacion  = 'GYEWLOADAPP01',
@pp_fecha     = '2025-11-21',
@pp_secuc     = 0

-------------------------------------------------------------------------------- 
-- VALIDAR QUE SE HAYA PASADO LA MARCA AL ARCHIVO DE TEXTOS DE LA LIQUIDACION --
--------------------------------------------------------------------------------
-- ABRIR RUTA \\172.26.60.145\Banco de Guayaquil\AAAAMMDD\Archivos\
-- ABRIR ARCHIVO: Liquidacion_Comercios_FE_AAAA-MM-DD
-- VERIFICAR QUE TENGA LA ETIQUETA: A10 SI ESTA EN S = COMERCIO LINK PAGOS, SI ESTA EN N = COMERCIO NO LINK PAGOS

---------------------------------------------------------------
--9.- GENERACION DE HTMLS DE LIQUIDACION DE COMERCIOS (*****)--
---------------------------------------------------------------
exec nts_general.dbo.sp_bgenerafile0098
@pp_empresa   = 1,
@pp_tipoeje   = 'B',
@pp_batchid   = 9,
@pp_procesoid = 131037,
@pp_usuario   = 'apl_sysbatch',
@pp_seculotpr = 23,
@pp_estacion  = 'GYEWLOADAPP01',
@pp_fecha     = '2025-11-19',
@pp_secuc     = 0

------------------------------------------
-- VALIDAR QUE SE HAYA GENERADO EL HTML --
------------------------------------------
-- ABRIR RUTA \\172.26.60.145\Banco de Guayaquil\AAAAMMDD\
-- ABRIR ARCHIVO HTML
-- VALIDAR QUE SI ES COMERCIO LINK DE PAGOS EL COSTO X TRX = 0.10 * el NUMERO DE PAGARES
-- VALIDAR QUE LOS VALORES CUADREN



---


USE [nts_tarjcred]
GO

DECLARE	@return_value int

EXEC	@return_value = [dbo].[sp_bcargaArchivosLiquidacionComercios]
		@pp_empresa = 1,
		@pp_tipoeje = '',
		@pp_batchid = 0,
		@pp_procesoid = 0,
		@pp_usuario = '',
		@pp_seculotpr = 0,
		@pp_estacion = '',
		@pp_fecha = '2025-11-21',
		@pp_secuc = 0

SELECT	'Return Value' = @return_value



USE [nts_tarjcred]
GO

DECLARE	@return_value int

EXEC	@return_value = [dbo].[sp_bgenera_archivo_facturacion_electronica_tcr8000]
		@pp_empresa = 1,
		@pp_tipoeje = '',
		@pp_batchid = 0,
		@pp_procesoid = 0,
		@pp_usuario = '',
		@pp_seculotpr = 0,
		@pp_estacion = '',
		@pp_fecha = '2025-11-10',
		@pp_secuc = 0

SELECT	'Return Value' = @return_value


USE [nts_tarjcred]
GO

DECLARE	@return_value int

EXEC	@return_value = [dbo].sp_helptext [sp_bgenera_archivo_facturacion_FE_PARALELO]
	    @pp_empresa = 1,
		@pp_tipoeje = '',
		@pp_batchid = 0,
		@pp_procesoid = 0,
		@pp_usuario = '',
		@pp_seculotpr = 0,
		@pp_estacion = '',
		@pp_fecha = '2025-11-10',
		@pp_secuc = 0,
		@opc = '3'

SELECT	'Return Value' = @return_value

GO

