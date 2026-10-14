# Portal Docente UDLA

Portal de consulta para docentes: resumen de actividad, búsqueda y seguimiento de estudiantes, resultados KPI e historial de partidas. Toda la información funcional se consulta desde PostgreSQL. El portal no incluye administración de cursos ni operaciones para crear, editar o eliminar registros.

## Requisitos y ejecución

- .NET SDK 10
- PostgreSQL con la base `portal_docente` y el esquema existente del sistema

En desarrollo, `appsettings.Development.json` configura el host local, puerto, base y usuario. Guarda la contraseña únicamente en User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:PortalCentral" "Host=localhost;Port=5432;Database=portal_docente;Username=postgres;Password=<tu-contraseña>" --project .\PortalDocente\PortalDocente.csproj
dotnet run --project .\PortalDocente\PortalDocente.csproj
```

Si ya configuraste User Secrets en este equipo, basta iniciar el proyecto con F5 o ejecutar el segundo comando. No guardes credenciales en `appsettings*.json` ni las compartas en el repositorio.

En otros ambientes, configura `ConnectionStrings__PortalCentral` como variable de entorno. Si PostgreSQL no está disponible durante el inicio, el portal registra el error y permanece iniciado; una solicitud que requiera la base devuelve una página de error genérica. Sin cadena de conexión, el inicio falla con un error de configuración explícito.

## Datos y acceso

El login consulta `usuarios`, comprueba que la cuenta esté activa, que tenga perfil en `docentes` y que `usuarios_roles` la vincule con el rol `docente` existente en `roles`. La contraseña se verifica con BCrypt; el portal no crea ni restablece credenciales.

El dashboard, la búsqueda, la paginación y los perfiles consultan los registros de PostgreSQL. Los listados se filtran por cohortes activas asignadas al docente autenticado. Los estados vacíos reflejan que la consulta no encontró registros; no se sustituyen por datos ficticios.

La aplicación no crea ni modifica tablas ni ejecuta migraciones. El esquema se administra en PostgreSQL; no ejecutes `dotnet ef database update` para este proyecto. El portal es de solo lectura para la información académica.

## Pruebas

Los datos sintéticos y el proveedor EF Core InMemory se usan únicamente en el proyecto de pruebas; no forman parte de la aplicación. Ejecuta:

```powershell
dotnet test .\PortalDocente.slnx
```
