   
     -----------declare @error varchar(max), @rc int--ejecucion de creacion de comercios datafast, opcion 1 carga en tabla, opcion 2 procesa registros de la tabla
exec @rc=[sp_bcargaComerciosDatafast]
        @pp_empresa     = 1,
        @pp_usuario     = 'jpiguave',
        @pp_estacion    = 'WLA',
        @pp_fecha       ='2026-03-02',
        @contador_tot   = null,
        @pp_preliminar  = 'N',
        @nombre_archivo = null,
        @opcion         = 2,
        @data           = null,
        @ofac           = null,
        @error          = @error output,
		@repoceso       = 'N' 
select @error, @rc

--comprobacion

SELECT * FROM nts_tarjcred.dbo.tc_comercios where co_ruc = '1793193718001'

select * from nts_tarjcred.dbo.tc_tcomerdiferi where cd_comercio in (258743)
select * from nts_tarjcred.dbo.tc_tcomerdiferi where cd_comercio in ()--AQUI INGRESAR EL COMERCIO NUEVO


SELECT * FROM nts_tarjcred.dbo.tc_comercios where co_ruc = '1750420463001'


select * from nts_tarjcred.dbo.tc_tcomerdiferi where cd_comercio in (231994)
select * from nts_tarjcred.dbo.tc_tcomerdiferi where cd_comercio in ()--AQUI INGRESAR EL COMERCIO NUEVO


SELECT * FROM nts_tarjcred.dbo.tc_comercios where co_ruc = '1090033944001' order by co_comercio desc


select * from nts_tarjcred.dbo.tc_tcomerdiferi where cd_comercio in (248827)
select * from nts_tarjcred.dbo.tc_tcomerdiferi where cd_comercio in ()--AQUI INGRESAR EL COMERCIO NUEVO


--para encontrar las comisiones que se van a configurar por valores default
/*

--CONSULTA POR COMERCIO. SOLO DEBE DE INGRESAR COMERCIO
--CONSULTA POR MCC SOLO DEBE DE INGRESAR MCC, EL COMERCIO DEBE DE IR NULL
DECLARE @MCC VARCHAR(4) = '5611', @comercio int = 248827

-- Nivel 1: Default por MCC específico
SELECT 'DEFAULT POR MCC' AS fuente, *
FROM tc_tcomerdiferi WITH (NOLOCK) 
WHERE cd_comercio = 248827 AND cd_categoria = @MCC

--CONSULTAR VALORES POR DEFAULT
SELECT *
 FROM tc_tcomerdiferi 
WHERE cd_comercio IS NULL AND cd_categoria IS NULL
*/