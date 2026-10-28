Select * from all_tables WHERE owner='BG_ADMIN'---ver todas las tablas
select * from  all_tables
Select * from all_tables WHERE owner='BG_ADMIN' and TABLE_NAME LIKE '%ORDE%'
 
select  * from BGB_REP.T_TCKDCXC;--consulta de algun tipo de tarjetas 

select  * from BGB_REP.T_AHMAEST WHERE  MSNOMB like '%FLORES PEREZ ABRAHAM%'--consulta de maestro de cuentas
select  * from BGB_REP.T_AHMAEST WHERE  MSCDID ='0954121646'--consulta de maestro de cuentas
select  * from USUARIO02.T_SCKIVAD----NULL
select  * from BGB_REP.T_ESTRUCTURA_MC--ALGUN TIPO DE REPORTE DE ESTRUCTURA
--cuenta jean pruebas 0029579665
select * from BG_ADMIN.TMP_SOKMOVI
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
--movimientos de cuentas ---------------------------------------------------------------------------------------------
select * from BG_ADMIN.T_AHKMOVA where AHKMOVA_CUENTA='29579665' order by MVFECH DESC;
--DELETE BG_ADMIN.T_AHKMOVA where AHKMOVA_CUENTA='29579665' 
--cuando el MVMOTV es EG7 es recargas--cuando es EFM es efectivo movil--cuando es 3A es consumo debito--cuando es OP son operaciones de transferencias
--MCTIPO-NC=nota credito,ND= nota de debito
--campo MVPOST N y el siguiente M no se que sera pero nunca se merefleejo en el cel
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////
select * from BG_ADMIN.T_AHKMOVA_AUX;--no hay nada en dev
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
select * from BG_ADMIN.T_AHKMOVH where AHKMOVH_CUENTA='23598820' and MVTIPO='NC'ORDER BY CTKMOVD_FECH_PROC DESC,MVHORA DESC;--movimientos de cuenta
select MVTIPO,MVMOTV,MVFECH,MVMONT,MVHORA from BG_ADMIN.T_AHKMOVH where AHKMOVH_CUENTA='23598820' ORDER BY MVFECH DESC,MVHORA DESC
select MVTIPO,MVMOTV,MVFECH,MVMONT,MVHORA from BG_ADMIN.T_AHKMOVH where AHKMOVH_CUENTA='29579665' AND MVTIPO='NC' ORDER BY MVFECH DESC,MVHORA DESC
select * from BG_ADMIN.T_AHKMOVH where AHKMOVH_CUENTA='29579665' AND MVTIPO='NC' ORDER BY MVFECH DESC,MVHORA DESC
--campo MVMOTV--=83 es Pagos a terceros--RT= retiros cajero
--Compra comercio KFC =30
--pago empresa electrica=CJU
--comision de la empresa electrica CMH
--ZJ=comision tranferencia interbancaria
--E10 transferencia interbancaria
--NDR=Renovacion bancontro y comision
--CUando ese campo esta vacio es deposito y el campo MCTipo es DP
--7C=Movimiento de meta
--7MP=Retiro de tu meta
--3A=Compra comercio
--CS= card servicio costo del servicion de la tarjeta
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
select * from BG_ADMIN.T_BCKMOVD ;--n
select * from BG_ADMIN.T_COKMOVD;--
select * from BG_ADMIN.T_COKMOVH;--
select * from BG_ADMIN.T_CSKMOVI;--
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
select * from BG_ADMIN.T_CTKMOVD where CTKMOVD_CUENTA='29579665'
--CAMPO MVMOTV 
--CJU=cobros servicios basicos
--CMH=comision por servicios basicos
--CK2=CObros Servicios intenet
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


























