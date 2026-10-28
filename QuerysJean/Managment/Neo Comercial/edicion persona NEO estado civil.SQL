--105

-- Ubicaciones en NEO


 Use BG_NEO

Select * from BG_NEO.dbo.bg_per_personas where numidentificacion= '0954121646'
Select * from BG_NEO.dbo.bg_per_personas where idPersona='2'
Select * from BG_NEO.dbo.bg_per_PersonasNaturales where idPersonaNatural='2'
Select * from BG_NEO.dbo.bg_per_PersonasNaturales where idPersonaNatural='10915906'

update BG_NEO.dbo.bg_per_PersonasNaturales set idCatDetEstadoCivil='2452' where idPersonaNatural='10915906'--editar a casado
update BG_NEO.dbo.bg_per_PersonasNaturales set idCatDetEstadoCivil='2455' where idPersonaNatural='10915906'--editar a soltero

Select distinct idCatDetEstadoCivil from BG_NEO.dbo.bg_per_PersonasNaturales
Select * from BG_NEO.dbo.bg_per_PersonasNaturales where idCatDetEstadoCivil='2455'


Select * from BG_NEO.dbo.BG_PER_DatosActualizar where NumIdentificacion='0954121646'
Select * from BG_NEO.dbo.BG_PER_TEMP_ClteFilDiario where

Select * from BG_NEO..PersonasNaturales where apellidos1='SANDOVAL'
and apellidos2='QUIMIS'

--delete BG_NEO..bg_per_personas where numidentificacion= '0903631349'



select * from bg_per_ubicaciones where idPersona = (select idPersona from bg_per_personas where numidentificacion= '0930247994')

Select * from BG_NEO..bg_per_ubicaciones WHERE IDPERSONA=(Select idPersona from BG_NEO..bg_per_personas where numidentificacion= '0940584550')
Select * from BG_NEO..bg_per_ubicaciones WHERE IDPERSONA=(Select idPersona from BG_NEO..bg_per_personas where numidentificacion= '0926149303')