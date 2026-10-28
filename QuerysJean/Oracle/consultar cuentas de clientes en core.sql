SELECT COALESCE(
    (SELECT MSCTA 
     FROM bg_admin.t_ahmaest 
     WHERE MSCDID = :cedula 
     FETCH FIRST 1 ROWS ONLY),

    (SELECT MTCUENTA 
     FROM bg_admin.t_maestro 
     WHERE MTRUCCED = 0954121646 
     FETCH FIRST 1 ROWS ONLY)
) AS CUENTA
FROM dual
 