# Features API para cierre de MVP

## 1. Objetivo

Este documento registra las funcionalidades de API necesarias para completar el MVP comercial de DistriFlow y su estado esperado.

El dominio y las funcionalidades documentadas en los archivos anteriores se consideran la base. Este archivo debe utilizarse como fuente de verdad para cierre y extensiones del backend, evitando duplicar comportamientos existentes o volver a marcar como pendiente lo que ya está implementado.

Estado actual del cierre MVP:

- Implementado en API: onboarding, configuración por distribuidora, JWT con tenant/rol, administración de usuarios, clientes, productos, listas de precios versionadas, trazabilidad de precios, ciclo de vida de pedidos, confirmación/cancelación transaccional, ledger de stock, idempotencia y concurrencia operativa.
- `Administrator` y `SalesRep` pueden operar pedidos dentro de su distribuidora. `SalesRep` conserva el alcance de sus propios pedidos.
- `RequireAvailableStock` es el valor predeterminado y evita stock negativo. `AllowNegativeStock` sigue siendo una política válida configurable por distribuidora.
- Alcance futuro conservado: catálogos geográficos `Province`/`City`, auditoría formal centralizada (`AuditEvent`), pruebas automatizadas exhaustivas e integraciones/reportes fuera del core.

El alcance se limita a:

- Enriquecer entidades existentes.
- Incorporar conceptos de dominio indispensables.
- Completar flujos críticos de pedidos, stock, clientes y usuarios.
- Mejorar trazabilidad, auditoría y consistencia.
- Mantener aislamiento por distribuidora.
- Evitar funcionalidades expresamente excluidas del MVP.

---

## 2. Principios de implementación

Las nuevas funcionalidades deben respetar las decisiones arquitectónicas actuales:

- Arquitectura por capas: `Api`, `Application`, `Domain` e `Infrastructure`.
- Sin CQRS, MediatR ni handlers por caso de uso.
- Controllers delgados.
- Servicios de aplicación para coordinar casos de uso.
- Repositorios definidos desde el dominio.
- Reglas de negocio dentro de agregados, entidades o servicios de dominio cuando corresponda.
- Uso del patrón `Result` y `ProblemDetails`.
- Multi-tenancy resuelto mediante el usuario autenticado.
- Ningún recurso de una distribuidora puede ser consultado o modificado desde otra.
- No eliminar registros de negocio físicamente salvo que exista una justificación explícita.
- No refactorizar funcionalidades existentes que ya funcionan, excepto cuando sea necesario para incorporar estas reglas.

---

## 3. Prioridades

### P0 — Obligatorio para MVP

Una funcionalidad P0 debe completarse antes de considerar la API lista para uso real.

### P1 — Importante para salida al mercado

Puede implementarse después de los flujos principales, pero debe estar disponible antes de ofrecer el producto a clientes reales.

### P2 — Mejora posterior

No bloquea el MVP inicial. Debe quedar documentada, pero fuera de la primera salida.

---

# 4. Features de cierre de API

## FEATURE API-MVP-01 — Configuración operativa por distribuidora

**Prioridad:** P0  
**Complejidad estimada:** Media

### Objetivo

Permitir que cada distribuidora configure reglas operativas propias sin modificar código ni compartir configuraciones con otros tenants.

### Nuevo concepto de dominio

`DistributorConfiguration` o `ConfiguracionDistribuidora`.

Debe existir una única configuración activa por distribuidora.

### Propiedades mínimas

- `DistributorId`
- `StockPolicy`
- `PriceFallbackPolicy`
- `OrderNumberPrefix`
- `NextOrderNumber`
- `TimeZone`
- `CreatedAt`
- `UpdatedAt`

### Políticas iniciales

#### Política de stock

Debe permitir definir al menos:

- No permitir confirmar pedidos sin stock suficiente.
- Permitir stock negativo.

Para el MVP, la política recomendada por defecto es no permitir stock negativo.

#### Fallback de precio

Debe permitir definir qué hacer cuando un producto no tiene un precio vigente:

- Rechazar la creación o confirmación del pedido.
- Usar el último precio válido conocido.

La política debe aplicarse de forma consistente en todos los flujos de pedidos.

#### Numeración de pedidos

Cada distribuidora debe poder configurar:

- Prefijo opcional.
- Próximo número secuencial.
- Generación única y atómica.

Ejemplo:

`PED-000001`

#### Zona horaria

La configuración debe almacenar una zona horaria válida.

Las fechas deben persistirse en UTC. La zona horaria se utilizará para interpretar fechas operativas, reportes y respuestas que requieran contexto local.

### Casos de uso

- Consultar configuración de la distribuidora actual.
- Actualizar configuración.
- Crear automáticamente una configuración inicial durante el onboarding.
- Obtener el próximo número de pedido de forma segura.

### Reglas

- Solo un usuario `Administrator` puede modificar la configuración.
- La configuración siempre pertenece a la distribuidora autenticada.
- No puede existir más de una configuración por distribuidora.
- El contador de pedidos no puede disminuir.
- La zona horaria debe ser válida.
- Los valores por defecto deben permitir operar inmediatamente después del onboarding.

### Criterios de aceptación

- Una distribuidora nueva recibe configuración predeterminada.
- Un administrador puede consultarla y modificarla.
- Una distribuidora no puede acceder a la configuración de otra.
- Dos pedidos creados de forma concurrente no reciben el mismo número.
- Las políticas configuradas son utilizadas por los flujos de stock y precio.

---

## FEATURE API-MVP-02 — Listas de precios con estado, vigencia y versión

**Prioridad:** P0  
**Complejidad estimada:** Alta

### Objetivo

Evitar modificaciones ambiguas de precios y poder determinar qué precio era válido al momento de generar un pedido.

### Entidad afectada

`PriceList` o `ListaDePrecios`.

### Propiedades a incorporar

- `Status`
- `ValidFrom`
- `ValidUntil`
- `Version`
- `ActivatedAt`
- `DeactivatedAt`
- `CreatedByUserId`
- `UpdatedByUserId`

### Estados mínimos

- `Draft`
- `Active`
- `Inactive`

### Reglas

- Una lista en borrador puede modificarse.
- Una lista activa no debe modificarse de forma que altere pedidos históricos.
- Para cambiar precios de una lista activa debe generarse una nueva versión.
- Solo puede utilizarse una versión activa y vigente.
- `ValidUntil` debe ser posterior a `ValidFrom`.
- No deben existir versiones activas superpuestas de la misma lista cuando su alcance sea equivalente.
- Inactivar una lista no debe afectar pedidos ya creados.
- Cada versión debe conservar su contenido histórico.

### Casos de uso

- Crear lista de precios en borrador.
- Modificar lista en borrador.
- Activar lista.
- Inactivar lista.
- Crear una nueva versión a partir de una lista existente.
- Consultar lista activa y vigente.
- Consultar historial de versiones.

### Criterios de aceptación

- No se puede utilizar una lista inactiva o fuera de vigencia.
- La activación valida fechas y consistencia.
- Los pedidos conservan el precio utilizado aunque posteriormente se cree otra versión.
- El historial permite identificar qué versión estaba activa en una fecha determinada.

---

## FEATURE API-MVP-03 — Trazabilidad del precio aplicado al pedido

**Prioridad:** P0  
**Complejidad estimada:** Media

### Objetivo

Conservar el origen del precio aplicado a cada línea de pedido.

### Entidad afectada

`OrderLine` o `LineaDePedido`.

### Propiedades a incorporar

- `PriceListId`
- `PriceListVersion`
- `SourceUnitPrice`
- `AppliedUnitPrice`
- `PriceOrigin`

### Orígenes mínimos

- `PriceList`
- `LastValidPrice`
- `Manual`

El origen manual solo debe existir si el dominio actual ya permite modificar precios manualmente. En caso contrario, debe quedar fuera del MVP.

### Reglas

- Cada línea debe conservar el precio unitario aplicado al crear o confirmar el pedido.
- La modificación posterior de la lista no debe alterar líneas existentes.
- Si se utiliza fallback, debe registrarse explícitamente.
- Si no existe un precio válido y la configuración indica rechazar, la operación debe fallar.
- La línea debe pertenecer a un pedido de la distribuidora autenticada.

### Criterios de aceptación

- Puede determinarse qué lista y versión originaron el precio.
- Los importes históricos no cambian al modificar precios.
- La API informa un error de negocio claro cuando no puede resolver un precio.

---

## FEATURE API-MVP-04 — Ciclo de vida y trazabilidad del pedido

**Prioridad:** P0  
**Complejidad estimada:** Media

### Objetivo

Modelar explícitamente las transiciones principales del pedido y conservar su historial básico.

### Entidad afectada

`Order` o `Pedido`.

### Propiedades a incorporar

- `CreatedAt`
- `ConfirmedAt`
- `CancelledAt`
- `CancellationReason`
- `Origin`
- `OrderNumber`
- `CreatedByUserId`
- `ConfirmedByUserId`
- `CancelledByUserId`
- `RowVersion` o mecanismo equivalente de concurrencia

### Orígenes mínimos

- `Online`
- `Offline`

`Offline` representa un pedido capturado posteriormente o generado desde un flujo local. No implica sincronización offline dentro del MVP.

### Reglas

- El número de pedido debe generarse según la configuración de la distribuidora.
- `ConfirmedAt` se asigna únicamente al confirmar.
- `CancelledAt` y `CancellationReason` son obligatorios al cancelar.
- Un pedido confirmado no puede volver a estado borrador.
- Un pedido cancelado no puede confirmarse nuevamente.
- Los timestamps de transición son inmutables.
- Cada transición debe registrar el usuario responsable.
- Debe detectarse y rechazarse una actualización basada en una versión desactualizada.

### Criterios de aceptación

- Cada pedido posee un número único dentro de la distribuidora.
- Las transiciones inválidas son rechazadas.
- La respuesta permite conocer cuándo y quién realizó cada transición.
- Los conflictos de concurrencia producen una respuesta controlada, preferentemente HTTP `409`.

---

## FEATURE API-MVP-05 — Confirmación robusta del pedido

**Prioridad:** P0  
**Complejidad estimada:** Alta

### Objetivo

Garantizar que confirmar un pedido sea una operación consistente, atómica y segura ante concurrencia.

### Validaciones obligatorias

Antes de confirmar:

- El pedido existe y pertenece a la distribuidora autenticada.
- El pedido se encuentra en un estado confirmable.
- El cliente está activo.
- Todos los productos están activos.
- Todas las líneas tienen cantidades válidas.
- Cada línea posee un precio válido.
- Existe stock suficiente cuando la política no permite stock negativo.
- La versión del pedido no fue modificada por otra operación.

### Comportamiento

La confirmación debe ejecutarse dentro de una transacción.

La operación debe:

1. Validar el pedido.
2. Validar cliente y productos.
3. Resolver y congelar precios si todavía no están congelados.
4. Validar stock.
5. Registrar movimientos de salida.
6. Actualizar existencias.
7. Marcar el pedido como confirmado.
8. Registrar fecha y usuario.
9. Registrar trazabilidad operativa.

Si cualquier paso falla, no debe persistirse ningún cambio parcial.

### Concurrencia

Debe implementarse un mecanismo que evite que dos confirmaciones consuman el mismo stock simultáneamente.

Puede resolverse mediante:

- Concurrencia optimista con token de versión y reintento controlado.
- Actualización condicional de existencias.
- Bloqueo transaccional específico.

La solución elegida debe evitar ventas dobles sin introducir bloqueos globales.

### Errores esperados

- Cliente inactivo.
- Producto inactivo.
- Precio inexistente.
- Stock insuficiente.
- Pedido en estado inválido.
- Conflicto de concurrencia.
- Recurso perteneciente a otro tenant.

### Criterios de aceptación

- No se confirma un pedido inválido.
- No se descuenta stock parcialmente.
- Dos confirmaciones concurrentes no pueden consumir el mismo stock disponible.
- Cada descuento genera su movimiento de stock.
- Ante un fallo, el pedido y el stock conservan su estado anterior.

---

## FEATURE API-MVP-06 — Ledger de movimientos de stock

**Prioridad:** P0  
**Complejidad estimada:** Alta

### Objetivo

Reemplazar las modificaciones de stock sin trazabilidad por un historial inmutable de entradas, salidas y ajustes.

### Nueva entidad

`StockMovement` o `MovimientoDeStock`.

### Propiedades mínimas

- `Id`
- `DistributorId`
- `ProductId`
- `Type`
- `Quantity`
- `PreviousQuantity`
- `ResultingQuantity`
- `ReferenceType`
- `ReferenceId`
- `Reason`
- `CreatedAt`
- `CreatedByUserId`

### Tipos mínimos

- `Entry`
- `Exit`
- `AdjustmentIncrease`
- `AdjustmentDecrease`
- `OrderCancellationReversal`

### Referencias mínimas

- Pedido.
- Cancelación de pedido.
- Ajuste manual.
- Carga inicial.

### Reglas

- Todo cambio en existencias debe generar un movimiento.
- Los movimientos no deben editarse ni eliminarse.
- La cantidad debe ser positiva; el tipo determina el sentido.
- `PreviousQuantity` y `ResultingQuantity` deben ser consistentes.
- Un movimiento debe registrar origen, fecha y usuario.
- Los movimientos deben estar aislados por distribuidora.
- Los ajustes manuales requieren motivo.
- Una referencia de negocio no debe generar el mismo movimiento dos veces.

### Casos de uso

- Registrar entrada.
- Registrar ajuste.
- Consultar movimientos por producto.
- Consultar movimientos por rango de fechas.
- Consultar movimientos por referencia.
- Obtener existencia actual.
- Revertir stock mediante un movimiento compensatorio.

### Criterios de aceptación

- Puede reconstruirse cómo se llegó a la existencia actual.
- Confirmar un pedido genera salidas vinculadas al pedido.
- Cancelar un pedido genera movimientos de reversa vinculados.
- Ningún endpoint modifica stock sin generar movimiento.

---

## FEATURE API-MVP-07 — Cancelación de pedido con reversa de stock

**Prioridad:** P0  
**Complejidad estimada:** Alta

### Objetivo

Cancelar pedidos sin perder trazabilidad ni dejar existencias inconsistentes.

### Comportamiento

Al cancelar un pedido confirmado:

1. Validar que el pedido pueda cancelarse.
2. Exigir motivo.
3. Verificar que la cancelación no haya sido procesada previamente.
4. Crear movimientos compensatorios por cada salida de stock.
5. Restaurar existencias.
6. Marcar el pedido como cancelado.
7. Registrar fecha y usuario.
8. Registrar trazabilidad operativa.

La operación debe ser transaccional.

### Reglas

- Un pedido ya cancelado no puede cancelarse nuevamente.
- Un pedido en borrador puede cancelarse sin movimientos de stock.
- Un pedido confirmado debe revertir exactamente los movimientos generados por su confirmación.
- La reversa no debe eliminar ni modificar movimientos anteriores.
- La reversa crea nuevos movimientos compensatorios.
- La cancelación debe ser idempotente ante reintentos técnicos.
- El motivo de cancelación es obligatorio.

### Criterios de aceptación

- Cancelar un pedido confirmado restaura el stock.
- Existe trazabilidad entre salidas y movimientos de reversa.
- Una repetición de la solicitud no duplica stock.
- Un fallo intermedio revierte toda la transacción.

---

## FEATURE API-MVP-08 — Soft delete e inactivación consistente

**Prioridad:** P0  
**Complejidad estimada:** Media

### Objetivo

Evitar eliminaciones físicas de información utilizada por operaciones históricas.

### Entidades mínimas afectadas

- Distribuidora.
- Usuario.
- Cliente.
- Producto.
- Lista de precios.

Debe revisarse el resto del dominio para aplicar la misma regla donde exista información histórica dependiente.

### Propiedades comunes sugeridas

- `IsActive`
- `DeactivatedAt`
- `DeactivatedByUserId`
- `DeactivationReason`

No todas las entidades necesitan exponer exactamente las mismas propiedades, pero deben conservar fecha, responsable y motivo cuando la inactivación tenga impacto operativo.

### Reglas

- Un registro referenciado por pedidos o movimientos no debe eliminarse físicamente.
- Los registros inactivos no pueden utilizarse en nuevas operaciones.
- Los registros inactivos deben continuar apareciendo en consultas históricas.
- Los listados operativos deben excluir inactivos por defecto.
- Las consultas administrativas pueden incluirlos mediante filtro.
- La reactivación debe validarse según el tipo de entidad.
- La desactivación no debe modificar operaciones históricas.

### Criterios de aceptación

- Un cliente inactivo no puede utilizarse en un pedido nuevo.
- Un producto inactivo no puede agregarse ni confirmarse.
- Los pedidos históricos siguen mostrando cliente y productos inactivos.
- No existen endpoints de eliminación física para entidades de negocio del MVP.

---

## FEATURE API-MVP-09 — Enriquecimiento del cliente

**Prioridad:** P1  
**Complejidad estimada:** Media

### Objetivo

Incorporar información comercial y fiscal mínima para operar con clientes reales.

### Entidad afectada

`Client` o `Cliente`.

### Propiedades a incorporar

- `Zone`
- `TaxCondition`
- `DeactivationReason`
- `DeactivatedAt`
- `DeactivatedByUserId`

### Condición fiscal

Debe modelarse como un valor controlado. Los valores concretos deben definirse según el mercado objetivo, sin implementar facturación fiscal dentro del MVP.

### Zona

Para el MVP puede representarse mediante un código o texto controlado por distribuidora.

No se debe incorporar geolocalización ni ruteo.

### Reglas

- La condición fiscal debe ser válida.
- La zona pertenece al contexto de la distribuidora.
- La inactivación requiere motivo.
- Un cliente inactivo no puede utilizarse en nuevos pedidos.
- La información histórica debe conservarse.

### Casos de uso

- Crear cliente con zona y condición fiscal.
- Actualizar información comercial.
- Desactivar cliente con motivo.
- Reactivar cliente.
- Filtrar clientes por zona, estado y condición fiscal.

### Criterios de aceptación

- Los filtros pueden utilizarse en listados.
- La API rechaza condiciones fiscales inválidas.
- La desactivación conserva responsable, fecha y motivo.

---

## FEATURE API-MVP-10 — Administración completa de usuarios

**Prioridad:** P0  
**Complejidad estimada:** Media

### Objetivo

Completar la administración de usuarios dentro de cada distribuidora.

### Entidad afectada

`User` o `Usuario`.

### Propiedades a incorporar

- `LastLoginAt`
- `CreatedByUserId`
- `DeactivatedAt`
- `DeactivatedByUserId`
- `DeactivationReason`

### Casos de uso obligatorios

- Crear usuario.
- Listar usuarios.
- Consultar usuario.
- Cambiar rol.
- Desactivar usuario.
- Reactivar usuario.
- Registrar último acceso exitoso.

### Reglas

- Solo `Administrator` puede administrar usuarios.
- Un usuario no puede administrar usuarios de otra distribuidora.
- No debe permitirse desactivar al último administrador activo.
- No debe permitirse que un administrador se quite su propio rol si deja a la distribuidora sin administradores.
- Un usuario inactivo no puede autenticarse.
- El email debe ser único dentro de la distribuidora.
- Todo cambio de rol o estado debe auditarse.
- `LastLoginAt` solo se actualiza después de una autenticación exitosa.

### Criterios de aceptación

- La distribuidora siempre conserva al menos un administrador activo.
- Los usuarios desactivados no pueden iniciar sesión.
- Los cambios de rol quedan registrados.
- Puede identificarse quién creó y quién desactivó cada usuario.

---

## FEATURE API-MVP-11 — Registro de auditoría para acciones sensibles

**Prioridad:** P1  
**Complejidad estimada:** Alta

### Objetivo

Registrar quién realizó cambios sensibles y qué información relevante fue modificada.

### Nueva entidad

`AuditEvent`.

### Propiedades mínimas

- `Id`
- `DistributorId`
- `UserId`
- `Action`
- `EntityType`
- `EntityId`
- `OccurredAt`
- `PreviousValues`
- `NewValues`
- `Metadata`

### Acciones mínimas auditables

- Cambio de rol de usuario.
- Activación o desactivación de usuario.
- Activación o desactivación de cliente.
- Activación o desactivación de producto.
- Cambio de configuración de distribuidora.
- Activación o inactivación de lista de precios.
- Confirmación de pedido.
- Cancelación de pedido.
- Ajuste manual de stock.

### Reglas

- Los eventos son inmutables.
- No deben almacenar contraseñas, tokens ni secretos.
- Los valores sensibles deben excluirse o anonimizarse.
- La auditoría debe generarse dentro de la misma transacción cuando forme parte de una operación crítica.
- Un tenant no puede consultar eventos de otro.
- Los eventos deben poder filtrarse por entidad, usuario, acción y fecha.

### Criterios de aceptación

- Puede conocerse quién realizó una acción sensible.
- Puede identificarse la entidad afectada y la fecha.
- Los eventos no pueden editarse ni eliminarse desde endpoints públicos.
- No se registran credenciales ni datos secretos.

---

## FEATURE API-MVP-12 — Manejo uniforme de concurrencia e idempotencia

**Prioridad:** P0  
**Complejidad estimada:** Alta

### Objetivo

Evitar duplicaciones y estados inconsistentes en operaciones críticas.

### Operaciones afectadas

- Generación del número de pedido.
- Confirmación de pedido.
- Cancelación de pedido.
- Descuento y reversa de stock.
- Activación de versiones de listas de precios.
- Ajustes manuales de stock.

### Requisitos

- Incorporar tokens de concurrencia en agregados críticos.
- Detectar actualizaciones basadas en versiones antiguas.
- Responder conflictos mediante un error uniforme.
- Evitar duplicar operaciones ante reintentos.
- Definir una clave de idempotencia o referencia única cuando corresponda.

### Reglas

- Los conflictos de concurrencia no deben devolver errores internos genéricos.
- Una operación reintentada con la misma clave no debe duplicar efectos.
- La idempotencia debe aplicarse especialmente a confirmación, cancelación y movimientos de stock.
- No debe utilizarse una estrategia de bloqueo global.

### Criterios de aceptación

- Solicitudes repetidas no duplican movimientos.
- Dos operaciones simultáneas sobre el mismo pedido no generan estados imposibles.
- La API devuelve HTTP `409` para conflictos de negocio o concurrencia cuando corresponda.

---

# 5. Cambios transversales

## 5.1 Migraciones

Cada feature que modifique persistencia debe incluir:

- Migración de esquema.
- Índices necesarios.
- Restricciones únicas.
- Foreign keys.
- Valor inicial seguro para datos existentes.
- Estrategia de backfill cuando una propiedad pase a ser obligatoria.

No debe asumirse que la base de datos estará vacía.

---

## 5.2 Contratos de API

Los endpoints nuevos deben:

- Mantener convenciones REST existentes.
- Utilizar DTOs específicos.
- No exponer entidades de dominio directamente.
- Devolver `ProblemDetails` para errores.
- Diferenciar errores de validación, autorización, conflicto y recurso inexistente.
- Incluir paginación en listados con crecimiento potencial.
- Permitir filtros solo cuando tengan utilidad operativa clara.

---

## 5.3 Autorización

Debe verificarse explícitamente:

- Usuario autenticado.
- Distribuidora obtenida desde claims.
- Rol requerido.
- Pertenencia del recurso al tenant.
- Estado activo del usuario.

No se debe aceptar `DistributorId` enviado por el cliente para resolver el tenant de operaciones normales.

---

## 5.4 Fechas

- Persistir fechas en UTC.
- Usar la zona horaria configurada solo para interpretación o presentación.
- Los timestamps de transición no deben poder modificarse directamente.
- Las fechas generadas por el servidor deben prevalecer sobre valores enviados por el cliente.

---

## 5.5 Observabilidad

Las operaciones críticas deben generar logs estructurados:

- Confirmación de pedido.
- Cancelación de pedido.
- Conflictos de stock.
- Errores de concurrencia.
- Cambios de configuración.
- Cambios de rol.
- Ajustes manuales de stock.

Los logs técnicos no reemplazan `AuditEvent`; la auditoría formal centralizada queda como alcance futuro.

---

# 6. Orden recomendado de implementación

## Etapa 1 — Base operativa

1. `API-MVP-01` Configuración operativa por distribuidora.
2. `API-MVP-04` Ciclo de vida y trazabilidad del pedido.
3. `API-MVP-06` Ledger de movimientos de stock.
4. `API-MVP-12` Concurrencia e idempotencia.

## Etapa 2 — Flujos críticos

5. `API-MVP-05` Confirmación robusta del pedido.
6. `API-MVP-07` Cancelación con reversa.
7. `API-MVP-08` Soft delete consistente.
8. `API-MVP-10` Administración completa de usuarios.

## Etapa 3 — Precios y datos comerciales

9. `API-MVP-02` Listas de precios versionadas.
10. `API-MVP-03` Trazabilidad del precio por línea.
11. `API-MVP-09` Enriquecimiento del cliente.

## Etapa 4 — Control y salida al mercado

12. `API-MVP-11` Auditoría de acciones sensibles.
13. Revisión integral de autorización por tenant.
14. Pruebas de concurrencia y transacciones.
15. Validación end-to-end de los flujos principales.

---

# 7. Flujos mínimos que deben funcionar de extremo a extremo

La API podrá considerarse funcional para el MVP cuando permita completar estos flujos:

## Flujo A — Onboarding y configuración

1. Crear distribuidora.
2. Crear administrador inicial.
3. Crear configuración predeterminada.
4. Autenticar al administrador.
5. Modificar configuración operativa.

## Flujo B — Administración

1. Crear usuarios.
2. Cambiar roles.
3. Desactivar y reactivar usuarios.
4. Crear clientes.
5. Desactivar y reactivar clientes.
6. Crear productos.
7. Desactivar y reactivar productos.

## Flujo C — Precios

1. Crear lista en borrador.
2. Agregar precios.
3. Activar una versión.
4. Resolver precio vigente.
5. Crear una nueva versión sin modificar pedidos anteriores.

## Flujo D — Pedido

1. Crear pedido.
2. Agregar líneas.
3. Resolver precios.
4. Validar cliente, productos y stock.
5. Confirmar.
6. Descontar stock.
7. Consultar trazabilidad.

## Flujo E — Cancelación

1. Cancelar pedido con motivo.
2. Revertir stock.
3. Registrar movimientos compensatorios.
4. Consultar responsable, fecha y trazabilidad operativa. La auditoría formal centralizada queda como alcance futuro.

## Flujo F — Stock

1. Registrar carga inicial.
2. Registrar entrada.
3. Registrar ajuste con motivo.
4. Consultar existencia.
5. Consultar historial de movimientos.

---

# 8. Definición de terminado del MVP de API

La API se considera lista para el MVP cuando:

- Todos los flujos mínimos funcionan end-to-end.
- La confirmación y cancelación de pedidos son transaccionales.
- No es posible vender dos veces el mismo stock por concurrencia cuando rige `RequireAvailableStock`.
- Todo cambio de stock genera un movimiento.
- Los pedidos conservan sus precios históricos.
- Los recursos están aislados por distribuidora.
- Los usuarios inactivos no pueden autenticarse.
- No se puede dejar una distribuidora sin administradores.
- Las entidades históricas no se eliminan físicamente.
- Las acciones sensibles tienen trazabilidad operativa; `AuditEvent` formal queda como alcance futuro.
- Los errores de negocio se devuelven de forma consistente.
- Las pruebas automatizadas exhaustivas de reglas críticas, transacciones y concurrencia quedan como endurecimiento posterior; para el cierre actual se valida con build y verificación dirigida.
- No quedan endpoints que permitan omitir las reglas definidas en este documento.

---

# 9. Fuera del MVP

No implementar dentro de este alcance:

- Facturación o integración con AFIP.
- Cuenta corriente.
- Cobranzas y conciliaciones.
- Promociones y descuentos complejos.
- Multimoneda.
- Múltiples depósitos.
- Gestión por lotes o vencimientos.
- Geolocalización y optimización de rutas.
- Portal de autoservicio para clientes.
- Devoluciones parciales.
- Integraciones con ERP.
- Sincronización offline real.
- Business intelligence avanzado.
- Contabilidad.
- Gestión logística avanzada.

Estas funcionalidades pueden analizarse después de estabilizar el MVP y validar su uso con clientes reales.
