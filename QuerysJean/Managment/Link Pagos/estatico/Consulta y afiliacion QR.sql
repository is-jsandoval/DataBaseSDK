USE [BG_TComercios]
GO

-- TC-001: Consulta exitosa (RUC con link activo)
DECLARE @rc  INT
DECLARE @msg VARCHAR(200)

EXEC @rc = Pagos.sp_consultarComercioQRPorRuc
    @ruc             = '0930247994001',
    @codigoRespuesta = @rc  OUTPUT,
    @mensaje         = @msg OUTPUT

SELECT @rc AS CodigoRetorno, @msg AS Mensaje
-- TC-002 Escenario A: RUC sin registro
DECLARE @rc  INT
DECLARE @msg VARCHAR(200)

EXEC @rc = Pagos.sp_consultarComercioQRPorRuc
    @ruc             = '9999999999999',
    @codigoRespuesta = @rc  OUTPUT,
    @mensaje         = @msg OUTPUT

SELECT @rc AS CodigoRetorno, @msg AS Mensaje
-- TC-002 Escenario B: RUC con link inactivo
-- Primero confirma que el RUC tiene le_estado='I'
SELECT le_codigoLink, le_estado
FROM Pagos.lp_comercios_link_estaticos
WHERE le_ruc = '0102057734001'  

DECLARE @rc  INT
DECLARE @msg VARCHAR(200)

EXEC @rc = Pagos.sp_consultarComercioQRPorRuc
    @ruc             = '0102057734001',  -- 
    @codigoRespuesta = @rc  OUTPUT,
    @mensaje         = @msg OUTPUT

SELECT @rc AS CodigoRetorno, @msg AS Mensaje

sp_afiliarComercioQR
sqlUSE [BG_TComercios]
GO

-- TC-003: Afiliación exitosa
-- Verifica primero que no exista registro previo
SELECT COUNT(*) AS registros_previos
FROM Pagos.lp_comercios_link_estaticos
WHERE le_ruc = '0954121646001'

DECLARE @rc  INT
DECLARE @msg VARCHAR(200)

EXEC @rc = Pagos.sp_afiliarComercioQR
    @ruc              = '0954121646001',
    @iva              = 'S',
    @tipoPago         = 'C',
    @usuario_creacion = 'testQA',
    @codigoRespuesta  = @rc  OUTPUT,
    @mensaje          = @msg OUTPUT

SELECT @rc AS CodigoRetorno, @msg AS Mensaje

-- Verifica el registro insertado
SELECT le_codigoLink, le_ruc, le_iva, le_diferido,
       le_hora_inicio, le_hora_fin, le_estado,
       fecha_creacion, usuario_creacion
FROM Pagos.lp_comercios_link_estaticos
WHERE le_ruc = '0954121646001'
-- TC-004 Escenario A: RUC no existe
DECLARE @rc  INT
DECLARE @msg VARCHAR(200)

EXEC @rc = Pagos.sp_afiliarComercioQR
    @ruc              = '9999999999999',
    @iva              = 'S',
    @tipoPago         = 'C',
    @usuario_creacion = 'testQA',
    @codigoRespuesta  = @rc  OUTPUT,
    @mensaje          = @msg OUTPUT

SELECT @rc AS CodigoRetorno, @msg AS Mensaje
-- TC-004 Escenario B: Comercio ya afiliado
-- Usa el mismo RUC del TC-003 que ya quedó afiliado
DECLARE @rc  INT
DECLARE @msg VARCHAR(200)

EXEC @rc = Pagos.sp_afiliarComercioQR
    @ruc              = '0954121646001',
    @iva              = 'S',
    @tipoPago         = 'C',
    @usuario_creacion = 'testQA',
    @codigoRespuesta  = @rc  OUTPUT,
    @mensaje          = @msg OUTPUT

SELECT @rc AS CodigoRetorno, @msg AS Mensaje
-- LIMPIEZA — ejecutar al finalizar las pruebas
DELETE FROM Pagos.lp_comercios_link_estaticos
WHERE le_ruc = '0954121646001'
AND usuario_creacion = 'testQA'