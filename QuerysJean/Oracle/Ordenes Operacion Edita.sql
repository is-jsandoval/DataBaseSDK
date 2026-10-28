--tabla clientes
select * from bg_admin.t_cltefil
WHERE CLCDID = '0903603348';
--tabla orden de operaciones
select * from BG_ADMIN.T_BCKORDE 
--buscar por numero de operacion
select * from BG_ADMIN.T_BCKORDE where BCNUMORD=''
--buscar por numero de cliente
select * from BG_ADMIN.T_BCKORDE where BCCLTE='2135940'

--eliminar Orden de operacion
--delete from BG_ADMIN.T_BCKORDE where BCCLTE='1421592'

--tablas de solicitud
select * from BG_ADMIN.T_SCKSCBA where SCKSCBA_BENEFICIARIO LIKE'%SANDOVAL TOMALA%'
select distinct SCKSCBA_TIPO_CREDITO from BG_ADMIN.T_SCKSCBA order by SCKSCBA_TIPO_CREDITO
--relacion cliente solicitud
select * from BG_ADMIN.t_SCKSCRL where SCKSCRL_CODIGO_IDENTIF='0903603348'


--borrar solicitudes

select * from BG_ADMIN.T_SCKSCBA where SCKSCBA_NUMERO_SOLICITUD in 
(select SCKSCRL_NUMERO_SOLICITUD from BG_ADMIN.t_SCKSCRL where SCKSCRL_CODIGO_IDENTIF='0903603348')

--delete from BG_ADMIN.T_SCKSCBA where SCKSCBA_NUMERO_SOLICITUD in 
(select SCKSCRL_NUMERO_SOLICITUD from BG_ADMIN.t_SCKSCRL where SCKSCRL_CODIGO_IDENTIF='1714552252')

--delete  from BG_ADMIN.t_SCKSCRL where SCKSCRL_CODIGO_IDENTIF='1714552252'
--clientes
select * from bg_admin.t_cltefil
WHERE CLCDID = '0903603348';