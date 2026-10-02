using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartHospital.Api.Data;

#nullable disable

namespace SmartHospital.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261002140000_AddAdmissionCheckupDate")]
public partial class AddAdmissionCheckupDate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "CheckupDate",
            table: "Admissions",
            type: "timestamp with time zone",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CheckupDate",
            table: "Admissions");
    }
}
