CREDENCIALES DE LA BASE
/*
172.26.60.246,4433
usrdesarrollo
Bg123456
*/

 --Asisgnar roles cunado se crea un usuario en bce

declare @usuario varchar(100)
declare @usuario_anterior varchar(100) set @usuario ='LIZA DEVON 3'
set @usuario_anterior ='FERNANDO MENDOZA'
insert into EasySeguridad..UserEmpServ (usuario,id_empresa,id_servicio,estado,visualizadetalle,condicion_pago)
select @usuario, id_empresa,id_servicio,estado,visualizadetalle,condicion_pago from EasySeguridad..UserEmpServ where usuario = @usuario_anterior
 
delete EasySeguridad..UserEmpServ where usuario ='LIZA DEVON 3' 
select Alias ,Tipo_Ident,Perfil_Referencia,Tipos_Firmas_General, * from EasySeguridad..Usuarios where NombreCorto= 'perfil 1 liza dev'
 