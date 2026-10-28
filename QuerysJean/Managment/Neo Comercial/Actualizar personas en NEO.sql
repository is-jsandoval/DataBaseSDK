--105

-- Ubicaciones en NEO


 Use BG_NEO

Select * from BG_NEO..bg_per_personas where numidentificacion= '0954121646'
select * from bg_per_ubicaciones where idPersona = (select idPersona from bg_per_personas where numidentificacion= '0930247994')

Select * from BG_NEO..bg_per_ubicaciones WHERE IDPERSONA=(Select idPersona from BG_NEO..bg_per_personas where numidentificacion= '0940584550')
Select * from BG_NEO..bg_per_ubicaciones WHERE IDPERSONA=(Select idPersona from BG_NEO..bg_per_personas where numidentificacion= '0926149303')