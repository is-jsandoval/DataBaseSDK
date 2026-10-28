--204
-- link estaticos
SELECT *   FROM [BG_TComercios].[Pagos].[lp_comercios_link_estaticos] where le_codigoLink='000001'


----------------------------------------------registra consumo Link
USE [BG_TComercios]
GO

EXEC [Pagos].[sp_registraConsumoLinkEstatico]
    @pp_comercio              = 258868,
    @pp_codigo_link_estatico  = '000001',
    @pp_cliente               = 'Jean San 11',
    @pp_motivo                = 'Pago de servicio',
    @pp_monto                 = 1500.00
------------------------------------------------------------------------

--204
-- link estaticos
SELECT *   FROM [BG_TComercios].[Pagos].lp_consumos_link_estatico order by ce_fecha desc

--eliminar 
delete from  [BG_TComercios].[Pagos].lp_consumos_link_estatico where ce_cliente='Jean San' and ce_motivo='test consumo desde enpoint'
