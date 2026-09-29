using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartHospital.Api.Data;

#nullable disable

namespace SmartHospital.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260928090000_AddStableQueueIdentifiers")]
public partial class AddStableQueueIdentifiers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "QueueCode", table: "QueueEntries", type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<DateOnly>(name: "QueueDate", table: "QueueEntries", type: "date", nullable: false, defaultValue: new DateOnly(2000, 1, 1));
        migrationBuilder.AddColumn<DateTime>(name: "StartedAt", table: "QueueEntries", type: "timestamp with time zone", nullable: true);

        migrationBuilder.Sql("""
            WITH ranked AS (
                SELECT q."Id", (a."ScheduledStart" AT TIME ZONE 'Asia/Colombo')::date AS queue_date,
                       row_number() OVER (PARTITION BY q."DoctorId", (a."ScheduledStart" AT TIME ZONE 'Asia/Colombo')::date ORDER BY q."CheckedInAt" NULLS FIRST, q."QueueNumber", q."Id") AS queue_number
                FROM "QueueEntries" q JOIN "Appointments" a ON a."Id" = q."AppointmentId"
            )
            UPDATE "QueueEntries" q
            SET "QueueDate" = ranked.queue_date, "QueueNumber" = ranked.queue_number,
                "QueueCode" = 'A-' || lpad(ranked.queue_number::text, 3, '0')
            FROM ranked WHERE ranked."Id" = q."Id";
            """);
        migrationBuilder.CreateIndex(name: "IX_QueueEntries_DoctorId_QueueDate_QueueNumber", table: "QueueEntries", columns: new[] { "DoctorId", "QueueDate", "QueueNumber" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_QueueEntries_DoctorId_QueueDate_QueueNumber", table: "QueueEntries");
        migrationBuilder.DropColumn(name: "QueueCode", table: "QueueEntries");
        migrationBuilder.DropColumn(name: "QueueDate", table: "QueueEntries");
        migrationBuilder.DropColumn(name: "StartedAt", table: "QueueEntries");
    }
}
