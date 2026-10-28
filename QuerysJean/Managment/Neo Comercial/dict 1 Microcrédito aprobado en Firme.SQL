--172.26.60.132
--- dict 1 , Microcrédito aprobado en Firme
 select top 100
      POL.CODIGO_ID as Cedula	
      ,[IdPersona]
	  ,[Clte]	 
	  ,POL.CLTE_ID
      ,[IdPerfil]
	  ,[IdPerfilAprobacion]
      ,[IdDictamen]
      ,[SegmentoEstrategico]
	  ,FuenteIngresoFinal 
	  ,FechaIngresoTrabajo
	  ,Ocupacion
	  ,NombreEmpresaTrabaja
	  ,[PerfilRiesgos]
      ,[SubPerfilRiesgos]
      ,[FechaPerfilRiesgo]
	  ,[IngresoFinal]
	  ,[Score]
	  ,MontoAnteriorCreditoMI
	  ,ValorDeudaMIenSF
      ,[FechaCalculo]
      ,[FechaBuro]
	  ,[FechaCarga]
	  ,[FechaActualizacion]
	  ,[CapacidadEndeudamientoMensual]
      ,[CapacidadEndeudamientoActualizado]
      ,[MontoCreditoActualizado]
      ,[CupoTarjetaActualizado]
      ,[MontoCreditoAutoActualizado]        
 FROM BG_DWHAPPS.PRECALIFICADA.[BG_PROC_PERSONA_CALIFICADA] 
 INNER JOIN BG_DWHAPPS.Politicas.POL_CLTE_POLITICAS POL 
     ON CLTE = POL.CLTE_ID
 where
 SegmentoEstrategico='MI' and
 IdDictamen = 1 and 
 Idperfil in (7,8,11,13) and
 PerfilRiesgos IN ('AA','AAA') AND
 ---SubPerfilRiesgos in (1,2) and 
 CapacidadEndeudamientoMensual > 100 and
 clte is not null and
 score > 0  and 
 FuenteIngresoFinal is not null and
 FechaIngresoTrabajo is not null and
 Ocupacion is not null and
 NombreEmpresaTrabaja is not null
 
 ORDER BY FechaActualizacion



  select top(100)  
idPerfil,
IdDictamen,
 SegmentoEstrategico,
SubPerfilRiesgos ,
 CapacidadEndeudamientoMensual ,
 clte ,
 score ,
 FuenteIngresoFinal ,
 FechaIngresoTrabajo ,
 Ocupacion ,
 NombreEmpresaTrabaja ,* from BG_DWHAPPS.PRECALIFICADA.[BG_PROC_PERSONA_CALIFICADA]  
  INNER JOIN BG_DWHAPPS.Politicas.POL_CLTE_POLITICAS POL 
     ON CLTE = POL.CLTE_ID where   POL.CODIGO_ID='1758868457'




 
  select top(1000)  
POL.CODIGO_ID,* from BG_DWHAPPS.PRECALIFICADA.[BG_PROC_PERSONA_CALIFICADA]  
  INNER JOIN BG_DWHAPPS.Politicas.POL_CLTE_POLITICAS POL 
     ON CLTE = POL.CLTE_ID 
	 where 	 idPerfil='8'
 and IdDictamen='0'
 and SegmentoEstrategico='MI'
 and SubPerfilRiesgos in (1)  
 and CapacidadEndeudamientoMensual > 100 
 and clte is not null 
 and score > 900   
 --and FuenteIngresoFinal is not null 
-- and FechaIngresoTrabajo is not null 
 --and Ocupacion is not null 
 and MaximaDeudaTc >900 
 and MaximoCupoTc>1000 
 and FuenteIngresoFinal='EQUIFAX' 
 and Ocupacion  is null 
 and SegmentoRiesgos is null 
 and NombreEmpresaTrabaja is null 
 and PerfilRiesgos='B2'
 and DiasMoraSistemaFinanciero is null
 and Ciudad is not null
 order by FechaActualizacion desc


 select * from BG_DWHAPPS.PRECALIFICADA.[BG_PROC_PERSONA_CALIFICADA]   where idPersona='10915906'--jean