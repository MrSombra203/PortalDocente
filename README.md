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

En el primer comando, sustituye `******` por la contraseña de PostgreSQL de este entorno antes de ejecutarlo.
Si ya configuraste User Secrets en este equipo, basta iniciar el proyecto con F5 o ejecutar el segundo comando. No guardes credenciales en `appsettings*.json` ni las compartas en el repositorio.

En otros ambientes, configura `ConnectionStrings__PortalCentral` como variable de entorno. Si PostgreSQL no está disponible durante el inicio, el portal registra el error y permanece iniciado; una solicitud que requiera la base devuelve una página de error genérica. Sin cadena de conexión, el inicio falla con un error de configuración explícito.

Neon puede proporcionar su URI PostgreSQL como `DATABASE_URL`; el portal la convierte a una cadena compatible con Npgsql y conserva TLS y `channel_binding`. `ConnectionStrings__PortalCentral` tiene prioridad cuando se configura explícitamente. El archivo `.env.local` generado por `neon env pull` no se carga automáticamente en .NET: importa sus variables al entorno del proceso antes de iniciar el portal.

`GET /health/neon` comprueba conectividad con PostgreSQL, la disponibilidad del JWKS de Neon Auth y la función `hello` declarada en `neon.ts`. La autenticación de usuarios del portal continúa usando el esquema y las cookies existentes; el JWKS probe solo comprueba el servicio administrado y no cambia el flujo de inicio de sesión.

El proyecto Neon está configurado para PostgreSQL, Auth y Functions. AI Gateway se omitió porque el plan Free no permite provisionarlo.

## Despliegue en Render

El repositorio incluye un `Dockerfile` multi-stage para .NET 10, un Blueprint `render.yaml` y el endpoint público `/health/live` para comprobar que la aplicación inició. El contenedor escucha en el puerto `10000`, confía en los encabezados del proxy HTTPS de Render y ejecuta la aplicación con el usuario no privilegiado de la imagen oficial. El `.dockerignore` excluye `.env.local`, `.neon`, archivos de compilación y dependencias locales.

1. Publica este repositorio en GitHub y en Render crea un **Blueprint** desde el repositorio. Render detectará `render.yaml` y construirá el servicio web `portal-docente` con Docker en Ohio, cerca del proyecto Neon `us-east-2`.
2. Cuando Render solicite `DATABASE_URL`, copia el valor actual de `DATABASE_URL` de `.env.local` (URI PostgreSQL con el host `-pooler`) o de Neon Console → **Connect**. Pégalo directamente en el campo secreto de Render; no lo agregues a `render.yaml`, al repositorio ni al Dockerfile. Si configuras o actualizas el valor después de crear el Blueprint, hazlo en **Environment** y selecciona guardar y desplegar.
3. Espera a que el build y el chequeo `/health/live` tengan éxito. Render usa el plan gratuito para evitar cargos inesperados; los servicios gratuitos pueden suspenderse cuando están inactivos y tardar en despertar.

La instancia necesita `DATABASE_URL` de Neon para conectar. El portal conserva su autenticación local con cookies; activar el servicio Neon Auth no migra por sí solo el inicio de sesión de la aplicación.

## Datos y acceso

El login consulta `usuarios`, comprueba que la cuenta esté activa y que tenga exactamente un rol permitido en `usuarios_roles`: `admin`, `docente` o `estudiante`. Los docentes necesitan perfil en `docentes`; los estudiantes necesitan perfil en `estudiantes`, pero sus credenciales se rechazan explícitamente en este portal. Las contraseñas se guardan con BCrypt.

Los administradores ingresan únicamente a la gestión de usuarios en `/Admin/Usuarios`. Desde allí pueden crear, consultar, actualizar y eliminar cuentas, asignando un rol único. No pueden consultar la información académica del docente. Las cuentas con matrículas, partidas o cohortes relacionadas no se eliminan; el último administrador activo tampoco puede quedar deshabilitado o perder su rol. La aplicación verifica la cuenta y su rol actual al validar cada solicitud autenticada.

El dashboard, la búsqueda, la paginación y los perfiles consultan los registros de PostgreSQL. Los listados se filtran por cohortes activas asignadas al docente autenticado. Los estados vacíos reflejan que la consulta no encontró registros; no se sustituyen por datos ficticios.

La aplicación no crea ni modifica tablas ni ejecuta migraciones. El esquema se administra en PostgreSQL; no ejecutes `dotnet ef database update` para este proyecto. El portal es de solo lectura para la información académica.

## Pruebas

Los datos sintéticos y el proveedor EF Core InMemory se usan únicamente en el proyecto de pruebas; no forman parte de la aplicación. Ejecuta:

```powershell
dotnet test .\PortalDocente.slnx
```
