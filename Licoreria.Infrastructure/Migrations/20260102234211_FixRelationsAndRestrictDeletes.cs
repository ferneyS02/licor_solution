using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Licoreria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixRelationsAndRestrictDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DetallesOrden_Productos_IdProducto",
                table: "DetallesOrden");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesMesa_Jornadas_IdJornada",
                table: "OrdenesMesa");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesMesa_Mesas_IdMesa",
                table: "OrdenesMesa");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesMesa_Usuarios_IdUsuario",
                table: "OrdenesMesa");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Categorias_IdCategoria",
                table: "Productos");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Usuarios",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Usuarios",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioActual",
                table: "Productos",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Productos",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Imagen",
                table: "Productos",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TipoPago",
                table: "Pagos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "Recargo",
                table: "Pagos",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoFinal",
                table: "Pagos",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoBase",
                table: "Pagos",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Mesas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "Total",
                table: "DetallesOrden",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioUnitario",
                table: "DetallesOrden",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "NombreProducto",
                table: "DetallesOrden",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Categorias",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Nombre",
                table: "Usuarios",
                column: "Nombre");

            migrationBuilder.AddForeignKey(
                name: "FK_DetallesOrden_Productos_IdProducto",
                table: "DetallesOrden",
                column: "IdProducto",
                principalTable: "Productos",
                principalColumn: "IdProducto",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesMesa_Jornadas_IdJornada",
                table: "OrdenesMesa",
                column: "IdJornada",
                principalTable: "Jornadas",
                principalColumn: "IdJornada",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesMesa_Mesas_IdMesa",
                table: "OrdenesMesa",
                column: "IdMesa",
                principalTable: "Mesas",
                principalColumn: "IdMesa",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesMesa_Usuarios_IdUsuario",
                table: "OrdenesMesa",
                column: "IdUsuario",
                principalTable: "Usuarios",
                principalColumn: "IdUsuario",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Categorias_IdCategoria",
                table: "Productos",
                column: "IdCategoria",
                principalTable: "Categorias",
                principalColumn: "IdCategoria",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DetallesOrden_Productos_IdProducto",
                table: "DetallesOrden");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesMesa_Jornadas_IdJornada",
                table: "OrdenesMesa");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesMesa_Mesas_IdMesa",
                table: "OrdenesMesa");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesMesa_Usuarios_IdUsuario",
                table: "OrdenesMesa");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Categorias_IdCategoria",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Nombre",
                table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Usuarios",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Usuarios",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(60)",
                oldMaxLength: 60);

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioActual",
                table: "Productos",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Productos",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "Imagen",
                table: "Productos",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TipoPago",
                table: "Pagos",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "Recargo",
                table: "Pagos",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoFinal",
                table: "Pagos",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoBase",
                table: "Pagos",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Mesas",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "Total",
                table: "DetallesOrden",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioUnitario",
                table: "DetallesOrden",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "NombreProducto",
                table: "DetallesOrden",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Categorias",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80);

            migrationBuilder.AddForeignKey(
                name: "FK_DetallesOrden_Productos_IdProducto",
                table: "DetallesOrden",
                column: "IdProducto",
                principalTable: "Productos",
                principalColumn: "IdProducto",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesMesa_Jornadas_IdJornada",
                table: "OrdenesMesa",
                column: "IdJornada",
                principalTable: "Jornadas",
                principalColumn: "IdJornada",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesMesa_Mesas_IdMesa",
                table: "OrdenesMesa",
                column: "IdMesa",
                principalTable: "Mesas",
                principalColumn: "IdMesa",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesMesa_Usuarios_IdUsuario",
                table: "OrdenesMesa",
                column: "IdUsuario",
                principalTable: "Usuarios",
                principalColumn: "IdUsuario",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Categorias_IdCategoria",
                table: "Productos",
                column: "IdCategoria",
                principalTable: "Categorias",
                principalColumn: "IdCategoria",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
