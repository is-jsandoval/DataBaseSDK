--172.26.60.105,4433

--Eliminar registro expediente neo

If OBJECT_ID (N'tempdb.dbo.#listExp', N'U') Is Not Null
DROP TABLE #listExp
-----------------------------------------------------------------------
declare @numIdentificacion varchar(13);
set  @numIdentificacion='0903603348'
SELECT idExpediente into #listExp from  [BG_NEO].[dbo].BG_EXP_Expediente where idPersona in (SELECT idPersona FROM [BG_NEO].[dbo].[BG_PER_Personas] where numIdentificacion=@numIdentificacion)
-------------------------------------------------------------------------
select  * from [BG_NEO].[dbo].BG_EXP_Expediente
where idPersona in (SELECT idPersona FROM [BG_NEO].[dbo].[BG_PER_Personas] where numIdentificacion=@numIdentificacion)
order by fechaIngreso desc


-------------------------------------------------------------------------

delete [BG_NEO].[dbo].BG_EXP_ExpedienteSolicitudProductoDetalle  where  idExpediente in (select idExpediente from #listExp)
delete [BG_NEO].[dbo].BG_EXP_ExpedienteSolicitudPersona  where  idExpediente in (select idExpediente from #listExp)
delete [BG_NEO].[dbo].BG_EXP_ExpedienteSolicitudProductoActivos  where  idExpediente in (select idExpediente from #listExp)
delete [BG_NEO].[dbo].BG_EXP_ExpedienteSolicitudProductoServicios  where  idExpediente in (select idExpediente from #listExp)
delete [BG_NEO].[dbo].BG_EXP_ExpedienteScoreConsumoDetalle where  idExpedienteScoreConsumo in (select [idCodigo] from  [BG_NEO].[dbo].BG_EXP_ExpedienteScoreConsumo where  idExpediente in (select idExpediente from #listExp))
delete [BG_NEO].[dbo].BG_EXP_ExpedienteScoreConsumo  where  idExpediente in (select idExpediente from #listExp)
delete [BG_NEO].[dbo].BG_EXP_ExpedienteSolicitud where  idExpediente in (select idExpediente from #listExp)
delete [BG_NEO].[dbo].BG_EXP_ExpedienteHistorico where  idExpediente in (select idExpediente from #listExp)
delete [BG_NEO].[dbo].BG_EXP_ExpedienteDetalle where idExpediente in (select idExpediente from #listExp)
delete [BG_NEO].[dbo].BG_EXP_ExpedienteDetalle where idExpediente in (select idExpediente from #listExp)
delete [BG_NEO].[dbo].BG_EXP_ExpedienteSolicitudPasivosFirmantes where idExpediente in (select idExpediente from #listExp)
delete [BG_NEO].[dbo].BG_EXP_ColaEnvioNotificaciones where idExpediente in (select idExpediente from #listExp)
delete [BG_NEO].[dbo].BG_EXP_LogContratacionSeguros where idExpediente in (select idExpediente from #listExp)
delete [BG_NEO].[dbo].BG_EXP_ExpedienteDocumentos where idExpediente in (select idExpediente from #listExp)
delete [BG_NEO].[dbo].BG_EXP_Expediente where idPersona in (SELECT idPersona FROM [BG_NEO].[dbo].[BG_PER_Personas] where numIdentificacion=@numIdentificacion)






--USE [BG_AfiliacionComercios]
--GO
 
--EXEC	[CREDITO_FACTURACION].[BG_SP_EXPEDIENTE_COMERCIOS_ADICIONAL]
--		@opcion = 1,
--		@idExpediente = 21048078,
--		@monto = 1000,
--		@porcentajeAbono = 6
 
 
--GO
 
--EXEC	[CREDITO_FACTURACION].[BG_SP_EXPEDIENTE_COMERCIOS_ADICIONAL]
--		@opcion = 2,
--		@idExpediente = 21048078,
--		@monto = NULL,
--		@porcentajeAbono = NULL