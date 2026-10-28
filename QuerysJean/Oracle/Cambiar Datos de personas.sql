--usuario: BG_TP 
--clave: Bg123456.

--------------------------------------------
--Para cambiar el correo del cliente en Core
--------------------------------------------
--Consulta de Mail 

select * from  bg_admin.t_cltefil h WHERE h.clcdid in('0954121646','0918077181');
select * from  bg_admin.t_cltefil h WHERE h.clcdid = '0903858967';


Update bg_admin.t_cltefil set CLFVTANUAL='20171230' WHERE clcdid ='0954121646'
select * from  bg_admin.t_cltefil h WHERE h.CLNOM1 like '%MOLINA REYES LILI%'


SELECT g.CLKDATO_EMAIL, g.* FROM bg_admin.T_CLKDATO g
WHERE g.clkdato_clave in (SELECT h.clkey FROM bg_admin.t_cltefil h WHERE h.clcdid = '1316396363');

UPDATE bg_admin.T_CLKDATO g
   SET g.CLKDATO_EMAIL = 'eljeansanpost3@gmail.com'
 WHERE g.clkdato_clave in (SELECT h.clkey 
                             FROM bg_admin.t_cltefil h
                            WHERE h.clcdid = '0903858967'); --cédula para cambiar

commit; --no olvidar ejecutar el commit en Oracle

------------------------------------0
SELECT clsegmento,clsegestra FROM bg_admin.t_cltefil h WHERE h.clcdid = '0954121646'--jean
update  bg_admin.t_cltefil   set CLSEGMENTO='NS', CLSEGESTRA='MI' WHERE clcdid = '0954121646'--jean
------------------------------------0

SELECT clsegmento,clsegestra FROM bg_admin.t_cltefil h WHERE h.clcdid = '0903858967'--gis



SELECT g.CLKDATO_EMAIL, g.* FROM bg_admin.T_CLKDATO g
WHERE g.clkdato_clave in (SELECT h.clkey FROM bg_admin.t_cltefil h WHERE h.clcdid in
('0151877339'   
,'0151979259'   
,'0604031096'   
,'0705108314'   
,'0706218534'   
,'0750313934'   
,'0802112862'   
,'0903858967'
,'0916555170'   
,'0916662216'
,'0916708720'   
,'0917284200'   
,'0920720398'   
,'0922361845'
,'0924360654'   
,'0925813701'   
,'0926924671'   
,'0927165902'   
,'0927217869'   
,'0930931795'   
,'0940832231'   
,'0944159185'   
,'0954121646'
,'0962648226'   
,'0963399175'   
,'0963871348'   
,'0964906317'   
,'0965117500'   
,'0966228793'   
,'0966590994'   
,'0966672388'   
,'1104893407'   
,'1105613929'   
,'1203984883'   
,'1204657645'   
,'1301999189'   
,'1306828136'   
,'1312060518'   
,'1314709419'   
,'1351492788'   
,'1712626504'   
,'1713957494'   
,'1719432286'   
,'1721789707'   
,'1722458559'
,'1723499206'
,'1752650661'   
,'1756365662'   
,'1758673659'   
,'1759483462'   
,'1760686178'   
,'1761935913') 
);