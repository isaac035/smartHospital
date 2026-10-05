using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartHospital.Api.Data;

#nullable disable

namespace SmartHospital.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260927180000_AddSpecificDateToDoctorSchedule")]
public partial class AddSpecificDateToDoctorSchedule : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateOnly>(
            name: "SpecificDate",
            table: "DoctorSchedules",
            type: "date",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_DoctorSchedules_DoctorId_SpecificDate",
            table: "DoctorSchedules",
            columns: new[] { "DoctorId", "SpecificDate" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_DoctorSchedules_DoctorId_SpecificDate",
            table: "DoctorSchedules");

        migrationBuilder.DropColumn(
            name: "SpecificDate",
            table: "DoctorSchedules");
    }
}
