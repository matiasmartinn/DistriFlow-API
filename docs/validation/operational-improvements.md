# Mejoras operativas: nombres, pedidos y dashboard

## Comportamiento entregado

- El administrador corrige el nombre de clientes activos mediante `PUT /api/Customers/{id}/commercial-info`.
  El campo opcional `name` mantiene la compatibilidad con clientes HTTP anteriores. El cambio conserva
  el identificador y los pedidos; las consultas muestran el nombre actual del cliente.
- `PUT /api/Auth/me/name`, con `{ "name": "Nombre" }`, modifica solo al usuario de la sesión y devuelve
  `CurrentUserDto`. Está disponible para ambos roles activos. No modifica datos de acceso ni permisos.
- Ambos nombres son obligatorios, se recortan y admiten hasta 200 caracteres; la regla se aplica en dominio.
- `GET /api/Orders/dashboard` devuelve `today`, `timeZone`, `nextDayStartsAtUtc`, `createdToday`,
  `draftCount`, `confirmedToday` y `recentOrders` (máximo diez). El repositorio cuenta todos los registros
  del alcance, sin cargar todos los pedidos en memoria. El preventista queda limitado a sus pedidos;
  el administrador consulta los de su distribuidora. No recibe identificadores para ampliar ese alcance.
- El día se calcula desde la configuración de la distribuidora, con inicio inclusivo y fin exclusivo.
  Confirmados hoy usa `ConfirmedAtUtc` y estado actual `Confirmed`; cancelar excluye el pedido del contador.
- `GET /api/Orders` admite `localDateFrom` y `localDateTo` (fechas inclusivas de calendario del tenant)
  y `dateField=Created|Confirmed`. Los filtros anteriores `dateFrom`/`dateTo` conservan su comportamiento
  para instantes UTC. No se permite combinar ambos tipos de rango en una petición.
- No hay migraciones ni cambios en stock o sus movimientos.

## Validaciones

Ejecutar desde la raíz de la API:

```powershell
dotnet build --no-restore --disable-build-servers -m:1
dotnet run --project verification/DistriFlow.Domain.Checks
dotnet run --project verification/DistriFlow.Operational.Checks
```

La última suite requiere API local iniciada en `http://localhost:5116/api/` y PostgreSQL local con
las migraciones existentes. Se puede cambiar la URL con `OPERATIONAL_API_URL`. Lee la configuración
de desarrollo y variables de entorno; rechaza API y host de base remotos. No aplica migraciones.

Crea dos tenants aislados con prefijo `operational-`, usuarios, cliente, producto y pedidos propios.
Inserta borradores con fechas controladas y cambia la fecha de creación de un pedido suyo para probar
la diferencia con confirmación. No modifica datos previos ni los elimina al terminar. No usar los tenants
de prueba para operar: algunos borradores tienen fechas futuras deliberadamente.

Resultado local: compilación API correcta, 12 checks de dominio y 9 escenarios de integración correctos:
sesión, nombres, compatibilidad, roles/tenant, más de una página, límites diarios, fechas de confirmación,
cancelación, cambio horario en medianoche y recursos inactivos. La suite existente
`scripts/verify-stage-three.mjs` pasó sus 18 escenarios de regresión del circuito completo.

La compilación inicial emitió NU1900 por acceso a la auditoría de NuGet. La suite nueva referencia
la versión 9.0.10 de EF Relational ya utilizada por la API para alinear sus dependencias de prueba.
No se incorporaron bibliotecas a la aplicación. CI incluye la nueva suite; su ejecución remota no se verificó.

## Archivos de esta entrega

- `.gitignore` (incluir esta guía mantenida)
- `src/DistriFlow.Domain/Customers/Customer.cs`
- `src/DistriFlow.Domain/Users/User.cs`
- `src/DistriFlow.Domain/Orders/Enums/OrderDateField.cs` (nuevo)
- `src/DistriFlow.Domain/Orders/IOrderRepository.cs`
- `src/DistriFlow.Application/Customers/DTOs/Requests/UpdateCustomerCommercialInfoRequest.cs`
- `src/DistriFlow.Application/Customers/CustomerService.cs`
- `src/DistriFlow.Application/Auth/DTOs/UpdateCurrentUserNameRequest.cs` (nuevo)
- `src/DistriFlow.Application/Auth/IAuthService.cs`
- `src/DistriFlow.Application/Auth/AuthService.cs`
- `src/DistriFlow.Application/Orders/DTOs/Requests/OrderListRequest.cs`
- `src/DistriFlow.Application/Orders/DTOs/Responses/OrderDashboardDto.cs` (nuevo)
- `src/DistriFlow.Application/Orders/Queries/OrderCalendar.cs` (nuevo)
- `src/DistriFlow.Application/Orders/Queries/IOrderQueryService.cs`
- `src/DistriFlow.Application/Orders/Queries/OrderQueryService.cs`
- `src/DistriFlow.Infrastructure/Repositories/OrderRepository.cs`
- `src/DistriFlow.Api/Controllers/AuthController.cs`
- `src/DistriFlow.Api/Controllers/OrdersController.cs`
- `verification/DistriFlow.Operational.Checks/DistriFlow.Operational.Checks.csproj` (nuevo)
- `verification/DistriFlow.Operational.Checks/Program.cs` (nuevo)
- `.github/workflows/check.yml` (ampliación del workflow preexistente en el workspace)
- `docs/product/mvp.md`
- `docs/validation/operational-improvements.md` (este archivo)

Los restantes cambios previos del workspace se conservaron.
