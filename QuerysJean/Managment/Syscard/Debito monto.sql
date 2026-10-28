--172.26.60.204

declare @DisponibleBCO money = 0, @WKMensajeHOST varchar(3000) = null

exec MASTER.dbo.spcs_nota_debito_credito 
@opcion			= '1',    --> 1 - Agregar     3 - Eliminar ND/NC.  
@preliminar		= 'D' ,    --> D - Definitiva    P - Preliminar (CONSULTA). 
@tipo			= 'NC',  --> NC - NotaCredito   ND - Nota Debito -- DP - Depositos indicar 
@motivo			= 'MDE', --> RC - Reverso Deposito  RD - Reverso Avance Debito. Rol Electronico envia el codigo de la EmpresaRol  
@tipoCuenta		= 'A',  --> A - Ahorro     C - Corriente  
@numeroCuenta	= '1261823', --> Numero de cuenta   
@monto			= 10000,   --> Monto   
@disponible		= @DisponibleBCO output, --> Disponible  
@fecha			= '2026-04-29',   --> Fecha   
@msgOut			= @WKMensajeHOST output  --> mensaje
select @DisponibleBCO,@WKMensajeHOST
 