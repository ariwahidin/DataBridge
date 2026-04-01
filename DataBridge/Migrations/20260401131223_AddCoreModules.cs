using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBridge.Migrations
{
    /// <inheritdoc />
    public partial class AddCoreModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ConnectionString = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MirrorJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceId = table.Column<int>(type: "int", nullable: false),
                    SourceQuery = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DestinationTable = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SyncMode = table.Column<int>(type: "int", nullable: false),
                    WatermarkColumn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastWatermark = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MirrorJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MirrorJobs_Sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmailConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MirrorJobId = table.Column<int>(type: "int", nullable: false),
                    Recipients = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NotifyOnFail = table.Column<bool>(type: "bit", nullable: false),
                    NotifyOnSuccess = table.Column<bool>(type: "bit", nullable: false),
                    SmtpHost = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SmtpPort = table.Column<int>(type: "int", nullable: false),
                    SmtpUser = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SmtpPassword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenderEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SenderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailConfigs_MirrorJobs_MirrorJobId",
                        column: x => x.MirrorJobId,
                        principalTable: "MirrorJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MirrorJobId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowsAffected = table.Column<int>(type: "int", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TriggeredBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobHistories_MirrorJobs_MirrorJobId",
                        column: x => x.MirrorJobId,
                        principalTable: "MirrorJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MirrorJobId = table.Column<int>(type: "int", nullable: false),
                    CronExpression = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobSchedules_MirrorJobs_MirrorJobId",
                        column: x => x.MirrorJobId,
                        principalTable: "MirrorJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$zacaANgHvAW1RVwf9Sj12uwJYbcdhxDp/z9kBwwiaZGnnzi9cGwFC");

            migrationBuilder.CreateIndex(
                name: "IX_EmailConfigs_MirrorJobId",
                table: "EmailConfigs",
                column: "MirrorJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobHistories_MirrorJobId",
                table: "JobHistories",
                column: "MirrorJobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobSchedules_MirrorJobId",
                table: "JobSchedules",
                column: "MirrorJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MirrorJobs_SourceId",
                table: "MirrorJobs",
                column: "SourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailConfigs");

            migrationBuilder.DropTable(
                name: "JobHistories");

            migrationBuilder.DropTable(
                name: "JobSchedules");

            migrationBuilder.DropTable(
                name: "MirrorJobs");

            migrationBuilder.DropTable(
                name: "Sources");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$Jss23HRrdngf/0M8RYDIXO4AP85I2vJlUfr53snBKSwy2QM1kKj6i");
        }
    }
}
