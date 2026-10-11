using Microsoft.EntityFrameworkCore;
using PortalDocente.Data.Entities;

namespace PortalDocente.Data;

public class PortalDbContext : DbContext
{
    public PortalDbContext(DbContextOptions<PortalDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Estudiante> Estudiantes => Set<Estudiante>();
    public DbSet<Docente> Docentes => Set<Docente>();
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<Cohorte> Cohortes => Set<Cohorte>();
    public DbSet<CursoEstudiante> CursosEstudiantes => Set<CursoEstudiante>();
    public DbSet<Escenario> Escenarios => Set<Escenario>();
    public DbSet<Nivel> Niveles => Set<Nivel>();
    public DbSet<Partida> Partidas => Set<Partida>();
    public DbSet<Kpi> Kpis => Set<Kpi>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Correo).HasColumnName("correo").HasMaxLength(320).IsRequired();
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
            e.Property(x => x.NombreCompleto).HasColumnName("nombre_completo").HasMaxLength(200).IsRequired();
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.UltimoAccesoEn).HasColumnName("ultimo_acceso_en");
            e.Property(x => x.CreadoEn).HasColumnName("creado_en");
            e.Property(x => x.ActualizadoEn).HasColumnName("actualizado_en");
            e.HasIndex(x => x.Correo).IsUnique();
        });

        modelBuilder.Entity<Estudiante>(e =>
        {
            e.ToTable("estudiantes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UsuarioId).HasColumnName("usuario_id");
            e.Property(x => x.CodigoInstitucional).HasColumnName("codigo_institucional").HasMaxLength(30);
            e.Property(x => x.CreadoEn).HasColumnName("creado_en");
            e.HasOne(x => x.Usuario).WithOne(x => x.Estudiante).HasForeignKey<Estudiante>(x => x.UsuarioId);
        });

        modelBuilder.Entity<Docente>(e =>
        {
            e.ToTable("docentes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UsuarioId).HasColumnName("usuario_id");
            e.Property(x => x.TituloAcademico).HasColumnName("titulo_academico").HasMaxLength(100);
            e.Property(x => x.CreadoEn).HasColumnName("creado_en");
            e.HasOne(x => x.Usuario).WithOne(x => x.Docente).HasForeignKey<Docente>(x => x.UsuarioId);
        });

        modelBuilder.Entity<Curso>(e =>
        {
            e.ToTable("cursos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(150).IsRequired();
            e.Property(x => x.PeriodoAcademico).HasColumnName("periodo_academico").HasMaxLength(20).IsRequired();
            e.Property(x => x.CreadoEn).HasColumnName("creado_en");
        });

        modelBuilder.Entity<Cohorte>(e =>
        {
            e.ToTable("cohortes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.CursoId).HasColumnName("curso_id");
            e.Property(x => x.DocenteId).HasColumnName("docente_id");
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
            e.Property(x => x.CreadoEn).HasColumnName("creado_en");
            e.HasOne(x => x.Curso).WithMany(x => x.Cohortes).HasForeignKey(x => x.CursoId);
            e.HasOne(x => x.Docente).WithMany().HasForeignKey(x => x.DocenteId);
        });

        modelBuilder.Entity<CursoEstudiante>(e =>
        {
            e.ToTable("cursos_estudiantes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.EstudianteId).HasColumnName("estudiante_id");
            e.Property(x => x.CohorteId).HasColumnName("cohorte_id");
            e.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(15);
            e.Property(x => x.MatriculadoEn).HasColumnName("matriculado_en");
            e.HasOne(x => x.Estudiante).WithMany(x => x.Matriculas).HasForeignKey(x => x.EstudianteId);
            e.HasOne(x => x.Cohorte).WithMany(x => x.Matriculas).HasForeignKey(x => x.CohorteId);
        });

        modelBuilder.Entity<Escenario>(e =>
        {
            e.ToTable("escenarios");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.CohorteId).HasColumnName("cohorte_id");
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
            e.Property(x => x.Dificultad).HasColumnName("dificultad").HasMaxLength(20);
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.CreadoEn).HasColumnName("creado_en");
            e.Property(x => x.ActualizadoEn).HasColumnName("actualizado_en");
            e.HasOne(x => x.Cohorte).WithMany(x => x.Escenarios).HasForeignKey(x => x.CohorteId);
        });

        modelBuilder.Entity<Nivel>(e =>
        {
            e.ToTable("niveles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.EscenarioId).HasColumnName("escenario_id");
            e.Property(x => x.Numero).HasColumnName("numero");
            e.Property(x => x.PresupuestoInicial).HasColumnName("presupuesto_inicial").HasPrecision(12, 2);
            e.HasOne(x => x.Escenario).WithMany(x => x.Niveles).HasForeignKey(x => x.EscenarioId);
        });

        modelBuilder.Entity<Partida>(e =>
        {
            e.ToTable("partidas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.EstudianteId).HasColumnName("estudiante_id");
            e.Property(x => x.NivelId).HasColumnName("nivel_id");
            e.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(64);
            e.Property(x => x.VersionRegistro).HasColumnName("version_registro");
            e.Property(x => x.OrigenRegistro).HasColumnName("origen_registro").HasMaxLength(20);
            e.Property(x => x.EstadoSincronizacion).HasColumnName("estado_sincronizacion").HasMaxLength(20);
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
            e.Property(x => x.FechaRecepcion).HasColumnName("fecha_recepcion");
            e.Property(x => x.Finalizada).HasColumnName("finalizada");
            e.Property(x => x.Eliminado).HasColumnName("eliminado");
            e.HasOne(x => x.Estudiante).WithMany(x => x.Partidas).HasForeignKey(x => x.EstudianteId);
            e.HasOne(x => x.Nivel).WithMany(x => x.Partidas).HasForeignKey(x => x.NivelId);
            e.HasQueryFilter(x => !x.Eliminado);
        });

        modelBuilder.Entity<Kpi>(e =>
        {
            e.ToTable("kpi");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PartidaId).HasColumnName("partida_id");
            e.Property(x => x.CostoTotal).HasColumnName("costo_total").HasPrecision(12, 2);
            e.Property(x => x.PctEntregasATiempo).HasColumnName("pct_entregas_a_tiempo").HasPrecision(5, 2);
            e.Property(x => x.UtilizacionRecursos).HasColumnName("utilizacion_recursos").HasPrecision(5, 2);
            e.Property(x => x.EficienciaRutas).HasColumnName("eficiencia_rutas").HasPrecision(5, 2);
            e.Property(x => x.PuntuacionGlobal).HasColumnName("puntuacion_global").HasPrecision(6, 2);
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
            e.HasOne(x => x.Partida).WithOne(x => x.Kpi).HasForeignKey<Kpi>(x => x.PartidaId);
            e.HasQueryFilter(x => !x.Partida.Eliminado);
        });
    }
}
