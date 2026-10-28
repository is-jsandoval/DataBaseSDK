--IP Servidor: 172.26.60.105,4433
--Usuario: usr_bizagi
--Clave: Bg123456 


USE BG_NEO
go
 
select * from BG_NEO..BG_PER_Personas pr with(nolock) where pr.numIdentificacion = '0954121646'
declare @opcion int = 
2-- 1. Solo consulta , 2. Consulta y actualiza oficial
declare @id varchar(13) = '0954121646'
declare @oficial int = 0, @opidOficial varchar(10) = '', @codigoEmpleado varchar(10) = '' , @oficinaOficialCore varchar(6) = ''
declare @opidOficialDefault varchar(5) = 'JMM', @idOficialDefault int = 0, @cambiarOficial bit = 0
--

select @idOficialDefault = idCodigo from BG_ADM_CatalogosDetalles with(nolock) where idCatalogo = 55 and strCodigoHost = @opidOficialDefault
if @opcion in (1,2)
begin
select * from BG_PER_Personas pr with(nolock) where pr.numIdentificacion = @id
if (@@ROWCOUNT = 0)
   select '002' idCodigo, 'Persona no registrada en Neo Comercial' mensaje
else
   begin
		print 'Persona existe en neo comercial'
		-- identificar el oficial --
		select @oficial = pr.idCatDetOficial from BG_PER_Personas pr with(nolock) where pr.numIdentificacion = @id
		if (@@ROWCOUNT = 0)
		begin
			select '003' idCodigo, 'Persona no tiene un oficial relacionado en Neo Comercial' mensaje
			set @cambiarOficial = 1
		end
		else
			begin
				select @opidOficial = o.strCodigoHost from BG_ADM_CatalogosDetalles o with(nolock) where o.idCatalogo = 55 and o.idCodigo = @oficial
				if (@@ROWCOUNT = 0)
				begin
				   select '004' idCodigo, 'Oficial no registrado en Catalogo de oficiales en Neo Comercial' mensaje
				   set @cambiarOficial = 1
				end
				else
				   begin
					   select @codigoEmpleado = strValor3 from BG_ADM_CatalogosDetalles us with(nolock) where us.idCatalogo = 154 and us.strCodigoHost = @opidOficial and us.idEstado = 1
					   if (@@ROWCOUNT = 0)
					   begin
					      select '005' idCodigo, 'Oficial no registrado como usuario en Catalogo de usuarios en Neo Comercial' mensaje
						  set @cambiarOficial = 1
					   end
					   else
					      begin
							select ev.OFICINA, ev.COD_OFICINA, ev.CODIGO, ev.NOMBRE + ' ' + ev.APELLIDO nombreOficial, PUESTO, @opidOficial opid, @oficial idOficial from BG_ADM_EstructuraEvolution ev with(nolock) where ev.CODIGO = @codigoEmpleado
							 if (@@ROWCOUNT = 0)
							 begin
							    select '006' idCodigo, 'Usuario no registrado en estructura de Evolution en Neo Comercial' mensaje
								set @cambiarOficial = 1
							 end
							 else
							    select '000' idCodigo, 'Persona tiene registro el oficial correcto' mensaje
						  end
				   end
			end
   end
end
if (@opcion = 2)
begin
	if (@cambiarOficial = 1)
	begin
		update BG_PER_Personas
		set idCatDetOficial = @idOficialDefault
		where numIdentificacion = @id
		if (@@ROWCOUNT > 0)
			select '000' idCodigo, 'Se actualizo el oficial' mensaje
		else
			select '006' idCodigo, 'No se actualizó el oficial' mensaje
	end
end
 
 /*
--TIPO DE DOCUMENTOS
 
select * from BG_ADM_TiposDocumentos
		 where idEstado=1 and (strNombreComercial like '%contrato%' or strNombreComercial like '%pagar%') 
		 select * from BG_ADM_TiposDocumentos
		 where idEstado=1 and idCodigo in(11,12,71)
 
		 --11 --> CONTRATO
		 --12 --> PAGARÉS
		 --71 --> CONTRATO APERTURA CUENTAS
		 */