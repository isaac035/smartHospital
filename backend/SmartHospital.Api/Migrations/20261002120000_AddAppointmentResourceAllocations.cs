using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartHospital.Api.Data;

#nullable disable

namespace SmartHospital.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261002120000_AddAppointmentResourceAllocations")]
public partial class AddAppointmentResourceAllocations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AppointmentResourceAllocations",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                AppointmentId = table.Column<int>(type: "integer", nullable: false),
                BedId = table.Column<int>(type: "integer", nullable: true),
                MedicalResourceId = table.Column<int>(type: "integer", nullable: true),
                PatientId = table.Column<int>(type: "integer", nullable: false),
                DoctorId = table.Column<int>(type: "integer", nullable: false),
                DepartmentId = table.Column<int>(type: "integer", nullable: true),
                ClinicalSpecialty = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Priority = table.Column<int>(type: "integer", nullable: false),
                AllocatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AppointmentResourceAllocations", x => x.Id);
                table.CheckConstraint("CK_AppointmentResourceAllocation_OneResource", "(\"BedId\" IS NOT NULL AND \"MedicalResourceId\" IS NULL) OR (\"BedId\" IS NULL AND \"MedicalResourceId\" IS NOT NULL)");
                table.ForeignKey("FK_AppointmentResourceAllocations_Appointments_AppointmentId", x => x.AppointmentId, "Appointments", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AppointmentResourceAllocations_Beds_BedId", x => x.BedId, "Beds", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AppointmentResourceAllocations_MedicalResources_MedicalResourceId", x => x.MedicalResourceId, "MedicalResources", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AppointmentResourceAllocations_Users_PatientId", x => x.PatientId, "Users", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AppointmentResourceAllocations_Users_DoctorId", x => x.DoctorId, "Users", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AppointmentResourceAllocations_Departments_DepartmentId", x => x.DepartmentId, "Departments", "Id", onDelete: ReferentialAction.SetNull);
            });
        migrationBuilder.CreateIndex(name: "IX_AppointmentResourceAllocations_PatientId", table: "AppointmentResourceAllocations", column: "PatientId");
        migrationBuilder.CreateIndex(name: "IX_AppointmentResourceAllocations_DoctorId", table: "AppointmentResourceAllocations", column: "DoctorId");
        migrationBuilder.CreateIndex(name: "IX_AppointmentResourceAllocations_DepartmentId", table: "AppointmentResourceAllocations", column: "DepartmentId");
        migrationBuilder.CreateIndex(name: "IX_AppointmentResourceAllocations_AppointmentId_IsActive", table: "AppointmentResourceAllocations", columns: new[] { "AppointmentId", "IsActive" });
        migrationBuilder.CreateIndex(name: "IX_AppointmentResourceAllocations_BedId", table: "AppointmentResourceAllocations", column: "BedId", unique: true, filter: "\"BedId\" IS NOT NULL AND \"IsActive\" = TRUE");
        migrationBuilder.CreateIndex(name: "IX_AppointmentResourceAllocations_MedicalResourceId", table: "AppointmentResourceAllocations", column: "MedicalResourceId", unique: true, filter: "\"MedicalResourceId\" IS NOT NULL AND \"IsActive\" = TRUE");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "AppointmentResourceAllocations");
}
