--CONSULTAR RESPUESTA DE ERROR 204
 
select * from nts_monitran..vw_catalogo where dc_cod = '0841' and ca_nombre = 'CodRespInterno'--217
select * from nts_monitran..vw_catalogo where dc_cod = '0012' and ca_nombre = 'CodRespInterno'--217
select distinct ca_nombre from nts_monitran..vw_catalogo where ca_nombre like '%resp%'