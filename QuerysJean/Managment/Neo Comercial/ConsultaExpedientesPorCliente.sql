--IP Servidor: 172.26.60.105,4433
--Usuario: usr_bizagi
--Clave: Bg123456 


--CONSULTA EXPEDIENTES DE UN CLIENTE
SELECT ex.idexpediente,
ex.usuarioCrea,
(select usuario.strvalor
from [BG_NEO].DBO.bg_adm_catalogosdetalles oficial WITH (NOLOCK),
[BG_NEO].DBO.BG_ADM_CatalogosDetalles usuario WITH (NOLOCK)
where oficial.idcodigo=pe.idCatDetOficial
and usuario.idCatalogo = 154 AND usuario.strCodigoHost = oficial.strCodigoHost and usuario.idEstado = 1) oficial
FROM [BG_NEO].DBO.BG_EXP_Expediente ex with(nolock) ,
[BG_NEO].DBO.BG_EXP_ExpedienteDetalle ed with(nolock),
[BG_NEO].DBO.BG_PRO_Productos pr with(nolock),
[BG_NEO].DBO.BG_PER_Personas pe with(nolock)
WHERE ex.idEstado not in(2847, 2849,2851,2949,3034,4808,6460,2850, 9667, 1929)
AND pr.idCodigoProductoPadre in (1,2,3,4,6,7,9,14,17,19,21,22,23,24,26,29,31,33,35,44,59)
AND ed.idExpediente = ex.idExpediente
AND pr.idCodigo = ed.idProducto
AND pe.idPersona = ex.idPersona
AND pe.numIdentificacion = '1758868457' 

    /*
        2847 = Rechazado
        2849 = Expirado
        2850 = Activado
        2851 = Terminado
        3034 = Rechazado Foto
        4808 = Solicitud Eliminada
        9667 = Credito no Recomendado
    */
select * from [BG_NEO].DBO.BG_EXP_ColaProcesosMigracionCuentas where fechaCrea >= '2025-06-16' and idExpediente='21032889' order by fechaCrea;
select * from [BG_NEO].DBO.BG_EXP_Expediente where  idExpediente='21032889' 


--172.26.60.105.4433
--usr_bizagi
--Bg123456
--CONSULTAR BIOMETRÍA NEO
--Consuta de los registros
SELECT top(10 )* FROM [BG_NEO].[dbo].[BG_EXP_RegistroValidacionBiometrica] 
where idProducto='2958' 
order by fechaCrea desc
