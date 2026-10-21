using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MetalFlow.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class MigracionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EMPRESA",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nif = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    RazonSocial = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Localidad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmailContacto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TelefonoContacto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Verificada = table.Column<bool>(type: "bit", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMPRESA", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MATERIAL",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Familia = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Designacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Norma = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MATERIAL", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CARGA_INVENTARIO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreArchivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FechaCarga = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FilasTotales = table.Column<int>(type: "int", nullable: false),
                    FilasValidas = table.Column<int>(type: "int", nullable: false),
                    FilasRechazadas = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CARGA_INVENTARIO", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CARGA_INVENTARIO_EMPRESA_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "EMPRESA",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "USUARIO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIO", x => x.Id);
                    table.ForeignKey(
                        name: "FK_USUARIO_EMPRESA_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "EMPRESA",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "INCIDENCIA_CARGA",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CargaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroFila = table.Column<int>(type: "int", nullable: false),
                    Campo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValorOriginal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TipoError = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INCIDENCIA_CARGA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_INCIDENCIA_CARGA_CARGA_INVENTARIO_CargaId",
                        column: x => x.CargaId,
                        principalTable: "CARGA_INVENTARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ITEM_INVENTARIO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    CargaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LargoMm = table.Column<decimal>(type: "decimal(9,2)", nullable: false),
                    AnchoMm = table.Column<decimal>(type: "decimal(9,2)", nullable: false),
                    EspesorMm = table.Column<decimal>(type: "decimal(7,2)", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PesoKg = table.Column<decimal>(type: "decimal(9,2)", nullable: true),
                    Ubicacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Estado = table.Column<byte>(type: "tinyint", nullable: false),
                    Clasificacion = table.Column<byte>(type: "tinyint", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ITEM_INVENTARIO", x => x.Id);
                    table.CheckConstraint("CK_ITEM_CANTIDAD", "[Cantidad] > 0");
                    table.CheckConstraint("CK_ITEM_DIMENSIONES", "[LargoMm] > 0 AND [AnchoMm] > 0 AND [EspesorMm] > 0");
                    table.ForeignKey(
                        name: "FK_ITEM_INVENTARIO_CARGA_INVENTARIO_CargaId",
                        column: x => x.CargaId,
                        principalTable: "CARGA_INVENTARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ITEM_INVENTARIO_EMPRESA_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "EMPRESA",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ITEM_INVENTARIO_MATERIAL_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "MATERIAL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "USUARIO_ROL",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rol = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIO_ROL", x => new { x.UsuarioId, x.Rol });
                    table.ForeignKey(
                        name: "FK_USUARIO_ROL_USUARIO_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "USUARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ANUNCIO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Precio = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    FechaPublicacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ANUNCIO", x => x.Id);
                    table.CheckConstraint("CK_ANUNCIO_PRECIO", "[Precio] >= 0");
                    table.ForeignKey(
                        name: "FK_ANUNCIO_ITEM_INVENTARIO_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ITEM_INVENTARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RESERVA",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaCompradoraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaReserva = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RESERVA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RESERVA_EMPRESA_EmpresaCompradoraId",
                        column: x => x.EmpresaCompradoraId,
                        principalTable: "EMPRESA",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RESERVA_ITEM_INVENTARIO_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ITEM_INVENTARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "MATERIAL",
                columns: new[] { "Id", "Designacion", "Familia", "Norma" },
                values: new object[,]
                {
                    { 1, "S235JR", "Acero al carbono", "EN 10025-2" },
                    { 2, "S275JR", "Acero al carbono", "EN 10025-2" },
                    { 3, "S355JR", "Acero al carbono", "EN 10025-2" },
                    { 4, "AISI 304", "Acero inoxidable", "EN 10088-2" },
                    { 5, "AISI 316L", "Acero inoxidable", "EN 10088-2" },
                    { 6, "EN AW-5754", "Aluminio", "EN 485-2" },
                    { 7, "EN AW-6082", "Aluminio", "EN 485-2" }
                });

            migrationBuilder.InsertData(
                table: "USUARIO",
                columns: new[] { "Id", "Activo", "Email", "empresa_id", "FechaAlta", "Nombre", "PasswordHash" },
                values: new object[] { new Guid("0f8fad5b-d9cb-469f-a165-70867728950e"), true, "admin@metalflow.local", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Administrador del sistema", "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy" });

            migrationBuilder.InsertData(
                table: "USUARIO_ROL",
                columns: new[] { "Rol", "UsuarioId" },
                values: new object[] { (byte)3, new Guid("0f8fad5b-d9cb-469f-a165-70867728950e") });

            migrationBuilder.CreateIndex(
                name: "UQ_ANUNCIO_ITEM",
                table: "ANUNCIO",
                column: "ItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CARGA_INVENTARIO_EmpresaId",
                table: "CARGA_INVENTARIO",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "UQ_EMPRESA_NIF",
                table: "EMPRESA",
                column: "Nif",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_INCIDENCIA_CARGA",
                table: "INCIDENCIA_CARGA",
                column: "CargaId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_BUSQUEDA_PARAMETRICA",
                table: "ITEM_INVENTARIO",
                columns: new[] { "Estado", "EspesorMm", "LargoMm", "AnchoMm" })
                .Annotation("SqlServer:Include", new[] { "MaterialId", "EmpresaId", "Cantidad" });

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_EMPRESA",
                table: "ITEM_INVENTARIO",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_INVENTARIO_CargaId",
                table: "ITEM_INVENTARIO",
                column: "CargaId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_MATERIAL",
                table: "ITEM_INVENTARIO",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "UQ_MATERIAL_DESIGNACION",
                table: "MATERIAL",
                column: "Designacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RESERVA_EmpresaCompradoraId",
                table: "RESERVA",
                column: "EmpresaCompradoraId");

            migrationBuilder.CreateIndex(
                name: "IX_RESERVA_ITEM_ESTADO",
                table: "RESERVA",
                columns: new[] { "ItemId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "UQ_USUARIO_EMAIL",
                table: "USUARIO",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_USUARIO_EMPRESA",
                table: "USUARIO",
                column: "empresa_id",
                unique: true,
                filter: "[empresa_id] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ANUNCIO");

            migrationBuilder.DropTable(
                name: "INCIDENCIA_CARGA");

            migrationBuilder.DropTable(
                name: "RESERVA");

            migrationBuilder.DropTable(
                name: "USUARIO_ROL");

            migrationBuilder.DropTable(
                name: "ITEM_INVENTARIO");

            migrationBuilder.DropTable(
                name: "USUARIO");

            migrationBuilder.DropTable(
                name: "CARGA_INVENTARIO");

            migrationBuilder.DropTable(
                name: "MATERIAL");

            migrationBuilder.DropTable(
                name: "EMPRESA");
        }
    }
}
