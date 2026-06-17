# Decisiones

> Consolida las decisiones de diseño del negocio. Las **tomadas** son la fuente de verdad y no se
> reabren sin acordarlo. Las **abiertas** NO deben resolverse unilateralmente por un agente:
> si una historia las toca, frenar y consultar.
>
> Fuentes: `docs/architecture/technical-decisions.md` (tomadas) y `docs/domain/` (pendientes).

## Decisiones ya tomadas (vigentes en v1)

| # | Decisión | Definición |
|---|----------|------------|
| D-01 | Momento de descuento de stock | Se descuenta **al confirmar** el pedido. |
| D-02 | Modelo de precios por cliente | **PriceList reutilizable** asignada a varios clientes (no precio individual). |
| D-03 | Fallback de precio | Si el producto no está en la lista del cliente, se usa el **precio base**. |
| D-04 | Stock vs Producto | `Stock` es **aggregate separado** de `Product`. |
| D-05 | Stock negativo | La política predeterminada `RequireAvailableStock` no permite stock negativo. `AllowNegativeStock` es una opción configurable por distribuidora. |
| D-06 | Cliente como usuario | El cliente-comercio **no es usuario** del sistema en el baseline. |
| D-07 | Offline | **No hay offline** en esta etapa. |
| D-08 | Facturación | **No hay facturación** ni comprobantes. |
| D-09 | Cuenta corriente | **No hay cuenta corriente** en v1 (saldo/crédito). |
| D-10 | WhatsApp | **No hay** conversión automática WhatsApp → venta. |
| D-11 | ERP | **No hay** integración con ERP. |
| D-12 | Logística | **No hay** logística (entrega, devoluciones, entregas parciales). |
| D-13 | Unidad de venta | Cada producto declara **una única `SaleUnit`**. Las cantidades usan decimal y no hay conversión unidad↔caja/bulto en v1. |
| D-14 | Dirección | `Address` guarda `Street`, `CityId` y `ZipCode` opcional. La provincia se obtiene desde la ciudad. |
| D-15 | Histórico de stock | En v1 se persiste saldo actual y ledger de movimientos de stock para entradas, ajustes, confirmación y cancelación de pedidos. |

> Estas decisiones acotan el alcance. No implementar nada que las contradiga sin pedido explícito.

## Decisiones abiertas (requieren acuerdo antes de implementar)

| # | Tema | Pregunta abierta | Impacto |
|---|------|------------------|---------|
| O-02 | Cuenta corriente futura | Decidido que NO en v1 (D-09). ¿Entra en una **v1.x** cercana? | Roadmap; es de lo más pedido por distribuidoras. |
| O-03 | Cliente como actor futuro | ¿Autoservicio de pedidos por el comercio más adelante? | Cambiaría el modelo de identidad/acceso. |
| O-04 | Identidad de pedidos offline | Cuándo se reactive offline (Fase 2): ¿quién/cómo asigna el Id para evitar duplicados al sincronizar? | Condiciona el dominio de `Order`. Diferido a Fase 2. |

## Cómo proceder ante una decisión abierta
1. No la resuelvas por tu cuenta dentro del código.
2. Implementá lo mínimo que NO comprometa la decisión (la opción más simple y reversible).
3. Dejá la pregunta marcada y consultá antes de avanzar si la historia depende de ella.
