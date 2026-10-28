--172.26.61.204
--monitran
--monitran


SELECT *  FROM tcom_LinkPagos.dbo.[lp_links] order by ln_fecha desc


SELECT*
    FROM BG_TComercios.Pagos.lp_links
    WHERE ln_ruc = '1204116998001'
	and ln_motivo like'%Compra%'
    ORDER BY ln_fecha DESC