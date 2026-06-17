# Flujos principales

> Flujos de negocio a nivel producto (sin pasos técnicos ni endpoints). Derivados de `docs/domain/`.
> Términos en español del negocio; ver `glossary.md` para el nombre en código.

## 1. Onboarding de distribuidora
Se registra una **Distributor** con su **User** administrador. A partir de ahí todo queda scoped a esa
distribuidora. (RF-01)

## 2. Gestión de catálogo y precios
El administrador da de alta **Products** (precio base + unidad de venta), arma **PriceLists** y asigna a
cada **Customer** una lista (o usa precio base si no tiene). (RF-06, RF-10, RF-12)

## 3. Alta de clientes-comercio
El administrador o el preventista registra los comercios con contacto, dirección y lista de precios asignada. (RF-14)

## 4. Toma de pedido (CORE)
Un `SalesRep` o `Administrator` de la distribuidora:
1. Inicia un **Order** en `Draft` para el cliente.
2. Agrega líneas (producto + cantidad). El **precio sale de la lista del cliente** y se fija por snapshot.
3. Mientras está en `Draft` puede modificar líneas y cantidades.

Reglas: cantidad > 0; no agregar productos discontinuados; no tomar pedidos a cliente inactivo ni
distribuidora suspendida. (RF-16 a RF-18)

## 5. Confirmación del pedido
El pedido pasa de `Draft` a `Confirmed`: queda fijado (snapshot de precios y cantidades), entra al
histórico del cliente y **descuenta stock**. Requiere al menos una línea. (RF-19, RF-24)

## 6. Cancelación del pedido
Un pedido puede cancelarse según su estado. Si ya había descontado stock, **se revierte**. Un pedido
`Cancelled` no se reabre ni modifica. (RF-20)

## 7. Gestión de stock
El administrador ingresa mercadería (reposición), ajusta por diferencias y consulta disponibilidad.
El stock se descuenta por efecto de los pedidos confirmados. La política predeterminada
`RequireAvailableStock` impide stock negativo; `AllowNegativeStock` puede habilitarlo por distribuidora. (RF-22 a RF-25)

## 8. Consulta de histórico por cliente
Ver los pedidos pasados de un comercio (qué compra, cada cuánto, a qué precios). Es lectura, sin reglas nuevas. (RF-26)

---

## Diagrama de estados — Order

```
        agregar/quitar líneas, cambiar cantidad
        ┌───────────────────────────────┐
        ▼                               │
   ┌─────────┐   confirmar        ┌───────────┐
   │  Draft  │ ─────────────────► │ Confirmed │
   └─────────┘                    └───────────┘
        │                               │
        │ cancelar                      │ cancelar (revierte stock)
        ▼                               ▼
                  ┌───────────┐
                  │ Cancelled │   (terminal — no se reabre)
                  └───────────┘
```

Estados futuros posibles (fuera de v1): `Prepared`, `Delivered`.
