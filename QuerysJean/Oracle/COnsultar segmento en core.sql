--CONSULTAR CEDULA Y EN NUEVA TABLA DE SEGMENTO

select * from BG_ADMIN.t_cltefil where clcdid ='2450137134';

select CLBANCA,CLSEGESTRA from BG_ADMIN.t_cltefil where clcdid ='2450137134';

-- CLSEGESTRA=SUB_SEGMENTO  - CLBANCA=COD_SUB_SEGMENTO

select * from BG_ADMIN.t_segmentacion WHERE IDENTIFICACION = '0954121646';
select * from BG_ADMIN.t_segmentacion WHERE IDENTIFICACION = '0903858967';
----------------------------------------------------------
update BG_ADMIN.t_segmentacion set SEGMENTO_CONSOLIDADO='MICRO',VENTAS='72',ANIO_VENTAS='20250127',SUB_SEGMENTO='MICRO',SEGMENTO_NUEVO='MICRO',COD_SUB_SEGMENTO='MI',COD_BANCA='M',CUADRANTE='Preservar',COD_CUADRANTE='7' 
where IDENTIFICACION = '0954121646';
----------------------------------------------------------

select IDENTIFICACION,SUB_SEGMENTO,COD_SUB_SEGMENTO from BG_ADMIN.t_segmentacion WHERE IDENTIFICACION = '0550137939'; --

--select clcdid,CLSEGESTRA,CLBANCA from BG_ADMIN.t_cltefil where clcdid ='1800000554'; --

--CONSULTA DEL CLIENTE
 
select * from BG_ADMIN.t_cltefil where clcdid ='1757986136'; --5985515