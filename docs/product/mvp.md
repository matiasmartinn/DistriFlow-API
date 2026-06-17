# MVP — Alcance de la v1

> El MVP es el **baseline mínimo** para que el producto tenga sentido: tomar pedidos con precio
> correcto y stock real. Nada más. Fuente: `docs/domain/` (Baseline mínimo recomendado).

## En alcance (v1)

### 1. Identidad y acceso (soporte)
- Alta/onboarding de **Distributor** (tenant) con su **User** administrador.
- Usuarios con rol `Administrator` o `SalesRep`. Email de usuario único a nivel sistema.
- Scoping por tenant: todo cuelga de `DistributorId`.
- Estados: `Distributor` (Active/Suspended); `User` (Active/Inactive).

### 2. Catálogo y precios
- **Product** con precio base, unidad de venta y estado (Active/Discontinued). Precio base ≥ 0.
- **PriceList** reutilizable: precio por producto, cada producto a lo sumo una vez. Precio ≥ 0.
- **Customer** con `PriceListId` opcional asignada (fallback a precio base si no tiene).

### 3. Toma de pedidos (CORE)
- **Order** con ciclo `Draft → Confirmed → Cancelled`.
- `Administrator` y `SalesRep` pueden iniciar, editar, confirmar y cancelar pedidos dentro de su distribuidora.
- Agregar/quitar líneas y cambiar cantidades **solo en Draft**.
- Precio unitario de cada línea = **snapshot** desde la lista del cliente al momento de agregar/confirmar.
- Total = suma de subtotales (calculado, nunca seteado a mano).
- Invariantes: cantidad > 0; confirmado con ≥ 1 línea; no agregar producto discontinuado;
  no tomar pedidos a cliente inactivo ni distribuidora suspendida.

### 4. Stock
- **Stock** por producto con cantidad disponible.
- Se **descuenta al confirmar** el pedido. Operaciones: ingresar, descontar, ajustar.
- La política predeterminada `RequireAvailableStock` impide stock negativo.
- `AllowNegativeStock` es una opción configurable por distribuidora cuando el negocio la habilita explícitamente.

### 5. Histórico
- Consulta de **pedidos por cliente** (solo lectura; sin reglas nuevas).

## Fuera de alcance (v1)

- Sincronización **offline** del preventista (riesgo técnico alto; se diseña aparte).
- **WhatsApp → venta** automático.
- **Ruteo / zonas** de preventistas.
- Integración **ERP** (export CSV simple podría entrar más adelante).
- **Cuenta corriente / cobranzas / saldo**.
- **Facturación / comprobantes / AFIP**.
- **Logística**: entrega, devoluciones, entregas parciales.
- Descuentos, promociones, precios con vigencia, multimoneda.
- Múltiples depósitos, lotes y vencimientos en stock.
- Autenticación avanzada, invitaciones de usuario, notificaciones (detalle técnico/infra).
- Catálogos geográficos **Province** y **City** para normalizar direcciones (lectura).
- Auditoría formal centralizada (`AuditEvent`) para acciones sensibles.

## Criterio de "MVP terminado"

Un usuario operativo (`Administrator` o `SalesRep`) puede, para una distribuidora dada:
1. Ver el catálogo y los precios que le corresponden a un cliente.
2. Crear un pedido en borrador, agregar líneas y confirmarlo con precios fijados.
3. Que el stock se descuente al confirmar y no quede negativo cuando rige `RequireAvailableStock`.
4. El administrador puede consultar el histórico de pedidos de ese cliente.

Todo respetando el aislamiento por tenant.
