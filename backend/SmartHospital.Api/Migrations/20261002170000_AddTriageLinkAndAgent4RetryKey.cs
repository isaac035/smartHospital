using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartHospital.Api.Data;

#nullable disable

namespace SmartHospital.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261002170000_AddTriageLinkAndAgent4RetryKey")]
public partial class AddTriageLinkAndAgent4RetryKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "ClientGenerationId", table: "AiMedicalReports", type: "uuid", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_AiMedicalReports_PatientId_ClientGenerationId", table: "AiMedicalReports", columns: new[] { "PatientId", "ClientGenerationId" }, unique: true, filter: "\"ClientGenerationId\" IS NOT NULL");

        migrationBuilder.CreateTable(
            name: "Agent1TriageResults",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                PatientId = table.Column<int>(type: "integer", nullable: false),
                AppointmentId = table.Column<int>(type: "integer", nullable: true),
                Symptoms = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Category = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                Priority = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                Confidence = table.Column<double>(type: "double precision", nullable: true),
                PossibleEmergency = table.Column<bool>(type: "boolean", nullable: false),
                EmergencyNotice = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                UsedDefaultCategory = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Agent1TriageResults", x => x.Id);
                table.ForeignKey("FK_Agent1TriageResults_Appointments_AppointmentId", x => x.AppointmentId, "Appointments", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_Agent1TriageResults_Users_PatientId", x => x.PatientId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "IX_Agent1TriageResults_AppointmentId", table: "Agent1TriageResults", column: "AppointmentId", unique: true, filter: "\"AppointmentId\" IS NOT NULL");
        migrationBuilder.CreateIndex(name: "IX_Agent1TriageResults_PatientId_CreatedAt", table: "Agent1TriageResults", columns: new[] { "PatientId", "CreatedAt" });
        migrationBuilder.CreateIndex(name: "IX_Agent1TriageResults_PatientId", table: "Agent1TriageResults", column: "PatientId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Agent1TriageResults");
        migrationBuilder.DropIndex(name: "IX_AiMedicalReports_PatientId_ClientGenerationId", table: "AiMedicalReports");
        migrationBuilder.DropColumn(name: "ClientGenerationId", table: "AiMedicalReports");
    }
}
