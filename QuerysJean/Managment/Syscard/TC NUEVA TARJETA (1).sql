--172.26.61.204.4433
--monitran
--monitran

-- BUSCAR TARJETA Credito
select ta_estados, ta_expira,ms_estados,ms_cuenta,ms_disponiblerotativo,ms_cuporotativo,ms_codigo_trato,ts_disponiblerotativo,
ms_disponiblediferido,ts_disponiblecuenta,ts_disponiblediferido,ts_porcencupo,ta_servicecode,*
from nts_tarjcred..tc_tarjetas
inner join nts_tarjcred..tc_maestro on ta_cuenta = ma_cuenta
inner join nts_clientes..cl_clientes on ma_cliente = cl_cliente
inner join nts_tarjcred..tc_maestro_saldos on ma_cuenta = ms_cuenta
inner join nts_tarjcred..tc_tarjetas_saldos on ts_tarjeta = ta_tarjeta
where ta_tarjeta like '523042%' and ta_princiadicio = 'P'
and ta_estados = '0000000000' and ms_estados = '0000000000'   --4540860008815348   4540860005786085
and ta_expira = '1126'  --4540630963316778


-- BUSCAR TARJETA Credito
select ta_estados, ta_expira,ms_estados,ms_cuenta,ms_disponiblerotativo,ms_cuporotativo,ms_codigo_trato,ts_disponiblerotativo,
ms_disponiblediferido,ts_disponiblecuenta,ts_disponiblediferido,ts_porcencupo,ta_servicecode,*
from nts_tarjcred..tc_tarjetas
inner join nts_tarjcred..tc_maestro on ta_cuenta = ma_cuenta
inner join nts_clientes..cl_clientes on ma_cliente = cl_cliente
inner join nts_tarjcred..tc_maestro_saldos on ma_cuenta = ms_cuenta
inner join nts_tarjcred..tc_tarjetas_saldos on ts_tarjeta = ta_tarjeta
where ta_tarjeta = '376650049775586' and ta_princiadicio = 'P' --09/28   8586


select * from nts_monitran..mt_catalogo where ca_nombre like '%CodRespInterno%'
select * from nts_monitran..vw_catalogo where ca_nombre like '%resp%'

select ta_estados, ta_expira,ms_estados,ms_cuenta,ms_disponiblerotativo,ms_cuporotativo,ms_codigo_trato,ts_disponiblerotativo,
ms_disponiblediferido,ts_disponiblecuenta,ts_disponiblediferido,ts_porcencupo,ta_servicecode,*
from nts_tarjcred..tc_tarjetas
inner join nts_tarjcred..tc_maestro on ta_cuenta = ma_cuenta
inner join nts_clientes..cl_clientes on ma_cliente = cl_cliente
inner join nts_tarjcred..tc_maestro_saldos on ma_cuenta = ms_cuenta
inner join nts_tarjcred..tc_tarjetas_saldos on ts_tarjeta = ta_tarjeta
where ta_tarjeta like '376653%' and ta_princiadicio = 'P'
and ta_estados = '0000000000' and ms_estados = '0000000000'   --4540860008815348   4540860005786085
and ta_expira = '1126'  --4540630963316778



-- TARJETA DEBITO
 
select top 400   ta_servicecode,*--ms_saldoactual ,ta_nuevatarjeta, ta_alias,ms_disponiblerotativo,* 
from nts_debito..db_tarjetas 
--inner join nts_tarjcred..tc_maestro_saldos on ta_cuenta = ms_cuenta 
where 
ta_estados ='0000000000' 
-- 5144400116797033 monto menor  5xxxx144400176615010
and ta_tarjeta ='5144403190090005'

select co_linkPagos,* from nts_tarjcred..tc_comercios where co_comercio = 232002

update nts_tarjcred..tc_comercios_alternos set ca_codalterno = '6000000002' where ca_comercio = 232002


select la_identifica,la_origenautorizacion,la_origenest,la_estado,* from nts_tarjcred..tc_atrnautorizalog where right(la_tarjeta,4) = '0506' order by la_fchsis desc







update nts_tarjcred..tc_atrnautorizalog set la_estado = 'U' where la_id in(623730822,623730818,623730741,623730740)