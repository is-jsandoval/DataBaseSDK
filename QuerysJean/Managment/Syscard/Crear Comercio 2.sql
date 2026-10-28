--172.26.61.204,4433



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