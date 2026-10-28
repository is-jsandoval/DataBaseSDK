
--Consulta clientes por segmento con/sin firma registrada
----Consulta de clientes segun el segmento
--2014	Micro	Segmento Estrategico
--2015	Pyme Grande	Segmento Estrategico
--2016	Empresarial	Segmento Estrategico
--2017	Corporativo	Segmento Estrategico
--2018	No tiene Segmento Estrategico	Segmento Estrategico
--2109	Preferente	Segmento Estrategico
--4609	Micro Plus	Segmento Estrategico
--4610	Micro Premium	Segmento Estrategico
--5745	Banca Privada	Segmento Estrategico
--5746	Masivo Digital	Segmento Estrategico
--5950	Colaborador	Segmento Estrategico
--11920	Masivo Persona	Segmento Estrategico
--11980	Pyme	Segmento Estrategico
--23755	Gold	Segmento Estrategico
--23756	Platinum	Segmento Estrategico
--23757	Silver	Segmento Estrategico
--81135848	Silver	Segmento Estrategico
--4767	JOVENES BM                       	Tabla de Segmento clientes
--4769	ADULTOS JOVENES BM               	Tabla de Segmento clientes
--4771	FAMILIA JOVENES BM               	Tabla de Segmento clientes
--2108 Avanti
--2109 Evolution/Preferente
--2014 Micro
--11920 Persona
--5745 Banca Privada
--5746 Masivo Digital
select per.idCatDetSegmentoEstrategico,per.numIdentificacion,adm.strValor,*
--per.numIdentificacion,per.Nombre, per.idCatDetSegmentoEstrategico,tieneRegistroFirma, adm.strValor, strValor2 , per.*, ub.*
from BG_NEO.dbo.BG_PER_Personas per
inner join BG_NEO.dbo.BG_ADM_CatalogosDetalles adm on  per.idCatDetSegmentoEstrategico= adm.idCodigo
inner join BG_NEO.dbo.BG_PER_Ubicaciones ub on ub.idpersona= per.idpersona
inner join BG_NEO.dbo.BG_EXP_SolicitudProducto sol on sol.numIdentificacion = per.numIdentificacion
where 
--idCatDetTipoIdentificacion =525 
  LEN(per.numIdentificacion)=13
--and idCatDetTipoIdentificacion is not null
--and idCatOficial is not null
--and ub.correoelectrónico is null
--and idCatDetSegmentoEstrategico in (11980)
--and tieneRegistroFirma ='N' --N no tiene y S si
--and per.numIdentificacion = '1791767756001'
ORDER BY per.numIdentificacion DESC


select per.idCatDetSegmentoEstrategico,per.numIdentificacion,adm.strValor,*


select per.idCatDetSegmentoEstrategico,per.numIdentificacion,adm.strValor,* 
from BG_NEO.dbo.BG_PER_Personas per
inner join BG_NEO.dbo.BG_ADM_CatalogosDetalles adm on  per.idCatDetSegmentoEstrategico= adm.idCodigo
where LEN(numIdentificacion)=13
and idCatDetSegmentoEstrategico in (11980)
and per.numIdentificacion = '0992107367001' --TENSOflow pyme

select per.idCatDetSegmentoEstrategico,per.numIdentificacion,adm.strValor,* 
from BG_NEO.dbo.BG_PER_Personas per
inner join BG_NEO.dbo.BG_ADM_CatalogosDetalles adm on  per.idCatDetSegmentoEstrategico= adm.idCodigo
where LEN(numIdentificacion)=13
and idCatDetSegmentoEstrategico in (2016)
and per.numIdentificacion = '0993390611001' --empresarial





select distinct adm.strValor
from BG_NEO.dbo.BG_PER_Personas per
inner join BG_NEO.dbo.BG_ADM_CatalogosDetalles adm on  per.idCatDetSegmentoEstrategico= adm.idCodigo
where LEN(numIdentificacion)=13






inner join BG_NEO.dbo.BG_ADM_CatalogosDetalles adm on  per.idCatDetSegmentoEstrategico= adm.idCodigo


select * from BG_NEO.[dbo].[BG_PER_PersonasJuridicas]