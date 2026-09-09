using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoSale.Vehicles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleReservationsAndCatalogOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    make = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    year = table.Column<short>(type: "smallint", nullable: false),
                    color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reservation_sale_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sold_sale_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicles", x => x.id);
                    table.CheckConstraint("ck_vehicles_price_positive", "price > 0");
                    table.CheckConstraint("ck_vehicles_status_references", "(status = 'Available' AND reservation_sale_id IS NULL AND sold_sale_id IS NULL) OR (status = 'Reserved' AND reservation_sale_id IS NOT NULL AND sold_sale_id IS NULL) OR (status = 'Sold' AND reservation_sale_id IS NULL AND sold_sale_id IS NOT NULL)");
                    table.CheckConstraint("ck_vehicles_version", "version > 0");
                    table.CheckConstraint("ck_vehicles_year", "year >= 1886");
                });

            migrationBuilder.CreateTable(
                name: "catalog_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_version = table.Column<int>(type: "integer", nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    next_attempt_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    lease_owner = table.Column<string>(type: "text", nullable: true),
                    lease_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_outbox", x => x.id);
                    table.CheckConstraint("ck_catalog_outbox_attempts", "attempts >= 0");
                    table.CheckConstraint("ck_catalog_outbox_vehicle_version", "vehicle_version > 0");
                    table.ForeignKey(
                        name: "FK_catalog_outbox_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_reservations",
                columns: table => new
                {
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    make_snapshot = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    model_snapshot = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    year_snapshot = table.Column<short>(type: "smallint", nullable: false),
                    color_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    price_snapshot = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    vehicle_version_snapshot = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confirmed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    released_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_reservations", x => x.sale_id);
                    table.CheckConstraint("ck_vehicle_reservations_price_positive", "price_snapshot > 0");
                    table.CheckConstraint("ck_vehicle_reservations_terminal_timestamp", "(status = 'Reserved' AND confirmed_at_utc IS NULL AND released_at_utc IS NULL) OR (status = 'Confirmed' AND confirmed_at_utc IS NOT NULL AND released_at_utc IS NULL) OR (status = 'Released' AND confirmed_at_utc IS NULL AND released_at_utc IS NOT NULL)");
                    table.CheckConstraint("ck_vehicle_reservations_version", "vehicle_version_snapshot > 0");
                    table.ForeignKey(
                        name: "FK_vehicle_reservations_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_outbox_lease_expiration",
                table: "catalog_outbox",
                column: "lease_expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_outbox_pending",
                table: "catalog_outbox",
                columns: new[] { "processed_at_utc", "next_attempt_at_utc", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_catalog_outbox_vehicle_version",
                table: "catalog_outbox",
                columns: new[] { "vehicle_id", "vehicle_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_reservations_vehicle_created",
                table: "vehicle_reservations",
                columns: new[] { "vehicle_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_status_price_id",
                table: "vehicles",
                columns: new[] { "status", "price", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_outbox");

            migrationBuilder.DropTable(
                name: "vehicle_reservations");

            migrationBuilder.DropTable(
                name: "vehicles");
        }
    }
}
