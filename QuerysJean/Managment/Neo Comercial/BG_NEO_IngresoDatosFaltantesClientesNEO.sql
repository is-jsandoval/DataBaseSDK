	--IP Servidor: 172.26.60.105,4433
	--Usuario: usr_bizagi
	--Clave: Bg123456		

-----------------------------------------------
--[INICIO] - INGRESO DATOS FALTANTES CLIENTES
-----------------------------------------------
use BG_NEO

declare @cedula varchar(10)='0903858967' --Aquí cambiar la cédula y ejecutar todo el bloque

----------------------
--DATOS TITULAR Y CONYUGE
----------------------
select * from BG_NEO.dbo.BG_PER_Personas where numIdentificacion='0954121646' 
Select * from BG_NEO.dbo.BG_PER_PersonasNaturales where idPersonaNatural='10915906' 
update [dbo].[BG_PER_PersonasNaturales]
set idCatSepBienes = 62, idCatDetSepBienes = 2159, idCatdetnivelestudios = 3103, tiempounionlibre = 0, numtiemporesidenciaextr = 0, poseerut = 0, numeroruc = 0, trabsectorpublico = 0, idcatvinculoparen = 101, idcatdetvinculoparen = 3048, vinculocargobg = 0, tienevinculo = 0, idcatactividadeconomica = 34, idcatdetactividadeconomica = 494, idcatsituacionlaboral = 47, idcatdetsituacionlaboral = 4630, tiempolaboraemp = 5, idcatorigeningresos = 118, idcatdetorigeningresos = 4624, nombreempresaant = 0, cargoant = 0, idcattipovivienda = 46, idcatdettipovivienda = 4597, tiemporesidenciaactual = 5, idcatcorrespondencia = 97, idcatdetcorrespondencia = 2832, notmedioselectronicos = 1, tienecorreo = 1, envioestadocuenta = 1937
where idPersonaNatural in (select idPersona from bg_per_personas where numidentificacion in (@cedula)) --0102937000

select top 10 * from [dbo].[BG_PER_PersonasNaturales] where idPersonaNatural in (select idPersona from bg_per_personas where numidentificacion in (@cedula))

update [dbo].[BG_PER_IngresosGastos] 
set sueldoMensual = 1000, gastosHogar = 100, gastosEducacion = 100, amortizacionPrestamos = 100, pagoTarjetaCredito = 100, arriendo = 100
where idPersonaNatural in (select idPersona from bg_per_personas where numidentificacion in (@cedula))

select * from BG_NEO.dbo.BG_PER_IngresosGastos where idpersonanatural in (select idPersona from BG_NEO.dbo.bg_per_personas where numidentificacion in ('0918167727'))

--PARA ACTUALIZAR CORREO Y TELEFONO, ESTO NO PERMITE HACERLO POR PANTALLA (ESTO PARA ASEGURAR QUE EL CORREO NOS LLEGUE A NOSOTROS Y NO AL CLIENTE)
update BG_PER_Ubicaciones
set celular = '0962740151', telefono = '0962740151', correoElectrónico = 'eljeansandovalpost4@gmail.com', idcatdetpais=2241, idCatProvincia = 4, idCatParroquia = 115, idCatDetParroquia = 3193, idCatDetProvincia = 4, idCatPais = 53, idCatTipoDireccion = 97, idcatciudad=5, idCatDetCiudad = 5, idCatDetEstado = 1, idCatEstado = 1, callePrincipal = 'Av Quito', calleInterseccion = 'ASDF', manzana = '12', villaSolar = '12', referenciaDireccion = 'JUAN MONTALVO  MZ.A-3  SL.14'
where idPersona in (select idPersona from bg_per_personas where numidentificacion in (@cedula))

select * from BG_PER_Ubicaciones where idPersona in (select idPersona from bg_per_personas where numidentificacion in (@cedula))




-----------------------------------------------
--[FIN] - INGRESO DATOS FALTANTES CLIENTES
-----------------------------------------------

---OJO----
--------------------------------------------------------------------------------
---CUANDO NO EXISTE EL DATO EN LA TABLA bg_per_ubicaciones, EJECUTAR ESTE INSERT
--------------------------------------------------------------------------------

insert into bg_per_ubicaciones(idpersona, idCatTipopersona, idCatDetTipoPersona, idcatdettipodireccion, idcatdetpais, idcatdetprovincia, correoElectrónico, celular)
select per.idpersona , 37,509, 2832, 2241, 18,  'ELJEANSANPOST3@GMAIL.COM', '0962740151'
from bg_per_personas per
where numidentificacion= '0903858967' --Cédula para insertar
and not exists (select * from   bg_per_ubicaciones ub
				where ub.idpersona= per.idpersona
				and idcatdettipodireccion = 2832)



insert into bg_per_ubicaciones(idpersona, idCatTipopersona, idCatDetTipoPersona, idcatdettipodireccion, idcatdetpais, idcatdetprovincia, correoElectrónico, celular)
select per.idpersona , 37,509, 2832, 2241, 18,  'ELJEANSANPOST3@GMAIL.COM', '0962740151'
from bg_per_personas per
where numidentificacion in 
('0151877339'   
,'0151979259'   
,'0604031096'   
,'0705108314'   
,'0706218534'   
,'0750313934'   
,'0802112862'   
,'0903858967'
,'0916555170'   
,'0916662216'
,'0916708720'   
,'0917284200'   
,'0920720398'   
,'0922361845'
,'0924360654'   
,'0925813701'   
,'0926924671'   
,'0927165902'   
,'0927217869'   
,'0930931795'   
,'0940832231'   
,'0944159185'   
,'0954121646'
,'0962648226'   
,'0963399175'   
,'0963871348'   
,'0964906317'   
,'0965117500'   
,'0966228793'   
,'0966590994'   
,'0966672388'   
,'1104893407'   
,'1105613929'   
,'1203984883'   
,'1204657645'   
,'1301999189'   
,'1306828136'   
,'1312060518'   
,'1314709419'   
,'1351492788'   
,'1712626504'   
,'1713957494'   
,'1719432286'   
,'1721789707'   
,'1722458559'
,'1723499206'
,'1752650661'   
,'1756365662'   
,'1758673659'   
,'1759483462'   
,'1760686178'   
,'1761935913') --Cédula para insertar
and not exists (select * from   bg_per_ubicaciones ub
				where ub.idpersona= per.idpersona
				and idcatdettipodireccion = 2832)


