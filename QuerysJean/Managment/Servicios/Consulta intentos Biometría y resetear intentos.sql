--172.26.60.105.4433
--usr_bizagi
--Bg123456
--CONSULTAR BIOMETRÍA NEO
--Consuta de los registros
SELECT top(10 )* FROM [BG_NEO].[dbo].[BG_EXP_RegistroValidacionBiometrica] 
 order by fechaCrea desc
 
 SELECT top(10 )* FROM [BG_NEO].[dbo].[BG_EXP_RegistroValidacionBiometrica] where identificacion='0903603348'
 order by fechaCrea desc

SELECT * FROM [BG_NEO].[dbo].[BG_EXP_RegistroValidacionBiometrica] 
where identificacion = '2100851043' and fechaCrea >= '2025-05-07 00:00:00.000'

SELECT * FROM [BG_NEO].[dbo].[BG_EXP_RegistroValidacionBiometrica] 
where   fechaCrea >= '2025-05-07 00:00:00.000' order by fechaCrea desc
 


--RESETEAR INTENTOS DE BIOMETRIA
--Tomar el código que esta con el bloqueo y le pones la fecha de ayer  
update [BG_NEO].[dbo].[BG_EXP_RegistroValidacionBiometrica] 
set fechabloqueo= getdate()-1  WHERE idCodigo = 24328 

--consultar intentos por cedula

--172.26.60.105.4433
--usr_bizagi
--Bg123456
--CONSULTAR BIOMETRÍA NEO
--Consuta de los registros
select numeroIntento,estadoValidacion, fechaBloqueo ,* from  
[BG_NEO].[dbo].[BG_EXP_RegistroValidacionBiometrica] 
WHERE identificacion = '0954121646'
--reiniciar segunda opcion
update [BG_NEO].[dbo].[BG_EXP_RegistroValidacionBiometrica] 
set numeroIntento=0 ,estadoValidacion='V', fechaBloqueo =NULL  WHERE identificacion = '0903858967'