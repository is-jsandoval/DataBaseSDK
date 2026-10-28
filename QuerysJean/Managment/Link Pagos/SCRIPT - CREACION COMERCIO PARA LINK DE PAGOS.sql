use nts_tarjcred
go
-------------------------------
------ CREACION DE COMERCIOS --SEGUNDO SANDOVAL
-------------------------------
declare @rc int = 0, @msgerr varchar(4000) = null

exec @rc = dbo.sp_crea_comercios_syscards 
@pp_estacion		 = 'testqa', 
@pp_usuario			 = 'testqa', 
@pp_opcion 			 = 'PA', 
@pp_bgpay 			 = 'N', 
@RUC                 = '0903603348001',
@NOMBRE_COMERCIAL    = 'SEGUNDO SANDOVAL TOMALA' ,
@MID                 = '7000007774',
@RAZON_SOCIAL        = 'SEGUNDO SANDOVAL TOMALA',
@MCC                 = '8062',
@DESC_GIRO           = 'HOSPITALES',
@TIPO_PERS           = 'N',
@PROVINCIA           = 'GUA',
@CIUDAD              = 'GYE',
@CALLE_PRINC         = 'JUNIN',
@NUM_DIREC           = '213',
@CALLE_SECUND        = 'PANAMA',
@SECTOR              = 'ROCAFUERTE',
@BARRIO              = 'CENTRO',
@TELEFONO1           = '2258447',
@CELULAR             = '0962740151',
@E_MAIL              = 'eljeansanpost@gmail.com',
@NOM_PROP            = 'SEGUNDO FORTUNATO',
@IDENT_PROP          = 'C',
@REPRES_LEGAL        = 'SEGUNDO FORTUNATO SANDOVAL TOMALA',
@IDENT_REPRES        = 'C',
@BCO_PAGADOR		 = '00017',
@TIPO_DE_CUENTA		 = '02',
@NUMERO_DE_CUENTA	 = '0035607345',
@TIP_IDENT_TIT_CTA	 = 'C',
@IDENT_TIT_CTA		 = '0903603348',
@TITULAR_CTA		 = '',
@BOT_CAL			 = 'S',
@OFAC_CAL			 = '000',
@BOT_POLITICAS		 = '',
@error				 = @msgerr out
select @rc, @msgerr


-- CODIGOBG	MID			TID
-- 256260	6000256260	BS000386

select * from nts_tarjcred..tc_comercios where co_ruc = '1790016714001'
select * from nts_tarjcred..tc_comercios where co_comercio = 256260

INSERT INTO BG_TComercios.Pagos.lp_comercios values(256262,'1790016714001','8062','BS256262','6000256262','BG JUAN PABLO JURIDICO','GYE','ECU','X',GETDATE(),GETDATE(),'zzJP','zzJP','1790016714001')
insert into BG_TComercios.Pagos.lp_limites values(256262,500)

--delete from BG_TComercios.Pagos.lp_limites where lt_comercio = 256262
--delete from BG_TComercios.Pagos.lp_comercios where co_comercio_syscard = 256262


-- SELECT * FROM NTS_TARJCRED..CLKRELA_DIARIO_TC

-- SI SALE ERROR SP_TVALIDACUENTAPERTENECECLIENTE

--insert into nts_tarjcred..CLKRELA_DIARIO_TC values ('U',CODIGOCLTECORE,CUENTAAH,'TIT','A',TIPOCUENTA)
--select * from nts_tarjcred..CLKRELA_DIARIO_TC

insert into nts_tarjcred..CLKRELA_DIARIO_TC values ('U','2135940','0035607345','TIT','A','AH')



declare @pp_comercio int = 256260
--------------------------------------------------------------------------------------
-- VALIDACION QUE EL COMERCIO ESTE CORRECTAMENTE CREADO PARA PRODUCTO LINK DE PAGOS --
--------------------------------------------------------------------------------------
-- VARIABLE CO_LINPAGOS TIENE QUE SER = S
select co_linkPagos,* from nts_tarjcred..tc_comercios with(nolock) where co_comercio = @pp_comercio

---------------------------------------
-- AGREGAMOS EL MID DE LINK DE PAGOS --
---------------------------------------
--insert into nts_tarjcred.dbo.tc_comercios_alternos values (1,@pp_comercio,'6000'+ convert(varchar(10),@pp_comercio))
select * from nts_tarjcred..tc_comercios_alternos with(nolock) where ca_comercio = @pp_comercio

-----------------------------------------
-- ACTUALIZACION DE COMISIONES CREDITO --
-----------------------------------------
update nts_tarjcred.dbo.tc_tcomerdiferi 
set cd_tasacomision = 3.00, cd_valorfijo = 0.10
where cd_tipodiferido = '0' and cd_marca in ('01','03','04') 
and cd_comercio = @pp_comercio

----------------------------------------
-- ACTUALIZACION DE COMISIONES DEBITO --
----------------------------------------
update nts_tarjcred.dbo.tc_tcomerdiferi 
set cd_tasacomision = 2.00,cd_valorfijo = 0.10
where cd_tipodiferido = '0' and cd_marca in ('06','05') 
and cd_comercio = @pp_comercio


update nts_tarjcred.dbo.tc_comercios 
set co_subtipocomercio = null, 
co_linkPagos = 'S',
co_codalterno = '6000' + CONVERT(varchar(10),co_comercio)
where co_comercio = @pp_comercio

-- ins

--delete from BG_TComercios.Pagos.lp_comercios where co_comercio_syscard = 255582
--delete from BG_TComercios.Pagos.lp_limites where lt_comercio = 255582





