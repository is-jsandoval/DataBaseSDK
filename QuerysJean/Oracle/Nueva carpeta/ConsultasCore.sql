--consulta cliente id para luesgo actualizar el celular en t_clkdato
select * from bg_admin.t_cltefil
WHERE  clnom1 like 'chubb%'
--83445
select * from bg_admin.t_clkdato

--consulta cliente
select * from bg_admin.t_cltefil
WHERE  clkey = 4694411
--CLCDID = '0850243262'

--actualiza segmento cliente
update bg_admin.t_cltefil
set clsegestra ='PE'
WHERE CLCDID = '1300917042'

--consulta cuenta ahorro
select * from bg_admin.t_ahmaest A,
bg_admin.t_cltefil CL
where  A.MSCDCL = CL.CLKEY
--AND msstat = 0
and a.mscdid = '1723882757'

--actualiza estado de la cuenta a Activo
update bg_admin.t_ahmaest A
set msstat = '0'
where  mscdid = '1002404133'


--consulta cuenta corriente
select * from bg_admin.t_maestro C
where mtrucced  = '1723882757'
order by mtrucced
--mtstatus = 1

--cuenta rol electronico
select * from bg_admin.t_rekcuen

--consulta cuenta amiga
select * from bg_admin.t_MAKCUEN
where makcuen_identificacion = '1723882757'



------------------------
--SOLICITUDES DE CREDITO
------------------------

--INTEGRANTES DEL CREDITO
SELECT * FROM BG_ADMIN.T_SCKSCRL 
WHERE SCKSCRL_CODIGO_IDENTIF = '0992251069001'
AND SCKSCRL_FECHA_INFORME > 20210601

--SOLICITUD
SELECT * FROM BG_ADMIN.T_SCKSCBA
WHERE SCKSCBA_NUMERO_SOLICITUD = 011000174
--SCKSCBA-TIPO-EXPEDIENTE = 'R' para saber si la solicitud fu e


SELECT *
FROM BG_ADMIN.T_SCKSCRL A 
JOIN  BG_ADMIN.T_SCKSCBA B 
ON  A.SCKSCRL_NUMERO_SOLICITUD = B.SCKSCBA_NUMERO_SOLICITUD
WHERE SCKSCRL_CODIGO_IDENTIF = '0992251069001'
and SCKSCBA_ESTATUS_SOLICITUD IN ('D','P', 'A') 


select  * 
FROM BG_ADMIN.T_BCKSLMC 
where MS_CDIDEN = '0992251069001' and
  MS_SITUAC in ('P','I','R','A')
AND MS_FECI > 0

---------------------
--ORDEN DE OPERACION
---------------------
select *
from BG_ADMIN.T_BCKORDE
where BCCLTE = 140840 
AND bcstatus = 'P'
 and BCPROD NOT IN('NN','NV','NM','IN', 'II', 'FE', 'GA','CC', 'JC','LG','XI','JR','IV')

--solicitudes sin orden liquidada
select  MS_SITUAC, MS_FEC_LIQUID, BCPROD, MS_CDCLTE, MS_BCOP, MS_PLAZA, MS_SECUEN, MS_TPIDEN, MS_CDIDEN, MS_FECI,
C.BCFEC_ING, (BCFEC_ING - NVL(MS_FECI,0)) AS DIAS_TRANSCURRIDOS
from BG_ADMIN.T_BCKSLMC  B  
  join BG_ADMIN.T_CLTEFIL  CL
      ON CL.CLKEY = B.MS_CDCLTE 
LEFT JOIN BG_ADMIN.T_BCKORDE   C 
   ON B.MS_CDCLTE = C.BCCLTE AND B.MS_BCOP = C.BCNUMORD
   WHERE  CL.CLCDID =  '1300917042'
    AND   MS_SITUAC in ('P','I','R','A')
       AND (MS_FEC_LIQUID IS NULL OR  MS_FEC_LIQUID = 0)


--una vez aprobada la orden se crea el credito
----------
--CREDITO
---------
--cabecera
select * from BG_ADMIN.T_SOKDODG
where sokdodg_cliente = 250518
and sokdodg_orden_operac = 863348

--detalle
SELECT  * FROM    BG_ADMIN.T_SOKDODT
 WHERE   
 SOKDODT_CLIENTE    = 4694411
AND     SOKDODT_TIPO       = 'MC'
AND     SOKDODT_NUMERO_DOC = 662238
AND     SOKDODT_ESTATUS NOT IN ('C', 'R')


select --SOKDODT_ESTATUS, SOKDODT_ESTATUS_REG
sum(sokdodt_capital) capital, sum(sokdodt_interes) interes 
from BG_ADMIN.T_SOKDODT
where sokdodt_cliente = 4694411
and sokdodt_numero_doc = 662238
--AND     SOKDODT_ESTATUS not IN ('C', 'R')
--AND     SOKDODT_ESTATUS_REG = 'A'
order by SOKDODT_ESTATUS
--SOKDODT_ESTATUS: C - CANCELADO, R - RENOVADA

--SEGUIMIENTO DE CREDITO
select *
from BG_ADMIN.T_OBLIGAC
where OBCLCLTE = 16829
AND OBDCDOCU = 544238

