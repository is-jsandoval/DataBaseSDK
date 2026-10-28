
declare @pp_comercio int = 43, @pp_empresa int = 1
 
select * from nts_tarjcred.dbo.tc_comercios with(nolock) where co_comercio = @pp_comercio
select * from nts_tarjcred.dbo.tc_comercio_empresa with(nolock) where ce_comercio = @pp_comercio and ce_empresa = @pp_empresa
select * from nts_tarjcred.dbo.tc_comercios_alternos with(nolock) where ca_comercio = @pp_comercio
select * from nts_tarjcred.dbo.tc_cafiliaciones with(nolock) where ca_comercio = @pp_comercio and ca_empresa = @pp_empresa
select * from nts_tarjcred.dbo.tc_comercio_marca with(nolock) where cm_comercio = @pp_comercio and cm_empresa = @pp_empresa
select * from nts_tarjcred.dbo.tc_tcomerdiferi with(nolock) where cd_comercio = @pp_comercio and cd_empresa = @pp_empresa
 


select * from nts_tarjcred.dbo.tc_comercios with(nolock) where co_comercio = @pp_comercio
select * from nts_tarjcred.dbo.tc_comercio_empresa with(nolock) where ce_comercio = @pp_comercio and ce_empresa = @pp_empresa
select * from nts_tarjcred.dbo.tc_comercios_alternos with(nolock) where ca_comercio = @pp_comercio
select * from nts_tarjcred.dbo.tc_cafiliaciones with(nolock) where ca_comercio = @pp_comercio and ca_empresa = @pp_empresa
select * from nts_tarjcred.dbo.tc_comercio_marca with(nolock) where cm_comercio = @pp_comercio and cm_empresa = @pp_empresa
select * from nts_tarjcred.dbo.tc_tcomerdiferi with(nolock) where cd_comercio = @pp_comercio and cd_empresa = @pp_empresa