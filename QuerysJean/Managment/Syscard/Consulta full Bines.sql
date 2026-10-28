SELECT 'tc_atrnautorizalog' as tabla,left(la_tarjeta,6)+'xxx'+right(la_tarjeta,6),
BL_DESCRIPCION,la_estado, bl_cred_deb, la_cproceso,la_autoriza,LA_FCHSIS,
 la_montod,la_montof,la_comercio,la_monedad,la_codresp,la_codrespinterno,
 dc_valor1,la_estado, la_tipomsg,la_id,la_c22_pos_entry 'Campo 22',la_Campo34 ,
 la_campo126_9cavv,la_campo126_8_XID, la_campo60, la_Campo60_respEmisor ,la_ECI,la_c123_reserved,la_c125_reserved,
*FROM nts_tarjcred..tc_atrnautorizalog ee 
left JOIN nts_tarjcred.. tc_bineslocales b  ON bl_bin= LEFT(la_tarjeta,6)
join nts_monitran..vw_catalogo on la_codrespinterno=dc_cod and  ca_nombre = 'codrespinterno' 
WHERE la_tarjeta in ('4540630099990478') 
AND la_fchsis between '2025-01-28 12:00:00.000' and '2025-01-28 18:47:00.607'
--and la_codrespinterno in ('0000','1010' )--and la_comercio='24331'  and  la_codrespinterno<>''
-- is not null-
--AND right(la_identifica,8)='NG047588'
--and la_codrespinterno ='0110'
select 'tc_atrnautoriza' as tabla,ta_comercio as comerciosyscard,ta_estado,ta_fchproceso, ta_autoriza, ta_tipomsg,
left(ta_tarjeta,6)+'xxx'+right(ta_tarjeta,6),ta_montod,ta_fchsis,ta_codresp,ta_codrespinterno,dc_valor1,
*from nts_tarjcred..tc_atrnautoriza aa 
join nts_monitran..vw_catalogo on ta_codrespinterno=dc_cod and  ca_nombre = 'codrespinterno' 
where ta_fchsis>='2025-01-28 12:00:00.000' and ta_tarjeta in ('4540630099990478')--and ta_codrespinterno ='0110'
 
 
SELECT 'tc_atrnautorizalog' as tabla,left(la_tarjeta,6)+'xxx'+right(la_tarjeta,6),BL_DESCRIPCION,la_estado, bl_cred_deb,
 la_cproceso,la_autoriza,LA_FCHSIS, la_montod,la_montof,la_comercio,la_monedad,la_codresp,la_codrespinterno,dc_valor1,la_estado,
  la_tipomsg,la_id,la_c22_pos_entry 'Campo 22',la_Campo34 ,la_campo126_9cavv,la_campo126_8_XID, la_campo60, la_Campo60_respEmisor ,
  la_ECI,la_c123_reserved,la_c125_reserved,
*FROM nts_tarjcred..tc_atrnautorizalog ee 
left JOIN nts_tarjcred.. tc_bineslocales b  ON bl_bin= LEFT(la_tarjeta,6)
join nts_monitran..vw_catalogo on la_codrespinterno=dc_cod and  ca_nombre = 'codrespinterno' 
WHERE la_tarjeta in ('4540630099990478') 
AND la_fchsis between '2025-01-29 12:00:00.000' and '2025-01-29 18:47:00.607'
--and la_codrespinterno in ('0000','1010' )
--and la_comercio='24331'  and  la_codrespinterno<>''-- is not null-
--AND right(la_identifica,8)='NG047588'
--and la_codrespinterno ='0110'
 
select 'tc_atrnautoriza' as tabla,ta_comercio as comerciosyscard,ta_estado,ta_fchproceso, ta_autoriza, ta_tipomsg,
left(ta_tarjeta,6)+'xxx'+right(ta_tarjeta,6),ta_montod,ta_fchsis,ta_codresp,ta_codrespinterno,dc_valor1,
*from nts_tarjcred..tc_atrnautoriza aa 
join nts_monitran..vw_catalogo on ta_codrespinterno=dc_cod and  ca_nombre = 'codrespinterno' 
where ta_fchsis>='2025-01-29 12:00:00.000' and ta_tarjeta in ('4540630099990478')--and ta_codrespinterno ='0110'
 
select  *from nts_tarjcred..tc_log_servicio  where ls_tarjeta = '4540630099990478'
and ls_fecsys1 between '2025-01-29 13:29:55.120' and '2025-01-29 18:54:04.893' order by ls_fecsys1 desc
 
select top 10 * from nts_tarjcred.dbo.tc_tcomerdiferi
select c2.* from nts_tarjcred..tc_comercios c1 left join nts_tarjcred..tc_comercios_alternos c2
on c1.co_comercio=c2.ca_comercio where co_comercio='127526'
 
select * from nts_tarjcred..tc_comercios where co_comercio='127526'
 
select bl_cred_deb, * from tc_bineslocales where  bl_bin = '550470'
 


 
select TOP 100 * from nts_tarjcred..tc_atrnautorizalog where la_origenautorizacion = 'DFAS' AND la_modocaptura IN (05) 
AND la_empresa = '1' and la_recurrente = 'n' and la_codrespinterno = '0000' and left (la_tarjeta,1) = '3'
 