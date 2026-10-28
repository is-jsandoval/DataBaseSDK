-- Servidor 172.26.60.132   
-- Usuario: apl_rcsa
-- Clave: Bg456789
-- Base BG_DWHAPPS
Use Base BG_DWHAPPS
/*
Consultas para obtener datos de prueba con diferentes idDictamen
Esas consultas están filtradas por IdDictamen principalmente, los otros criterios de búsqueda son más específicos.
Al final de los dos primeros queries, se ha agregado una consulta hacia la base de registro civil para traer los casados
*/
select top 1000 * from Politicas.POL_CLTE_POLITICAS where tipo_id = 'R' AND NOMBRE IS NOT NULL ORDER BY NOMBRE ASC

----- dictamen 1 	Multicrédito en Firme 
select top 20 POL.CODIGO_ID as Cedula	
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
 IdDictamen = 1 and 
 Idperfil in (1,2,3,4,5,6) and
 PerfilRiesgos IN ('AA','AAA') AND
 --SubPerfilRiesgos in (1,2) and 
 CapacidadEndeudamientoMensual > 300 and
 clte is not null and
 score > 700  and 
 FuenteIngresoFinal is not null and
 FechaIngresoTrabajo is not null and
 Ocupacion is not null and
 NombreEmpresaTrabaja is not null and
 SegmentoEstrategico is not null
 --Esta parte del where, lo agregué para el tema de seguro de cónyuge
 AND EXISTS (SELECT CEDULA
                 FROM [172.26.60.248].REGCIVIL.dbo.REGISTRO_CIVIL R
                 WHERE R.CEDULA COLLATE SQL_Latin1_General_CP1_CI_AS = POL.CODIGO_ID COLLATE SQL_Latin1_General_CP1_CI_AS AND DES_ESTADO_CIVIL = 'CASADO'
                )
				ORDER BY idpersona desc
 
 select * from [PRECALIFICADA].[BG_TC_CAMPAÑAS]

 1801739655   - 1823062

 0930958079   

 update [PRECALIFICADA].[BG_TC_CAMPAÑAS]
 set id_persona = 1823062
 where identificacion = 1801739655

 1200571915

 ---- dictamen 2 Multicrédito Condicionado
 select top 20
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
 FROM PRECALIFICADA.[BG_PROC_PERSONA_CALIFICADA] 
 INNER JOIN Politicas.POL_CLTE_POLITICAS POL 
     ON CLTE = POL.CLTE_ID
 where
 IdDictamen = 2 and
 idperfil in (1,2,3,4,5,6) and 
 ----PerfilRiesgos IN ('AA','AAA') AND
 ---SubPerfilRiesgos in (1,2) and 
 CapacidadEndeudamientoMensual > 300 and
 clte is not null and
 score > 0 AND
 FuenteIngresoFinal is not null and
 FechaIngresoTrabajo is not null and
 Ocupacion is not null and
 NombreEmpresaTrabaja is not null
 --Esta parte del where, lo agregué para el tema de seguro de cónyuge
  AND EXISTS (SELECT *
                 FROM [172.26.60.248].REGCIVIL.dbo.REGISTRO_CIVIL R
                 WHERE R.CEDULA COLLATE SQL_Latin1_General_CP1_CI_AS = POL.CODIGO_ID COLLATE SQL_Latin1_General_CP1_CI_AS AND DES_ESTADO_CIVIL = 'CASADO'
                )
				--ORDER BY clte desc

 
 ---- Este query devuelve todos los Negados
 ---- dictamen 0 es negado
 select top 20
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
 FROM PRECALIFICADA.[BG_PROC_PERSONA_CALIFICADA] 
 INNER JOIN Politicas.POL_CLTE_POLITICAS POL 
     ON CLTE = POL.CLTE_ID
 where
 IdDictamen = 0 and
 idperfil in (1,2,3,4,5,6) and 
 PerfilRiesgos NOT IN ('AA','AAA') AND
 SubPerfilRiesgos in (1,2) and 
 ---CapacidadEndeudamientoMensual > 300 and
 clte is not null and
 score < 500 AND
 FuenteIngresoFinal is not null and
 FechaIngresoTrabajo is not null and
 Ocupacion is not null and
 NombreEmpresaTrabaja is not null
 
 ----------- dict 1 , Microcrédito aprobado en Firme
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
 FROM PRECALIFICADA.[BG_PROC_PERSONA_CALIFICADA] 
 INNER JOIN Politicas.POL_CLTE_POLITICAS POL 
     ON CLTE = POL.CLTE_ID
 where
 --SegmentoEstrategico='MI' and
 IdDictamen = 1 and 
 Idperfil in (7,8,11,13) and
 PerfilRiesgos IN ('AA','AAA') AND
 --SubPerfilRiesgos in (1,2) and 
 CapacidadEndeudamientoMensual > 100 and
 clte is not null and
 score > 0  and 
 FuenteIngresoFinal is not null and
 FechaIngresoTrabajo is not null and
 Ocupacion is not null and
 NombreEmpresaTrabaja is not null
 ORDER BY FechaActualizacion
 
 ---- DICT 2 Microcrédito condicionado
 select top 20
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
 FROM PRECALIFICADA.[BG_PROC_PERSONA_CALIFICADA] 
 INNER JOIN Politicas.POL_CLTE_POLITICAS POL 
     ON CLTE = POL.CLTE_ID
 where
 IdDictamen = 2 and 
 Idperfil in (7,8,11,13) and
 ---PerfilRiesgos IN ('AA','AAA') AND
 ---SubPerfilRiesgos in (1,2) and 
 CapacidadEndeudamientoMensual > 100 and
 clte is not null and
 score > 0  and 
 FuenteIngresoFinal is not null and
 FechaIngresoTrabajo is not null and
 Ocupacion is not null and
 NombreEmpresaTrabaja is not null  and POL.CODIGO_ID = '0927002865'
  
 -------- NEGADOS
 ---- dictamen 0
 select top 20
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
 FROM PRECALIFICADA.[BG_PROC_PERSONA_CALIFICADA] 
 INNER JOIN Politicas.POL_CLTE_POLITICAS POL 
     ON CLTE = POL.CLTE_ID
 where
 IdDictamen = 0 and
 idperfil in (1,2,3,4,5,6) and 
 PerfilRiesgos IN ('AA','AAA') AND
 SubPerfilRiesgos in (1,2) and 
 ---CapacidadEndeudamientoMensual > 300 and
 clte is not null and
 score > 0 AND
 FuenteIngresoFinal is not null and
 FechaIngresoTrabajo is not null and
 Ocupacion is not null and
 NombreEmpresaTrabaja is not null  and POL.CODIGO_ID = '0927002865'
 
 ----------- dictamen -1 , Multicrédito sin dictamen
 select top 20
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
 FROM PRECALIFICADA.[BG_PROC_PERSONA_CALIFICADA] 
 INNER JOIN Politicas.POL_CLTE_POLITICAS POL 
     ON CLTE = POL.CLTE_ID
 where POL.CODIGO_ID = '0927002865'
 
 