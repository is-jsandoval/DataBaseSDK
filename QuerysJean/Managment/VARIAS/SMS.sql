--Consulta de sms (OTP) generado
--172.26.60.179,4433
--sms_admin/sms_admin



select * from sms_cliente.dbo.sms_enviados order by en_codigo desc
select * from sms_cliente.dbo.sms_enviados  where en_telefono='0954121646' order by en_hora_env desc
select * from sms_cliente.dbo.sms_enviados   order by en_hora_env desc
select * from sms_cliente.dbo.sms_codigos  
/*

BG informa que el codigo para contratar su producto Credito Empresarial es 783418. El tiempo de vigencia del codigo de seguridad es 3 minutos.


*/


