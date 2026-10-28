select * from t_sokdodt, t_cltefil
where sokdodt_fecha_vcto >= 20230412 --BETWEEN 20230411 and 20230413
--and sokdodt_fecha_conc > 20180101
and clkey = sokdodt_cliente
and cltpid = 'R'
and sokdodt_estatus = ' '
and sokdodt_tipO not in ('VP','AX','XE')
and SOKDODT_TIPO = 'AD'-- son prestamos con tabla de amortizacion y tienen dividendos vencidos y por vencer
--and sokdodt_cliente ='2707469'
order by sokdodt_fecha_vcto asc

