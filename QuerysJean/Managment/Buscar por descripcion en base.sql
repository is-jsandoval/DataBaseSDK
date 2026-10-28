SELECT  
    o.name AS stored_procedure,
    m.definition
FROM sys.sql_modules m
JOIN sys.objects o ON m.object_id = o.object_id
WHERE o.type = 'P'
  AND m.definition LIKE '%lp_comercios%';



  172.26.61.24,4433
AppCreabd
Borrar12,