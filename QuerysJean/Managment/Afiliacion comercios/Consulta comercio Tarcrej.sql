	--172.26.61.204:4433

	select top(100) * from nts_tarjcred.dbo.tc_comercios
	use nts_tarjcred
 
	go
 
	declare @pp_comercio int = 43, @pp_empresa int = 1
 
	select * from nts_tarjcred.dbo.tc_comercios with(nolock) where co_comercio = 43
	select * from nts_tarjcred.dbo.tc_comercio_empresa with(nolock) where ce_comercio = 43 and ce_empresa = @pp_empresa
	select * from nts_tarjcred.dbo.tc_comercios_alternos with(nolock) where ca_comercio = 43
	select * from nts_tarjcred.dbo.tc_cafiliaciones with(nolock) where ca_comercio = 43
	select * from nts_tarjcred.dbo.tc_comercio_marca with(nolock) where cm_comercio = 43
	select * from nts_tarjcred.dbo.tc_tcomerdiferi with(nolock) where cd_comercio = 43
 


	  /*
	select top(100) * from nts_tarjcred.dbo.tc_comercios order by co_fchapertura desc 
	select top(100)* from nts_tarjcred.dbo.tc_comercio_empresa order by ce_ultfchactualiza 
	select top(100)* from nts_tarjcred.dbo.tc_comercios_alternos order by 
	select top(100)* from nts_tarjcred.dbo.tc_cafiliaciones order by 
	select top(100)* from nts_tarjcred.dbo.tc_comercio_marca order by
	select top(100)* from nts_tarjcred.dbo.tc_tcomerdiferi order by*/

	SELECT * FROM BG_TComercios.Pagos.lp_comercios  order by co_fch_modificacion desc

	select * from nts_tarjcred.dbo.tc_comercios where co_estado='A' and co_descripcion like '%ltda%'
	select * from nts_tarjcred.dbo.tc_comercios where co_estado='A' and co_tipoident= 'C'
	select * from nts_tarjcred.dbo.tc_comercios where co_ruc='0923493654001'

	select distinct (co_observacion) from nts_tarjcred.dbo.tc_comercios 
	------------------------------------------------------------------------------------
	SELECT *
	FROM (
		SELECT TOP(10000)
			co_ruc AS Lruc,
			COUNT(*) AS cantidad,
			MAX(co_fchapertura) AS ultima_fecha_apertura
		FROM nts_tarjcred.dbo.tc_comercios 

		WHERE co_ruc NOT IN (
			SELECT co_ruc
			FROM BG_TComercios.Pagos.lp_comercios
			WHERE co_estado = 'A'

		)
	
		and d.co_ruc like '09%'
		-- AND co_ruc = '0190400336001'
		GROUP BY co_ruc
		ORDER BY ultima_fecha_apertura DESC
	) AS t
	WHERE cantidad = 1;

	----------------------------------------------------------

	SELECT t.r_ruc,
       t.num,
       t.ultima_fecha_apertura
FROM (
    SELECT
        d.co_ruc AS r_ruc,
        COUNT(*) AS num,
        MAX(d.co_fchapertura) AS ultima_fecha_apertura
    FROM nts_tarjcred..tc_comercios AS d
    INNER JOIN nts_tarjcred..tc_comercio_empresa  AS ce ON d.co_comercio = ce_comercio
    INNER JOIN nts_tarjcred..tc_comercios_alternos AS b ON b.ca_comercio = d.co_comercio
    WHERE ISNULL(d.co_linkPagos,'N') = 'N'
      AND d.co_estado = 'A'
	  and d.co_ruc like '09%'
      AND NOT EXISTS (
          SELECT 1
          FROM BG_TComercios.Pagos.lp_comercios c
          WHERE c.co_estado = 'A'
            AND c.co_ruc = d.co_ruc
      )
    GROUP BY d.co_ruc
) AS t
WHERE t.num = 1
ORDER BY t.ultima_fecha_apertura DESC;   -- <— orden final válido
