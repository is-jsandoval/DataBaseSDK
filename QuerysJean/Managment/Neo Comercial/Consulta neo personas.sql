--172.26.60.105,4433

/****** Script para el comando SelectTopNRows de SSMS  ******/
SELECT *
  FROM [BG_NEO].[dbo].[BG_PER_Personas] where numIdentificacion='0954121646'


  /****** Script para el comando SelectTopNRows de SSMS  ******/
SELECT TOP (1000) *
  FROM [BG_NEO].[dbo].[BG_PER_PersonasNaturales]

  /****** Script para el comando SelectTopNRows de SSMS  ******/
SELECT *
  FROM [BG_NEO].[dbo].[BG_PER_DatosActualizar] where NumIdentificacion='1758868457'