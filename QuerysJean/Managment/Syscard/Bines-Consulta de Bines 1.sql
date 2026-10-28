--172.26.61.204.4433
--nts_tarjcred


select * from tc_bines where ob_tipoBin in ('06')  and ob_bin = '514440'


--bines propios
--bines visa credito--no valida
--select * from nts_tarjcred..tc_bines where ob_tipoBin in ('01') 
--bines amex credito--no valida
--select * from nts_tarjcred..tc_bines where ob_tipoBin in ('02')
--bines mastercard credito
select * from nts_tarjcred..tc_bines where ob_tipoBin in ('03') and ob_estado='A'
--bines amex credito
select * from nts_tarjcred..tc_bines where ob_tipoBin in ('04') and ob_estado='A'
--bines visa debito internacional
select * from nts_tarjcred..tc_bines where ob_tipoBin in ('05') and ob_estado='A'
--bines mastercard debito
select * from nts_tarjcred..tc_bines where ob_tipoBin in ('06') and ob_estado='A'
--bines mastercard --no valida
--select * from tc_bines where ob_tipoBin in ('6') 
select * from [nts_tarjcred].[dbo].[tc_bineslocales]