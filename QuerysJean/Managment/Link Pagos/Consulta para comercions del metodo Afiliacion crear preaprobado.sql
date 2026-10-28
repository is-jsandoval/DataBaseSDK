--204
select * from  BG_TComercios.Pagos.lp_comercios where co_ruc='1757516727001' order by  co_fch_creacion desc
select co_linkPagos,* from nts_tarjcred.dbo.tc_comercios where co_comercio='226123'



delete from BG_TComercios.Pagos.lp_limites where lt_comercio =(Select co_comercio_syscard from BG_TComercios.Pagos.lp_comercios where co_ruc='1757516727001')
delete  from  BG_TComercios.Pagos.lp_comercios where co_ruc='1757516727001'
Update nts_tarjcred.dbo.tc_comercios set co_linkPagos = NULL  where co_comercio='226123'

