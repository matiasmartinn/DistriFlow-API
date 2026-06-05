# Visión

## El problema

Las **distribuidoras chicas** (bebidas, kiosco, almacén, limpieza, etc.) trabajan con un equipo de
**preventistas** que visitan comercios y levantan pedidos. Hoy ese proceso suele vivir en papel,
WhatsApp o planillas: el preventista no tiene a mano el precio que le corresponde a cada cliente,
el dueño no sabe el stock real al momento de vender, y el histórico de qué compra cada comercio se pierde.

Esto genera errores de precio, ventas de productos sin stock, y falta de visibilidad para reponer y vender mejor.

## La propuesta de valor

Un **SaaS multi-distribuidora** donde:

- El **preventista** arma el pedido frente al comercio con **el precio correcto de ese cliente**, valida
  cantidades y stock, y lo confirma. El pedido queda fijado (snapshot de precios) y entra al histórico.
- El **administrador/dueño** mantiene catálogo, listas de precios, stock y clientes, y revisa los pedidos.

El diferencial está en que la **toma de pedidos** es rica y correcta: precio por cliente, stock real,
e histórico por comercio. El resto del sistema existe para que ese flujo sea posible.

## Usuarios objetivo

- **Preventista** — Vendedor de campo. Toma pedidos para los comercios asignados. App probablemente móvil.
  No administra catálogo ni precios.
- **Administrador / Dueño** — Gestiona catálogo, listas de precios, stock, clientes y usuarios. Revisa pedidos.
  En distribuidoras muy chicas, una sola persona cumple ambos roles.
- **Cliente (comercio)** — Sujeto del dominio, **no** usuario del sistema en el baseline. Recibe la visita.

## Las 4 funcionalidades core

1. **Toma de pedidos** con precio correcto por cliente y estados de pedido.
2. **Stock real** que se descuenta al confirmar el pedido.
3. **Listas de precio por cliente** (reutilizables, con fallback a precio base).
4. **Histórico de pedidos por cliente**.

## Mercado y contexto

Orientado a **Latinoamérica, puntualmente Argentina** y zonas cercanas. Implica español como idioma de
cara al usuario y un modelo de direcciones con provincia/ciudad. No se busca competir en otros contextos
en esta etapa.

## No-objetivos (lo que DistriFlow NO es)

- No es un **ERP** ni un sistema de **facturación** (sin comprobantes, sin AFIP).
- No es un sistema de **logística/reparto** (sin ruteo, sin entregas parciales, sin devoluciones).
- No es **cuenta corriente / cobranzas** (sin saldos ni límites de crédito en v1).
- No hace **conversión automática de WhatsApp a venta**.
- No integra con ERPs externos (a lo sumo export CSV simple a futuro).

Ver alcance detallado en [mvp.md](mvp.md) y horizonte temporal en [roadmap.md](roadmap.md).
