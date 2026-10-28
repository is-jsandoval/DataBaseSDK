--usuario: BG_TP 
--clave: Bg123456.

--------------------------------------------
--Para cambiar el correo del cliente en Core
--------------------------------------------
--Consulta de Mail 
SELECT g.CLKDATO_EMAIL, g.* FROM bg_admin.T_CLKDATO g
WHERE g.clkdato_clave in (SELECT h.clkey FROM bg_admin.t_cltefil h WHERE h.clcdid = '0903858967');

UPDATE bg_admin.T_CLKDATO g
   SET g.CLKDATO_EMAIL = 'TUCORREO@bancoguayaquil.com'
 WHERE g.clkdato_clave in (SELECT h.clkey 
                             FROM bg_admin.t_cltefil h
                            WHERE h.clcdid = '0903858967'); --cédula para cambiar

commit; --no olvidar ejecutar el commit en Oracle