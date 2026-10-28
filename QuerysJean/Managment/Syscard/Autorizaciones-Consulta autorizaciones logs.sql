--172.26.61.204,4433
--CONSULTAR RESPUESTA DE ERROR
select * from nts_monitran..vw_catalogo where dc_cod = '0842' and ca_nombre = 'CodRespInterno'--217
--produbanco 
select la_codresp,la_codrespinterno,la_estado,la_iva,* from nts_tarjcred..tc_atrnautorizalog where la_tarjeta='4166830040328900' order by la_fchsis desc
--pichincha visa debito
select la_codresp,la_codrespinterno,la_estado,la_iva,* from nts_tarjcred..tc_atrnautorizalog order by la_fchsis desc
select la_estado,la_iva,* from nts_tarjcred..tc_atrnautorizalog where la_tarjeta like '438108%' order by la_fchsis desc
select la_estado,la_iva,* from nts_tarjcred..tc_atrnautorizalog where la_tarjeta = '4166830046213932' order by la_fchsis desc
--produbanco visa debito
select la_estado,la_iva,* from nts_tarjcred..tc_atrnautorizalog where  la_codresp='00' and  la_tarjeta like '416683%' order by la_fchsis desc
--bolivarian visa debito
select la_estado,la_iva,* from nts_tarjcred..tc_atrnautorizalog where la_tarjeta like '475395%' order by la_fchsis desc
select la_estado,la_iva,* from nts_tarjcred..tc_atrnautorizalog where la_tarjeta = '4753950201287965' order by la_fchsis desc
--titaniun visa credito
select la_estado,la_iva,* from nts_tarjcred..tc_atrnautorizalog where la_tarjeta like '450081%' order by la_fchsis desc
select la_estado,la_iva,* from nts_tarjcred..tc_atrnautorizalog where la_tarjeta like '4500810003034336%' order by la_fchsis desc


select la_estado,la_iva,* from nts_tarjcred..tc_atrnautorizalog where la_tarjeta='4540630099990478' order by la_fchsis desc
select ta_iva,* from nts_tarjcred..tc_atrnautoriza where ta_tarjeta='376653002305953' order by ta_fchsis desc

select * from nts_tarjcred..tc_atrnautorizalog where la_tarjeta='376653002069971' order by la_fchsis desc
select * from nts_tarjcred..tc_atrnautoriza where ta_tarjeta='376653002069971' order by ta_fchsis desc

--376653xxxxxxxx002069971
select * from nts_tarjcred..tc_atrnautorizalog where la_tarjeta='376653002069971' order by la_fchsis desc
select * from nts_tarjcred..tc_atrnautoriza where ta_tarjeta='376653002069971' order by ta_fchsis desc

--SALDO ACTUAL = 377.85------------AUTORIZACIONEROT=5060.19---INICIAL

--SALDO ACTUAL = 468.95------------AUTORIZACIONEROT=4969.09---SEGUNDO

--SALDO ACTUAL = 91.10------------AUTORIZACIONEROT=91.10---TOTAL

select ms_saldoactual,ms_autorizacionesrot,* from nts_tarjcred..tc_maestro_saldos where ms_cuenta=1923632
select la_estado,* from nts_tarjcred..tc_atrnautorizalog where la_tarjeta='376653002069971' and convert(varchar(10),la_fchsis,120) = '2025-05-06'
select ta_estado,* from nts_tarjcred..tc_atrnautoriza where ta_tarjeta='376653002069971' and convert(varchar(10),ta_fchsis,120) = '2025-05-06'


select tn_origen,tn_fechaliquida,* from nts_tarjcred..tc_transacciones where tn_tarjeta='376653002069971' and convert(varchar(10),tn_fchtrx,120) = '2025-05-06'

exec nts_tarjcred.dbo.sp_bprocesa_linkpagos
@pp_empresa   = 1,
@pp_tipoeje   = 'B',
@pp_batchid   = 31,
@pp_procesoid = 9012,
@pp_usuario   = 'apl_sysbatch',
@pp_seculotpr = 2,
@pp_estacion  = 'GYEWLOADAPP01',
@pp_fecha     = '2025-09-15',
@pp_secuc     = 0

update nts_tarjcred..tc_empresa set em_fchproceso = '2025-07-02',em_fchaprocesar='2025-07-02'

		

select top(100)* from nts_tarjcred..tc_atrnautoriza 
where ta_autoriza='536781'
where ta_codresp='00'
and left(ta_tipomsg,2) in('01','02')
and isnull(ta_tipotran,'00') in('00','05')

select top(100)* from nts_tarjcred..tc_atrnautoriza 
inner join nts_tarjcred..tc_log_autoriza on ta_autoriza=al_autoriza and ta_tarjeta=al_tarjeta
where ta_autoriza='536781'

select top(100)la_estado,* from nts_tarjcred..tc_atrnautorizalog WHERE la_tarjeta='376653002069971' order by la_fchsis desc