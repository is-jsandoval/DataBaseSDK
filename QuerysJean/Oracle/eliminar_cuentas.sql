--eliminar cuenta corriente
delete bg_admin.t_maestro where MTCUENTA  = '0036240040'
--eliminar cuenta de ahorro
delete bg_admin.t_ahmaest where MSCTA='0001046378'

--eliminar cuenta corriente
select * from bg_admin.t_maestro where MTCUENTA  = '0036240040'
--eliminar cuenta de ahorro
select * from bg_admin.t_ahmaest where MSCTA='0001046378'


--consultar cuenta corriente por cedula
select * from bg_admin.t_maestro where MTRUCCED  = '1750192195'
--consultar cuenta de ahorro por cedula
select * from bg_admin.t_ahmaest where MSCDID='0704643170'