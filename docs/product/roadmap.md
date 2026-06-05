# Roadmap

> Horizonte tentativo por fases. No son fechas comprometidas; ordenan prioridades.
> El detalle accionable de cada fase vive en `backlog.md`.

## Fase 1 — Baseline (MVP)
**Objetivo:** tomar pedidos con precio correcto y stock real, multi-tenant.

- Distributor + User con roles (Administrator / SalesRep) y scoping por tenant.
- Catálogo: Product (precio base, unidad, estado).
- Precios: PriceList reutilizable + asignación a Customer (fallback a precio base).
- Order CORE: Draft → Confirmed → Cancelled, snapshot de precios, total calculado, invariantes.
- Stock con descuento al confirmar e invariante de no-negativo.
- Histórico de pedidos por cliente.
- Catálogos geográficos (Province / City).

Criterio de salida: ver "MVP terminado" en [mvp.md](mvp.md).

## Fase 1.x — Cercana (a definir prioridad)
Mejoras de alto valor para distribuidoras, sin cambiar el core:

- **Cuenta corriente / saldo del cliente** — muy pedida; definir si entra acá (ver `open-decisions.md` #7).
- **Export CSV** simple de pedidos/histórico.
- Filtros y paginación en consultas (pedidos, productos, clientes).
- Reportes básicos (ventas por período, productos más vendidos).

## Fase 2 — Movilidad y campo
- **Sincronización offline** del preventista. Riesgo técnico alto: condiciona identidad de pedidos
  generados offline (ver `open-decisions.md` #9). Requiere diseño técnico dedicado.
- App móvil del preventista pulida.

## Fase 3 — Logística y operaciones (exploratorio)
- Estados de pedido extendidos (`Prepared`, `Delivered`), entregas.
- Ruteo / gestión de zonas de preventistas.
- Stock avanzado: múltiples depósitos, lotes, vencimientos, reservas.

## Más adelante / no comprometido
- Conversión **WhatsApp → venta**.
- Integración con **ERP**.
- Facturación / comprobantes / AFIP.
- Descuentos, promociones, precios con vigencia, multimoneda.
- Cliente-comercio como actor (autoservicio de pedidos) — cambia el modelo de identidad (ver `open-decisions.md` #8).

---

> Regla: nada de fases posteriores se implementa "de paso" mientras se construye una fase anterior.
> Mantener el diff mínimo y el alcance acotado (ver `AGENTS.md`).
