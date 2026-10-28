/*
   ---------------------------------------------------------
   Server : 172.26.61.204
   User   : monitran
   Pass   : monitran
   ========================================================= */

   -- Links generados
SELECT *
FROM [BG_TComercios].[Pagos].[lp_links]
WHERE ln_ruc = '0954121646001'
ORDER BY ln_fecha DESC;


/* =========================================================
   AUTORIZACIONES SYSCARD
   ========================================================= */

SELECT la_estado, * FROM nts_tarjcred.dbo.tc_atrnautorizalog where la_comercio='258549'  and la_codresp='00' and la_fchsis>='2026-01-29'
--update  nts_tarjcred.dbo.tc_atrnautorizalog set la_fchsis='2026-01-29 22:29:52.913',la_fchproceso='2026-01-29 22:29:52.913' where la_comercio='258549'  and la_codresp='00' and la_fchsis>='2026-01-30'
SELECT la_fchproceso,la_estado, * FROM nts_tarjcred.dbo.tc_atrnautorizalog where la_comercio='258549'  ORDER BY la_fchsis DESC;
SELECT la_estado, * FROM nts_tarjcred.dbo.tc_atrnautorizalog where la_comercio='258549' and la_id='679363914' ORDER BY la_fchsis DESC;
SELECT la_estado, * FROM nts_tarjcred.dbo.tc_atrnautorizalog where la_id='679363948' ORDER BY la_fchsis DESC;

select la_estado, * from nts_tarjcred.dbo.tc_atrnautorizalog where la_id =('679363948')
select la_estado,* from nts_tarjcred.dbo.tc_atrnautorizalog where la_id =('679363914')
update nts_tarjcred.dbo.tc_atrnautorizalog set la_fchproceso='2026-01-28 22:45:36.970', la_fchsis='2026-01-28 22:45:36.970' where  la_id='679363949'
--update nts_tarjcred.dbo.tc_atrnautorizalog set la_fchproceso='2026-01-01 22:22:36.970', la_fchsis='2026-01-28 22:22:36.970' where la_comercio='258549' and la_id='679363948' or la_id='679363948'
--update nts_tarjcred.dbo.tc_atrnautorizalog set la_fchproceso='2026-01-30 13:22:36.970', la_fchsis='2026-01-30 13:22:36.970' where la_id='679363955' 
-- Comercio por Syscard
SELECT *
FROM [BG_TComercios].[Pagos].[lp_comercios]
WHERE co_ruc='0954121646001'

-- Comercio en Syscard por RUC
SELECT *
FROM nts_tarjcred.dbo.tc_comercios
WHERE co_ruc = '0954121646001'

update nts_tarjcred.dbo.tc_comercios set co_metodoaimpuesto='R10' WHERE co_ruc = '0954121646001'


/* =========================================================
   INSERT COMERCIO (EJEMPLO)
   ========================================================= */
/*
INSERT INTO [BG_TComercios].[Pagos].[lp_comercios]
(
    co_comercio_syscard,
    co_ruc,
    co_giro,
    co_terminal,
    co_mid,
    co_descripcion,
    co_ciudad,
    co_pais,
    co_estado,
    co_fch_creacion,
    co_fch_modificacion,
    co_usr_modificacion,
    co_estacion_modificacion
)
VALUES
(
    '107487',
    '1204116998001',
    '5732',
    'BS000010',
    '1000080010',
    'ELECTRODOMESTICOS DIGITAL MARTy',
    'GYE',
    'ECU',
    'A',
    '2025-05-22 10:31:48.310',
    '2025-05-22 10:31:48.310',
    'TestQA',
    'zzsystem'
);
*/


/* =========================================================
   LÍMITES DE COMERCIO
   ========================================================= */

-- Consultar comercio
SELECT *
FROM [BG_TComercios].[Pagos].[lp_comercios]
WHERE co_ruc = '0930247994001';

SELECT *
FROM [BG_TComercios].[Pagos].[lp_comercios]
WHERE co_ruc = '0920134921001';

-- Consultar límites
SELECT *
FROM [BG_TComercios].[Pagos].[lp_limites]
WHERE lt_comercio = '255357';

-- Insertar límite
INSERT INTO [BG_TComercios].[Pagos].[lp_limites]
VALUES ('255357', '500');

-- Inactivar comercio
UPDATE [BG_TComercios].[Pagos].[lp_comercios]
SET co_estado = 'X'
WHERE co_ruc = '0920134921001';




/* =========================================================
   CONSULTA GENERAL DE LÍMITES
   ========================================================= */

SELECT *
FROM [BG_TComercios].[Pagos].[lp_limites];


/* =========================================================
   EJECUCIÓN BATCH LINK DE PAGOS
   ========================================================= */

EXEC nts_tarjcred.dbo.sp_bprocesa_linkpagos
    @pp_empresa   = 1,
    @pp_tipoeje   = 'B',
    @pp_batchid   = 31,
    @pp_procesoid = 9012,
    @pp_usuario   = 'apl_sysbatch',
    @pp_seculotpr = 2,
    @pp_estacion  = 'GYEWLOADAPP01',
    @pp_fecha     = '2025-10-13',
    @pp_secuc     = 0;


/* =========================================================
   LINKS DE PAGO
   ========================================================= */

-- Links por RUC
SELECT * FROM [BG_TComercios].Pagos.lp_links WHERE ln_ruc = '0954121646001' ORDER BY ln_fecha DESC
--update [BG_TComercios].Pagos.lp_links set ln_estado= 'A' , ln_codigoError=00 , ln_fecha_utilizacion= null WHERE ln_ruc = '0954121646001' 
--update [BG_TComercios].Pagos.lp_links_logs set lg_estado= 'A', lg_codigoError=00 , lg_fecha_utilizacion= null   WHERE lg_ruc = '0954121646001' 

-- Logs de links
SELECT * FROM [BG_TComercios].Pagos.lp_links_logs  WHERE lg_ruc = '0954121646001' ORDER BY lg_fecha DESC;


/* =========================================================
   JOIN COMERCIOS + LÍMITES
   ========================================================= */

SELECT
    lt_comercio,
    lt_monto_maximo,
    *
FROM [BG_TComercios].[Pagos].[lp_comercios] c
LEFT JOIN [BG_TComercios].[Pagos].[lp_limites] l
    ON c.co_comercio_syscard = l.lt_comercio
WHERE c.co_comercio_syscard = '77700012';


/* =========================================================
   CONSULTAS GENERALES
   ========================================================= */

SELECT *
FROM [BG_TComercios].[Pagos].[lp_comercios];

SELECT *
FROM [BG_TComercios].[Pagos].[lp_links]
ORDER BY ln_fecha DESC;

SELECT *
FROM [BG_TComercios].[Pagos].[lp_links_logs]
ORDER BY lg_fecha DESC;

SELECT *
FROM [BG_TComercios].[Pagos].[lp_limites];


/* =========================================================
   ELIMINACIÓN DE LINKS (CONTROLADA)
   ========================================================= */
/*
DELETE FROM [BG_TComercios].[Pagos].[lp_links_logs]
WHERE lg_ruc = '0926649369001';

DELETE FROM [BG_TComercios].[Pagos].[lp_links]
WHERE ln_ruc = '0926649369001';
*/


/* =========================================================
   MODIFICACIONES DE FECHA (PRUEBAS)
   ========================================================= */
/*
UPDATE [BG_TComercios].[Pagos].[lp_links]
SET ln_fecha = '2025-05-05 10:17:06.180'
WHERE ln_audit_number = '698443';

UPDATE [BG_TComercios].[Pagos].[lp_links_logs]
SET lg_fecha = '2025-05-05 10:17:06.180'
WHERE lg_audit_number = '698443';
*/


/* =========================================================
   CONSULTAS FINALES DE CONTROL
   ========================================================= */

-- Logs de links usados
SELECT *
FROM [BG_TComercios].[Pagos].[lp_links_logs]
WHERE lg_ruc = '0930247994001'
  AND lg_estado = 'U'
ORDER BY lg_fecha DESC;



-- Comercio registrado
SELECT *
FROM [BG_TComercios].[Pagos].[lp_comercios]
WHERE co_ruc = '0930247994001';

-- Límites configurados
SELECT *
FROM [BG_TComercios].[Pagos].[lp_limites];

-- Comercio en Syscard
SELECT *
FROM nts_tarjcred.dbo.tc_comercios
WHERE co_ruc = '0921288684001';


/* =========================================================
   INSERT DE COMERCIO – EJEMPLO FINAL
   ========================================================= */
/*
INSERT INTO [BG_TComercios].[Pagos].[lp_limites]
VALUES ('107487', '150.50');

INSERT INTO [BG_TComercios].[Pagos].[lp_comercios]
(
    co_comercio_syscard,
    co_ruc,
    co_giro,
    co_terminal,
    co_mid,
    co_descripcion,
    co_ciudad,
    co_pais,
    co_estado,
    co_fch_creacion,
    co_fch_modificacion,
    co_usr_modificacion,
    co_estacion_modificacion
)
VALUES
(
    '1000008',
    '0930247994001',
    '5732',
    '1S000010',
    '1000080010',
    'Test1 JC',
    'GYE',
    'ECU',
    'A',
    '2025-05-22 10:31:48.310',
    '2025-05-22 10:31:48.310',
    'TestQA',
    'zzsystem'
);
*/
