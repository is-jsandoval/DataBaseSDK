--172.26.60.105.4433
SELECT Top(10) *
  FROM [BG_NEO].[dbo].BG_EXP_Expediente ex
 inner join  [BG_NEO].[dbo].BG_EXP_ExpedienteDetalle ed on ex.idExpediente=ed.idExpediente  
  where ex.idExpediente='21058159'

  select * from [BG_NEO].[dbo].BG_PER_Personas where idPersona='7484379' 



SELECT*
  FROM [BG_NEO].[dbo].[BG_PRO_Productos]  where idCodigo='2954'--CREDIGO


  SELECT*
  FROM [BG_NEO].[dbo].[BG_PRO_Productos]  where numtipoProducto='254'--CREDIGO

SELECT*
  FROM [BG_NEO].[dbo].[BG_PRO_Productos] where idCodigoProductoPadre='50'



SELECT Top(10) *
  FROM [BG_NEO].[dbo].BG_EXP_Expediente ex
 inner join  [BG_NEO].[dbo].BG_EXP_ExpedienteDetalle ed on ex.idExpediente=ed.idExpediente  
  where ex.idExpediente='21058159'

  select * from [BG_NEO].[dbo].BG_PER_Personas where idPersona='7484379' 

--ver los estados de expediente
SELECT distinct strValor2
  FROM [BG_NEO].[dbo].BG_EXP_Expediente ex
		left join [BG_NEO].[dbo].BG_EXP_ExpedienteDetalle exd with(nolock) on ex.idExpediente = exd.idExpediente
		left join [BG_NEO].[dbo].BG_PER_Personas pe with(nolock) on pe.idPersona = ex.idPersona
		left join [BG_NEO].[dbo].BG_PRO_Productos pro with(nolock) on pro.idCodigo = exd.idProducto
		left join [BG_NEO].[dbo].BG_EXP_AnalisisCreditoCalculos ac with(nolock) on ac.idExpediente = ex.idExpediente
		left join [BG_NEO].[dbo].BG_EXP_ExpedienteControlCustodio cc with(nolock) on cc.idExpediente = ex.idExpediente and cc.idEstado = 1
		LEFT JOIN [BG_NEO].[dbo].BG_ADM_CatalogosDetalles  cat1 on cat1.idCodigo = ex.idEstado
  where exd.idProducto='250'
