--consulta cliente
select * from bg_admin.t_cltefil where CLNOM1 like '%' ORDER BY CLNOM1
select * from bg_admin.t_cltefil WHERE   CLCDID = '0905227690' --identificación

UPDATE bg_admin.t_cltefil SET CLTEL11='0962740151' WHERE   CLCDID = '0954121646' --identificación
desc bg_admin.t_cltefil

Select * from all_tables WHERE owner='BG_ADMIN' and TABLE_NAME LIKE '%CLK%'

select * from bg_admin.t_cltefil where CLCDID = '0903858967' OR CLCDID = '0954121646' or CLCDID='0930247994' --identificación

select * from bg_admin.t_cltefil where CLCDID = '0954121646' 
select * from bg_admin.t_cltefil where CLCDID = '0903858967' --gis
--editar contactabvilidad 

UPDATE BG_ADMIN.T_CLKDATO clk SET clk.CLKDATO_EMAIL = 'IS-EROMERO@BANCOGUAYAQUIL.COM', clk.CLKDATO_CELULAR = '0964084805'
WHERE clk.CLKDATO_CLAVE = (SELECT CLKEY FROM BG_ADMIN.T_CLTEFIL c where c.CLCDID = '0903858967')
commit

Select * from  BG_ADMIN.T_CLKDATO where CLKDATO_CLAVE = (SELECT CLKEY FROM BG_ADMIN.T_CLTEFIL c where c.CLCDID = '0103976536')

UPDATE bg_admin.t_cltefil 
SET
CLZONA='091' ,CLPROV='9',CLCASI='000000',CLACTIBG='6620000',CLCLACRM='2',CLSTAC='c',CLCLAS='A',CLACTV='0',CLALTPOT='P',CLINCR='108722',CLSECECO='132',CLCDLG='4',CLACTIVIDAD='035',CLCLAS1='1',CLBCAANT='M',CLMAIE='N',CLBANCA='M',CLVTAS='72',CLNAME2='NEGOCIO PROPIO - COMERCIAL',CLSEGESTRA='MP',CLVERSRI='C'
WHERE CLCDID = '0930247994' --identificación
--clkey       id cliente
--CLSEGESTRA  Segmento Estratégico
--CLSECECO    Sector Económico
--CLVTAS      Ventas
--consulta cuenta ahorro
select MSCDID IDENTIFICACION, CL.CLNOM1 NOMBRES, MSCTA NUMERO_CTA, MSSTAT ESTADO, MSSAUS SALDO, MSEFUS SALDO_EFECTIVO, MSORIGCTA ORIGENCTA, MSMTST MOTIVO,
MSNTIT, MSTTIT, MSNREP, MSNFAU
from bg_admin.t_ahmaest A,
bg_admin.t_cltefil CL
where  A.MSCDCL = CL.CLKEY
and a.mscdid = '1900928993'
--and ORIGENCTA= 'NW'
--AND msstat = 0



--consulta cuenta ahorro
select *
from bg_admin.t_ahmaest A,
bg_admin.t_cltefil CL
where  A.MSCDCL = CL.CLKEY
and a.mscdid = '0905227690'
--and ORIGENCTA= 'NW'
--AND msstat = 0

--delete from bg_admin.t_ahmaest where MSCTA='0001046378'

desc bg_admin.t_cltefil

--actualiza segmento cliente
update bg_admin.t_cltefil
set clsegestra ='PE'
WHERE CLCDID = '1300917042';

select * from bg_admin.t_cltefil
WHERE CLNOM1='PULLA ZAMBRANO GARY LENIN'

select * from bg_admin.t_cltefil WHERE CLCDID = '1758868457'
COMMIT;

update bg_admin.t_cltefil set CLCASI='039050' WHERE CLCDID = '0903858967';
COMMIT;
select distinct CLCASI from bg_admin.t_cltefil where CLCASI like '%390%';



--actualiza saldo de la cuenta Ahorros
update bg_admin.t_ahmaest A
set MSSAUS = 2000, MSEFUS = 2000 
where  mscdid = '0954121646'

select * from bg_admin.t_ahmaest A
where  MSCTA='0055160435'


select * from bg_admin.t_ahmaest A
where  MSCDID='0605202852'


select * from bg_admin.t_maestro A
where  MTRUCCED='1790006697001'

update bg_admin.t_ahmaest set MSSTAT='1'  where  MSCDID ='0954121646'--inactiva
update bg_admin.t_ahmaest set MSSTAT='0'  where  MSCDID ='0954121646'--activa


update bg_admin.t_ahmaest  set MSSTAT='1'
where  MSCDID='2300303787';
update bg_admin.t_maestro  set MTSTATUS='1'
where  MTRUCCED = '2300303787';
---------------------consulta de cuentas 
select * from bg_admin.t_ahmaest
where  MSCDID = '0958541521065';

select * from  bg_admin.t_maestro 
where  MTRUCCED = '0958541521065';
--estados de la cuenta de ahorro

/*
MSSTAT
0 ACTIVA
1 INACTIVA
2 INACTIVA/AUDITORIA
3 FONDOS BLOQUEADOS
5 CERRADA
*/
--editar estado consulta cuenta corriente
update bg_admin.t_maestro set MTSTATUS='2'
where  MTCUENTA = '0029579665'

select * from bg_admin.t_maestro 
where  MTRUCCED = '0605202852'


update bg_admin.t_maestro  set MTSTATUS='1'
where  MTRUCCED = '0605202852'

select * from bg_admin.t_maestro 
where  MTCUENTA = '0031042062'
select * from bg_admin.t_cltefil where CLCDID='0954121646'

update bg_admin.t_cltefil SET CLTEL11='962740151' ,CLTEL12='962740151' where CLCDID='0954121646'
--consulta cuenta corriente
select 
mtrucced IDENTIFICACION, CL.CLNOM1 NOMBRES,  MTCUENTA NUMERO_CTA , MTSTATUS ESTADO, MTSALTOT SALDO, MTEFECTV SALDO_EFECTIVO
from bg_admin.t_maestro C,
bg_admin.t_cltefil CL
where C.MTCODCLT = CL.CLKEY
AND MTCUENTA = '0025624122'
--AND MTSALTOT > 5000 
AND mtrucced  = '0924737430'
--mtstatus = 1

update bg_admin.t_maestro C
set MTSTATUS = 0
where mtrucced  = '0928429737'


--consulta cuenta amiga
select makcuen_identificacion IDENTIFICACION, MAKCUEN_NRO_CUENTA NUMERO_CTA, MAKCUEN_ESTATUS_CUENTA ESTADO, MAKCUEN_SALDO_TOTAL SALDO, MAKCUEN_SALDO_EFECTIVO SALDO_EFECTIVO
from bg_admin.t_MAKCUEN
where makcuen_identificacion = '0953439585'



--cuenta rol electronico
select * from bg_admin.t_rekcuen
where rekcuen_ident_numero like '%5367681062630080%'

select * from bg_admin.t_rekmovm
where rekmovm_tarj like '%62605009%'

--Movimientos

select CTKMOVD_CUENTA, CTKMOVD_FECHA, CTKMOVD_HORA, MVTIPO, MVMOTV, MVFECH, MVCTA, MVDOCU, MVMONT, CTKMOVD_NOTA_DIREC,MVCIUD, MVTPDP,CTKMOVD_TERM_ADICIONA, MVHORA 
FROM BG_ADMIN.T_CTKMOVD 
WHERE CTKMOVD_CUENTA = 3812456 AND CTKMOVD_FECHA = 20220928 AND MVMOTV IN ('QI1', 'QI2', 'QI3', 'QI4', 'RJE','RJA','RJB','RJC','RJD','RJE','RJF','RJG')



UPDATE BG_ADMIN.T_CLKDATO clk SET clk.CLKDATO_EMAIL = 'ELJEANSANPOST4@GMAIL.COM', clk.CLKDATO_CELULAR = '0962740151'
WHERE clk.CLKDATO_CLAVE = (SELECT CLKEY FROM BG_ADMIN.T_CLTEFIL c where c.CLCDID = '0930247994')
commit

select CLKDATO_EMAIL, CLKDATO_CELULAR from
BG_ADMIN.T_CLKDATO
WHERE CLKDATO_CLAVE = (SELECT CLKEY FROM BG_ADMIN.T_CLTEFIL c where c.CLCDID = '0930247994')



select * from
BG_ADMIN.T_CLKDATO
WHERE CLKDATO_CLAVE = (SELECT CLKEY FROM BG_ADMIN.T_CLTEFIL c where c.CLCDID = '0930247994')



select * from bg_admin.t_cltefil
where CLCDID='1792987334001'

update bg_admin.t_cltefil
set clvtas = 1600
where clkey = 5785742