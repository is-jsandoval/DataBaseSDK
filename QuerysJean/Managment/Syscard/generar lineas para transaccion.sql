use nts_tarjcred
go

declare @tarjeta varchar(16), @comercio int, @fecha_1 datetime, @fecha_2 datetime, @terminal varchar(8)
declare @codalterno varchar(12),@contador int,@loteexterno varchar(7),@ruc varchar(13),@id int
------------------------------------LLENAR DATOS AQUI---------------------------------------------------------
select 
@comercio	= 43  --comercio asignado al POS
,@terminal	='NP123123' --right(la_identifica,8) --terminal asignado al banco
,@codalterno = '1791310199'
--------------------------------------------------------------------------------------------------------------

select @loteexterno=right(convert(varchar,FLOOR(RAND()*(9999999-1)+1))+'0000000',7)


select @contador=1--@@ROWCOUNT
select @ruc=co_ruc from nts_tarjcred..tc_comercios where co_comercio=@comercio


select '1'+
left(@codalterno+'000000000000',12)+
CONVERT(varchar,getdate(),12)+
@loteexterno+--numero random loteexterno
@terminal+
replicate(' ',467)

union all


select '2'+
convert(char(19),la_tarjeta)+
la_cproceso+
CONVERT(varchar,la_fchsis,12)+
right('00'+convert(varchar,DATEPART(HOUR,la_fchsis)),2)+ right('00'+CONVERT(varchar, DATEPART(MINUTE,la_fchsis)),2)+ right('00'+convert(varchar,DATEPART(DAY,la_fchsis)),2)+
la_origentrn+
la_autoriza+
right('0000000000000'+replace(convert(varchar,la_montod),'.',''),13)+
'1'+--online o offline pendiente
right('00'+la_tipodiferido,2)+
right('00'+isnull(la_plazodiferido,'00'),2)+
la_c22_pos_entry+
la_monedad+
'0000000000000'+
right('0000000000000'+replace(convert(varchar,la_comisionavance),'.',''),13)+
'0000000000000'+
'0000000000000'+
'0000000000000'+
'00'+
right('000'+convert(varchar,la_puntosutilizados),3)+
'0000000000000'+
'0000000000000'+
'0000000000000'+
'0000000000000'+
'0000000000000'+
'0000000000000'+
'000'+--campo23
REPLICATE(' ',255)+
' '+
'  '+
--replicate(' ',16)+
left(isnull(la_preauthID,'')+replicate(' ',15),15)+
' '+
'.'


from tc_atrnautorizalog
where la_id in (select top(10) la_id from nts_tarjcred.dbo.tc_atrnautorizalog where la_comercio=@ruc and la_codresp='00' order by la_fchsis desc )

UNION ALL

select '3'+
left(@codalterno+'000000000000',12)+
CONVERT(varchar,getdate(),12)+
@loteexterno+--numero random loteexterno
right(replicate('0',6)+convert(varchar,@contador),6)+
replicate(' ',469)

union all

select '9'+
CONVERT(varchar,getdate(),12)+
right(replicate('0',6)+convert(varchar,@contador+2),6)+
@ruc+
'BANCO DE GUAYAQUIL'+
replicate(' ',457)




