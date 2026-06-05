# Producto — DistriFlow

Documentación de **producto y negocio** de DistriFlow. Describe *qué* se construye y *por qué*.
No define implementación técnica (eso vive en `docs/architecture/`) ni reglas de dominio detalladas
(eso vive en `docs/domain/`).

> Idioma: estos documentos están en **español** (lenguaje del negocio). El código se escribe en
> **inglés**; los mensajes de error de la API, en español. Ver `glossary.md` para el mapeo de términos.

---

## Orden de lectura sugerido (para agentes)

1. **[vision.md](vision.md)** — problema, propuesta de valor, usuarios y no-objetivos.
2. **[glossary.md](glossary.md)** — lenguaje ubicuo español ↔ nombre en código (leer antes de programar).
3. **[mvp.md](mvp.md)** — alcance de la v1: qué entra y qué no.
4. **[requirements.md](requirements.md)** — requerimientos funcionales y no funcionales por módulo.
5. **[main-flows.md](main-flows.md)** — flujos principales del negocio, a nivel producto.
6. **[roadmap.md](roadmap.md)** — fases y versiones previstas.
7. **[backlog.md](backlog.md)** — épicas e historias priorizadas para implementar.
8. **[open-decisions.md](open-decisions.md)** — decisiones abiertas y ya tomadas. No resolver las abiertas sin acordar.

---

## Cómo encaja con el resto de la documentación

| Carpeta              | Responde a        | Idioma  | Estado |
|----------------------|-------------------|---------|--------|
| `AGENTS.md` (raíz)   | Reglas para agentes | EN    | Punto de entrada |
| `docs/domain/`       | Dominio conceptual (DDD) | ES | Canónico |
| `docs/product/`      | Producto y negocio | ES     | Este directorio |
| `docs/architecture/` | Convenciones técnicas | EN  | Cómo implementar |

Regla de precedencia ante conflicto: **dominio > producto > arquitectura** para *qué* hay que construir;
**arquitectura** manda en *cómo* se implementa. Si un documento de producto contradice al dominio,
gana el dominio y hay que corregir el de producto.

---

## Principios de producto

- **El CORE es la toma de pedidos.** Toda decisión de alcance prioriza que el preventista pueda
  cargar un pedido con el precio correcto del cliente y stock real.
- **Distribuidoras chicas primero.** Simplicidad sobre completitud. Nada de ERP, facturación ni logística en v1.
- **Multi-tenant desde el día uno.** Todo está scoped a una `Distributor`; los datos nunca se cruzan entre tenants.
- **No sobre-construir.** No se implementan features fuera del baseline documentado sin pedido explícito.
