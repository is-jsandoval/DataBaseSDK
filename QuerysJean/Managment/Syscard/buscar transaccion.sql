select l.la_ECI, l.la_3ds_aav,* from nts_tarjcred.dbo.tc_log_servicio a with (nolock)
inner join nts_tarjcred..tc_atrnautorizalog l with (nolock) on a.ls_tarjeta=l.la_tarjeta
inner join nts_tarjcred.dbo.tc_tarjetas t with (nolock) on t.ta_tarjeta = a.ls_tarjeta
inner join nts_tarjcred.dbo.tc_iso8583_ext_in b with (nolock) on b.ex_id = a.ls_sec
inner join nts_tarjcred.dbo.tc_iso8583_ext_out c with (nolock) on c.ex_id=b.ex_id
where ls_tarjeta like '4%' 
--and ( b.c48_additional_data_priv_use like '%9203%' and b.c48_additional_data_priv_use like '42070103212' and b.c48_additional_data_priv_use like'%4328kB%')
and l.la_tokenReqId is not null


select * from nts_tarjcred.dbo.tc_log_servicio a with (nolock)
where ls_tarjeta like '416683%' 

select l.la_ECI, l.la_3ds_aav,* from nts_tarjcred.dbo.tc_log_servicio a with (nolock)
inner join nts_tarjcred..tc_atrnautorizalog l with (nolock) on a.ls_tarjeta=l.la_tarjeta
inner join nts_tarjcred.dbo.tc_iso8583_ext_in b with (nolock) on b.ex_id = a.ls_sec
inner join nts_tarjcred.dbo.tc_iso8583_ext_out c with (nolock) on c.ex_id=b.ex_id
where ls_tarjeta like '416683%' 

select top(100)* from nts_tarjcred.dbo.tc_log_servicio
select top(100)* from nts_tarjcred..tc_atrnautorizalog
select top(100)* from nts_tarjcred.dbo.tc_tarjetas 
select top(100)* from nts_tarjcred.dbo.tc_iso8583_ext_in 
select top(100)* from nts_tarjcred.dbo.tc_iso8583_ext_out 
