/**
172.26.60.105,4433
usr_bizagi
Bg_123456
*/
USE BG_NEO
SELECT * FROM [BG_NEO].[dbo].[BG_EXP_SolicitudContactoMicrocredito] where identificacion = '0903603348'



SELECT * FROM [BG_NEO].[dbo].[BG_EXP_SolicitudContactoMicrocredito] where identificacion='0940231277' order by fechaSolicitud desc


select top 100 *
from  [BG_NEO].[dbo].BG_ADM_CorreosEnviados
where FechaEnvio >= '2026-08-03' order by FechaEnvio desc

select top 10 *
from  [BG_NEO].[dbo].BG_ADM_CorreosEnviados
where strAsunto= 'Respuesta a tu formulario de Microcrédito'
and FechaEnvio >= '2026-08-03' order by FechaEnvio desc

select top 100 *
from  [BG_NEO].[dbo].BG_ADM_CorreosEnviados
where usrIngreso='GGP' order by FechaEnvio desc