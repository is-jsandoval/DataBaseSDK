--172.26.60.105,4433
--usr_bizagi

---------------------------------------------------------------Para editar correo
MERGE BG_NEO.dbo.bg_per_ubicaciones AS target
USING (
    SELECT 
        per.idpersona,
        37 AS idCatTipopersona,
        509 AS idCatDetTipoPersona,
        2832 AS idcatdettipodireccion,
        2241 AS idcatdetpais,
        18 AS idcatdetprovincia,
        'BRYAN.CEDEO@GMAIL.COM' AS correoElectrónico,--cambiar correo
        '0962740151' AS celular--cambiar celular
    FROM BG_NEO.dbo.bg_per_personas per
    WHERE per.numidentificacion in --cedulas ingresar
    ('0903858967') 
) AS source
ON target.idpersona = source.idpersona
   AND target.idcatdettipodireccion = source.idcatdettipodireccion

WHEN MATCHED THEN
    UPDATE SET
        target.correoElectrónico = source.correoElectrónico,
        target.celular = source.celular,
        target.idcatdetpais = source.idcatdetpais,
        target.idcatdetprovincia = source.idcatdetprovincia

WHEN NOT MATCHED THEN
    INSERT (
        idpersona,
        idCatTipopersona,
        idCatDetTipoPersona,
        idcatdettipodireccion,
        idcatdetpais,
        idcatdetprovincia,
        correoElectrónico,
        celular
    )
    VALUES (
        source.idpersona,
        source.idCatTipopersona,
        source.idCatDetTipoPersona,
        source.idcatdettipodireccion,
        source.idcatdetpais,
        source.idcatdetprovincia,
        source.correoElectrónico,
        source.celular
    );--------aqui termina



    --select comprobar
    SELECT 
    per.idpersona,
    per.numidentificacion,
    ub.idcatdettipodireccion,
    ub.correoElectrónico,
    ub.celular,
    ub.idcatdetpais,
    ub.idcatdetprovincia
FROM BG_NEO.dbo.bg_per_personas per
LEFT JOIN BG_NEO.dbo.bg_per_ubicaciones ub
    ON ub.idpersona = per.idpersona
    AND ub.idcatdettipodireccion = 2832
WHERE per.numidentificacion IN (
    '0903858967'
);