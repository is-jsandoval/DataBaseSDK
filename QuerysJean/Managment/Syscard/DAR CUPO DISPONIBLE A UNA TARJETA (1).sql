--172.26.61.204.4433
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
--tarjetas validas:
--376650049775586-09/28-8586
--376653002069971-26/12-2377
-----t credito
--***376653VV549162456-05/27-9340
--***523042VV1095004510-28/06-174
--***454063VV0007027280-09/28-252
--***523042VV250xxx0386146-09/28-731 5230422500386146
------------------------
-- TARJETA DE PRUEBAS --
------------------------


--tarjeta: '4110760000016129' cuenta: --  expira: 0227 cvv: 799
--tarjeta: '095412164' cuenta: 761741 expira: 0427 cvv: 719
--tarjeta: '5230420092049182' cuenta: 92049  expira: 0527 cvv: 679
--tarjeta: '5144402438894002' cuenta: 9916308  expira: 0727 cvv: 317

-----t debito
--***5144400334002001-08/28-461

select top(100)* from nts_tarjcred..tc_tarjetas where ta_tarjeta ='5144400334002001'
select top(100)* from nts_tarjcred..tc_tarjetas where ta_cuenta ='549162'
select top(100)* from nts_tarjcred..tc_tarjetas where   ta_cliente='545468' and ta_expira='0527' and ta_cuenta='549162'
select top(100)* from nts_tarjcred..tc_maestro where ma_cuenta='549162'
select * from nts_tarjcred..tc_log_servicio where ls_tarjeta like '3%' order by ls_fecsys1 desc
--log 
select la_estado,* from nts_tarjcred.dbo.tc_atrnautorizalog  where la_tarjeta = '5230422500386146' order by la_fchsis desc 

select la_estado,* from nts_tarjcred.dbo.tc_atrnautorizalog  where la_tarjeta like '514440%' order by la_fchsis desc 
select * from nts_tarjcred..tc_log_servicio where ls_tarjeta like '514440%' order by ls_fecsys1 desc
--comisiones y desglose de trx
select tn_fechaliquida,* from nts_tarjcred.dbo.tc_transacciones where tn_tarjeta like '514440%' order by tn_fchtrx desc
--dar cupo************
select ms_disponiblerotativo,* from  nts_tarjcred..tc_maestro_saldos  where ms_cuenta = '2157880'
--update nts_tarjcred..tc_maestro_saldos  set ms_disponiblerotativo ='20000'where ms_cuenta = '549162'
--update nts_tarjcred..tc_tarjetas set ta_expira= 1226 where ta_tarjeta = '376653002305953'

 
select * from  nts_tarjcred..tc_maestro_saldos where ms_cuenta='9916245'

select ta_servicecode,* from  nts_tarjcred..tc_tarjetas where ta_tarjeta= '376653003555051' -- cuenta 1719

select ms_saldoactual,* from  nts_tarjcred..tc_maestro_saldos 

where ms_cuenta in ('9915847')

update nts_tarjcred..tc_maestro_saldos 
set ms_saldoactual = 10000
where ms_cuenta = '549162'

 
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
select ta_estados,ta_expira,ta_servicecode,ta_tarjeta,ms_disponiblerotativo,ms_disponiblediferido,ms_autorizacionesrot,ms_autorizacionesdif,ms_saldoactual,ts_disponiblerotativo,ts_disponiblediferido,ts_autorizacionesrot,ts_autorizacionesdif,* from nts_tarjcred..tc_tarjetas 
inner join nts_tarjcred..tc_maestro on ta_cuenta = ma_cuenta
inner join nts_tarjcred..tc_maestro_saldos on ta_cuenta = ms_cuenta
inner join nts_tarjcred..tc_tarjetas_saldos  on ts_tarjeta = ta_tarjeta
where ta_estados = '0000000000' and
ta_tarjeta like '514440%'


--consultar comercio activo

select* from nts_tarjcred..tc_comercios_alternos where ca_codalterno like '6%' ca_comercio='232002'
select* from nts_tarjcred..tc_agregador_terminal where at_mid='6000232002'
select* from nts_tarjcred..tc_comercios where co_linkPagos='S'
select* from BG_TComercios.Pagos.lp_comercios where co_comercio_syscard='258292'
select* from BG_TComercios.Pagos.lp_comercios where co_comercio_syscard='258292'
select* from nts_tarjcred..tc_comercios_alternos where ca_comercio='231976'
select * from nts_monitran..mt_catalogo where ca_nombre like '%CodRespInterno%'


SELECT 'tc_atrnautorizalog' as tabla,left(la_tarjeta,6)+'xxx'+right(la_tarjeta,6),BL_DESCRIPCION,bl_cred_deb, la_cproceso,
la_autoriza,LA_FCHSIS, la_montod,la_comercio,la_codrespinterno,la_modocaptura, la_cvv2_ind, dc_valor1,la_ubicacion,la_producto,la_recurrente,la_enrola_recurrente,la_estado,la_tipomsg,la_id,la_c22_pos_entry 'Campo 22',
la_Campo34 ,la_campo126_9cavv,la_campo126_8_XID, la_campo60, la_Campo60_respEmisor ,la_ECI,la_c123_reserved,la_c125_reserved
FROM nts_tarjcred..tc_atrnautorizalog ee 
left JOIN nts_tarjcred.. tc_bineslocales b  ON bl_bin= LEFT(la_tarjeta,6)
join nts_monitran..vw_catalogo on la_codrespinterno=dc_cod and  ca_nombre = 'codrespinterno' 
WHERE la_tarjeta in ('5367814889444006') AND la_fchsis between '2025-07-01 00:00:00.000' and '2025-07-01 23:00:00.000' order by la_fchsis desc

  exec nts_tarjcred.dbo.sp_bprocesa_linkpagos
@pp_empresa   = 1,
@pp_tipoeje   = 'B',
@pp_batchid   = 31,
@pp_procesoid = 9012,
@pp_usuario   = 'apl_sysbatch',
@pp_seculotpr = 2,
@pp_estacion  = 'GYEWLOADAPP01',
@pp_fecha     = '2025-10-13',
@pp_secuc     = 0