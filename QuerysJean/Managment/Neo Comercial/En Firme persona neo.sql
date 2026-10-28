----- dictamen 1 	Multicrédito en Firme 
select top 1000 POL.CODIGO_ID as Cedula	
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
FROM PRECALIFICADA.[BG_PROC_PERSONA_CALIFICADA] 
INNER JOIN Politicas.POL_CLTE_POLITICAS POL 
     ON CLTE = POL.CLTE_ID
where 
IdDictamen = 1 
--and POL.CODIGO_ID='1723382220'
--and Idperfil in (1,2,3,4,5,6) and
and PerfilRiesgos IN ('AA','AAA') 
--SubPerfilRiesgos in (1,2) and 
--CapacidadEndeudamientoMensual > 300 and
--clte is null and
--score > 700  and 
and FuenteIngresoFinal ='DATAFAST'
and FechaIngresoTrabajo is not null
and Ocupacion is not null
--NombreEmpresaTrabaja is not null and
and SegmentoEstrategico = 'PE'


--select distinct FuenteIngresoFinal from PRECALIFICADA.[BG_PROC_PERSONA_CALIFICADA]