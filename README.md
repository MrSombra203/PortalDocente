# Portal Docente UDLA

Demo de solo lectura para que el docente consulte el progreso de estudiantes: resumen de actividad, búsqueda por estudiante, resultados KPI e historial de partidas. No administra cursos ni escenarios y no modifica resultados.

## Ejecutar la demo

Requiere el SDK de .NET 10.

```powershell
dotnet run --project .\PortalDocente\PortalDocente.csproj
```

En modo demo, la aplicación carga datos ficticios en una base EF Core InMemory. Acceso:

- Correo: `docente.demo@udla.edu.ec`
- Contraseña: `DemoPortal2026!`

La interfaz identifica los datos como demostrativos y **no muestra la contraseña en pantalla**. Estas credenciales no deben usarse en un entorno real. Preferible configurar la cadena PostgreSQL con User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:PortalCentral" "Host=localhost;Port=5432;Database=portal_docente;Username=<usuario>;Password=<contraseña>" --project .\PortalDocente\PortalDocente.csproj
```

## Conectar la base central

El portal lee el esquema PostgreSQL central del proyecto. Antes de iniciar, aplica el script **Base potal web.sql** a la base `portal_docente` y configura variables de entorno; no guardes credenciales en `appsettings*.json`.

```powershell
$env:Demo__UseInMemoryData = "false"
$env:ConnectionStrings__PortalCentral = "Host=localhost;Port=5432;Database=portal_docente;Username=<usuario>;Password=<contraseña>"
dotnet run --project .\PortalDocente\PortalDocente.csproj
```

`Base de datos local.sql` corresponde al almacenamiento offline del cliente/simulador. El portal no se conecta directamente a esa base: consulta los resultados que ya fueron sincronizados al esquema central.