--172.26.60.105,4433

--2850 liquidado
use BG_NEO
update  [BG_NEO].[dbo].BG_EXP_Expediente set idEstado =2851 where idExpediente in (21046499)	

select idEstado , idEtapa ,opidGestor ,*  from [BG_NEO].[dbo].BG_EXP_Expediente where idExpediente in (21046845)
select idEstado , idEtapa ,opidGestor ,* from [BG_NEO].[dbo].BG_EXP_Expediente order by fechaCrea desc 
select * from [BG_NEO].[dbo].BG_PRO_Campos
select * from [BG_NEO].[dbo].BG_PRO_Definicion
select * from [BG_NEO].[dbo].BG_PRO_Productos where strNombre like '%comer%'


 Select idEstado,* from [BG_NEO].[dbo].BG_EXP_Expediente where idExpediente in (21041827)	
 ---------------------------------------------------------------
 --editar expediente mediante identificacion
update [BG_NEO].[dbo].BG_EXP_Expediente 
set idEstado =2851 
where idPersona in (SELECT idPersona FROM [BG_NEO].[dbo].[BG_PER_Personas] where numIdentificacion='1801972009') 
and idEstado='2846'


--editar expediente mediante identificacion
update [BG_NEO].[dbo].BG_EXP_Expediente 
set idEstado =2851 
where idPersona in (SELECT idPersona FROM [BG_NEO].[dbo].[BG_PER_Personas] where numIdentificacion='0903858967') -- Diaz 1722458559
and idEstado='2846'


(SELECT * FROM [BG_NEO].[dbo].[BG_PER_Personas] where numIdentificacion='0903858967') 


update [BG_NEO].[dbo].BG_EXP_Expediente 
set idEstado =2851 
where id
 ---------------------------------------------------------------
 select * from [BG_NEO].[dbo].BG_EXP_Expediente where  idEstado='2846'
 SELECT * FROM [BG_NEO].[dbo].[BG_PER_Personas] where numIdentificacion='0954121646'


 update  [BG_NEO].[dbo].BG_EXP_Expediente set idEstado = 5604, idEtapa = 5602 where idExpediente in (21046845) --Para revivir expedientes
 

  select * from [BG_NEO].[dbo].BG_PRO_Productos where strNombre like '%Multicredito%'