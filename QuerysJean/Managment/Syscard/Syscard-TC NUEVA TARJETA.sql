--172.26.61.204,4433
--monitran
--monitran


-- BUSCAR TARJETA 
select ta_estados,ms_estados,ms_cuenta,ms_disponiblerotativo,ms_cuporotativo,ms_codigo_trato,ts_disponiblerotativo,
ms_disponiblediferido,ts_disponiblecuenta,ts_disponiblediferido,ts_porcencupo,ta_servicecode,*
from nts_tarjcred..tc_tarjetas
inner join nts_tarjcred..tc_maestro on ta_cuenta = ma_cuenta
inner join nts_clientes..cl_clientes on ma_cliente = cl_cliente
inner join nts_tarjcred..tc_maestro_saldos on ma_cuenta = ms_cuenta
inner join nts_tarjcred..tc_tarjetas_saldos on ts_tarjeta = ta_tarjeta
where ta_tarjeta like '523042%' and ta_princiadicio = 'P'
and ta_estados = '0000000000' and ms_estados = '0000000000'   --4540860008815348   4540860005786085
and ta_expira = '1126'  --4540630963316778

-- BUSCAR TARJETA 
select ta_estados,ms_estados,ms_cuenta,ms_disponiblerotativo,ms_cuporotativo,ms_codigo_trato,ts_disponiblerotativo,
ms_disponiblediferido,ts_disponiblecuenta,ts_disponiblediferido,ts_porcencupo,ta_servicecode,*
from nts_tarjcred..tc_tarjetas
inner join nts_tarjcred..tc_maestro on ta_cuenta = ma_cuenta
inner join nts_clientes..cl_clientes on ma_cliente = cl_cliente
inner join nts_tarjcred..tc_maestro_saldos on ma_cuenta = ms_cuenta
inner join nts_tarjcred..tc_tarjetas_saldos on ts_tarjeta = ta_tarjeta
where cl_identifica='0951914316'
--and ta_princiadicio = 'P'
--and ta_estados = '0000000000' and ms_estados = '0000000000'   --4540860008815348   4540860005786085
--and ta_expira = '1126'  --4540630963316778


select top 10 'tc_maestro'as tbl,* from 
nts_tarjcred..tc_maestro
select top 10 'cl_clientes'as tbl,* from 
nts_clientes..cl_clientes
select top 10 'tc_maestro_saldos'as tbl,* from 
nts_tarjcred..tc_maestro_saldos
select top 10 'tc_tarjetas_saldos'as tbl,* from 
nts_tarjcred..tc_tarjetas_saldos