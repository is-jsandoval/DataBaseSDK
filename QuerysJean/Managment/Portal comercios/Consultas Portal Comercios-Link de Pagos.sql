--- sp para ingresar usuarios
use BG_PortalComercios
exec sp_helptext[sp_ConUsuarioCriterio]


SELECT  usuario_creacion,*  FROM [BG_PortalComercios].[dbo].[Usuario] order by fecha_creacion desc
