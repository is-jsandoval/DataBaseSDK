--172.26.60.105,4433		
USE BG_NEO;


 
SELECT top 10

    p.numIdentificacion AS IDDD,
	tal.[IdPersona],
    tal.SegmentoEstrategico AS RRRR,
    COALESCE(u.idPersona, 'NO-DATA') AS idPersona,
    COALESCE(u.telefono, 'NO-DATA') AS telefono,
    COALESCE(u.correoElectrónico, 'NO-DATA') AS correoElectrónico,
    COALESCE(u.celular, 'NO-DATA') AS celular,
    COALESCE(CONVERT(VARCHAR,p.FechaIngresoCLTE,120), 'NO-DATA') AS FechaIngresoCLTE,
    COALESCE(pn.idcatnivelestudios, -1) AS idcatnivelestudios,
    COALESCE(pn.idCatDetNivelEstudios, -1) AS idCatDetNivelEstudios,
    COALESCE(pn.poseeRut, -1) AS poseeRut,
    COALESCE(pn.numTiempoResidenciaExtr, -1) AS numTiempoResidenciaExtr,
    COALESCE(pn.trabSectorPublico, 0) AS trabSectorPublico,
    COALESCE(pn.idCatActividadEconomica, -1) AS idCatActividadEconomica,
    COALESCE(pn.idCatDetActividadEconomica, -1) AS idCatDetActividadEconomica,
    COALESCE(pn.idCatSituacionLaboral, -1) AS idCatSituacionLaboral,
    COALESCE(pn.idCatDetSituacionLaboral, -1) AS idCatDetSituacionLaboral,
    COALESCE(pn.nombreEmpresa, 'NO-DATA') AS nombreEmpresa,
    COALESCE(pn.cargo, 'NO-DATA') AS cargo,
    COALESCE(pn.tiempoLaboraEmp, -1) AS tiempoLaboraEmp,
    COALESCE(pn.idCatOrigenIngresos, -1) AS idCatOrigenIngresos,
    COALESCE(pn.idCatDetOrigenIngresos, -1) AS idCatDetOrigenIngresos,
    COALESCE(pn.esEmpleado, 0) AS esEmpleado,
    COALESCE(pn.idCatTipoVivienda, -1) AS idCatTipoVivienda,
    COALESCE(pn.idCatDetTipoVivienda, -1) AS idCatDetTipoVivienda,
    COALESCE(pn.tiempoResidenciaActual, -1) AS tiempoResidenciaActual,
    COALESCE(pn.idCatCorrespondencia, -1) AS idCatCorrespondencia

FROM BG_PER_Personas p

INNER JOIN BG_PER_Ubicaciones u ON u.idPersona = p.idPersona

INNER JOIN BG_PER_PersonasNaturales pn ON pn.idPersonaNatural = p.idPersona

INNER JOIN [172.26.60.132].BG_DWHAPPS.PRECALIFICADA.BG_PROC_PERSONA_CALIFICADA tal ON tal.[IdPersona] = p.idPersona

WHERE 

    COALESCE(u.correoElectrónico, 'NO-DATA') <> 'NO-DATA'

    AND COALESCE(u.celular, 'NO-DATA') <> 'NO-DATA'

    AND COALESCE(pn.idCatNivelEstudios, -1) <> -1

    AND COALESCE(pn.idCatDetNivelEstudios, -1) <> -1

    AND COALESCE(pn.idCatSituacionLaboral, -1) <> -1

    AND COALESCE(pn.tiempoLaboraEmp, -1) <> -1

    AND COALESCE(pn.idCatTipoVivienda, -1) <> -1

-- AND p.numIdentificacion = '2200365704'

ORDER BY p.FechaIngresoCLTE DESC;

 
/*
select *FROM BG_PER_Personas


and p.numIdentificacion in (
'2200365704',
'0101347938',
'0101604023',
'0101792844',
'0101810307',
'0102601440',
'0102691953',
'0104183827',
'0104856265',
'0105728455',
'0106050297',
'0107021792',
'0150619930',
'0201574639',
'0401584891',
'0700379159',
'0701860694',
'0703393157',
'0703426130',
'0704418490',
'0704922962',
'0707055158',
'0907458525',
'0910178714',
'0911360055',
'0911785129',
'0912152980',
'0912597002',
'0914079066',
'0916194616',
'0916565633',
'0916708720',
'0916777030',
'0916863913',
'0916997240',
'0918167727',
'0918613167',
'0920532231',
'0921036455',
'0921825451',
'0922309026',
'0922539044',
'0923348536',
'0924062243',
'0924174758',
'0924383896',
'0924829708',
'0924849912',
'0925710766',
'0926058769',
'0926390329',
'0927745687',
'0928259415',
'0929013100',
'0929554525',
'0930383443',
'0930656061',
'0931673289',
'0950319517',
'0950332213',
'0953575669',
'0961117389',
'0962229613',
'0963479274',
'0963524137',
'0963637392',
'1003564547',
'1004227698',
'1101486379',
'1102213897',
'1103029177',
'1104476047',
'1104583107',
'1104893407',
'1202650998',
'1205549403',
'1206246983',
'1307995082',
'1308820115',
'1309290516',
'1309559811',
'1310116783',
'1311121337',
'1311727562',
'1312072505',
'1312261694',
'1312583600',
'1312910969',
'1312956723',
'1313153437',
'1703843373',
'1704213048',
'1704532827',
'1705019972',
'1705246120',
'1705391132',
'1706912761',
'1710404474',
'1710570894',
'1712394442',
'1713411203',
'1713504304',
'1713813077',
'1713989232',
'1714398615',
'1714605092',
'1714739172',
'1715745863',
'1716408594',
'1718021627',
'1719747550',
'1720938800',
'1721690855',
'1722121090',
'1722213871',
'1723499206',
'1724468838',
'1725345019',
'1726968678',
'1727325134',
'1728868231',
'1729719920',
'1756365662',
'1758205114',
'1758868457',
'1759057928',
'1760294650',
'1801972009',
'1803089166',
'1803650447'
)*/