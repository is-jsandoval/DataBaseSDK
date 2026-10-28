--172.26.61.204

SELECT  * FROM BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG where NUMERO_RUC='0954121646001'--no esta en tabla contribuyentes
SELECT  * FROM BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG where CODIGO_CIIU='C182003'
Select * from BG_TComercios.Pagos.BG_TB_CatastroGrandesContribuyentes where RUC='1721719654001' --no esta en tabla contribuyentes

EXEC BG_TComercios.[Pagos].[SP_ObtenerContribuyentePorRUC] @pRUC = '0190151700001';

update  BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG where NUMERO_RUC='0954121646001'

------------------------------------------------------------------------------
SELECT  * FROM BG_TComercios.Pagos.SRI_ESTABLECIMIENTOS_BG where NUMERO_RUC='1722587977001'-- si  esta en tabla contribuyentes
Select * from BG_TComercios.Pagos.BG_TB_CatastroGrandesContribuyentes where RUC='1722587977001' --si esta en tabla contribuyentes


EXEC BG_TComercios.[Pagos].[SP_ObtenerContribuyentePorRUC] @pRUC = '1722587977001';





--insert into BG_TComercios.Pagos.BG_TB_CatastroGrandesContribuyentes (RUC)values ('0190151700001')