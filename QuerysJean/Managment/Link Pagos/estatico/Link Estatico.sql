--204
-- link estaticos
SELECT *   FROM [BG_TComercios].[Pagos].[lp_comercios_link_estaticos] where le_codigoLink='000001'

-------------------------------------------------------------------------------------sp_Consult Link estatico
USE [BG_TComercios]
GO

DECLARE @mensaje varchar(max)

EXEC [Pagos].[sp_consulta_link_estatico]
    @pp_codigoLink = '000001',  -- pon el código real aquí
    @pp_mensajeRespuesta = @mensaje OUTPUT

SELECT @mensaje AS MensajeRespuesta
-------------------------------------------------------------------------------------Editar link estatico

--update [BG_TComercios].[Pagos].[lp_comercios_link_estaticos] set  le_ruc='0954121646001' where  le_codigoLink='000001'


-------------------------------------------------------------------------------------Editar rango de hora link estatico
--UPDATE [BG_TComercios].[Pagos].[lp_comercios_link_estaticos]SET le_hora_inicio = CONVERT(time, DATEADD(HOUR, 2, GETDATE())),le_hora_fin= CONVERT(time, DATEADD(HOUR, 4, GETDATE()))
WHERE le_codigoLink = '000008'



--------------------------------------------------------------------------------------comercio para link estatico
select * from [BG_TComercios].[Pagos].[lp_comercios_link_estaticos] where le_ruc ='0954121646001'
select * from [BG_TComercios].[Pagos].lp_consumos_link_estatico where le_ruc ='0954121646001'

--insert into [BG_TComercios].[Pagos].[lp_comercios_link_estaticos] values ('000091','0954121646001','N','C','08:00:00.0000000','20:00:00.0000000','A','2026-03-30 15:23:17.120','QATEST','2026-04-01 10:33:49.947','TESTQA','170.238.171.161')




select * from [BG_TComercios].[Pagos].lp_links_logs order by lg_fecha desc
select * from [BG_TComercios].[Pagos].lp_links     order by ln_fecha desc


select * from nts_tarjcred.dbo.tc_atrnautorizalog order by la_fchsis desc