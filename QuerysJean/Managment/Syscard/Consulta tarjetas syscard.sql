--172.26.61.204,4433



SELECT TOP (10000) *
  FROM [nts_tarjcred].[dbo].[sc_tarjeta] ta
  left join [nts_tarjcred].[dbo].[sc_tc_maestro] ma on ta.ta_cuenta=ma.ma_cuenta


  SELECT TOP (10000) *
  FROM [nts_tarjcred].[dbo].[sc_tarjeta] ta
  left join [nts_tarjcred].[dbo].tc_maestro ma on ta.ta_cuenta=ma.ma_cuenta and ta_Tarjeta='5144400334002001'


  select * from nts_tarjcred..tc_transacciones_hist where th_comercio='259396'