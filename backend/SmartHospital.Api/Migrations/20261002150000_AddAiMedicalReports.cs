using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartHospital.Api.Data;

#nullable disable

namespace SmartHospital.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261002150000_AddAiMedicalReports")]
public partial class AddAiMedicalReports : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AiMedicalReports",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                PatientId = table.Column<int>(type: "integer", nullable: false),
                AppointmentId = table.Column<int>(type: "integer", nullable: true),
                VersionNumber = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ContentJson = table.Column<string>(type: "jsonb", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AiMedicalReports", x => x.Id);
                table.ForeignKey("FK_AiMedicalReports_Appointments_AppointmentId", x => x.AppointmentId, "Appointments", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_AiMedicalReports_Users_PatientId", x => x.PatientId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_AiMedicalReports_AppointmentId", "AiMedicalReports", "AppointmentId");
        migrationBuilder.CreateIndex("IX_AiMedicalReports_PatientId_CreatedAt", "AiMedicalReports", new[] { "PatientId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_AiMedicalReports_PatientId_VersionNumber", "AiMedicalReports", new[] { "PatientId", "VersionNumber" }, unique: true);
        migrationBuilder.CreateIndex("IX_AiMedicalReports_ReportId", "AiMedicalReports", "ReportId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "AiMedicalReports");
}
