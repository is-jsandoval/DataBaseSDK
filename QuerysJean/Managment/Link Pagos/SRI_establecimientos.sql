--204
select * from BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG where NUMERO_RUC='0930482104001'
--UPDATE BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG set NUMERO_RUC='0930482104001' WHERE ID=2
--UPDATE BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG set NUMERO_RUC='0958541521065' WHERE ID=5
SELECT * FROM BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG  WHERE ID=5

SELECT * FROM BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG where FANTASIA_COMERCIAL like '%JEAN%'

SELECT * FROM BG_TComercios.Pagos.lp_comercios WHERE CO_RUC='0190151700001'
SELECT * FROM BG_TComercios.Pagos.lp_comercios where co_comercio_syscard='258523'
SELECT * FROM BG_TComercios.Pagos.lp_comercios order by co_fch_creacion desc

SELECT * FROM nts_tarjcred.dbo.tc_comercios WHERE CO_RUC='1791321278001'


SELECT co_cedulagerente,ce_cuentacomercio,* FROM nts_tarjcred.dbo.tc_comercios
left join nts_tarjcred.dbo.tc_comercio_empresa on ce_comercio=co_comercio
WHERE CO_RUC='0190151700001'


update BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG set NUMERO_RUC='0954121646002' where id='254'
select * from BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG where NUMERO_RUC='0954121646001'
update BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG set ESTADO_CONTRIBUYENTE='INACTIVO' where NUMERO_RUC='0954121646001'
update BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG set RAZON_SOCIAL='PULLA ZAMBRANO GARY LENIN' where NUMERO_RUC='1311722423001'
update BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG set RAZON_SOCIAL='SANDOVAL QUIMIS JEAN CARLO' , FANTASIA_COMERCIAL='SANDOVAL QUIMIS JEAN CARLO' where ID=4
update BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG set ESTADO_CONTRIBUYENTE='ACTIVO' where NUMERO_RUC='0932320567001'

update BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG set TIPO_CONTRIBUYENTE='PERSONAS NATURALES' where NUMERO_RUC='0954121646001'

select * from BG_TComercios.Pagos.lp_comercios where co_ruc='2300303787001'
select * from BG_TComercios.Pagos.lp_comercios where co_ruc='0903603348001'
select * from BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG where NUMERO_RUC='0954121646001'
--delete BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG where NUMERO_RUC='0958541521065'
select * from BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG where NUMERO_RUC='2300303787001'
select * from BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG where CLASE_CONTRIBUYENTE like '%ESPECIAL%' and  TIPO_CONTRIBUYENTE='SOCIEDAD'
update BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG set CATEGORIA=null where NUMERO_RUC='1790006697001'
select distinct CLASE_CONTRIBUYENTE  from BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG

select * from BG_TComercios.Pagos.lp_actividades
select * from BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG where RAZON_SOCIAL like '%KEVIN%' 

insert into BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG
(NUMERO_RUC,TIPO_CONTRIBUYENTE,CLASE_CONTRIBUYENTE,CATEGORIA,CONTRIBUYENTE_ESPECIAL,CODIGO_CIIU,ESTADO_CONTRIBUYENTE,FECHA_INICIO_ACT,FANTASIA_COMERCIAL,RAZON_SOCIAL,PROVINCIA,CANTON)
values ('0954121646001','PERSONAS NATURALES','GEN','','NO','C264001','ACTIVO','2025-12-17','JEAN CARLO SANDOVAL QUIMIS','JEAN CARLO SANDOVAL QUIMIS','GUAYAS','GUAYAQUIL')

--update nts_tarjcred.dbo.tc_comercios set co_estado='I'where co_comercio='258532'

select top 1 sr.NUMERO_RUC as co_ruc from BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG SR
left join BG_TComercios.Pagos.lp_actividades ac on sr.CODIGO_CIIU=ac.ac_codigo_actividad
left join nts_tarjcred.dbo.tc_comercios co on sr.NUMERO_RUC=co.co_ruc
left join BG_TComercios.Pagos.lp_comercios lp on co.co_comercio=lp.co_comercio_syscard
Where sr.TIPO_CONTRIBUYENTE='PERSONAS NATURALES' 
and sr. ESTADO_CONTRIBUYENTE='ACTIVO'
and sr.FANTASIA_COMERCIAL is not null
and sr.RAZON_SOCIAL is not null
and co.co_ruc is null
and ac.ac_codigo_actividad is null
and sr.NUMERO_RUC NOT IN (
    SELECT co_ruc
    FROM nts_tarjcred.dbo.tc_comercios
    WHERE co_linkPagos IN ('X', 'S')
)
--ofac
select * from BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG where RAZON_SOCIAL='PULLA ZAMBRANO GARY LENIN' 


select NUMERO_RUC from BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG
select * from BG_TComercios.Pagos.lp_actividades

select top 1 sr.NUMERO_RUC as co_ruc from BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG SR
left join nts_tarjcred.dbo.tc_comercios co on sr.NUMERO_RUC=co.co_ruc
left join BG_TComercios.Pagos.lp_comercios lp on co.co_comercio=lp.co_comercio_syscard
Where sr.TIPO_CONTRIBUYENTE='PERSONAS NATURALES' 
and sr. ESTADO_CONTRIBUYENTE='ACTIVO'
and sr.FANTASIA_COMERCIAL is not null
and sr.RAZON_SOCIAL is not null
and co.co_ruc is null
and sr.NUMERO_RUC NOT IN (
    SELECT co_ruc
    FROM nts_tarjcred.dbo.tc_comercios
    WHERE co_linkPagos IN ('X', 'S')
)

select  * from  BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG where ESTADO_CONTRIBUYENTE!='ACTIVO'