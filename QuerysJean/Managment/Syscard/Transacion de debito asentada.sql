--204
--tabla de autorizacion a transaccion asentadas
select tn_total, tn_subtotal,tn_impuesto,tn_comision,'|||||||||',tn_tasacomision,tn_retencioniva,tn_retencionftetasa,tn_retencionivatasa,tn_tasaiva,tn_ivacomision,
tn_base_imponible,tn_base_imponible_cero,tn_comision_neto,tn_comision_iva,tn_comision_netoSRI,tn_comision_ivaSRI,tn_retencionfteSRI,tn_retencionivaSRI,
tn_comision_fija,tn_iva_comision_fija,tn_tasacomisionenMDR,tn_comisionenMDR,* from nts_tarjcred.dbo.tc_transacciones where tn_tarjeta = '5230420092049182'



select la_estado,* from nts_tarjcred.dbo.tc_atrnautorizalog where la_id in ( '697529900','679359543')

select f10_comision,f10_retencionfte,f10_retencioniva,f10_subtotal,f10_impuesto,f10_retencionftetasa,f10_retencionivatasa,
f10_tasaiva,f10_ivacomision,f10_base_imponible_cero,f10_comision_neto,f10_comision_iva,f10_comision_ivaSRI,f10_retencionfteSRI,
f10_retencionivaSRI,f10_comision_fija,f10_iva_comision_fija,f10_tasacomisionenMDR,f10_comisionenMDR,'||||||||||||',f10_onus,* from nts_intercambio..ic_outincf10 where  f10_secuencial in ('646570193','646500727')--f10_authorization = '001377' and f10_comercio = 259451 and f10_fechaproceso = '260429' and










133-4.93=128.07 en la cuenta
133 total de la compra
17.34 es el iva de la compra
115.65 es el subtotal de la compra



comision 4.93
comision neta 4.29
ivacomision 0.73 
comision iva y iva sri  0.64
retencion a la fuente 2.22



