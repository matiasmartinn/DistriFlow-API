# Domain.md — Distribuidoras / Preventistas

> Modelo de dominio conceptual (DDD) previo a implementación. **No** contiene base de datos, endpoints, EF Core, DTOs ni estructura de carpetas.
> Referencia de estilo: VETE (dominio claro, aggregates ricos, value objects, baseline mínimo, sin sobreingeniería). El dominio en sí es propio de este proyecto.

---

## Resumen del dominio

Sistema de gestión de pedidos para **distribuidoras chicas** y sus **equipos de preventa**. El preventista visita comercios, arma un pedido con los productos y los precios que correspondan a ese cliente, y lo confirma. La distribuidora mantiene su catálogo, sus listas de precios y su stock, y ve el histórico de pedidos por cliente.

El **dominio central (core)** es la **toma de pedidos**: es lo que da valor diferencial y donde viven las reglas más ricas (precio correcto por cliente, cantidades válidas, stock, estados del pedido). El resto (catálogo, clientes, stock, usuarios) son dominios de soporte que existen para que el pedido sea posible.

Es un **SaaS multi-distribuidora**: cada distribuidora es un tenant aislado. Toda la información (productos, clientes, pedidos, stock, usuarios) pertenece a una distribuidora y nunca se cruza entre tenants. Análogo conceptual a `Clinic` en VETE.

**Distinción de capas (para no contaminar el dominio):**
- **Dominio real:** productos, precios, clientes-comercio, pedidos, stock, sus reglas e invariantes.
- **Aplicación:** orquestación de casos de uso (tomar pedido, confirmar, ajustar stock), scoping por distribuidora, autorización por rol.
- **Detalles técnicos (fuera de este doc):** persistencia, sincronización offline, export CSV, mensajería WhatsApp, autenticación.

---

## Actores

- **Preventista** — Vendedor de campo. Toma pedidos para los comercios que tiene asignados/visita. Usa la app (probablemente móvil, posiblemente offline). No administra catálogo ni precios.
- **Administrador / Dueño de la distribuidora** — Gestiona catálogo, listas de precios, stock, clientes y usuarios. Revisa pedidos. En distribuidoras muy chicas, una sola persona cumple este rol y también toma pedidos.
- **Cliente (comercio)** — El negocio que recibe la visita y a quien se le vende (kiosco, almacén, etc.). **No es usuario del sistema en el baseline**: es un *sujeto* del dominio, no un actor que opera la app.
- **Sistema** — Tareas automáticas (recalcular totales, futuros recordatorios/sincronización). Relevante recién fuera del baseline.

> **Duda (ver Decisiones pendientes):** ¿El cliente-comercio llegará a ser actor (autoservicio de pedidos) en el futuro? Hoy se asume que no.

---

## Flujos principales

> Flujos de negocio reales, descritos a nivel conceptual (sin pasos técnicos ni endpoints).

1. **Alta / onboarding de distribuidora**
   Se registra una distribuidora con su usuario administrador. A partir de ahí todo queda scoped a esa distribuidora.

2. **Gestión de catálogo y precios**
   El administrador da de alta productos (con su precio base y unidad de venta) y arma listas de precios. Asigna a cada cliente una lista de precios (o usa el precio base si no tiene lista).

3. **Alta de clientes-comercio**
   El administrador (o el preventista) registra los comercios a los que se vende, con su contacto, dirección y lista de precios asignada.

4. **Toma de pedido (flujo core)**
   El preventista o el administrador de la distribuidora crea un pedido para ese cliente, agrega líneas (producto + cantidad). El precio de cada línea sale de la lista de precios del cliente. Mientras está en borrador puede modificarlo. Al terminar, lo confirma.

5. **Confirmación del pedido**
   El pedido pasa de borrador a confirmado. Queda fijado (snapshot de precios y cantidades), entra al histórico del cliente y descuenta stock.

6. **Cancelación del pedido**
   Un pedido puede cancelarse según su estado. Si ya había descontado stock, se revierte.

7. **Gestión de stock**
   El administrador ingresa mercadería (reposición), ajusta por diferencias y consulta disponibilidad. El stock se descuenta por efecto de los pedidos.

8. **Consulta de histórico por cliente**
   Ver los pedidos pasados de un comercio (qué compra, cada cuánto, a qué precios). Es lectura; no introduce reglas nuevas.

---

## Bounded Contexts

Para una distribuidora chica el sistema puede vivir como **un solo bounded context** dividido en módulos. Se identifican igualmente las fronteras conceptuales por si el sistema crece:

- **Pedidos (CORE)** — Toma y ciclo de vida del pedido. Es el corazón del producto.
- **Catálogo y Precios** — Productos, unidades de venta, listas de precios.
- **Clientes** — Registro de comercios y su lista de precios asignada.
- **Inventario / Stock** — Disponibilidad actual por producto.
- **Identidad y Acceso (soporte)** — Distribuidora (tenant), usuarios y roles (preventista / administrador).

> Decisión de baseline: tratarlos como **módulos dentro de un mismo contexto**, no como microservicios ni contextos físicamente separados. Separar más adelante solo si aparece fricción real.

---

## Aggregates

> Cada aggregate referencia a otros **por identidad (Id)**, nunca por composición de objetos cruzando fronteras. Todos están scoped a una `DistribuidoraId`.

### Distribuidora (tenant)

Responsabilidad:
* Representar al suscriptor del SaaS y ser la frontera de aislamiento (tenant) de todos los demás aggregates.

Aggregate root:
* `Distribuidora`

Entidades internas:
* Ninguna en baseline (datos de la empresa son atributos/VOs).

Value Objects:
* `DatosDeContacto` (email, teléfono), `Direccion` (opcional).

Propiedades principales:
* Id, nombre, datos de contacto, estado.

Estados:
* `Activa`, `Suspendida` (p. ej. por falta de pago de la suscripción).

Reglas de negocio:
* El nombre/email de la distribuidora es único a nivel sistema.
* Una distribuidora suspendida no puede operar (no se toman pedidos).

Operaciones conceptuales:
* `registrar`, `suspender`, `reactivar`.

Relaciones con otros aggregates:
* Es la raíz de scoping: todo (`Usuario`, `Producto`, `Cliente`, `Pedido`, `Stock`) pertenece a una `DistribuidoraId`.

No incluir todavía:
* Planes/suscripción, facturación del SaaS, límites por plan.

---

### Usuario

Responsabilidad:
* Representar a quien opera el sistema dentro de una distribuidora, con su rol.

Aggregate root:
* `Usuario`

Entidades internas:
* Ninguna en baseline.

Value Objects:
* `Email`, `Rol` (`Administrador` | `Preventista`).

Propiedades principales:
* Id, DistribuidoraId, nombre, email, rol, estado.

Estados:
* `Activo`, `Inactivo`. (Invitación/onboarding de usuarios se deja fuera del baseline.)

Reglas de negocio:
* Email de usuario único a nivel sistema para permitir login sin identificar la distribuidora.
* El rol determina qué puede hacer (preventista: tomar pedidos propios; administrador: catálogo, precios, stock, usuarios y pedidos de la distribuidora).

Operaciones conceptuales:
* `crear`, `cambiarRol`, `desactivar`.

Relaciones con otros aggregates:
* Un `Pedido` referencia al `Usuario` que lo tomó o creó, por Id.

No incluir todavía:
* Autenticación, tokens, invitaciones por email, recuperación de contraseña (son detalle de aplicación/infra, no dominio).

---

### Producto

Responsabilidad:
* Representar un ítem del catálogo que la distribuidora vende, con su unidad de venta y precio base.

Aggregate root:
* `Producto`

Entidades internas:
* Ninguna en baseline.

Value Objects:
* `UnidadDeVenta` (una única unidad declarada por producto, sin conversiones en v1), `Money` (precio base), `Sku`/`Codigo` (opcional).

Propiedades principales:
* Id, DistribuidoraId, nombre, código/SKU (opcional), unidad de venta, precio base, estado.

Estados:
* `Activo`, `Discontinuado` (no se puede agregar a nuevos pedidos, pero queda en históricos).

Reglas de negocio:
* Precio base ≥ 0.
* Un producto discontinuado no puede agregarse a un pedido nuevo.
* Código/SKU único dentro de la distribuidora (si se usa).

Operaciones conceptuales:
* `crear`, `cambiarPrecioBase`, `discontinuar`, `reactivar`.

Relaciones con otros aggregates:
* Referenciado por `LineaDePedido`, por entradas de `ListaDePrecios` y por `Stock`, siempre por `ProductoId`.

No incluir todavía:
* Categorías/rubros, variantes, imágenes, proveedores, costos.

> Decisión vigente: el **stock NO es atributo de Producto**. Producto = catálogo/identidad; Stock = cantidad operativa.

---

### ListaDePrecios

Responsabilidad:
* Definir el precio de venta de los productos para un conjunto de clientes (precio "por cliente").

Aggregate root:
* `ListaDePrecios`

Entidades internas:
* `PrecioDeProducto` (productoId + precio). Modelable como entidad interna o colección de VOs; vive **dentro** de la lista.

Value Objects:
* `Money` (precio por producto), nombre de lista.

Propiedades principales:
* Id, DistribuidoraId, nombre, conjunto de `PrecioDeProducto`.

Estados:
* `Activa`, `Inactiva` (opcional en baseline).

Reglas de negocio:
* Cada producto aparece a lo sumo una vez en una lista.
* Precio ≥ 0.
* Si un producto no figura en la lista del cliente, se usa el precio base del producto (regla de fallback).

Operaciones conceptuales:
* `definirPrecio(producto, precio)`, `quitarProducto`, `renombrar`.

Relaciones con otros aggregates:
* Referencia productos por `ProductoId`.
* Un `Cliente` referencia su `ListaDePreciosId`.

No incluir todavía:
* Descuentos por volumen, vigencias/fechas, promociones, precios por moneda.

> Decisión vigente: una lista de precios es reutilizable y puede asignarse a varios clientes.

---

### Cliente (comercio)

Responsabilidad:
* Representar al comercio al que la distribuidora le vende y a quien se le toman pedidos.

Aggregate root:
* `Cliente`

Entidades internas:
* Ninguna en baseline.

Value Objects:
* `DatosDeContacto`, `Direccion`.

Propiedades principales:
* Id, DistribuidoraId, nombre/razón social, contacto, dirección, `ListaDePreciosId` asignada (opcional), estado.

Estados:
* `Activo`, `Inactivo`.

Reglas de negocio:
* Un cliente tiene a lo sumo una lista de precios asignada; si no tiene, aplica precio base.
* No se toman pedidos a un cliente inactivo.

Operaciones conceptuales:
* `crear`, `asignarListaDePrecios`, `actualizarContacto`, `desactivar`.

Relaciones con otros aggregates:
* Referencia `ListaDePreciosId`.
* Referenciado por `Pedido` vía `ClienteId`.

No incluir todavía:
* Cuenta corriente / saldo, límite de crédito, geolocalización, asignación de preventista por zona.

---

### Pedido (CORE)

Responsabilidad:
* Capturar lo que un cliente pide en una visita, con los precios correctos, y gobernar su ciclo de vida e invariantes. Es el aggregate más rico.

Aggregate root:
* `Pedido`

Entidades internas:
* `LineaDePedido` (una por producto pedido). Solo existe dentro del pedido; no se accede ni modifica fuera de su raíz.

Value Objects:
* `Cantidad` (cantidad + unidad), `Money` (precio unitario y subtotales), `EstadoPedido`.

Propiedades principales:
* Id, DistribuidoraId, ClienteId, PreventistaId (Usuario), fecha, estado, líneas, total.
* Por línea: ProductoId, descripción del producto (snapshot), `Cantidad`, precio unitario (snapshot al momento del pedido), subtotal.

Estados:
* `Borrador` → `Confirmado` → `Cancelado`.
* (Posibles futuros: `Preparado`, `Entregado` — fuera de baseline.)

Reglas de negocio (invariantes):
* Un pedido confirmado debe tener al menos una línea.
* Cantidad de cada línea > 0.
* El precio unitario de cada línea se **fija (snapshot)** desde la lista del cliente al agregar/confirmar; cambios posteriores de precio no alteran pedidos ya confirmados.
* El total es siempre la suma de los subtotales de las líneas (no se setea a mano).
* No se puede agregar un producto discontinuado.
* Solo se puede editar (agregar/quitar líneas, cambiar cantidades) en estado `Borrador`.
* Un pedido `Cancelado` no se puede reabrir ni modificar.
* No se toman pedidos para cliente inactivo ni distribuidora suspendida.

Operaciones conceptuales:
* `iniciar(cliente, preventista)`, `agregarLinea(producto, cantidad)`, `cambiarCantidad`, `quitarLinea`, `confirmar`, `cancelar`. (El total se recalcula internamente, no es una operación pública.)

Relaciones con otros aggregates:
* Referencia `ClienteId`, `PreventistaId`, y `ProductoId` por línea — todo por Id.
* Toma precios de la `ListaDePrecios` del cliente al construir las líneas (lectura; el resultado se snapshotea).
* Al confirmarse impacta `Stock` (vía aplicación, no composición directa).

No incluir todavía:
* Descuentos por pedido, observaciones por línea, entrega parcial, devoluciones, comprobante/factura, medios de pago.

> Decisión vigente: el stock se descuenta al **confirmar** el pedido.

---

### Stock

Responsabilidad:
* Conocer la disponibilidad de cada producto y aplicar la política de stock de la distribuidora.

Aggregate root:
* `Stock` (uno por producto dentro de la distribuidora).

Entidades internas:
* En baseline ninguna. En v1 se persiste saldo actual y ledger de movimientos.

Value Objects:
* `Cantidad`.

Propiedades principales:
* Id, DistribuidoraId, ProductoId, cantidad disponible.

Estados:
* No tiene estados explícitos en baseline (la cantidad es el dato relevante).

Reglas de negocio:
* Con la política predeterminada `RequireAvailableStock`, la cantidad disponible nunca es negativa.
* `AllowNegativeStock` puede habilitar stock negativo por decisión explícita de la distribuidora.
* Descontar más de lo disponible es inválido cuando rige `RequireAvailableStock`.

Operaciones conceptuales:
* `ingresar(cantidad)`, `descontar(cantidad)`, `ajustar(cantidadNueva)`.

Relaciones con otros aggregates:
* Referencia `ProductoId`.
* Es afectado por la confirmación/entrega de `Pedido` (coordinado en la capa de aplicación).

No incluir todavía:
* Múltiples depósitos/ubicaciones, lotes, vencimientos y reservas.

> Decisión vigente: `Stock` es un aggregate separado de `Producto`.

---

## Baseline mínimo recomendado

Lo mínimo para que el producto tenga sentido (toma de pedidos con precio correcto y stock real):

1. **Distribuidora + Usuario** con roles `Administrador` / `Preventista` y scoping por tenant.
2. **Producto** con precio base, unidad de venta y estado activo/discontinuado.
3. **ListaDePrecios** reutilizable y **Cliente** con lista asignada (con fallback a precio base).
4. **Pedido** rico: borrador → confirmado → cancelado, líneas con snapshot de precio, total calculado, invariantes.
5. **Stock** con disponibilidad y descuento al confirmar; por defecto no permite negativo, salvo configuración explícita `AllowNegativeStock`.
6. **Histórico de pedidos por cliente** (consulta de lectura, sin reglas nuevas).

Esto cubre las 4 funcionalidades core del MVP (toma de pedidos, stock real, listas de precio por cliente, histórico por cliente) sin agregar nada más.

---

## Qué queda fuera por ahora

- **Sincronización offline** del preventista (mencionada en el MVP). Es un requerimiento técnico/de aplicación importante, **no del dominio**; impacta cómo se generan identidades y se concilian pedidos, pero no las reglas de negocio. Marcado como riesgo alto a resolver en diseño técnico, no aquí.
- Conversión automática **WhatsApp → venta**.
- **Ruteo / gestión de zonas** de preventistas.
- **Integración con ERP**; export CSV simple podría entrar, integración no.
- **Cuenta corriente / cobranzas / saldo del cliente** (relevante para distribuidoras, pero no en v1 — ver duda).
- **Facturación / comprobantes / AFIP**.
- Entrega/logística, devoluciones, entregas parciales.
- Descuentos, promociones, precios con vigencia, multimoneda.
- Múltiples depósitos, lotes y vencimientos en stock.
- Autenticación, invitaciones de usuario, notificaciones (detalle técnico/infra).

---

## Decisiones pendientes

1. **Cuenta corriente:** ¿entra en una v1.x cercana? Es de las cosas que más piden las distribuidoras; definir si es baseline+1 o más lejos.
2. **Cliente-comercio como futuro actor:** ¿autoservicio de pedidos por parte del comercio en el futuro? Cambia el modelo de identidad/acceso si sí.
3. **Identidad de pedidos generados offline:** quién/cómo asigna el Id para evitar duplicados al sincronizar (decisión técnica, pero condiciona el dominio del `Pedido`).

---
*Generado: 2026-06-04 — base conceptual para que otro agente implemente. No es diseño técnico ni de datos.*
