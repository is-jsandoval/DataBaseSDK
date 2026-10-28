--172.26.60.105,4433

USE BG_NEO
GO
SELECT p.numIdentificacion, COALESCE(u.idPersona, 'NO-DATA') as idPersona , COALESCE(u.telefono, 'NO-DATA') as telefono
							, COALESCE(u.correoElectrónico, 'NO-DATA') as correoElectrónico, COALESCE(u.celular, 'NO-DATA') as celular
							, COALESCE(p.FechaIngresoCLTE, 'NO-DATA') as FechaIngresoCLTE
							, COALESCE(pn.idcatnivelestudios, -1) as idcatnivelestudios, COALESCE(pn.idCatDetNivelEstudios, -1) as idCatDetNivelEstudios
							, COALESCE(pn.poseeRut, -1) as poseeRut , COALESCE(pn.numTiempoResidenciaExtr, -1) as numTiempoResidenciaExtr
							, COALESCE(pn.trabSectorPublico, 0) as trabSectorPublico , COALESCE(pn.idCatActividadEconomica, -1) as idCatActividadEconomica
							, COALESCE(pn.idCatDetActividadEconomica, -1) as idCatDetActividadEconomica, COALESCE(pn.idCatSituacionLaboral, -1) as idCatSituacionLaboral
							, COALESCE(pn.idCatDetSituacionLaboral, -1) as idCatDetSituacionLaboral , COALESCE(pn.nombreEmpresa, 'NO-DATA') as nombreEmpresa
							, COALESCE(pn.cargo, 'NO-DATA') as cargo, COALESCE(pn.tiempoLaboraEmp, -1) as tiempoLaboraEmp
							, COALESCE(pn.idCatOrigenIngresos, -1) as idCatOrigenIngresos, COALESCE(pn.idCatDetOrigenIngresos, -1) as idCatDetOrigenIngresos
							, COALESCE(pn.esEmpleado, 0) as esEmpleado, COALESCE(pn.idCatTipoVivienda, -1) as idCatTipoVivienda
							, COALESCE(pn.idCatDetTipoVivienda, -1) as idCatDetTipoVivienda, COALESCE(pn.tiempoResidenciaActual, -1) as tiempoResidenciaActual
							, COALESCE(pn.idCatCorrespondencia, -1) as idCatCorrespondencia
FROM BG_PER_Personas p 
inner join BG_PER_Ubicaciones u ON u.idPersona=p.idPersona
inner join BG_PER_PersonasNaturales pn  ON pn.idPersonaNatural= p.idPersona
and p.numIdentificacion in (
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
)
WHERE correoElectrónico <> 'NO-DATA' 
and  celular <> 'NO-DATA'
and idCatNivelEstudios <> -1
and idCatDetNivelEstudios <> -1
and idCatSituacionLaboral <> -1
and tiempoLaboraEmp <>-1
and idCatTipoVivienda <> -1
order by p.FechaIngresoCLTE desc