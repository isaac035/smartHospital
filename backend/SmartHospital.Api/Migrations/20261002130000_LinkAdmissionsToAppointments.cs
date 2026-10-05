using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartHospital.Api.Data;

#nullable disable

namespace SmartHospital.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261002130000_LinkAdmissionsToAppointments")]
public partial class LinkAdmissionsToAppointments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "AppointmentId",
            table: "Admissions",
            type: "integer",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Admissions_AppointmentId",
            table: "Admissions",
            column: "AppointmentId",
            unique: true,
            filter: "\"AppointmentId\" IS NOT NULL");

        migrationBuilder.AddForeignKey(
            name: "FK_Admissions_Appointments_AppointmentId",
            table: "Admissions",
            column: "AppointmentId",
            principalTable: "Appointments",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Admissions_Appointments_AppointmentId",
            table: "Admissions");

        migrationBuilder.DropIndex(
            name: "IX_Admissions_AppointmentId",
            table: "Admissions");

        migrationBuilder.DropColumn(
            name: "AppointmentId",
            table: "Admissions");
    }
}
