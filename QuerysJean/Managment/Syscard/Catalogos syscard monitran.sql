--204

select * from nts_monitran..vw_catalogo where ca_nombre like '%TipoCuenta%'

select * from nts_monitran..vw_catalogo where ca_nombre like '%Institucion%' --10 pichincha   --37 BOLIVARIANO     --36 Produbanco  -24 CYTIBANCK
select * from nts_monitran..vw_catalogo where ca_nombre like '%Institucion%' and dc_valor1 like '%R01%'
select * from nts_monitran..vw_catalogo where ca_nombre like '%Institucion%' and dc_cod like '%R01%'
 