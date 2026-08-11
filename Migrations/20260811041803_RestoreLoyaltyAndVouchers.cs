using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quản_lý_quán_cafe.Migrations
{
    /// <inheritdoc />
    public partial class RestoreLoyaltyAndVouchers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PointDiscountAmount",
                table: "Orders",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsLoyaltyCustomerAssigned",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SubtotalAmount",
                table: "Orders",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "VoucherCode",
                table: "Orders",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VoucherDiscountAmount",
                table: "Orders",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "VoucherID",
                table: "Orders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RewardPoints",
                table: "Customers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE "Orders"
                SET "SubtotalAmount" = COALESCE(
                    (SELECT SUM(CAST("Subtotal" AS NUMERIC))
                     FROM "OrderDetails"
                     WHERE "OrderDetails"."OrderID" = "Orders"."OrderID"
                       AND "OrderDetails"."IsDeleted" = 0),
                    CAST("TotalAmount" AS NUMERIC),
                    0)
                WHERE CAST("SubtotalAmount" AS NUMERIC) = 0;
                """);

            migrationBuilder.CreateTable(
                name: "PointHistories",
                columns: table => new
                {
                    PointHistoryID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerID = table.Column<int>(type: "INTEGER", nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    BalanceAfter = table.Column<int>(type: "INTEGER", nullable: false),
                    TransactionType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OrderID = table.Column<int>(type: "INTEGER", nullable: true),
                    ActorUserID = table.Column<int>(type: "INTEGER", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointHistories", x => x.PointHistoryID);
                    table.CheckConstraint("CK_PointHistories_BalanceAfter_NonNegative", "\"BalanceAfter\" >= 0");
                    table.CheckConstraint("CK_PointHistories_Points_NotZero", "\"Points\" <> 0");
                    table.ForeignKey(
                        name: "FK_PointHistories_Customers_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "Customers",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PointHistories_Orders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PointHistories_Users_ActorUserID",
                        column: x => x.ActorUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Vouchers",
                columns: table => new
                {
                    VoucherID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DiscountType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vouchers", x => x.VoucherID);
                    table.CheckConstraint("CK_Vouchers_Discount", "(\"DiscountType\" = 'Percent' AND CAST(\"DiscountValue\" AS NUMERIC) > 0 AND CAST(\"DiscountValue\" AS NUMERIC) <= 100) OR (\"DiscountType\" = 'Fixed' AND CAST(\"DiscountValue\" AS NUMERIC) > 0)");
                });

            migrationBuilder.CreateTable(
                name: "OrderPointRedemptions",
                columns: table => new
                {
                    OrderPointRedemptionID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderID = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomerID = table.Column<int>(type: "INTEGER", nullable: false),
                    PointHistoryID = table.Column<int>(type: "INTEGER", nullable: true),
                    PointsUsed = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPointRedemptions", x => x.OrderPointRedemptionID);
                    table.CheckConstraint("CK_OrderPointRedemptions_Discount_Positive", "CAST(\"DiscountAmount\" AS NUMERIC) > 0");
                    table.CheckConstraint("CK_OrderPointRedemptions_Points_Positive", "\"PointsUsed\" > 0");
                    table.CheckConstraint("CK_OrderPointRedemptions_Sequence_NonNegative", "\"Sequence\" >= 0");
                    table.ForeignKey(
                        name: "FK_OrderPointRedemptions_Customers_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "Customers",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderPointRedemptions_Orders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderPointRedemptions_PointHistories_PointHistoryID",
                        column: x => x.PointHistoryID,
                        principalTable: "PointHistories",
                        principalColumn: "PointHistoryID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_VoucherID",
                table: "Orders",
                column: "VoucherID");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Phone",
                table: "Customers",
                column: "Phone",
                unique: true,
                filter: "\"Phone\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPointRedemptions_CustomerID",
                table: "OrderPointRedemptions",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPointRedemptions_OrderID_CustomerID",
                table: "OrderPointRedemptions",
                columns: new[] { "OrderID", "CustomerID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderPointRedemptions_PointHistoryID",
                table: "OrderPointRedemptions",
                column: "PointHistoryID",
                unique: true,
                filter: "\"PointHistoryID\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PointHistories_ActorUserID",
                table: "PointHistories",
                column: "ActorUserID");

            migrationBuilder.CreateIndex(
                name: "IX_PointHistories_CustomerID",
                table: "PointHistories",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_PointHistories_IdempotencyKey",
                table: "PointHistories",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PointHistories_OrderID",
                table: "PointHistories",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_Code",
                table: "Vouchers",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Vouchers_VoucherID",
                table: "Orders",
                column: "VoucherID",
                principalTable: "Vouchers",
                principalColumn: "VoucherID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Vouchers_VoucherID",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "OrderPointRedemptions");

            migrationBuilder.DropTable(
                name: "Vouchers");

            migrationBuilder.DropTable(
                name: "PointHistories");

            migrationBuilder.DropIndex(
                name: "IX_Orders_VoucherID",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Phone",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PointDiscountAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsLoyaltyCustomerAssigned",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SubtotalAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VoucherCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VoucherDiscountAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VoucherID",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RewardPoints",
                table: "Customers");
        }
    }
}
