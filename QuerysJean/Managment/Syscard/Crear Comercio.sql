--172.26.61.204,4433


--"ruc_compania": "0992323450001",
--        "identificacion": "0917087900",
--        "cargo": "PRESIDENTE",
--        "anios_directorio": "2021-09-17T09:42:16",
--        "cargo_directorio": "PRESIDENTE",
--        "nombres_completos": "HAHN CUCALON FEDERICO JOSE",
--        "fecha_nacimiento": "1978-02-10T00:00:00",
--        "identificacion_conyuge": "0910830595",
--        "nombres_conyuge": "ORELLANA AMPUERO ESTEFANIA",
--        "nacionalidad": "ECUATORIANA",
--        "lugar_nacimiento": "GUAYAS",
--        "estado_civil": "CASADO",
--        "direccion_domiciliaria": "BALSAMOS 210 ENTRE CALLE 1ERA Y V. EMILIO ESTRADA   SO 2 OF-68",
--        "email": "fhahnc@gmail.com",
--        "tipo_telefono": "CELULAR",
--        "telefono": "0994501644"
--------------------------
-- CREACION DE COMERCIOS --
---------------------------


use nts_tarjcred
exec  sp_crea_comercios_syscards 
@pp_estacion = 'QAJean', 
@pp_usuario = 'QAJean', 
@pp_opcion 			 = 'PA', 
@pp_bgpay 			 = 'N', --si o no es BGPAY
@RUC                 = '0602982209001',
@NOMBRE_COMERCIAL    = 'Adicional Test Automations',
@MID                 = '9999982209',--Si es BGPAY no poner  -- si no es si hay que poner mid
@RAZON_SOCIAL        = 'Adicional Test Automations',
@MCC                 = '5411',
@DESC_GIRO           = 'HOSPITALES',
@TIPO_PERS           = 'N',
@PROVINCIA           = 'GUA',
@CIUDAD              = 'GYE',
@CALLE_PRINC         = 'JUNIN',
@NUM_DIREC           = '524',
@CALLE_SECUND        = 'CORDOVA',
@SECTOR              = 'ROCAFUERTE',
@BARRIO              = 'CENTRO',
@TELEFONO1           = '0957770969',
@CELULAR             = '0957770969',
@E_MAIL              = 'is-jsandoval@bancoguayaquil.com',
@NOM_PROP            = 'Jean Sandoval',
@IDENT_PROP          = '0602982209',
@REPRES_LEGAL        = 'Jean Sandoval',
@IDENT_REPRES        = '0602982209',
@BCO_PAGADOR		 = '00018',
@TIPO_DE_CUENTA		 = '02',
@NUMERO_DE_CUENTA	 = '0025284369',
@TIP_IDENT_TIT_CTA	 = 'C',
@IDENT_TIT_CTA		 = '',
@TITULAR_CTA		 = '',
@BOT_CAL			 = 'S',
@OFAC_CAL			 = '000',
@BOT_POLITICAS		 = '',
@CODIGO_RET_IVA	='R70',
@CODIGO_RET_FUENTE = 'R01'

use nts_tarjcred
exec  sp_crea_comercios_syscards 
@pp_estacion = 'QAJean', 
@pp_usuario = 'QAJean', 
@pp_opcion 			 = 'PA', 
@pp_bgpay 			 = 'S', --si o no es BGPAY
@RUC                 = '0903603348001',
@NOMBRE_COMERCIAL    = 'PRUEBAS QA JEAN',
@MID                 = '',--Si es BGPAY no poner  -- si no es si hay que poner mid
@RAZON_SOCIAL        = 'PRUEBAS QA JEAN',
@MCC                 = '5411',
@DESC_GIRO           = 'HOSPITALES',
@TIPO_PERS           = 'N',
@PROVINCIA           = 'GUA',
@CIUDAD              = 'GYE',
@CALLE_PRINC         = 'JUNIN',
@NUM_DIREC           = '524',
@CALLE_SECUND        = 'CORDOVA',
@SECTOR              = 'ROCAFUERTE',
@BARRIO              = 'CENTRO',
@TELEFONO1           = '0930555420',
@CELULAR             = '0930555420',
@E_MAIL              = 'eljeansanpost4@gmail.com',
@NOM_PROP            = 'DIAZ ALBAN CRISTHIAN DAVID',
@IDENT_PROP          = '		',
@REPRES_LEGAL        = 'DIAZ ALBAN CRISTHIAN DAVID',
@IDENT_REPRES        = '1722458559',
@BCO_PAGADOR		 = '00010',
@TIPO_DE_CUENTA		 = '01',
@NUMERO_DE_CUENTA	 = '37425540',
@TIP_IDENT_TIT_CTA	 = 'C',
@IDENT_TIT_CTA		 = '1722458559',
@TITULAR_CTA		 = 'DIAZ ALBAN CRISTHIAN DAVID',
@BOT_CAL			 = 'S',
@OFAC_CAL			 = '000',
@BOT_POLITICAS		 = '',
@CODIGO_RET_IVA	='R70',
@CODIGO_RET_FUENTE = 'R01'

--3

use nts_tarjcred
exec  sp_crea_comercios_syscards 
@pp_estacion = 'QAJean', 
@pp_usuario = 'QAJean', 
@pp_opcion 			 = 'PA', 
@pp_bgpay 			 = 'N', --si o no es BGPAY
@RUC                 = '0908383219001',
@NOMBRE_COMERCIAL    = 'PRUEBAS QA JEAN',
@MID                 = '77770908',--Si es BGPAY no poner  -- si no es si hay que poner mid
@RAZON_SOCIAL        = 'PRUEBAS QA JEAN',
@MCC                 = '5411',
@DESC_GIRO           = 'HOSPITALES',
@TIPO_PERS           = 'N',
@PROVINCIA           = 'GUA',
@CIUDAD              = 'GYE',
@CALLE_PRINC         = 'JUNIN',
@NUM_DIREC           = '524',
@CALLE_SECUND        = 'CORDOVA',
@SECTOR              = 'ROCAFUERTE',
@BARRIO              = 'CENTRO',
@TELEFONO1           = '0930555420',
@CELULAR             = '0930555420',
@E_MAIL              = 'eljeansanpost4@gmail.com',
@NOM_PROP            = 'DIAZ ALBAN CRISTHIAN DAVID',
@IDENT_PROP          = '		',
@REPRES_LEGAL        = 'DIAZ ALBAN CRISTHIAN DAVID',
@IDENT_REPRES        = '1722458559',
@BCO_PAGADOR		 = '00010',
@TIPO_DE_CUENTA		 = '01',
@NUMERO_DE_CUENTA	 = '37425540',
@TIP_IDENT_TIT_CTA	 = 'C',
@IDENT_TIT_CTA		 = '1722458559',
@TITULAR_CTA		 = 'DIAZ ALBAN CRISTHIAN DAVID',
@BOT_CAL			 = 'S',
@OFAC_CAL			 = '000',
@BOT_POLITICAS		 = '',
@CODIGO_RET_IVA	='R70',
@CODIGO_RET_FUENTE = 'R01'
-- codigobg mid tid
--258292	6000258292	BS003159
--consultar 
select co_linkPagos,* from nts_tarjcred..tc_comercios where co_ruc='0925971699001'
--update nts_tarjcred.dbo.tc_comercio_empresa set ce_institucionbanco='24' where cE_comercio='174668'
select co_linkPagos,* from nts_tarjcred..tc_comercios where co_ruc='0930555420001'




select co_linkPagos,* from nts_tarjcred..tc_comercios where co_ruc='0750313934001'
select co_cedulagerente,ce_institucionbanco,ce_cuentacomercio,co_linkPagos,* from nts_tarjcred..tc_comercios  left join nts_tarjcred..tc_comercio_empresa on co_comercio=ce_comercio  where co_comercio='183208'
select co_linkPagos,* from nts_tarjcred..tc_comercios where co_ruc='0750313934001'
select * from nts_tarjcred..tc_comercio_empresa where ce_comercio in ('227813','257745')
select * from nts_tarjcred..tc_comercio_empresa where ce_comercio='258292'
select * from nts_tarjcred..tc_comercios_alternos where ca_codalterno='1791310199'
select * from BG_TComercios.Pagos.lp_limites where lt_comercio='258292'
select co_comercio_syscard,* from BG_TComercios.Pagos.lp_comercios where co_ruc='0954121646001'

update  nts_tarjcred..tc_comercios set co_cedulagerente=' ' where co_ruc='0954121646001'
select co_cedulagerente,co_linkPagos,* from nts_tarjcred..tc_comercios where co_ruc='0954121646001'

--cREAR COMERCIO LINK DE PAGOS
insert into [BG_TComercios].[Pagos].[lp_comercios] 
(co_comercio_syscard,co_ruc,co_giro,co_terminal,co_mid,co_descripcion,co_ciudad,co_pais,co_estado,co_fch_creacion,co_fch_modificacion,co_usr_modificacion,co_estacion_modificacion)  
Values('257074','0954121646001','8062','BS000031','1000080004','Jean Sandoval Quimis','GYE','ECU','A','2025-12-03 10:31:48.310','2025-12-03 10:31:48.310','TestQA','TestQA')

--cREAR COMERCIO LINK DE PAGOS
insert into [BG_TComercios].[Pagos].[lp_comercios] 
(co_comercio_syscard,co_ruc,co_giro,co_terminal,co_mid,co_descripcion,co_ciudad,co_pais,co_estado,co_fch_creacion,co_fch_modificacion,co_usr_modificacion,co_estacion_modificacion)  
Values('202035','0954121646001','8062','BS000031','1000080004','Jean Sandoval Quimis','GYE','ECU','A','2025-12-03 10:31:48.310','2025-12-03 10:31:48.310','TestQA','TestQA')



update nts_tarjcred..tc_comercios set co_linkPagos='S' where co_comercio='202035'

Select * from BG_TComercios.Pagos.lp_comercios




sELECT * FROM  [BG_TComercios].[Pagos].[lp_comercios] WHERE co_ruc='0954121646001'
update  [BG_TComercios].[Pagos].[lp_comercios] set co_estado='X'where co_ruc='0954121646001'

---para crear cuenta del core
--insert into nts_tarjcred..CLKRELA_DIARIO_TC values ('U',CODIGOCLTECORE,CUENTAAH,'TIT','A',TIPOCUENTA)
--select * from nts_tarjcred..CLKRELA_DIARIO_TC

insert into nts_tarjcred..CLKRELA_DIARIO_TC values ('U',CODIGOCLTECORE,CUENTAAH,'TIT','A',TIPOCUENTA)


insert into nts_tarjcred..CLKRELA_DIARIO_TC values ('U','4551652','37425540','TIT','A','C')
select * from nts_tarjcred..CLKRELA_DIARIO_TC where CLKRELA_CTA_CLTE1='3874847'
delete nts_tarjcred..CLKRELA_DIARIO_TC where CLKRELA_CTA_CLTE2='37425540'