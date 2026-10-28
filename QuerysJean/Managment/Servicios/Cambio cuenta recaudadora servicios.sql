
--172.26.61.204


select * from nts_tarjcred.dbo.tc_comercios where 
SELECT co_ruc, COUNT(co_ruc) AS Conteo from nts_tarjcred.dbo.tc_comercios 
where co_ruc like '09%' -- and co_estado ='A'
GROUP BY co_ruc HAVING COUNT(co_ruc)= 3 ORDER BY Conteo DESC;

select ce_institucionbanco,ce_cuentacomercio,* from nts_tarjcred.dbo.tc_comercios
left join nts_tarjcred.dbo.tc_comercio_empresa on ce_comercio=co_comercio
where co_ruc='0922361845001'

select ce_institucionbanco,ce_cuentacomercio,* from nts_tarjcred.dbo.tc_comercios
left join nts_tarjcred.dbo.tc_comercio_empresa on ce_comercio=co_comercio
where co_ruc='0954121646001'

update nts_tarjcred.dbo.tc_comercio_empresa set ce_institucionbanco='14',ce_cuentacomercio='7644684' where ce_comercio='258584'
-----------------------------------------------------------------------------------------------------------------------------
--Registro comercio no BG
-----------------------------------------------------------------------------------------------------------------------------
select ce_institucionbanco,ce_cuentacomercio,* from nts_tarjcred.dbo.tc_comercios
left join nts_tarjcred.dbo.tc_comercio_empresa on ce_comercio=co_comercio--0922361845
where co_ruc='0992670762001'

--update nts_tarjcred.dbo.tc_comercio_empresa set ce_institucionbanco='10' , ce_cuentacomercio='9875613' where ce_comercio='174668'
update nts_tarjcred.dbo.tc_comercio_empresa set ce_institucionbanco='17' where ce_comercio='174668'
-----------------------------------------------------------------------------------------------------------------------------
--Registro con 1 comercios 1 BG 
-----------------------------------------------------------------------------------------------------------------------------
select ce_institucionbanco,ce_cuentacomercio,* from nts_tarjcred.dbo.tc_comercios
left join nts_tarjcred.dbo.tc_comercio_empresa on ce_comercio=co_comercio
where co_ruc='0954121646001'
sd
-----------------------------------------------------------------------------------------------------------------------------
--Registro con 3 comercios 1 BG 
-----------------------------------------------------------------------------------------------------------------------------
select ce_institucionbanco,ce_cuentacomercio,* from nts_tarjcred.dbo.tc_comercios
left join nts_tarjcred.dbo.tc_comercio_empresa on ce_comercio=co_comercio
where co_ruc='0900326265001'

update nts_tarjcred.dbo.tc_comercio_empresa set ce_institucionbanco='10' , ce_cuentacomercio='584684'where ce_comercio='15237'
update nts_tarjcred.dbo.tc_comercio_empresa set ce_institucionbanco='14' , ce_cuentacomercio='524245' where ce_comercio='30589'
update nts_tarjcred.dbo.tc_comercio_empresa set ce_institucionbanco='17' , ce_cuentacomercio='584684' where ce_comercio='207845'


update nts_tarjcred.dbo.tc_comercios set co_estado='A' where co_comercio='21873'
update nts_tarjcred.dbo.tc_comercios set co_estado='A' where co_comercio='30589'
update nts_tarjcred.dbo.tc_comercios set co_estado='I' where co_comercio='47850'
-----------------------------------------------------------------------------------------------------------------------------
--Registro con 1 comercios 1 BG- Comercios Inactivo- los que tiene otro tipo de estado X
-----------------------------------------------------------------------------------------------------------------------------
select ce_institucionbanco,ce_cuentacomercio,* from nts_tarjcred.dbo.tc_comercios
left join nts_tarjcred.dbo.tc_comercio_empresa on ce_comercio=co_comercio
where co_ruc='0992456078001'--Dermalia

select ce_institucionbanco,ce_cuentacomercio,* from nts_tarjcred.dbo.tc_comercios
left join nts_tarjcred.dbo.tc_comercio_empresa on ce_comercio=co_comercio
where co_ruc='0900326265000'

-----------------------------------------------------------------------------------------------------------------------------
--Registro con 1 comercios 1 BG- Comercios Inactivo
-----------------------------------------------------------------------------------------------------------------------------
select ce_institucionbanco,ce_cuentacomercio,* from nts_tarjcred.dbo.tc_comercios
left join nts_tarjcred.dbo.tc_comercio_empresa on ce_comercio=co_comercio
where co_ruc='0905133753001'--Dermalia
--update nts_tarjcred.dbo.tc_comercio_empresa set ce_institucionbanco='10', ce_cuentacomercio='84657752' where ce_comercio='384089'

-----------------------------------------------------------------------------------------------------------------------------
--Registro comercio inexistente
-----------------------------------------------------------------------------------------------------------------------------


-----------------------------------------------------------------------------------------------------------------------------
--Registro con 3 comercios 3 BG 
-----------------------------------------------------------------------------------------------------------------------------



-----------------------------------------------------------------------------------------------------------------------------
--Registro con 3 comercios 0 BG 
-----------------------------------------------------------------------------------------------------------------------------


-----------------------------------------------------------------------------------------------------------------------------
--tenga cuenta empresas
-----------------------------------------------------------------------------------------------------------------------------


