If OBJECT_ID (N'tempdb.dbo.#comerciosLista', N'U') Is Not Null
DROP TABLE #comerciosLista
-----------------------------------------------------------------------
select co_comercio,co_ruc into #comerciosLista from 
nts_tarjcred.dbo.tc_comercios where co_linkpagos='S'and co_ruc not in ('0954121646001','0903603348001') and co_comercio  in ('259451','259452','255405') 
-------------------------------------------------------------------------------------------------------para resetear los comercios sin link de pagos 
update nts_tarjcred.dbo.tc_comercios set co_linkPagos =null where co_comercio  in (select co_comercio from #comerciosLista)
-----------------------------------------------------------------------------------------------------------------------------
delete BG_TComercios.Pagos.lp_links_logs where lg_ruc   in (select co_ruc from #comerciosLista)
delete BG_TComercios.Pagos.lp_links where ln_ruc  in (select co_ruc from #comerciosLista)
delete BG_TComercios.Pagos.lp_consumos_link_estatico where ce_codigo_link_estatico in (select le_codigoLink from BG_TComercios.Pagos.lp_comercios_link_estaticos where le_ruc  in (select co_ruc from #comerciosLista))
delete BG_TComercios.Pagos.lp_comercios_link_estaticos where le_ruc   in (select co_ruc from #comerciosLista)
delete BG_TComercios.Pagos.lp_limites where lt_comercio   in (select co_comercio from #comerciosLista)
delete BG_TComercios.Pagos.lp_comercios where co_ruc  in (select co_ruc from #comerciosLista)
