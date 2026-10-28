-- ---------------------------------------------------------------------------------------------------------------- --
--
-- Obetener Datos para pruebas : POLITICAS BURO
-- Server: 172.26.60.18,4433 / usr_bizagi / Bgxxxx
-- ---------------------------------------------------------------------------------------------------------------- --
declare @tipo_identificacion varchar(1) = 'C';
declare @identificacion varchar(13) = '1350878185';
declare @politica varchar(20) = 'POLITICA1'; -- POLITICA2, POLITICA8, POLITICA9, POLITICA10, POLITICA16, POLITICA17
declare @politica_valor varchar(2) = 'S';
declare @sql nvarchar(max);
declare @registros varchar(2) = '10';
-- ---------------------------------------------------------------------------------------------------------------- --
-- Obtiene n registros --
-- ---------------------------------------------------------------------------------------------------------------- --
set @sql = N'
select top ' + @registros + ' bdp.*
from BG_BURO.dbo.BURO_DEUDOR_POLITICAS_BG as bdp with(nolock)
where ' + QUOTENAME(@politica) + N' = @valor
and   NUMERO_DOCUMENTO = ISNULL(@identifica,NUMERO_DOCUMENTO)
order by bdp.NUMERO_DOCUMENTO';

exec sp_executesql
    @sql,
    N'@valor varchar(2)',
    @valor = @politica_valor;
-- ---------------------------------------------------------------------------------------------------------------- --

/*
select top 10 *
from BG_BURO.dbo.BURO_DEUDOR_POLITICAS_BG as bdp with(nolock)
where POLITICA1 = 'S'
union
select top 10 *
from BG_BURO.dbo.BURO_DEUDOR_POLITICAS_BG as bdp with(nolock)
where POLITICA2 = 'S'
union 
select top 10 *
from BG_BURO.dbo.BURO_DEUDOR_POLITICAS_BG as bdp with(nolock)
where POLITICA8 = 'S'
union 
select top 10 *
from BG_BURO.dbo.BURO_DEUDOR_POLITICAS_BG as bdp with(nolock)
where  POLITICA9 = 'S'
union 
select top 10 *
from BG_BURO.dbo.BURO_DEUDOR_POLITICAS_BG as bdp with(nolock)
where POLITICA10 = 'S'
union 
select top 10 *
from BG_BURO.dbo.BURO_DEUDOR_POLITICAS_BG as bdp with(nolock)
where POLITICA16 = 'S'
union 
select top 10 *
from BG_BURO.dbo.BURO_DEUDOR_POLITICAS_BG as bdp with(nolock)
where POLITICA17 = 'S'
ORDER BY 2
*/