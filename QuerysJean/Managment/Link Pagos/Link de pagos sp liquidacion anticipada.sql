USE [nts_tarjcred]
GO
/****** Object:  StoredProcedure [dbo].[sp_bprocesa_linkpagos]    Script Date: 01/07/2025 11:08:37 a.m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER procedure [dbo].[sp_bprocesa_linkpagos]  
(  
 @pp_empresa    smallint,  
 @pp_tipoeje    char,  
 @pp_batchid    integer,  
 @pp_procesoid  integer,  
 @pp_usuario    usuario,  
 @pp_seculotpr  smallint,  
 @pp_estacion   varchar(20),  
 @pp_fecha      datetime,  
 @pp_secuc      integer  
)  
as  
  
declare 
    @o_msgerr           varchar(256),
    @msg                varchar(256),
    @io_lote            int,
    @io_secuencia       int,
    @io_recap           varchar(15) = null,
    @rc                 int,
    @ta_fchproceso      datetime,
    @ta_comercio        int,
    @ta_monedad         varchar(8),
    @ta_tarjeta         varchar(20),
    @ta_fchsis          datetime,
    @ta_autoriza        varchar(6),
    @ta_tipodiferido    varchar(3),
    @tipotrx            catalogo,
    @trxrazon           catalogo = '0',
    @ta_plazodiferido   int,
    @ta_origentrn       catalogo,
    @ta_refnumber       varchar(12),
    @ta_montod          money,
    @leidos             int = 0,
    @noprocesados       int = 0,
    @procesados         int = 0,
    @hora_hasta         varchar(12),
    @loteexterno        varchar(7),
    @w_loteexterno      varchar(7),
    @w_ta_comercio      int,
    @ta_marca           varchar(8),
    @w_ta_marca         varchar(8),
    @w_existe           int,
    @fecha_hasta        datetime,
    @w_marca            varchar(8),
    @fecha_proceso      datetime,
    @co_categoria       varchar(8),
    @fecha_desde        datetime,
    @impuesto           money = 0,
    @subtotal           money = 0
   
set nocount on    
    
select 'inicio del proceso :' + convert(varchar,getdate(), 109) 

declare @pp_fechaant datetime = @pp_fecha -- Fecha de proceso actual  
exec nts_tarjcred.dbo.sp_bprxdialaboral 1, @pp_fechaant output, 0 ,-1

    
--------------------------------  
-- OBTENEMOS FECHA DE PROCESO --  
--------------------------------  
select @fecha_proceso = em_fchproceso 
from nts_tarjcred..tc_empresa with(nolock) 
where em_empresa = @pp_empresa  
  
--------------------------
-- OBTIENE BINES DEBITO --
--------------------------
select ob_host from nts_tarjcred.dbo.tc_bines with(nolock) where ob_host = 1

----------------------------------------------------------------------------  
-- OBTENEMOS RANGO DE HORAS PARA EL OBTENER LAS AUTORIZACIONES A PROCESAR --  
----------------------------------------------------------------------------  
select @hora_hasta = pg_valor1  
from nts_monitran.dbo.mt_param_general with(nolock)  
where pg_id = 'RHLINKPAGOS' and pg_empresa = @pp_empresa  
    if @@rowcount = 0 or @@error <> 0    
        begin    
            select @msg = 'ERROR, No existe parametro general rango de horas RHLINKPAGOS'    
            goto FIN    
        end   
  
if ISDATE(@hora_hasta) = 0  
    begin
        select @msg = 'ERROR, El valor del parametro general no es tipo time'
        goto FIN
    end  
  
select @fecha_desde = convert(datetime,convert(varchar,@pp_fechaant,110) + ' 00:00:00.000')  
select @fecha_hasta = convert(datetime,convert(varchar,@pp_fecha,110) + ' ' + @hora_hasta)  
  
---------------------------------------------------------  
-- OBTIENE COMERCIOS PARAMETRIZADOS COMO LINK DE PAGOS --  
---------------------------------------------------------  
select   
co_comercio, co_categoria  
into #comercios  
from nts_tarjcred..tc_comercios  with(nolock)  
where isnull(co_linkPagos,'N') = 'S'  
  
print '@fecha_desde: ' + convert(varchar(100),@fecha_desde)  
print '@fecha_hasta: ' + convert(varchar(100),@fecha_hasta)  

  
select   
a.ta_comercio,   a.ta_fchproceso, a.ta_monedad, a.ta_tarjeta,  
a.ta_fchsis,   a.ta_autoriza,  a.ta_montod, a.ta_tipodiferido,  
a.ta_plazodiferido,  a.ta_refnumber,  a.ta_marca,  c.co_categoria, a.ta_identifica , ta_iva,ta_id, ta_estado
into #autorizaciones_temp   
from nts_tarjcred..tc_atrnautoriza a with(nolock)  
inner join #comercios c with(nolock) on c.co_comercio = a.ta_comercio  
where a.ta_empresa   = @pp_empresa  
 and a.ta_estado    is null  
 and a.ta_codresp   = '00'  
 and left(a.ta_tipomsg , 2) in ('01','02')  
 and isnull(a.ta_tipotran,'00') in ('00','05')  
 and a.ta_fchsis    between @fecha_desde and @fecha_hasta  
 and a.ta_cuenta    is not null  
  
--------------------------------------  
-- ADQUIRENCIAS PARA BOTON DE PAGOS --  
--------------------------------------  
insert into #autorizaciones_temp  
select   
a.ta_comercio,   a.ta_fchproceso, a.ta_monedad, a.ta_tarjeta,      
a.ta_fchsis,   a.ta_autoriza,  a.ta_montod, a.ta_tipodiferido,      
a.ta_plazodiferido,  a.ta_refnumber,  a.ta_marca,  c.co_categoria, a.ta_identifica  , ta_iva,ta_id, ta_estado
from nts_tarjcred..tc_atrnautoriza a with(nolock)      
inner join #comercios c with(nolock) on c.co_comercio = a.ta_comercio      
where      
     a.ta_estado    is null      
 and a.ta_codresp   = '00'      
 and left(a.ta_tipomsg , 2) in ('01','02')      
 and isnull(a.ta_tipotran,'00')   in ('00','05')      
 and a.ta_fchsis    between @fecha_desde and @fecha_hasta  
 and isnull(a.ta_cuenta,0) = 0
   
--------------------------------------------------------------------------------------------------  
-- DE LAS AUTORIZACION ENCONTRADAS TOMAMOS SOLO LAS QUE FUERON REALIZADAS POR EL BOTON DE PAGOS --  
--------------------------------------------------------------------------------------------------  
select *   
into #autorizaciones   
from #autorizaciones_temp   
where left(replace(ta_identifica,'DFAS-',''),2) = 'BS'

-------------------------------------
-- ACEPTAR TRANSACCIONES DE DEBITO --
-------------------------------------
insert into #autorizaciones
select * from #autorizaciones_temp where isnull(ta_identifica,'') = '' and left(ta_tarjeta,6) in (select ob_host from #binesdebito)

-----------------------------------------------------
-- ELIMIAR TRANSACCIONES UTILIZADAS Y/O REVERSADAS --
-----------------------------------------------------
delete #autorizaciones 
from #autorizaciones
inner join nts_tarjcred.dbo.tc_atrnautorizalog on la_id = ta_id
and la_estado = 'U'
if @@ROWCOUNT > 0
    begin
        print 'ELIMINANDO autorizaciones ya compensadas: ' + convert(varchar(100),@@rowcount)
    end

declare c_autorizaciones cursor for  
  
select  
ta_comercio, ta_fchproceso, ta_monedad,ta_tarjeta,  
ta_fchsis, ta_autoriza, ta_montod, ta_tipodiferido,  
ta_plazodiferido, ta_refnumber, ta_marca, co_categoria, ta_iva  
from #autorizaciones  
order by ta_comercio, ta_marca, ta_fchsis  
  
open c_autorizaciones  
  
while 1 = 1  
 begin  
  fetch c_autorizaciones into @ta_comercio, @ta_fchproceso, @ta_monedad,@ta_tarjeta,  
         @ta_fchsis, @ta_autoriza, @ta_montod, @ta_tipodiferido,  
         @ta_plazodiferido, @ta_refnumber, @ta_marca, @co_categoria, @impuesto  
  if @@FETCH_STATUS != 0  
   break  
  
  select @leidos = @leidos + 1  
    
  select @loteexterno = right(convert(varchar,@ta_fchsis,112),7)  
  
  If (@w_ta_comercio IS NULL) OR (@w_ta_comercio != @ta_comercio) OR  
  (@w_loteexterno IS NULL) OR (@w_loteexterno != @loteexterno)  
   Begin  
    set @w_ta_comercio  = @ta_comercio  
    set @w_loteexterno = @loteexterno  
    set @w_ta_marca     = @ta_marca  
    set @io_recap  = NULL  
    set @io_lote  = NULL  
    set @io_secuencia = 0  
   End  
  Else  
   Begin  
    if (@w_ta_marca IS NULL) OR (@w_ta_marca != @ta_marca)  
     begin  
      set @io_lote  = NULL  
      set @io_recap  = NULL  
      set @io_secuencia = 0  
      set @w_ta_marca     = @ta_marca  
     end  
   End  
  
  if not @io_lote is null  
   begin  
    set @w_existe = null  
  
    select  @w_existe = 1  
    from nts_tarjcred..tc_tcabtrx with(nolock)  
    where ct_lote = @io_lote and ct_empresa = @pp_empresa  
  
    if isnull(@w_existe,0) <> 1   
     begin  
      set @io_lote      = NULL  
      set @io_recap  = NULL  
      set @io_secuencia = 0  
     end  
   end  
    
  select @o_msgerr  = null,  
    @tipotrx  = null,  
    @trxrazon  = '0'  
  
  select  @tipotrx = CASE WHEN @ta_tipodiferido=0 and (@co_categoria = 5541 or @co_categoria = 5542)  
           THEN '204' end   
  
  if @tipotrx = '204'   
   select @trxrazon = '00'  
     
  if @tipotrx is null  
    select @tipotrx = CASE @ta_tipodiferido   
         WHEN 0 THEN '202'  
          else '203'  
         end  
    
  -----------------------------------------------------------------------------------  
  --Para Reenganche, si existe ya en la tabla liquidadepositados, tomamos el recap --  
  -----------------------------------------------------------------------------------  
  
  if @io_recap is null  
   begin  
    select @w_marca = dc_cod   
    From nts_monitran..vw_catalogo with(nolock)  
    where ca_nombre = 'TiposBines' and dc_valor2 = @ta_marca  
  
    select @io_recap = rd_recap,  
      @io_lote = rd_lote  
    from nts_tarjcred..tc_cliquidadepositados with(nolock)  
    where rd_empresa  = @pp_empresa  
     and rd_marca  = @w_marca  
     and rd_comercio  = @ta_comercio       
     and rd_fchproceso = @fecha_proceso  
   end  
  -----------------------------------------------------------------------------------  
  
  begin tran  
  
  begin try  
     
   
   set @impuesto = isnull(@impuesto,0)
   set @subtotal = @ta_montod - isnull(@impuesto,0)
  
   EXEC @rc = sp_tinserta_trx  
        @i_user					= 'zzSystemsLP'    ,  -- Usuario que realiza la transaccion.  
        @i_empresa				= @pp_empresa    ,  -- Empresa que realiza la transaccion.  
        @i_msgerr				= @o_msgerr OUTPUT   ,  -- Mensaje descriptivo del error.  
        @i_lote					= @io_lote OUTPUT   ,       -- Lote con que se reqistra la transaccion (INPUT/OUTPUT).  
        @i_secuencia			= @io_secuencia OUTPUT  ,  -- Secuencia con que se reqistra la transaccion (INPUT/OUTPUT).  
        @i_comercio				= @ta_comercio    ,       -- Codigo de comercio, requerido para trans. de compras.  
        @i_recap				= @io_recap OUTPUT   ,       -- Numero de Recap, requerido para trans. de compras.  
        @i_fchrecap				= @ta_fchproceso   ,       -- Fecha de Recap, requerido para trans. de compras.  
        @i_moneda				= @ta_monedad    ,     -- Codigo de moneda, por defecto esta 840=Dolar.  
        @i_tarjeta				= @ta_tarjeta    ,       -- Numero de tarjeta que realiza la transaccion.  
        @i_fchtrx				= @ta_fchsis    ,       -- Fecha de transaccion, si no viene toma la del sistema.  
        @i_autorizacion			= @ta_autoriza    ,       -- Numero de autorizacion, requerido para trans. de compras.  
        @i_subtotal				= @subtotal     ,  -- Subtotal de la transaccion.  
        @i_impuesto				= @impuesto     ,       -- Impuesto de la transaccion de compras.  
        @i_servicio				= 0       ,       -- Servicio de la transaccion de compras.   
        @i_propina				= 0       ,       -- Propina de la transaccion de compras.  
        @i_total				= @ta_montod    ,     -- Total de la transaccion.  
        @i_tipotrx				= @tipotrx     ,       -- Tipo de transaccion (202=compras,402=N/C,502=N/D,302=Pagos...).  
        @i_trxrazon				= @trxrazon     ,       -- Razon o motivo de transaccion (regularmente para n/d, n/c).  
        @i_tipodiferido			= @ta_tipodiferido   ,       -- Tipo del diferido para trans. de compras.  
        @i_mesesdiferido		= @ta_plazodiferido   ,       -- Meses para el diferido.  
        @i_origentrxs			= 'LP'      ,       -- Origen de la transaccion: 01=ingreso por SysCards,02=Incoming,03=Internet...07=Banco.  
        @i_loteexterno			= @loteexterno    ,       -- Lote externo (datafast, otros)  
        @i_preliminar			= 'N'      ,  
        @i_referencia			= @ta_refnumber    ,  
        @i_base_imponible		= 0       ,       -- fcanar 2012-05-08   
        @i_base_imponible_cero	= 0         -- fcanar 2012-05-08      
    if @rc != 0   
     begin  
      Print isnull(@o_msgerr,'Error en el proceso sp_tinserta_trx')  
      rollback tran  
      select @noprocesados = @noprocesados + 1  
     end  
    else  
     begin  
      commit tran  
      select @procesados = @procesados + 1  
     end  
  end try  
  begin catch  
   rollback tran  
   select @msg = ERROR_MESSAGE ()  
   Print 'Error al procesar: ' + @msg  
   select @msg = null     
   select @noprocesados = @noprocesados + 1  
  end catch  
 end  
close c_autorizaciones  
deallocate c_autorizaciones  
  
FIN:        
 drop table #comercios  
 drop table #autorizaciones  
 select 'fin del proceso   :' + convert(varchar,getdate(),109)       
 select 'Registros Leidos  : ' + convert(Varchar,isnull(@leidos,0))  
 select 'Registros No Procesados : ' + convert(Varchar,isnull(@noprocesados,0))  
 select 'Registros Procesados : ' + convert(Varchar,isnull(@procesados,0))       
 if @msg is not null        
 begin        
  select @msg          
  return -1        
 end      
  
  
return 0  