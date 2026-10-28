-- VALIDAR EN LA TABLA DE COMERCIOS LOS CAMPOS: --
 co_subtipocomercio = NULL o VACIO
 co_pertenececadena = 'N'
 co_cod_matriz = 0

select co_subtipocomercio,co_pertenececadena,co_cod_matriz,* from nts_tarjcred.dbo.tc_comercios where co_comercio = 255535
 VALIDAR QUE : --
 cd_valorfijo = 0.30 para todos ROT/DIFER
 cd_tasacomision = 2% solo pARA ROTATIVO (ROTATIVO = CD_TIPODIFERI = 0)

select cd_valorfijo,cd_tasacomision,* from nts_tarjcred.dbo.tc_tcomerdiferi where cd_comercio = 255535
 

 -----------------------------------------------
 LIQUIDACION ANTICIPADA PARA LINK DE PAGOS --
---------------------------------------------

exec nts_tarjcred.dbo.sp_bprocesa_linkpagos

@pp_empresa   = 1
@pp_tipoeje   = 'B'
@pp_batchid   = 31
@pp_procesoid = 9012
@pp_usuario   = 'apl_sysbatch'
@pp_seculotpr = 2
@pp_estacion  = 'GYEWLOADAPP01'
@pp_fecha     = '2026-02-03'
@pp_secuc     = 0
 
exec nts_tarjcred.dbo.sp_bprocesaliquidacion_dv

@pp_empresa   = 1
@pp_tipoeje   = 'B'
@pp_batchid   = 2
@pp_procesoid = 236
@pp_usuario   = 'apl_sysbatch'
@pp_seculotpr = 7
@pp_estacion  = 'GYEWLOADAPP01'
@pp_fecha     = '2026-02-03'
@pp_secuc     = 0
@pp_comercio = 255535
 
exec nts_tarjcred.dbo.sp_tcr1011_4_dv4 -- RESPALDO

@empresa      = 1
@pp_tipoeje   = 'B'
@pp_batchid   = 9
@pp_procesoid = 131500
@pp_usuario   = 'apl_sysbatch'
@pp_seculotpr = 21
@pp_estacion  = 'GYEWLOADAPP01'
@fecha        = '2026-02-03'
@pp_comercio  = 255535
@pp_secuc     = 0
 
 --------------------------------------------------------
 GENERACION DE ARCHIVO DE TEXTO LIQUIDACION COMERCIOS --
--------------------------------------------------------

exec nts_tarjcred.dbo.sp_tcr8000

@pp_empresa   = 1
@pp_tipoeje   = 'B'
@pp_batchid   = 9
@pp_procesoid = 131501
@pp_usuario   = 'apl_sysbatch'
@pp_seculotpr = 22
@pp_estacion  = 'GYEWLOADAPP01'
@pp_fecha     = '2026-02-03'
@pp_secuc     = 0
 ---------------------------------------------------
 GENERACION DE HTMLS DE LIQUIDACION DE COMERCIOS --
---------------------------------------------------

exec nts_general.dbo.sp_bgenerafile0098

@pp_empresa   = 1
@pp_tipoeje   = 'B'
@pp_batchid   = 9
@pp_procesoid = 131037
@pp_usuario   = 'apl_sysbatch'
@pp_seculotpr = 23
@pp_estacion  = 'GYEWLOADAPP01'
@pp_fecha     = '2026-02-03'
@pp_secuc     = 0
 
listo ya esta todo
 