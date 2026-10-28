---172.26.60.61,4433
--monitran
--Bg123456
SELECT *
  FROM [BG_InternalApps].[LDP_ContratosContacto_MC].[BG_MC_ContratosPersonas] 
  where Identificacion='0903858967' order by FechaHoraAceptacion

delete [BG_InternalApps].[LDP_ContratosContacto_MC].[BG_MC_ContratosPersonas] where 
Identificacion='0903858967'
