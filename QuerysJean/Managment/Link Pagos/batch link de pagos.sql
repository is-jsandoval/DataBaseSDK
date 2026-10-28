use BG_TComercios
go

--drop table Bg_TComercios.Pagos.lp_logs_batch

select * from Bg_TComercios.Pagos.lp_logs_batch

create table Bg_TComercios.Pagos.lp_logs_batch
(
	lb_secuencial int identity(1,1),
	lb_fchini		datetime,
	lb_fchfin		datetime,
	lb_fchproceso	date,
	lb_estado		varchar(1),
	lb_proceso		varchar(50),
	lb_texto		NVARCHAR(MAX)
)


declare @ct_texto NVARCHAR(MAX) = null,
@lb_fchini		datetime = '',
@lb_fchfin		datetime = '',
@lb_fchproceso	date = '',
@lb_estado		varchar(1) = 'T',
@lb_proceso		varchar(50) = 'sp_test_dv',
@lb_texto		NVARCHAR(MAX) = NULL,
@enter			nvarchar(100) =  CHAR(13) + CHAR(10)

set @ct_texto = 'PARAMETROS:' + @enter

set @ct_texto = @ct_texto + '@lb_fchini		Tipo = datetime		valor = ' + CONVERT(VARCHAR(100),@lb_fchini) + @enter
set @ct_texto = @ct_texto + '@lb_fchfin		Tipo = datetime		valor = ' + CONVERT(VARCHAR(100),@lb_fchfin) + @enter
set @ct_texto = @ct_texto + '@lb_fchproceso	Tipo = date			valor = ' + CONVERT(VARCHAR(100),@lb_fchproceso) + @enter
set @ct_texto = @ct_texto + '@lb_estado		Tipo = varchar(1)	valor = ' + CONVERT(VARCHAR(100),@lb_estado) + @enter
set @ct_texto = @ct_texto + '@lb_proceso	Tipo = varchar(50)	valor = ' + CONVERT(VARCHAR(100),@lb_proceso) + @enter
set @ct_texto = @ct_texto + '@lb_texto		Tipo = text			valor = ' + CONVERT(VARCHAR(100),@ct_texto) + @enter

set @ct_texto = @ct_texto + 'INICIO PROCESO' + @enter
set @ct_texto = @ct_texto + 'HIZO ALGO' + @enter
set @ct_texto = @ct_texto + 'FIN PROCESO' + @enter


SET @lb_texto = @ct_texto


exec BG_TComercios.Pagos.sp_registra_logs 
@lb_fchini = @lb_fchini, 
@lb_fchfin = @lb_fchfin, 
@lb_fchproceso = @lb_fchproceso, 
@lb_estado = @lb_estado,
@lb_proceso = @lb_proceso, 
@lb_texto = @lb_texto

SELECT * FROM Bg_TComercios.Pagos.lp_logs_batch

---

begin tran

select top 10 ln_audit_number,ln_estado,ln_fecha_actualizacion,ln_fecha,ln_motivo from pagos.lp_links where ln_audit_number='491129'
select top 10 lg_audit_number,lg_estado,lg_fecha_actualizacion,lg_fecha,lg_motivo from pagos.lp_links_logs where lg_audit_number='491129'
 
select top 10 ln_audit_number,ln_estado,ln_fecha_actualizacion,ln_fecha,ln_motivo from pagos.lp_links where ln_audit_number='243749'
select top 10 lg_audit_number,lg_estado,lg_fecha_actualizacion,lg_fecha,lg_motivo from pagos.lp_links_logs where lg_audit_number='243749'

select top 10 ln_audit_number,ln_estado,ln_fecha_actualizacion,ln_fecha,ln_motivo from pagos.lp_links where ln_audit_number='799325'
select top 10 lg_audit_number,lg_estado,lg_fecha_actualizacion,lg_fecha,lg_motivo from pagos.lp_links_logs where lg_audit_number='799325'


exec  sp_helptext sp_batch_actualizaLinks @pp_fchproceso = '2026-02-06'


select top 10 ln_audit_number,ln_estado,ln_fecha_actualizacion,ln_fecha,ln_motivo from pagos.lp_links where ln_audit_number='491129'
select top 10 lg_audit_number,lg_estado,lg_fecha_actualizacion,lg_fecha,lg_motivo from pagos.lp_links_logs where lg_audit_number='491129'
 
select top 10 ln_audit_number,ln_estado,ln_fecha_actualizacion,ln_fecha,ln_motivo from pagos.lp_links where ln_audit_number='243749'
select top 10 lg_audit_number,lg_estado,lg_fecha_actualizacion,lg_fecha,lg_motivo from pagos.lp_links_logs where lg_audit_number='243749'

select top 10 ln_audit_number,ln_estado,ln_fecha_actualizacion,ln_fecha,ln_motivo from pagos.lp_links where ln_audit_number='799325'
select top 10 lg_audit_number,lg_estado,lg_fecha_actualizacion,lg_fecha,lg_motivo from pagos.lp_links_logs where lg_audit_number='799325'

select * from Bg_TComercios.Pagos.lp_logs_batch

rollback tran



