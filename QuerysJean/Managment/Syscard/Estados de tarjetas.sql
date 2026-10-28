--172.26.61.204,4433

Select * from nts_Tarjcred..tc_Testadostarj
where es_estado = '7' --séptima posición de izquierda a derecha
and es_subestado = 'A' --número que va en la posición que indica el es_estado 1000000000
 
--TARJETAS DE DÉBITO
select cl_identifica,ta_tarjeta,ta_estados,* from nts_debito..db_tarjetas
inner join nts_clientes..cl_clientes on cl_cliente = ta_cliente
where ta_estados = '1000000000' --Activas
 
--TARJETAS DE CRÉDITO
select ta_nuevatarjeta,ta_tarjeta,ta_estados,* from nts_tarjcred..tc_tarjetas
inner join nts_clientes..cl_clientes on cl_cliente = ta_cliente
inner join nts_tarjcred..tc_maestro on ma_cuenta=ta_cuenta
where ta_estados = '0000000000' --Activas
and ta_nuevatarjeta is NULL
order by ta_fchapertura desc
 
select ms_disponiblerotativo,* from nts_tarjcred..tc_maestro_saldos
where ms_cuenta = 1868453
 
select ms_disponiblerotativo,ta_nuevatarjeta,ta_tarjeta,ta_estados,* from nts_tarjcred..tc_tarjetas
inner join nts_clientes..cl_clientes on cl_cliente = ta_cliente
inner join nts_tarjcred..tc_maestro_saldos on ms_cuenta = ta_cuenta
where ta_estados = '0000009000' --Bloqueadas
and ta_nuevatarjeta is NULL
order by ta_fchapertura desc
 
select ms_disponiblerotativo,ta_nuevatarjeta,ta_tarjeta,ta_estados,* from nts_tarjcred..tc_tarjetas
inner join nts_clientes..cl_clientes on cl_cliente = ta_cliente
inner join nts_tarjcred..tc_maestro_saldos on ms_cuenta = ta_cuenta
where ta_estados = '0000009000' --Bloqueadas
and ta_nuevatarjeta is NULL
AND CONVERT(DATE, ta_expira) <= CAST(GETDATE() AS DATE)
order by ta_fchapertura desc


--expirada
SELECT
    ms_disponiblerotativo,
    ta_nuevatarjeta,
    ta_tarjeta,
    ta_estados,
    *
FROM nts_tarjcred..tc_tarjetas
INNER JOIN nts_clientes..cl_clientes 
    ON cl_cliente = ta_cliente
INNER JOIN nts_tarjcred..tc_maestro_saldos 
    ON ms_cuenta = ta_cuenta
WHERE ta_estados = '0000009000' -- Bloqueadas
  AND ta_nuevatarjeta IS NULL
  AND EOMONTH(
        CONVERT(DATE, 
            '20' + RIGHT(ta_expira, 2) + '-' + LEFT(ta_expira, 2) + '-01'
        )
      ) < CAST(GETDATE() AS DATE)
ORDER BY ta_fchapertura DESC