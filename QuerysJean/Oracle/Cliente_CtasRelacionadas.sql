--consulta cliente
select * from bg_admin.t_cltefil
WHERE  --clkey = 4694411
CLCDID = '0905227690';


select * from bg_admin.t_cltefil where CLCORR='C'

--correo y celular del cliente
select * from bg_admin.t_clkdato
where clkdato_clave = 4694411

--cuentas relacionadas del cliente
select A.CLKRELA_CTA_CLTE2 CUENTA,  B.CLKRELA_CTA_CLTE2 CLKEY 
from bg_admin.t_clkrela A, --CLIENTE,
bg_admin.t_clkrela B --CUENTA
where A.CLKRELA_CTA_CLTE2 = B.CLKRELA_CTA_CLTE1 
AND B.CLKRELA_TIPREL = 'A' --TIPO_RELACION CUENTA
AND B.CLKRELA_CTA_CLTE2 = '5684211'  --CLKEY