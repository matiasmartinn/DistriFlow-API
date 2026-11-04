# Validación API

## Configuración y arranque

Requisitos: SDK .NET 9, PostgreSQL y Node.js para la suite HTTP.
Usar `appsettings.Example.json` para crear `src/DistriFlow.Api/appsettings.Development.json`.
Configurar conexión a una base de desarrollo, Jwt:Secret, emisor/audiencia y orígenes CORS.
No versionar configuración local ni secretos.

Ejecutar desde este repositorio:

```powershell
dotnet build
dotnet run --project verification/DistriFlow.Domain.Checks
dotnet run --project src/DistriFlow.Api --launch-profile http
```

En una base nueva aplicar migraciones con:

```powershell
dotnet ef database update --project src/DistriFlow.Infrastructure --startup-project src/DistriFlow.Api
```

La aplicación no aplica migraciones automáticamente. No ejecutar el comando contra una base ajena
o productiva por el solo hecho de hacer una validación.

## Integración

La suite requiere una API local que use PostgreSQL, no repositorios simulados:

```powershell
$env:STAGE3_API_URL = 'http://localhost:5106/api'
node scripts/verify-stage-three.mjs
```

El valor predeterminado de la suite es `http://localhost:5116/api`, puerto utilizado para las pruebas.
La suite rechaza hosts remotos, crea dos tenants sintéticos con prefijo `stage3-` por ejecución,
usuarios, clientes, listas, pedidos y 102 productos. No borra ni reutiliza datos previos.
El reporte `docs/validation/stage-three-api.json` contiene IDs/resultados, sin tokens ni contraseñas,
y se sobrescribe en cada ejecución. Está ignorado por Git.

La última ejecución de la etapa 3 pasó 18/18 escenarios sobre la base local existente.
Las ejecuciones exploratorias anteriores también dejaron datos sintéticos; no se eliminaron.
Se verificaron roles, tenant, stock, precios, concurrencia e idempotencia. No equivale a validación visual.
No se aplicaron migraciones sobre esa base: ya tenía la última migración.

## CI

`.github/workflows/check.yml` construye en Release, ejecuta 12 checks del dominio,
levanta PostgreSQL efímero, aplica las migraciones y ejecuta la suite HTTP.
Las credenciales incluidas en el workflow son exclusivamente para ese servicio efímero de CI.
El workflow no se ha ejecutado en GitHub durante esta sesión; comprobar su primer resultado al publicarlo.

Para auditoría ejecutar `dotnet list DistriFlow-API.sln package --vulnerable --include-transitive`.
Si hay advertencias NU1900 o fallos de conexión a NuGet, el mensaje final de ausencia de paquetes
vulnerables no acredita una auditoría completa: registrar el límite y repetir con acceso al origen.

## Archivos de las etapas 4 y 5

- `.github/workflows/check.yml`: CI con auditoría, build, checks y PostgreSQL efímero (nuevo).
- `.gitignore`: versionar guías mantenidas sin reportes locales.
- `README.md`: comandos y alcance.
- `verification/DistriFlow.Domain.Checks/DistriFlow.Domain.Checks.csproj`: ejecutable de regresiones (nuevo).
- `verification/DistriFlow.Domain.Checks/Program.cs`: 12 escenarios del dominio (nuevo).
- `docs/architecture/technical-foundations.md`: pruebas y geografía según implementación.
- `docs/architecture/technical-decisions.md`: fallback y ledger interno.
- `docs/product/mvp.md`: versiones de listas, snapshot y exclusiones.
- `docs/product/open-decisions.md`: decisiones D-03 y D-15 alineadas al backend y restricciones.
- `docs/validation/local-validation.md`: configuración y validación (nuevo).

El runner se ejecuta directamente y no altera la solución ni dependencias de los cuatro proyectos.
