--172.26.6.105,4433
--tabla expedientes participantes
SELECT TOP (1000) t2.Nombre,t1.*FROM [BG_NEO].[dbo].[BG_EXP_ExpedienteParticipantesEvaluadorPoliticas] t1
left join BG_NEO.dbo.BG_PER_Personas t2 on t1.idPersonaNeo=t2.idPersona
  ORDER BY fechaCreacion DESC 


select * from [BG_NEO].[dbo].BG_PRO_ValidaReglasParticipantes

--tablas validacion politicas general
SELECT TOP (1000) t3.Nombre,t1.*FROM [BG_NEO].[dbo].[BG_EXP_ValidacionPoliticas] t1 
left join [BG_NEO].[dbo].[BG_EXP_ExpedienteParticipantesEvaluadorPoliticas] t2 on t1.idExpediente=t2.idExpediente
left join BG_NEO.dbo.BG_PER_Personas t3 on t2.idPersonaNeo=t3.idPersona
--where t1.idProceso = 158
ORDER BY fechaSolicitud DESC

--tablas validacion politicas detalle
SELECT TOP (1000) *FROM [BG_NEO].[dbo].[BG_EXP_ValidacionPoliticasDetalle]
 -- where idProceso = 158
  ORDER BY fechaModificacion DESC
  
 
  
--update [BG_NEO].[dbo].BG_PRO_ValidaReglasParticipantes
--set actualizaInfoBuro = 1
--where idValidaReglaParticipante = 2