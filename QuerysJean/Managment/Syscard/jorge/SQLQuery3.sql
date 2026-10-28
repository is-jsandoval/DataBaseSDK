INSERT INTO [BG_TComercios].[Pagos].[lp_parametrizacion3DS]
       ([tds_marca]
       ,[tds_LocalInternacional]
       ,[tds_Listabines]
       ,[tds_giro]
       ,[tds_monto]
       ,[tds_estado]
       ,[tds_usuarioactualiza]
       ,[tds_fchactualiza])
VALUES (null,            -- Marca de la tarjeta
        null,   -- Local o Internacional
        null,   -- Lista de BINES separados por coma
        '4545',          -- Giro del comercio
        150.00,            -- Monto mínimo/máximo según regla
        'A',          -- Estado de la regla
        'TEST_QA',      -- Usuario que actualiza
        GETDATE());        -- Fecha de actualización

select * from [BG_TComercios].[Pagos].[lp_parametrizacion3DS]