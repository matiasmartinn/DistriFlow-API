# Backlog

> Épicas e historias priorizadas para construir la v1. Orden pensado para respetar **dependencias**:
> cada épica se apoya en las anteriores. Referencias `RF-xx` → `requirements.md`.
> Prioridad: **P0** = imprescindible para el MVP. Todo lo listado acá es P0 salvo aclaración o secciones marcadas como alcance futuro.

## Convención de historia
`Como <rol> quiero <objetivo> para <beneficio>` + criterios de aceptación clave.
El detalle de invariantes está en `docs/domain/`. El "cómo" técnico, en `docs/architecture/`.

---

## EPIC-0 — Fundaciones técnicas
Base compartida antes de las verticales de negocio.
- **H-0.1** DbContext de EF Core en Infrastructure y configuración de conexión.
- **H-0.2** `Repository<TEntity, TId>` genérico (GetById, GetAll, Add, Update, Remove, Exists). Ver `repositories.md`.
- **H-0.3** `BaseController` + `HandleResult` para mapear resultados de aplicación a HTTP. Ver `controllers.md`.
- **H-0.4** Patrón de resultado/errores con **mensajes en español** para el cliente (RNF-02).
- **H-0.5** Scoping multi-tenant: resolver `DistributorId` del contexto y aplicarlo a las consultas (RNF-01).

> Dependencia: todas las épicas siguientes dependen de EPIC-0.

## EPIC-1 — Catálogos geográficos (alcance futuro)
Vertical simple para normalizar direcciones; no bloquea el cierre del MVP operativo.
- **H-1.1** `Province`: GET /api/provinces y GET /api/provinces/{id:int} (RF-27).
- **H-1.2** `City`: GET /api/cities/by-province/{provinceId:int} (RF-28).
- **H-1.3** Seed de provincias/ciudades (datos estáticos / CSV).

## EPIC-2 — Identidad y acceso (Distributor + User)
- **H-2.1** Registrar `Distributor` con su `User` administrador (RF-01).
- **H-2.2** Suspender/reactivar `Distributor`; bloquear operación si está suspendida (RF-02).
- **H-2.3** Crear `User` con rol; email de usuario único a nivel sistema (RF-03).
- **H-2.4** Cambiar rol y desactivar `User` (RF-04).

## EPIC-3 — Catálogo de productos (Product)
- **H-3.1** Crear `Product` (nombre, unidad de venta, precio base ≥ 0, SKU opcional único) (RF-06).
- **H-3.2** Cambiar precio base (RF-07).
- **H-3.3** Discontinuar/reactivar `Product` (RF-08).
- **H-3.4** Listar/consultar productos del tenant (RF-09).

## EPIC-4 — Precios (PriceList)
- **H-4.1** Crear/renombrar `PriceList` (RF-10).
- **H-4.2** Definir/quitar precio de producto en lista (único por lista, ≥ 0) (RF-11).
- **H-4.3** Resolver precio para un cliente: lista del cliente o, si no figura, precio base (RF-13).

## EPIC-5 — Clientes (Customer)
- **H-5.1** Registrar `Customer` con contacto, dirección y `PriceList` opcional (RF-14).
- **H-5.2** Asignar/cambiar lista de precios del cliente (RF-12).
- **H-5.3** Actualizar contacto y desactivar `Customer` (RF-15).

## EPIC-6 — Pedidos (Order) — CORE
Aggregate más rico; usa command/query separation (`cqrs-style.md`).
- **H-6.1** Iniciar `Order` en `Draft` para un cliente. Puede operarlo `Administrator` o `SalesRep` de la misma distribuidora (RF-16).
- **H-6.2** Agregar línea con snapshot de precio desde la lista del cliente (RF-17).
- **H-6.3** Cambiar cantidad y quitar líneas en `Draft` (RF-18).
- **H-6.4** Confirmar pedido (≥ 1 línea, recalcula total, descuenta stock) (RF-19, RF-21).
- **H-6.5** Cancelar pedido (revierte stock si corresponde) (RF-20).
- **H-6.6** Bloqueos: cliente inactivo, distribuidora suspendida, producto discontinuado.

> Dependencia: requiere EPIC-3, EPIC-4, EPIC-5 y EPIC-7 (stock) para confirmar.

## EPIC-7 — Stock
- **H-7.1** Ingresar mercadería a un producto (RF-22).
- **H-7.2** Ajustar stock a cantidad nueva (RF-23).
- **H-7.3** Descontar al confirmar pedido; `RequireAvailableStock` impide negativo por defecto y `AllowNegativeStock` puede habilitarlo por distribuidora (RF-24).
- **H-7.4** Consultar disponibilidad por producto (RF-25).

## EPIC-8 — Histórico
- **H-8.1** Consultar pedidos por cliente, lectura, con filtros/paginación básicos (RF-26).

---

## Fuera del backlog v1
Cuenta corriente, offline, WhatsApp, ERP, facturación, logística, reportes avanzados,
catálogos geográficos y auditoría formal centralizada.
Ver [roadmap.md](roadmap.md) y [open-decisions.md](open-decisions.md).
