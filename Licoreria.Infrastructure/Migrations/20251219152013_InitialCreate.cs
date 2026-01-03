using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Licoreria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    IdCategoria = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.IdCategoria);
                });

            migrationBuilder.CreateTable(
                name: "Jornadas",
                columns: table => new
                {
                    IdJornada = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FechaJornada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaHoraInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaHoraFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsuarioInicio = table.Column<int>(type: "integer", nullable: false),
                    UsuarioCierre = table.Column<int>(type: "integer", nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jornadas", x => x.IdJornada);
                });

            migrationBuilder.CreateTable(
                name: "Mesas",
                columns: table => new
                {
                    IdMesa = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mesas", x => x.IdMesa);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.IdUsuario);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    IdProducto = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    PrecioActual = table.Column<decimal>(type: "numeric", nullable: false),
                    Stock = table.Column<int>(type: "integer", nullable: false),
                    Imagen = table.Column<string>(type: "text", nullable: true),
                    IdCategoria = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.IdProducto);
                    table.ForeignKey(
                        name: "FK_Productos_Categorias_IdCategoria",
                        column: x => x.IdCategoria,
                        principalTable: "Categorias",
                        principalColumn: "IdCategoria",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrdenesMesa",
                columns: table => new
                {
                    IdOrden = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdMesa = table.Column<int>(type: "integer", nullable: false),
                    IdJornada = table.Column<int>(type: "integer", nullable: false),
                    FechaHoraInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaHoraCierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TipoPago = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesMesa", x => x.IdOrden);
                    table.ForeignKey(
                        name: "FK_OrdenesMesa_Jornadas_IdJornada",
                        column: x => x.IdJornada,
                        principalTable: "Jornadas",
                        principalColumn: "IdJornada",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrdenesMesa_Mesas_IdMesa",
                        column: x => x.IdMesa,
                        principalTable: "Mesas",
                        principalColumn: "IdMesa",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrdenesMesa_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetallesOrden",
                columns: table => new
                {
                    IdDetalle = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdOrden = table.Column<int>(type: "integer", nullable: false),
                    IdProducto = table.Column<int>(type: "integer", nullable: false),
                    NombreProducto = table.Column<string>(type: "text", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Total = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesOrden", x => x.IdDetalle);
                    table.ForeignKey(
                        name: "FK_DetallesOrden_OrdenesMesa_IdOrden",
                        column: x => x.IdOrden,
                        principalTable: "OrdenesMesa",
                        principalColumn: "IdOrden",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesOrden_Productos_IdProducto",
                        column: x => x.IdProducto,
                        principalTable: "Productos",
                        principalColumn: "IdProducto",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    IdPago = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdOrden = table.Column<int>(type: "integer", nullable: false),
                    MontoBase = table.Column<decimal>(type: "numeric", nullable: false),
                    TipoPago = table.Column<string>(type: "text", nullable: false),
                    Recargo = table.Column<decimal>(type: "numeric", nullable: false),
                    MontoFinal = table.Column<decimal>(type: "numeric", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.IdPago);
                    table.ForeignKey(
                        name: "FK_Pagos_OrdenesMesa_IdOrden",
                        column: x => x.IdOrden,
                        principalTable: "OrdenesMesa",
                        principalColumn: "IdOrden",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesOrden_IdOrden",
                table: "DetallesOrden",
                column: "IdOrden");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesOrden_IdProducto",
                table: "DetallesOrden",
                column: "IdProducto");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesMesa_IdJornada",
                table: "OrdenesMesa",
                column: "IdJornada");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesMesa_IdMesa",
                table: "OrdenesMesa",
                column: "IdMesa");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesMesa_IdUsuario",
                table: "OrdenesMesa",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_IdOrden",
                table: "Pagos",
                column: "IdOrden");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_IdCategoria",
                table: "Productos",
                column: "IdCategoria");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesOrden");

            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "OrdenesMesa");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "Jornadas");

            migrationBuilder.DropTable(
                name: "Mesas");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
