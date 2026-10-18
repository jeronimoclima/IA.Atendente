using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IA.Atendente.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarHistoricoConversas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessoesAtendimento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentificadorCliente = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessoesAtendimento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessoesAtendimento_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mensagens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessaoAtendimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Conteudo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mensagens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mensagens_SessoesAtendimento_SessaoAtendimentoId",
                        column: x => x.SessaoAtendimentoId,
                        principalTable: "SessoesAtendimento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mensagens_SessaoAtendimentoId",
                table: "Mensagens",
                column: "SessaoAtendimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_SessoesAtendimento_TenantId",
                table: "SessoesAtendimento",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Mensagens");

            migrationBuilder.DropTable(
                name: "SessoesAtendimento");
        }
    }
}
