--172.26.60.61,4433

SELECT * FROM [BG_InternalApps].[LDP_ContratosContacto_MC].[BG_MC_Contratos]
where [Descripcion]='CONTRATO LINK DE PAGO'
or ID='4049'

SELECT *  FROM [BG_InternalApps].[LDP_ContratosContacto_MC].[BG_MC_ContratosPersonas]
where IdContrato='4049'
order by FechaHoraAceptacion


--172.26.60.61,4433

SELECT * FROM [BG_InternalApps].[LDP_ContratosContacto_MC].[BG_MC_Contratos]
where [Descripcion] like '%Q5%'
or ID='4049'

SELECT *  FROM [BG_InternalApps].[LDP_ContratosContacto_MC].[BG_MC_ContratosPersonas]
where IdContrato='4049'
order by FechaHoraAceptacion