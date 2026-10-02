using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartHospital.Api.Data;

#nullable disable

namespace SmartHospital.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261002180000_AddMissingEmrClinicalTables")]
public partial class AddMissingEmrClinicalTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ClinicalDiagnoses",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                MedicalRecordId = table.Column<int>(type: "integer", nullable: false),
                PatientId = table.Column<int>(type: "integer", nullable: false),
                DoctorId = table.Column<int>(type: "integer", nullable: false),
                Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                Severity = table.Column<int>(type: "integer", nullable: false),
                Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                DiagnosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClinicalDiagnoses", x => x.Id);
                table.ForeignKey("FK_ClinicalDiagnoses_MedicalRecords_MedicalRecordId", x => x.MedicalRecordId, "MedicalRecords", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_ClinicalDiagnoses_Users_DoctorId", x => x.DoctorId, "Users", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ClinicalDiagnoses_Users_PatientId", x => x.PatientId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ClinicalTreatmentPlans",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                MedicalRecordId = table.Column<int>(type: "integer", nullable: false),
                PatientId = table.Column<int>(type: "integer", nullable: false),
                DoctorId = table.Column<int>(type: "integer", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Category = table.Column<int>(type: "integer", nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Goals = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                Interventions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                TargetDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ReviewDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClinicalTreatmentPlans", x => x.Id);
                table.ForeignKey("FK_ClinicalTreatmentPlans_MedicalRecords_MedicalRecordId", x => x.MedicalRecordId, "MedicalRecords", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_ClinicalTreatmentPlans_Users_DoctorId", x => x.DoctorId, "Users", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ClinicalTreatmentPlans_Users_PatientId", x => x.PatientId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "EmrAuditLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<int>(type: "integer", nullable: false),
                Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                EntityId = table.Column<int>(type: "integer", nullable: true),
                PatientId = table.Column<int>(type: "integer", nullable: true),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Metadata = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                IsSuccess = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmrAuditLogs", x => x.Id);
                table.ForeignKey("FK_EmrAuditLogs_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "MedicalRecordVersions",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                MedicalRecordId = table.Column<int>(type: "integer", nullable: false),
                VersionNumber = table.Column<int>(type: "integer", nullable: false),
                ChangedByUserId = table.Column<int>(type: "integer", nullable: false),
                ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ChangeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                ChangeSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                ChiefComplaint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Symptoms = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                ExaminationNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Diagnosis = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                TreatmentPlan = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                FollowUpDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                PreviousChiefComplaint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                PreviousSymptoms = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                PreviousExaminationNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                PreviousDiagnosis = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                PreviousTreatmentPlan = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                PreviousFollowUpDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MedicalRecordVersions", x => x.Id);
                table.ForeignKey("FK_MedicalRecordVersions_MedicalRecords_MedicalRecordId", x => x.MedicalRecordId, "MedicalRecords", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_MedicalRecordVersions_Users_ChangedByUserId", x => x.ChangedByUserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_ClinicalDiagnoses_DoctorId", "ClinicalDiagnoses", "DoctorId");
        migrationBuilder.CreateIndex("IX_ClinicalDiagnoses_MedicalRecordId", "ClinicalDiagnoses", "MedicalRecordId");
        migrationBuilder.CreateIndex("IX_ClinicalDiagnoses_PatientId", "ClinicalDiagnoses", "PatientId");
        migrationBuilder.CreateIndex("IX_ClinicalTreatmentPlans_DoctorId", "ClinicalTreatmentPlans", "DoctorId");
        migrationBuilder.CreateIndex("IX_ClinicalTreatmentPlans_MedicalRecordId", "ClinicalTreatmentPlans", "MedicalRecordId");
        migrationBuilder.CreateIndex("IX_ClinicalTreatmentPlans_PatientId", "ClinicalTreatmentPlans", "PatientId");
        migrationBuilder.CreateIndex("IX_EmrAuditLogs_EntityType_EntityId", "EmrAuditLogs", new[] { "EntityType", "EntityId" });
        migrationBuilder.CreateIndex("IX_EmrAuditLogs_PatientId", "EmrAuditLogs", "PatientId");
        migrationBuilder.CreateIndex("IX_EmrAuditLogs_Timestamp", "EmrAuditLogs", "Timestamp");
        migrationBuilder.CreateIndex("IX_EmrAuditLogs_UserId", "EmrAuditLogs", "UserId");
        migrationBuilder.CreateIndex("IX_MedicalRecordVersions_ChangedByUserId", "MedicalRecordVersions", "ChangedByUserId");
        migrationBuilder.CreateIndex("IX_MedicalRecordVersions_MedicalRecordId_VersionNumber", "MedicalRecordVersions", new[] { "MedicalRecordId", "VersionNumber" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ClinicalDiagnoses");
        migrationBuilder.DropTable(name: "ClinicalTreatmentPlans");
        migrationBuilder.DropTable(name: "EmrAuditLogs");
        migrationBuilder.DropTable(name: "MedicalRecordVersions");
    }
}
