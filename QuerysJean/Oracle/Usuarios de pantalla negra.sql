-------------------------------------
--DESBLOQUEAR USUARIO
-------------------------------------
UPDATE BG_ADMIN.T_SECURY SET SE_STATUS = ' ', SE_FVS = 0,SE_KEY='Bg123456' WHERE SE_OPI = 'LP6';
COMMIT;
 
en SE_OPI pones tu OPID

select * from BG_ADMIN.T_SECURY where SE_OPI='JP6'
 
--Para consultar el OPID, aquí buscas por nombre, le pones en mayúsculas, por ejemplo pones tu apellido Mera

SELECT * FROM T_SECURY T WHERE upper(T.SE_NOM) like '%LIZA%' 
SELECT * FROM T_SECURY T WHERE upper(T.SE_NOM) like '%ANGELICA%' AND UPPER(T.SE_OPI) ='AP0'
SELECT * FROM T_SECURY T WHERE UPPER(T.SE_OPI) ='AP0' OR UPPER(T.SE_OPI) ='JC0' 

--Usuario: BG_TP 
--clave: Bg123456
---------------------------------------------------------
--Reseteo de Usuario con Clave Gen�rica en Pantalla Negra
---------------------------------------------------------

--AQUI SE OBTIENE EL OPID POR SI NO LO RECUERDO
SELECT * FROM T_SECURY T WHERE upper(T.SE_NOM) like '%JEAN%';

--ACTUALIZAMOS LA TABLA PARA ACTIVAR EL USUARIO Y LE AGREGAMOS LA CLAVE GEN�RICA Bg123456
UPDATE BG_ADMIN.T_SECURY SET SE_STATUS = ' ', SE_FVS = 0, SE_KEY = '�l���̇' WHERE SE_OPI = 'LM6'; --escribimos el opid del usuario que se va a modificar
COMMIT; --no olvidar ejecutar el commit





--Usuario: BG_TP 
--clave: Bg123456
---------------------------------------------------------
--Agregar Permisos para acceder a transacciones
---------------------------------------------------------

--Con el opid hacemos like para revisar las opciones que tiene acceso la persona a la cual le vamos a copiar la línea de permisos
SELECT * FROM T_TABLGEN_01 where UPPER(TBCODIGO) like ('BR1%')

--Usualmente comienza con el opid seguido de la transacción, por ejemplo 'BR1SFIC'

--Luego hacemos un insert, con el OPID destino, seguido de la transacción 'JC0SFIC'
INSERT INTO T_TABLGEN_01 (tbtipo,tbcodigo,tbdescri) VALUES ('OI', 'JC0SFIC   ', '11111111111                                                                                                                                                                                                                                         ');
COMMIT; --no olvidar ejecutar el commit