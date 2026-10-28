--172.26.60.105


select Nombre,strValor,idCatDetSegmentoEstrategico,* from  BG_NEO.dbo.BG_PER_Personas  per
inner join BG_NEO.dbo.BG_ADM_CatalogosDetalles adm on  per.idCatDetSegmentoEstrategico= adm.idCodigo
--inner join BG_NEO.dbo.BG_PER_Ubicaciones ub on ub.idpersona= per.idpersona
--inner join BG_NEO.dbo.BG_EXP_SolicitudProducto sol on sol.numIdentificacion = per.numIdentificacion
where per.numIdentificacion in ('0954121646' ,'0903858967','1722458559')


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
select *
--per.numIdentificacion,per.Nombre, per.idCatDetSegmentoEstrategico,tieneRegistroFirma, adm.strValor, strValor2 , per.*, ub.*
from BG_NEO.dbo.BG_PER_Personas per
inner join BG_NEO.dbo.BG_ADM_CatalogosDetalles adm on  per.idCatDetSegmentoEstrategico= adm.idCodigo
inner join BG_NEO.dbo.BG_PER_Ubicaciones ub on ub.idpersona= per.idpersona
inner join BG_NEO.dbo.BG_EXP_SolicitudProducto sol on sol.numIdentificacion = per.numIdentificacion
where idCatDetTipoIdentificacion =525 
and idClienteClt is not null 
and idCatDetTipoIdentificacion is not null
and idCatOficial is not null
and ub.correoelectrónico is null
and idCatDetSegmentoEstrategico in (2109,5745)
and tieneRegistroFirma ='N' --N no tiene y S si
and per.numIdentificacion = '0912152980'
ORDER BY per.numIdentificacion DESC

---Consulta de clientes que solo tienen una cuenta con/sin firma registrada
SELECT TOP 100
    per.numIdentificacion,
    per.Nombre,
    per.idCatDetSegmentoEstrategico,
    sol.tieneRegistroFirma,
    adm.strValor,
    adm.strValor2,
    per.*,
    ub.*
FROM BG_NEO.dbo.BG_PER_Personas per
INNER JOIN BG_ADM_CatalogosDetalles adm 
    ON per.idCatDetSegmentoEstrategico = adm.idCodigo
INNER JOIN BG_PER_Ubicaciones ub 
    ON ub.idpersona = per.idpersona
INNER JOIN BG_EXP_SolicitudProducto sol 
    ON sol.numIdentificacion = per.numIdentificacion
WHERE per.idCatDetTipoIdentificacion = 525
    AND per.idClienteClt IS NOT NULL
    AND per.idCatDetTipoIdentificacion IS NOT NULL
    AND per.idCatOficial IS NOT NULL
	AND ub.correoElectrónico IS NULL
    AND per.idCatDetSegmentoEstrategico IN (2014)
    AND sol.tieneRegistroFirma = 'N'
GROUP BY per.numIdentificacion, per.Nombre, per.idCatDetSegmentoEstrategico, sol.tieneRegistroFirma, adm.strValor, adm.strValor2, per.idpersona, ub.idpersona, ub.correoelectronico
HAVING COUNT(DISTINCT per.numIdentificacion) = 1;

--consulta identificaciones segmento 
select Nombre,strValor,idCatDetSegmentoEstrategico,numIdentificacion,* from  BG_NEO.dbo.BG_PER_Personas  per
inner join BG_NEO.dbo.BG_ADM_CatalogosDetalles adm on  per.idCatDetSegmentoEstrategico= adm.idCodigo
--inner join BG_NEO.dbo.BG_PER_Ubicaciones ub on ub.idpersona= per.idpersona
--inner join BG_NEO.dbo.BG_EXP_SolicitudProducto sol on sol.numIdentificacion = per.numIdentificacion
where  per.idCatDetSegmentoEstrategico IN (2014) 


select Nombre,strValor,idCatDetSegmentoEstrategico,*
--per.numIdentificacion,per.Nombre, per.idCatDetSegmentoEstrategico,tieneRegistroFirma, adm.strValor, strValor2 , per.*, ub.*
from BG_NEO.dbo.BG_PER_Personas per
inner join BG_NEO.dbo.BG_ADM_CatalogosDetalles adm on  per.idCatDetSegmentoEstrategico= adm.idCodigo
where  per.numIdentificacion = '1104476047'
ORDER BY per.numIdentificacion DESC