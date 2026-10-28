--172.26.61.204,4433

select la_codrespinterno, la_codresp, la_tipomsg, * from nts_tarjcred..tc_atrnautorizalog 
where  la_tarjeta ='5144409999042004'  
order by la_fchsis desc  
 
select ls_servicio,ls_codrespinterno, ls_msgtype ,*  from nts_tarjcred..tc_log_servicio
where ls_tarjeta = '5144409999042004' and ls_servicio='DFAS'
order by ls_fecsys1 desc
 
select c4_monto, c6_monto_cardbill,* from nts_tarjcred..tc_iso8583_ext_in 
where c38_autorizacion='xxxx' 
order by  ex_fechatrn_in desc    
 
select c39_resp_code,c38_autorizacion, * from nts_tarjcred..tc_iso8583_ext_out
where c2_tarjeta='xxxxx'  
order by  ex_fechatrn desc


select * from nts_tarjcred..tc_tarjetas where ta_tarjeta='376653002305953'