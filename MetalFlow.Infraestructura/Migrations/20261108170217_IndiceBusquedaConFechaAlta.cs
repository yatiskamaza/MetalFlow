using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalFlow.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class IndiceBusquedaConFechaAlta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ITEM_BUSQUEDA_PARAMETRICA",
                table: "ITEM_INVENTARIO");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_BUSQUEDA_PARAMETRICA",
                table: "ITEM_INVENTARIO",
                columns: new[] { "Estado", "EspesorMm", "LargoMm", "AnchoMm" })
                .Annotation("SqlServer:Include", new[] { "MaterialId", "EmpresaId", "Cantidad", "FechaAlta" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ITEM_BUSQUEDA_PARAMETRICA",
                table: "ITEM_INVENTARIO");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_BUSQUEDA_PARAMETRICA",
                table: "ITEM_INVENTARIO",
                columns: new[] { "Estado", "EspesorMm", "LargoMm", "AnchoMm" })
                .Annotation("SqlServer:Include", new[] { "MaterialId", "EmpresaId", "Cantidad" });
        }
    }
}
