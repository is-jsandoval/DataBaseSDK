declare @comercio varchar(13);
set @comercio='206963';

delete from BG_TComercios.Pagos.lp_limites where lt_comercio  = @comercio
delete from BG_TComercios.Pagos.lp_links_logs where lg_ruc = @comercio
delete from BG_TComercios.Pagos.lp_links where ln_ruc = @comercio
delete from BG_TComercios.Pagos.lp_comercios where co_comercio_syscard = @comercio
delete from BG_TComercios.Pagos.lp_links where ln_ruc = @comercio
delete from BG_TComercios.Pagos.lp_links_logs where lg_ruc = @comercio

