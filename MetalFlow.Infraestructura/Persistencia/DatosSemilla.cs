using Microsoft.EntityFrameworkCore;

namespace MetalFlow.Infraestructura.Persistencia;

/// <summary>
/// Datos de referencia incorporados en la migración inicial.
///
/// Comprende únicamente el catálogo normalizado de materiales (RD-03). La
/// cuenta del administrador no se aprovisiona por esta vía: su contraseña
/// constituye información sensible y, conforme al criterio expuesto en el
/// apartado 5.1, ningún secreto debe figurar en los artefactos versionados,
/// ni siquiera en forma derivada. Dicha cuenta se crea en el arranque de la
/// aplicación a partir de variables de entorno, según se describe en la clase
/// InicializadorDeDatos.
/// </summary>
internal static class DatosSemilla
{
    public static void Aplicar(ModelBuilder modelBuilder)
    {
        SembrarMateriales(modelBuilder);
    }

    /// <summary>
    /// Catálogo inicial de aleaciones conforme a la designación de la norma
    /// EN 10025-2 para aceros estructurales y a las designaciones habituales
    /// de inoxidables y aluminios en manufactura metálica.
    ///
    /// Su presencia es condición necesaria para el funcionamiento del proceso
    /// de ingesta: una tabla de normalización vacía rechazaría la totalidad de
    /// las filas de cualquier archivo procesado.
    /// </summary>
    private static void SembrarMateriales(ModelBuilder mb)
    {
        mb.Entity<Dominio.Entidades.Material>().HasData(
            new { Id = 1, Familia = "Acero al carbono", Designacion = "S235JR", Norma = "EN 10025-2" },
            new { Id = 2, Familia = "Acero al carbono", Designacion = "S275JR", Norma = "EN 10025-2" },
            new { Id = 3, Familia = "Acero al carbono", Designacion = "S355JR", Norma = "EN 10025-2" },
            new { Id = 4, Familia = "Acero inoxidable", Designacion = "AISI 304", Norma = "EN 10088-2" },
            new { Id = 5, Familia = "Acero inoxidable", Designacion = "AISI 316L", Norma = "EN 10088-2" },
            new { Id = 6, Familia = "Aluminio", Designacion = "EN AW-5754", Norma = "EN 485-2" },
            new { Id = 7, Familia = "Aluminio", Designacion = "EN AW-6082", Norma = "EN 485-2" }
        );
    }
}
