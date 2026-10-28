	--172.26.61.204,4433
--monitran
--monitran

-- DAR SALDO DISPONIBLE A TC
--ms_disponiblerotativo es el cupo que puede utilizar
--ms_avances
--ms_creditos
--ms_avancesatmnum
 
--update nts_tarjcred..tc_tarjetas 
--set ta_expira = 300
--where ta_tarjeta = '5230420021039092'


--update nts_tarjcred..tc_maestro_saldos
--set ms_disponiblerotativo = 200
--where ms_cuenta = '21039'

--saldos por cuenta de tarjeta
select * from  nts_tarjcred..tc_maestro_saldos where ms_cuenta='9916245'


--cuenta de la tarjeta para visualizar en saldos
select * from  nts_tarjcred..tc_tarjetas where ta_tarjeta= '376653003555051' -- cuenta 1719

--consulta de saldos por cuenta
select ms_saldoactual,* from  nts_tarjcred..tc_maestro_saldos 
where ms_cuenta in ('9915847')

update nts_tarjcred..tc_maestro_saldos 

set ms_saldoactual = 0

where ms_cuenta = '9916245'

 
-- Actualizar fecha de expiracion 

--update nts_tarjcred..tc_tarjetas 

--set ta_expira = 1126

--where ta_cuenta = '1684046'
-------------------

--update nts_tarjcred..tc_tarjetas 

--set ta_expira = 1126

--where ta_tarjeta = '5230420024418001'



select * from nts_tarjcred..tc_scoring_buro where br_nume_identidad= '0924099864' --'0920944469'--23246

--update nts_tarjcred..tc_scoring_buro 

--set br_puntaje_buro = 70

--where br_nume_identidad= '0920944469'
select * from nts_clientes..cl_clientes where cl_identifica='1709295255'--cl cliente 170735

select * from nts_clientes..cl_clientes where cl_cliente='23246'--cl cliente 170735  0924099864

select * from nts_tarjcred..tc_scoring_buro 

--CAPTURAR SALDOS CONSULTAR SALDOS DE LA TARJETAS DE CREDITO
--376650045517388   4334370125665873

select ms_disponiblerotativo,ms_disponiblediferido,ms_autorizacionesrot,ms_autorizacionesdif,ms_saldoactual,ts_disponiblerotativo,ts_disponiblediferido,ts_autorizacionesrot,ts_autorizacionesdif,* from nts_tarjcred..tc_tarjetas 
inner join nts_tarjcred..tc_maestro on ta_cuenta = ma_cuenta
inner join nts_tarjcred..tc_maestro_saldos on ta_cuenta = ms_cuenta
inner join nts_tarjcred..tc_tarjetas_saldos  on ts_tarjeta = ta_tarjeta
where ta_estados = '0000000000' and
ta_tarjeta = '5230420099990146'


--este query siguiente es para consultar tarjeta de débito
 
-- TARJETA DEBITO
 
select top 400   ms_saldoactual , ta_alias,* from nts_debito..db_tarjetas 
inner join nts_tarjcred..tc_maestro_saldos on ta_cuenta = ms_cuenta where ta_estados ='0000000000' -- 5144400116797033 monto menor  5xxxx144400176615010
and ta_tarjeta like '514440%'
--y cuando tu deseas actualizar el saldo para transaccionar con una tarjeta de debito debes de actualizarle en la tabla del core a la cuenta ya sea de ahorro o corriente
 
 --consulta debito tarjeta por cedula
 select top 400   ms_saldoactual , ta_alias,* 
 from 
 nts_debito..db_tarjetas 
 left join nts_debito..db_maestro on ta_cuenta = ma_cuenta
left join nts_debito..db_maestro_saldos on ta_cuenta = ms_cuenta
left join nts_clientes..cl_clientes on ma_cliente = cl_cliente
inner join nts_debito..db_maestro_saldos on ma_cuenta = ms_cuenta
inner join nts_debito..db_tarjetas_saldos on ts_tarjeta = ta_tarjeta
where cl_identifica='0951914316'


 select * 
 from  nts_clientes..cl_clientes  where cl_identifica ='0954121646'
 select top 100 * from 
 nts_debito..db_maestro where ma_cliente  ='6033165'
 select top 100 *
 from nts_debito..db_maestro_saldos where ms_cuenta = '5329995'
 select top 100 *
 from nts_debito..a1_db_tarjetas where ta_cuenta = '5329995'
 



where ta_estados ='0000000000' -- 5144400116797033 monto menor  5xxxx144400176615010
and ta_tarjeta like '514440%'
 
--cuenta de la tarjeta para visualizar en saldos
select * from  nts_tarjcred..tc_tarjetas where ta_tarjeta= '376653003555051' -- cuenta 1719