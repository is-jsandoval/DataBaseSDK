
--------------------------------------------------------------------------
--CONSULTAR CEDULA Y EN NUEVA TABLA DE SEGMENTO

select * from BG_ADMIN.t_cltefil where clcdid ='0954121646';

select CLBANCA,CLSEGESTRA from BG_ADMIN.t_cltefil where clcdid ='2450137134';

-- CLSEGESTRA=SUB_SEGMENTO  - CLBANCA=COD_SUB_SEGMENTO

select distinct TIPO_PERSONA from BG_ADMIN.t_segmentacion
select * from BG_ADMIN.t_segmentacion WHERE TIPO_PERSONA=''
select * from BG_ADMIN.t_segmentacion WHERE IDENTIFICACION = '0954121646';--Jean
select * from BG_ADMIN.t_segmentacion WHERE IDENTIFICACION = '1722458559';--DIAZ
select * from BG_ADMIN.t_segmentacion WHERE IDENTIFICACION = '0903858967';--GIZ


select * from BG_ADMIN.t_segmentacion WHERE IDENTIFICACION IN ( '0954121646','0903858967','1722458559');--GIZ y Jean



select IDENTIFICACION,SUB_SEGMENTO,COD_SUB_SEGMENTO from BG_ADMIN.t_segmentacion WHERE IDENTIFICACION = '0550137939'; --

--select clcdid,CLSEGESTRA,CLBANCA from BG_ADMIN.t_cltefil where clcdid ='1800000554'; --

--CONSULTA DEL CLIENTE

select * from BG_ADMIN.t_cltefil where clcdid ='1757986136'; --5985515