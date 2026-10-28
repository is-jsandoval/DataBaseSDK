--172.26.61.61.4433

/****** consultar campaña ******/
Use [BG_AfiliacionComercios]
SELECT *  FROM [BG_AfiliacionComercios].[CREDITO_FACTURACION].[PERSONA_CAMPANA_MVP] order by fechaModificacion desc

/****** consultar campaña ******/
Use [BG_AfiliacionComercios]
x|

  SELECT TOP (1000) *
  FROM [BG_AfiliacionComercios].[CREDITO_FACTURACION].[PERSONA_CAMPANA_MVP] 
  
update [BG_AfiliacionComercios].[CREDITO_FACTURACION].[PERSONA_CAMPANA_MVP] set procesadoBG=1200000 where identificacion='0954121646'

update [BG_AfiliacionComercios].[CREDITO_FACTURACION].[PERSONA_CAMPANA_MVP] set finCampana='2027-01-27' where identificacion='1805309307'

SELECT TOP (1000) *
  FROM [BG_AfiliacionComercios].[CREDITO_FACTURACION].[PERSONA_CAMPANA_MVP] where identificacion='1805309307'


 --intentos de biometria
 SELECT *
  FROM [BG_AfiliacionComercios].[dbo].[IntentoBiometrico] where Cedula='1311726614'

  /****** insertar cliente campaña ******/
Use [BG_AfiliacionComercios]
insert into [BG_AfiliacionComercios].[CREDITO_FACTURACION].[PERSONA_CAMPANA_MVP]
Values
('1805309307','C','60000.00','1200000.00','1900000.00','600000.00','2025-12-18 15:01:33.100','TESTQA4','2025-12-18 15:01:33.100','TESTQA4','2026-02-05','2027-02-19')

--delete [BG_AfiliacionComercios].[CREDITO_FACTURACION].[PERSONA_CAMPANA_MVP] where identificacion='0951914316'


select distinct FormaPago from [BG_AfiliacionComercios].[CREDITO_FACTURACION].ABONO_CREDIPOS_2026

select * from [BG_AfiliacionComercios].[CREDITO_FACTURACION].ABONO_CREDIPOS_2026 where Identificacion='0903858967'

select * from [BG_AfiliacionComercios].[CREDITO_FACTURACION].ABONO_CREDIPOS_2026 where Identificacion='0903858967' and FechaMovimiento= '2026-05-27'
update [BG_AfiliacionComercios].[CREDITO_FACTURACION].ABONO_CREDIPOS_2026 set  FormaPago='CA' where Identificacion='0903858967' and FechaMovimiento= '2026-05-27'