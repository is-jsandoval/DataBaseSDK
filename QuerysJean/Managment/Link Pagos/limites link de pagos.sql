--172.26.61.204,4433
declare @ruc varchar(13);
declare @com varchar(13);
set @ruc='0954121646001';--cambiar por numero de ruc 

set @com =(select co_comercio_syscard from BG_TComercios.Pagos.lp_comercios where co_ruc=@ruc)
-----------------------------------------------------------------------------------------------------------------------------
select lt_consumido_diario,lt_consumido_quincenal,lt_consumido_mensual, * from BG_TComercios.Pagos.lp_limites where  lt_comercio=@com
select * from BG_TComercios.Pagos.lp_links_logs where lg_ruc = @ruc order by lg_fecha desc
select * from BG_TComercios.Pagos.lp_links where ln_ruc = @ruc

--reiniciar comercio
update BG_TComercios.Pagos.lp_limites set lt_consumido_diario=0,lt_consumido_quincenal=0,lt_consumido_mensual=0 where lt_comercio=@com

--limitar comercio
--update BG_TComercios.Pagos.lp_limites set lt_consumido_diario=1000,lt_consumido_quincenal=5000,lt_consumido_mensual=10000 where lt_comercio=@com