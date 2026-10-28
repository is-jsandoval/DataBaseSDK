--172.26.61.204.4433
--nts_tarjcred


select top(100) *from nts_tarjcred..tc_log_autoriza
inner join nts_tarjcred..tc_bines on left(al_tarjeta,6)=ob_bin
inner join  nts_tarjcred..tc_atrnautoriza on al_tarjeta=ta_tarjeta and al_autoriza=ta_autoriza
where  left(al_tarjeta,6)='514440' and ob_estado='A'
and ta_codresp='00'
and left(ta_tipomsg,2) in('01','02')
and isnull(ta_tipotran,'00') in('00','05')

--exec sp_helptext sp_bprocesa_linkpagos	

select distinct ta_identifica from nts_tarjcred..tc_atrnautoriza

select top(100) al_trama_in,*from nts_tarjcred..tc_log_autoriza
inner join nts_tarjcred..tc_bines on left(al_tarjeta,6)=ob_bin
inner join  nts_tarjcred..tc_atrnautoriza on al_tarjeta=ta_tarjeta and al_autoriza=ta_autoriza
where  
--ta_codresp='00'
 left(ta_tipomsg,2) in('01','02')
and isnull(ta_tipotran,'00') in('00','05')
and left(al_tarjeta,6)='514440'
and al_tarjeta='5144402673607002'
and al_monto like '%123%'
-- ta_comercio like '%232002%'
--and ob_estado='A'	

select top(100) *from nts_tarjcred..tc_atrnautoriza 
left join nts_tarjcred..tc_log_autoriza on ta_tarjeta=al_tarjeta
and ta_autoriza=al_autoriza
where ta_tarjeta='5144403190090005'


select top(100)ca_codalterno,* from nts_tarjcred..tc_atrnautoriza 
left join nts_tarjcred..tc_comercios_alternos  on ta_comercio=ca_comercio
where left(ta_tarjeta,6)='536781' and ta_comercio='232002' and ta_montof='1.44'



select top(100) al_trama_in,*from nts_tarjcred..tc_log_autoriza WHERE LEFT(al_tarjeta,6)='450806' 
inner join nts_tarjcred..tc_bines on left(al_tarjeta,6)=ob_bin
inner join  nts_tarjcred..tc_atrnautoriza on al_tarjeta=ta_tarjeta and al_autoriza=ta_autoriza
where  
al_tarjeta='5144402673607002' and al_monto like '587'


select top(100)* from nts_tarjcred..tc_atrnautoriza where  left(ta_tarjeta,6)='450806'
select top(100)* from nts_tarjcred..tc_atrnautorizalog where  left(la_tarjeta,6)='450806'