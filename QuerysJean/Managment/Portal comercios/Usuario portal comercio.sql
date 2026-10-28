--172.26.61.61,4433


/****** Script para el comando SelectTopNRows de SSMS  ******/
SELECT *
  FROM [BG_PortalComercios].[dbo].[Usuario] where identificacion='0926445354'

/*  eliminar usuario Portal comercios
delete [BG_PortalComercios].[dbo].usuario_roles where usuario_id=(SELECT id FROM [BG_PortalComercios].[dbo].[Usuario] where identificacion='0903603348')
delete [BG_PortalComercios].[dbo].RelacionUsuarioEmpresa where id_usuario=(SELECT id FROM [BG_PortalComercios].[dbo].[Usuario] where identificacion='0903603348')
delete [BG_PortalComercios].[dbo].Contactabilidad where id_usuario=(SELECT id FROM [BG_PortalComercios].[dbo].[Usuario] where identificacion='0903603348')
delete [BG_PortalComercios].[dbo].[Usuario] where identificacion='0903603348'
*/

  update [BG_PortalComercios].[dbo].[Usuario] set estado='0' where id='69D082DF-B320-41AE-B824-822DC17F0070'

  --portal empresas
 -- CREDENCIALES DE LA BASE
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
select * from EasySeguridad..UserEmpServ
select Alias ,Tipo_Ident,Perfil_Referencia,Tipos_Firmas_General, * from EasySeguridad..Usuarios where NombreCorto= 'perfil 6 BLOQUEO'
select @usuario, id_empresa,id_servicio,estado,visualizadetalle,condicion_pago from EasySeguridad..UserEmpServ where usuario = @usuario_anterior
 
delete EasySeguridad..UserEmpServ where usuario ='LIZA DEVON 3'