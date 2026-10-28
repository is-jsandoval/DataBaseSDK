--***************AMEX
--tarjeta: '376653549162456' cuenta:  549162 expira: 0527 cvv: 9340--AMEX --cuenta 549162--t6a_expira 0527 --ta_cliente=545468  
 select * from nts_tarjcred..tc_tarjetas where   ta_cliente='545468' and ta_expira='0527' and ta_cuenta='549162'
 update  nts_tarjcred..tc_tarjetas set ta_tarjeta ='376653549162456' where  ta_cliente='545468' and ta_expira='0527' and ta_cuenta='549162'

select * from nts_tarjcred.dbo.tc_tarjetas_saldos where ts_tarjeta='376653549162456'
--update nts_tarjcred.dbo.tc_tarjetas_saldos set ts_tarjeta='376653549162456' where ts_tarjeta='340000000002534'

--***************MASTERCARD
--tarjeta: '5230422500386146' cuenta:  549162 expira: 0928 cvv: 731--MASTER --cuenta 549162--t6a_expira 0527 --ta_cliente=545468  
 select * from nts_tarjcred..tc_tarjetas where ta_cliente='3035068' and ta_expira='0928' and ta_cuenta='9915419'
 update  nts_tarjcred..tc_tarjetas set ta_tarjeta ='5200000000002151' where ta_cliente='3035068' and ta_expira='0928' and ta_cuenta='9915419'

select * from nts_tarjcred.dbo.tc_tarjetas_saldos where ts_tarjeta='5230422500386146'
--update nts_tarjcred.dbo.tc_tarjetas_saldos set ts_tarjeta='5200000000002151' where ts_tarjeta='5230422500386146'



--***************VISA
--tarjeta: '5230422500386146' cuenta:  549162 expira: 0527 cvv: 9340--AMEX --cuenta 549162--t6a_expira 0527 --ta_cliente=545468  
 select * from nts_tarjcred..tc_tarjetas where ta_cliente='3035068' and ta_expira='0928' and ta_cuenta='9915419'
 update  nts_tarjcred..tc_tarjetas set ta_tarjeta ='5230422500386146' where ta_cliente='3035068' and ta_expira='0928' and ta_cuenta='9915419'

select * from nts_tarjcred.dbo.tc_tarjetas_saldos where ts_tarjeta='4000000000002503'
--update nts_tarjcred.dbo.tc_tarjetas_saldos set ts_tarjeta='5230422500386146' where ts_tarjeta='5200000000002151'
