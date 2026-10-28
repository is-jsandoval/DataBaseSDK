  /****** Script para el comando SelectTopNRows de SSMS  ******/
SELECT *
  FROM [nts_tarjcred].[dbo].[tc_agregador_terminal]

SELECT *
  FROM [nts_tarjcred].[dbo].tc_comercios where co_comercio='232002'


  SELECT *
  FROM [nts_tarjcred].[dbo].tc_comercios_alternos where ca_codalterno='6000000002'

SELECT *
  FROM [nts_tarjcred].[dbo].tc_comercio_empresa

use nts_tarjcred
  SELECT ca_comercio,co_codalterno,co_mid,*
  FROM [nts_tarjcred].[dbo].tc_comercios left join  [nts_tarjcred].[dbo].tc_comercios_alternos on co_comercio=ca_comercio order by co_ultestacionactualiza DESC
