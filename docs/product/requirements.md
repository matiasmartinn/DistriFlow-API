# Requerimientos

> Requerimientos a nivel producto. Las reglas detalladas e invariantes viven en `docs/domain/`.
> IDs estables para referenciar desde `backlog.md`. RF = funcional, RNF = no funcional.

## Requerimientos funcionales

### Identidad y acceso (Distributor / User)
- **RF-01** Registrar una distribuidora junto con su usuario administrador.
- **RF-02** Suspender y reactivar una distribuidora. Una distribuidora suspendida no puede operar.
- **RF-03** Crear usuarios con rol `Administrator` o `SalesRep`; email único dentro de la distribuidora.
- **RF-04** Cambiar el rol y desactivar usuarios.
- **RF-05** Todo dato y operación está scoped a la distribuidora del usuario autenticado.

### Catálogo (Product)
- **RF-06** Crear productos con nombre, unidad de venta, precio base (≥ 0) y SKU opcional (único si se usa).
- **RF-07** Cambiar el precio base de un producto.
- **RF-08** Discontinuar y reactivar un producto. Un producto discontinuado no se agrega a pedidos nuevos.
- **RF-09** Listar/consultar productos del catálogo de la distribuidora.

### Precios (PriceList)
- **RF-10** Crear y renombrar listas de precios.
- **RF-11** Definir/quitar el precio de un producto en una lista (cada producto a lo sumo una vez, precio ≥ 0).
- **RF-12** Asignar una lista de precios a un cliente.
- **RF-13** Resolver el precio de un producto para un cliente: precio de su lista; si no figura, precio base.

### Clientes (Customer)
- **RF-14** Registrar comercios con contacto, dirección y lista de precios opcional.
- **RF-15** Actualizar contacto y desactivar clientes. No se toman pedidos a un cliente inactivo.

### Pedidos (Order) — CORE
- **RF-16** Iniciar un pedido en `Draft` para un cliente, asociado al preventista que lo toma.
- **RF-17** Agregar líneas (producto + cantidad > 0) solo en `Draft`; el precio unitario se fija por snapshot.
- **RF-18** Cambiar cantidad y quitar líneas solo en `Draft`.
- **RF-19** Confirmar el pedido (requiere ≥ 1 línea); pasa a `Confirmed` y descuenta stock.
- **RF-20** Cancelar el pedido; si había descontado stock, se revierte.
- **RF-21** El total se recalcula como suma de subtotales; nunca se setea manualmente.

### Stock
- **RF-22** Ingresar mercadería (reposición) a un producto.
- **RF-23** Ajustar el stock a una cantidad nueva.
- **RF-24** Descontar stock al confirmar un pedido; nunca permitir negativo.
- **RF-25** Consultar disponibilidad por producto.

### Histórico
- **RF-26** Consultar los pedidos pasados de un cliente (lectura).

### Catálogos geográficos
- **RF-27** Listar provincias y obtener una por id.
- **RF-28** Listar ciudades filtradas por provincia.

## Requerimientos no funcionales

- **RNF-01 Multi-tenant** — Aislamiento estricto por `DistributorId`; ninguna consulta cruza tenants.
- **RNF-02 Idioma** — Código en inglés; mensajes de error/validación de la API en español.
- **RNF-03 Arquitectura** — Clean Architecture en capas (Domain, Application, Infrastructure, Api).
  Las dependencias solo apuntan hacia Domain. Ver `docs/architecture/`.
- **RNF-04 Consistencia** — Seguir los golden paths existentes; no introducir patrones nuevos sin pedido.
- **RNF-05 Persistencia** — EF Core en Infrastructure; las interfaces de repositorio viven en Domain.
- **RNF-06 Integridad del dominio** — Las invariantes se garantizan en el dominio, no en controllers.
- **RNF-07 Trazabilidad de precios** — Un pedido confirmado conserva el snapshot de precios; cambios
  posteriores de precio no alteran pedidos confirmados.
- **RNF-08 Preparado para móvil** — La API sirve a un cliente probablemente móvil para el preventista
  (offline queda fuera de v1, ver `open-decisions.md`).
