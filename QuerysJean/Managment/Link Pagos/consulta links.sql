--172.26.61.204,4433
--Ejecutar todo el script primero hacer el select
declare @ruc varchar(13);
declare @com varchar(13);
set @ruc='0954121646001';--cambiar por numero de ruc 

--select * from nts_tarjcred.dbo.tc_cafiliaciones Where ca_comercio
--select 'Registros en syscard',* from nts_tarjcred.dbo.tc_comercios where co_ruc =@ruc and co_linkPagos='S'--hacer primero el select!!!!!!!!!
set @com =(select co_comercio_syscard from BG_TComercios.Pagos.lp_comercios where co_ruc=@ruc)
-----------------------------------------------------------------------------------------------------------------------------
select * from BG_TComercios.Pagos.lp_limites where  lt_comercio=@com
select * from BG_TComercios.Pagos.lp_links_logs where lg_ruc = @ruc
select * from BG_TComercios.Pagos.lp_links where ln_ruc = @ruc




-- 25 horas atrás
declare @numeroAD varchar(13);
set @numeroAD='729511';
UPDATE BG_TComercios.Pagos.lp_links SET ln_fecha = DATEADD(HOUR, -25, GETDATE()) WHERE ln_audit_number = @numeroAD
UPDATE BG_TComercios.Pagos.lp_links_logs SET lg_fecha = DATEADD(HOUR, -25, GETDATE()) WHERE lg_audit_number = @numeroAD

-- 2 horas atrás
declare @numeroA varchar(13);
set @numeroA='0954121646001';
UPDATE BG_TComercios.Pagos.lp_links_logs SET lg_fecha = DATEADD(HOUR, -2, GETDATE()) WHERE lg_audit_number = @numeroA
UPDATE BG_TComercios.Pagos.lp_links_logs SET lg_fecha = DATEADD(HOUR, -2, GETDATE()) WHERE lg_audit_number = @numeroA

select top 1 NUMERO_RUC from BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG as sr
left join nts_tarjcred.dbo.tc_comercios as co on sr.NUMERO_RUC=co.co_ruc and co.co_ruc is NULL 
left join BG_TComercios.Pagos.lp_comercios as lp on sr.NUMERO_RUC=lp.co_ruc and lp.co_ruc is NULL
where sr.ESTADO_CONTRIBUYENTE='ACTIVO' and sr.NUMERO_RUC like '09%'
order by co_fchapertura desc


select * from BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG where ESTADO_CONTRIBUYENTE='ACTIVO'
select * from BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG where NUMERO_RUC='0952502441001'