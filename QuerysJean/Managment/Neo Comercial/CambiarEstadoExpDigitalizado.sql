--IP Servidor: 172.26.60.105,4433
--Usuario: usr_bizagi
--Clave: Bg123456 
/*
use BG_NEO
go
update BG_EXP_ExpedienteDocumentos
set idEstadoDocumento=3082
where idExpediente=*/

select * from BG_NEO..BG_EXP_ExpedienteDocumentos where idExpediente='21033912'