# Glosario — Lenguaje ubicuo (Español ↔ Código en inglés)

> El dominio se documenta en **español** (lenguaje del negocio). El código se escribe en **inglés**.
> Esta tabla es la fuente de verdad para traducir cada término del dominio a su nombre en código.
> No inventar nombres nuevos: si un concepto del dominio aparece acá, usar exactamente este nombre en inglés.

## Aggregates

| Dominio (ES)        | Código (EN)   | Notas |
|---------------------|---------------|-------|
| Distribuidora       | `Distributor` | Tenant. Raíz de aislamiento; todo cuelga de `DistributorId`. |
| Usuario             | `User`        | Opera el sistema dentro de una distribuidora. |
| Producto            | `Product`     | Ítem del catálogo de la distribuidora. |
| Lista de precios    | `PriceList`   | Precios por producto, reutilizable entre clientes. |
| Cliente (comercio)  | `Customer`    | Comercio al que se le vende. No es usuario del sistema. |
| Pedido              | `Order`       | Aggregate CORE. Toma y ciclo de vida del pedido. |
| Stock               | `Stock`       | Disponibilidad por producto. Aggregate separado de `Product`. |

## Entidades y Value Objects

| Dominio (ES)         | Código (EN)     | Tipo | Notas |
|----------------------|-----------------|------|-------|
| Línea de pedido      | `OrderLine`     | Entidad interna de `Order` | No se accede fuera de su raíz. |
| Precio de producto   | `ProductPrice`  | Entidad/VO dentro de `PriceList` | productId + precio. |
| Datos de contacto    | `ContactInfo`   | Value Object | email, teléfono. |
| Dirección            | `Address`       | Value Object | street + `CityId` + zip code opcional; provincia derivada de ciudad. Opcional. |
| Unidad de venta      | `SaleUnit`      | Value Object | Una por producto; sin conversiones unidad↔bulto/caja en v1. |
| Precio / dinero      | `Money`         | Value Object | monto + (moneda futura). Precio ≥ 0. |
| Cantidad             | `Quantity`      | Value Object | cantidad + unidad. > 0 en líneas. |
| Código / SKU         | `Sku`           | Value Object | opcional, único por distribuidora. |

## Roles y estados

| Dominio (ES)    | Código (EN)     | Notas |
|-----------------|-----------------|-------|
| Rol             | `Role`          | Value Object. |
| Administrador   | `Administrator` | Gestiona catálogo, precios, stock, usuarios y puede operar pedidos de su distribuidora. |
| Preventista     | `SalesRep`      | Toma y opera sus pedidos en el campo. |
| Estado de pedido| `OrderStatus`   | Value Object. |
| Borrador        | `Draft`         | Estado de `Order`. Único estado editable. |
| Confirmado      | `Confirmed`     | Pedido fijado (snapshot de precios). |
| Cancelado       | `Cancelled`     | No se reabre ni modifica. |
| Activo          | `Active`        | Estado genérico. |
| Inactivo        | `Inactive`      | Estado genérico. |
| Discontinuado   | `Discontinued`  | Estado de `Product`. No se agrega a pedidos nuevos. |
| Suspendida      | `Suspended`     | Estado de `Distributor`. No puede operar. |

## Catálogos (geográficos)

| Dominio (ES) | Código (EN) | Notas |
|--------------|-------------|-------|
| Provincia    | `Province`  | Catálogo estático. |
| Ciudad / Localidad | `City` | Catálogo, pertenece a una provincia. |

## Operaciones del dominio (conceptual → método sugerido)

| Operación (ES)                | Método (EN, sugerido) |
|-------------------------------|-----------------------|
| iniciar pedido                | `Order.Create` / `Order.Start` |
| agregar línea                 | `Order.AddLine` |
| cambiar cantidad              | `Order.ChangeQuantity` |
| quitar línea                  | `Order.RemoveLine` |
| confirmar pedido              | `Order.Confirm` |
| cancelar pedido               | `Order.Cancel` |
| definir precio                | `PriceList.SetPrice` |
| asignar lista de precios      | `Customer.AssignPriceList` |
| ingresar stock                | `Stock.Increase` |
| descontar stock               | `Stock.Decrease` |
| ajustar stock                 | `Stock.Adjust` |
| cambiar precio base           | `Product.ChangeBasePrice` |
| discontinuar producto         | `Product.Discontinue` |

> Los nombres de método son sugerencias coherentes con el dominio. Antes de crear uno nuevo,
> buscar si ya existe un método equivalente en el aggregate y reutilizar el estilo existente.
