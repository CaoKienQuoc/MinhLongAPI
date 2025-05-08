using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Emit;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataAccessLayer
{
    public class MinhLongDbContext : DbContext
    {
        public MinhLongDbContext(DbContextOptions options) : base(options) { }
        public MinhLongDbContext() { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            optionsBuilder.UseSqlServer(configuration.GetConnectionString("ServerConnection"));
        }


        // 🔥 Định nghĩa DbSet cho các bảng
        public DbSet<User> Users { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<AgencyAccount> AgencyAccounts { get; set; }
        public DbSet<AgencyLevel> AgencyLevels { get; set; }
        public DbSet<AgencyAccountLevel> AgencyAccountLevels { get; set; }
        public DbSet<RegisterAccount> RegisterAccounts { get; set; }
        public DbSet<Province> Provinces { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<Ward> Wards { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<TaxConfig> TaxConfigs { get; set; }
        public DbSet<ImportTransaction> ImportTransactions { get; set; }
        public DbSet<ImportTransactionDetail> ImportTransactionDetails { get; set; }
        public DbSet<Batch> Batches { get; set; }
        public DbSet<WarehouseProduct> WarehouseProduct { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<RequestProduct> RequestProducts { get; set; }
        public DbSet<RequestProductDetail> RequestProductDetails { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<WarehouseReceipt> WarehouseReceipts { get; set; }
        public DbSet<ExportWarehouseReceipt> ExportWarehouseReceipts { get; set; }
        public DbSet<ExportWarehouseReceiptDetail> ExportWarehouseReceiptDetail { get; set; }
        public DbSet<ExportTransaction> ExportTransactions { get; set; }
        public DbSet<ExportTransactionDetail> ExportTransactionDetails { get; set; }
        public DbSet<WarehouseLedger> WarehouseLedgers { get; set; }
        public DbSet<PaymentHistory> PaymentHistories { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<RequestExport> RequestExports { get; set; }
        public DbSet<RequestExportDetail> RequestExportDetails { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<OTPEmail> OTPEmails { get; set; }
        public DbSet<WarehouseTransferRequest> WarehouseTransferRequests { get; set; }
        public DbSet<WarehouseTransferProduct> WarehouseTransferProducts { get; set; }
        public DbSet<TemporaryStockExport> TemporaryStockExports { get; set; }
        public DbSet<ReturnRequest> ReturnRequests { get; set; }
        public DbSet<ReturnRequestDetail> ReturnRequestDetails { get; set; }
        public DbSet<ReturnRequestImage> ReturnRequestImages { get; set; }
        public DbSet<ReturnWarehouseReceipt> ReturnWarehouseReceipts { get; set; }
        public DbSet<ReturnWarehouseReceiptDetail> ReturnWarehouseReceiptDetails { get; set; }
        public DbSet<DamagedStock> DamagedStocks { get; set; }
        public DbSet<Contract> Contract { get; set; }
        public DbSet<RegisterAccountContract> RegisterAccountContract { get; set; }
        public DbSet<Notification> Notification { get; set; }
        public DbSet<AgencyScoreHistory> AgencyScoreHistory { get; set; }
        public DbSet<AgencyPromotionRequest> AgencyPromotionRequest { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 🏷️ **Định danh bảng**
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<Employee>().ToTable("Employee");
            modelBuilder.Entity<Role>().ToTable("Role");
            modelBuilder.Entity<UserRole>().ToTable("UserRole");
            modelBuilder.Entity<Permission>().ToTable("Permission");
            modelBuilder.Entity<RolePermission>().ToTable("RolePermission");
            modelBuilder.Entity<AgencyAccount>().ToTable("AgencyAccount");
            modelBuilder.Entity<AgencyLevel>().ToTable("AgencyLevel");
            modelBuilder.Entity<AgencyAccountLevel>().ToTable("AgencyAccountLevel");
            modelBuilder.Entity<RegisterAccount>().ToTable("RegisterAccount");
            modelBuilder.Entity<Province>().ToTable("Province");
            modelBuilder.Entity<District>().ToTable("District");
            modelBuilder.Entity<Ward>().ToTable("Ward");
            modelBuilder.Entity<Address>().ToTable("Address");
            modelBuilder.Entity<Warehouse>().ToTable("Warehouse");
            modelBuilder.Entity<Product>().ToTable("Product");
            modelBuilder.Entity<ProductCategory>().ToTable("ProductCategory");
            modelBuilder.Entity<TaxConfig>().ToTable("TaxConfig");
            modelBuilder.Entity<ImportTransaction>().ToTable("ImportTransaction");
            modelBuilder.Entity<ImportTransactionDetail>().ToTable("ImportTransactionDetail");
            modelBuilder.Entity<Batch>().ToTable("Batch");
            modelBuilder.Entity<WarehouseProduct>().ToTable("WarehouseProduct");
            modelBuilder.Entity<Order>().ToTable("Order");
            modelBuilder.Entity<RequestProduct>().ToTable("RequestProduct");
            modelBuilder.Entity<RequestProductDetail>().ToTable("RequestProductDetail");
            modelBuilder.Entity<Image>().ToTable("Image");
            modelBuilder.Entity<WarehouseReceipt>().ToTable("WarehouseReceipt");
            modelBuilder.Entity<ExportWarehouseReceipt>().ToTable("ExportWarehouseReceipt");
            modelBuilder.Entity<ExportTransaction>().ToTable("ExportTransaction");
            modelBuilder.Entity<ExportTransactionDetail>().ToTable("ExportTransactionDetail");
            modelBuilder.Entity<WarehouseLedger>().ToTable("WarehouseLedger");
            modelBuilder.Entity<PaymentHistory>().ToTable("PaymentHistory");
            modelBuilder.Entity<PaymentTransaction>().ToTable("PaymentTransaction");
            modelBuilder.Entity<RequestExport>().ToTable("RequestExport");
            modelBuilder.Entity<RequestExportDetail>().ToTable("RequestExportDetail");
            modelBuilder.Entity<OrderDetail>().ToTable("OrderDetail");
            modelBuilder.Entity<WarehouseTransferRequest>().ToTable("WarehouseTransferRequest");
            modelBuilder.Entity<WarehouseTransferProduct>().ToTable("WarehouseTransferProduct");
            modelBuilder.Entity<TemporaryStockExport>().ToTable("TemporaryStockExport");
            modelBuilder.Entity<ReturnRequest>().ToTable("ReturnRequest");
            modelBuilder.Entity<ReturnRequestDetail>().ToTable("ReturnRequestDetail");
            modelBuilder.Entity<ReturnRequestImage>().ToTable("ReturnRequestImage");
            modelBuilder.Entity<ReturnWarehouseReceipt>().ToTable("ReturnWarehouseReceipt");
            modelBuilder.Entity<ReturnWarehouseReceiptDetail>().ToTable("ReturnWarehouseReceiptDetail");
            modelBuilder.Entity<DamagedStock>().ToTable("DamagedStock");
            modelBuilder.Entity<Contract>().ToTable("Contract");
            modelBuilder.Entity<RegisterAccountContract>().ToTable("RegisterAccountContract");
            modelBuilder.Entity<Notification>().ToTable("Notification");
            modelBuilder.Entity<AgencyScoreHistory>().ToTable("AgencyScoreHistory");
            modelBuilder.Entity<AgencyPromotionRequest>().ToTable("AgencyPromotionRequest");
            modelBuilder.Entity<ChatMessage>().ToTable("ChatMessage");

            // 🔥 **Cấu hình quan hệ**
            modelBuilder.Entity<Ward>()
                .HasOne(w => w.District)
                .WithMany(d => d.Wards)
                .HasForeignKey(w => w.DistrictId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<District>()
                .HasOne(d => d.Province)
                .WithMany(p => p.Districts)
                .HasForeignKey(d => d.ProvinceId)
                .OnDelete(DeleteBehavior.Cascade);

            // ❌ **Fix lỗi "Multiple Cascade Paths"**
            modelBuilder.Entity<Address>()
                .HasOne(a => a.Ward)
                .WithMany(w => w.Addresses)
                .HasForeignKey(a => a.WardId)
                .OnDelete(DeleteBehavior.NoAction); // 🔥 Fix lỗi

            modelBuilder.Entity<Address>()
                .HasOne(a => a.District)
                .WithMany(d => d.Addresses)
                .HasForeignKey(a => a.DistrictId)
                .OnDelete(DeleteBehavior.NoAction); // 🔥 Fix lỗi

            modelBuilder.Entity<Address>()
                .HasOne(a => a.Province)
                .WithMany(p => p.Addresses)
                .HasForeignKey(a => a.ProvinceId)
                .OnDelete(DeleteBehavior.NoAction); // 🔥 Fix lỗi

            // 🔥 Cấu hình khóa chính tự động tăng
            modelBuilder.Entity<User>().Property(u => u.UserId).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<Employee>().Property(e => e.EmployeeId).ValueGeneratedOnAdd();
            modelBuilder.Entity<AgencyAccount>().Property(a => a.AgencyId).ValueGeneratedOnAdd();
            modelBuilder.Entity<Warehouse>().Property(w => w.WarehouseId).ValueGeneratedOnAdd();
            modelBuilder.Entity<Product>().Property(p => p.ProductId).ValueGeneratedOnAdd();
            modelBuilder.Entity<ProductCategory>().Property(pc => pc.CategoryId).ValueGeneratedOnAdd();
            modelBuilder.Entity<TaxConfig>().Property(tc => tc.TaxId).ValueGeneratedOnAdd();
            modelBuilder.Entity<ImportTransaction>().Property(it => it.ImportTransactionId).ValueGeneratedOnAdd();
            modelBuilder.Entity<ImportTransactionDetail>().Property(itd => itd.ImportTransactionDetailId).ValueGeneratedOnAdd();
            modelBuilder.Entity<WarehouseProduct>().Property(i => i.WarehouseProductId).ValueGeneratedOnAdd();
            modelBuilder.Entity<Order>().Property(o => o.OrderId).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<Batch>().Property(b => b.BatchId).ValueGeneratedOnAdd();
            modelBuilder.Entity<OrderDetail>().Property(od => od.OrderDetailId).HasDefaultValueSql("NEWID()"); // Dành cho kiểu GUID
            modelBuilder.Entity<RequestProduct>().Property(r => r.RequestProductId).ValueGeneratedOnAdd();
            modelBuilder.Entity<RequestProductDetail>().Property(rpd => rpd.RequestDetailId).ValueGeneratedOnAdd(); // Dành cho kiểu bigint tự động tăng
            modelBuilder.Entity<Image>().Property(i => i.ImageId).ValueGeneratedOnAdd();
            modelBuilder.Entity<WarehouseReceipt>().Property(wr => wr.WarehouseReceiptId).ValueGeneratedOnAdd();
            modelBuilder.Entity<WarehouseLedger>().Property(wr => wr.WarehouseLedgerId).ValueGeneratedOnAdd();
            modelBuilder.Entity<ExportTransaction>().Property(wr => wr.ExportTransactionId).ValueGeneratedOnAdd();
            modelBuilder.Entity<ExportTransactionDetail>().Property(wr => wr.ExportTransactionDetailId).ValueGeneratedOnAdd();
            modelBuilder.Entity<ExportWarehouseReceipt>().Property(wr => wr.ExportWarehouseReceiptId).ValueGeneratedOnAdd();
            modelBuilder.Entity<OTPEmail>().Property(wr => wr.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<PaymentHistory>().Property(ph => ph.PaymentHistoryId).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<PaymentTransaction>().Property(pt => pt.TransactionId).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<WarehouseTransferRequest>().Property(wr => wr.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<WarehouseTransferProduct>().Property(wr => wr.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<TemporaryStockExport>().Property(wr => wr.TemporaryStockExportId).ValueGeneratedOnAdd();
            modelBuilder.Entity<ReturnRequest>().Property(wr => wr.ReturnRequestId).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<ReturnRequestDetail>().Property(wr => wr.ReturnRequestDetailId).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<ReturnRequestImage>().Property(wr => wr.ReturnRequestImageId).ValueGeneratedOnAdd();
            modelBuilder.Entity<ReturnWarehouseReceipt>().Property(wr => wr.ReturnWarehouseReceiptId).ValueGeneratedOnAdd();
            modelBuilder.Entity<ReturnWarehouseReceiptDetail>().Property(wr => wr.ReturnWarehouseReceiptDetailId).ValueGeneratedOnAdd();
            modelBuilder.Entity<DamagedStock>().Property(wr => wr.DamagedStockId).ValueGeneratedOnAdd();
            modelBuilder.Entity<Contract>().Property(wr => wr.ContractId).ValueGeneratedOnAdd();
            modelBuilder.Entity<RegisterAccountContract>().Property(wr => wr.ContractId).ValueGeneratedOnAdd();
            modelBuilder.Entity<AgencyScoreHistory>().Property(wr => wr.AgencyScoreHistoryId).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<AgencyPromotionRequest>().Property(wr => wr.AgencyPromotionRequestId).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<ChatMessage>().Property(wr => wr.ChatMessageId).HasDefaultValueSql("NEWID()");

            // 🔥 **Cấu hình quan hệ nhiều - nhiều**
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId);

            // 🔥 **Quan hệ User - Employee (1-1)**
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.User)
                .WithOne(u => u.Employee)
                .HasForeignKey<Employee>(e => e.UserId);

            // 🔥 **Quan hệ User - AgencyAccount (1-1)**
            modelBuilder.Entity<AgencyAccount>()
                .HasOne(a => a.User)
                .WithOne(u => u.AgencyAccount)
                .HasForeignKey<AgencyAccount>(a => a.UserId);

            // Mối quan hệ: AgencyAccount (1) <-> (N) AgencyScoreHistory
            modelBuilder.Entity<AgencyScoreHistory>()
                .HasOne(s => s.Agency)
                .WithMany(a => a.ScoreHistories)
                .HasForeignKey(s => s.AgencyId)
                .OnDelete(DeleteBehavior.Cascade); // hoặc Restrict nếu không muốn xóa theo

            // Mối quan hệ: AgencyAccount (1) <-> (N) AgencyPromotionRequest
            modelBuilder.Entity<AgencyPromotionRequest>()
                .HasOne(p => p.Agency)
                .WithMany(a => a.PromotionRequests)
                .HasForeignKey(p => p.AgencyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Liên kết Warehouse với Address (1 Warehouse - 1 Address)
            modelBuilder.Entity<Warehouse>()
                .HasOne(w => w.Address)
                .WithOne(a => a.Warehouse) // 🔥 1-1 Mapping
                .HasForeignKey<Warehouse>(w => w.AddressId)
                .OnDelete(DeleteBehavior.Cascade);
            // Cấu hình quan hệ ProductCategory
            modelBuilder.Entity<ProductCategory>()
                .HasOne(pc => pc.ParentCategory)
                .WithMany()
                .HasForeignKey(pc => pc.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductCategory>()
                .HasOne(c => c.Creator)
                .WithMany()
                .HasForeignKey(c => c.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ProductCategory>()
                .HasOne(c => c.Updater)
                .WithMany()
                .HasForeignKey(c => c.UpdatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // Cấu hình quan hệ Product
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.TaxConfig)
                .WithMany()
                .HasForeignKey(p => p.TaxId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Creator)
                .WithMany()
                .HasForeignKey(p => p.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Updater)
                .WithMany()
                .HasForeignKey(p => p.UpdatedBy)
                .OnDelete(DeleteBehavior.NoAction);


            modelBuilder.Entity<AgencyAccountLevel>()
    .HasOne(aal => aal.Agency)
    .WithMany(a => a.AgencyAccountLevels) // thêm chỗ này
    .HasForeignKey(aal => aal.AgencyId);


            // 🔥 **Cấu hình giá trị decimal**
            modelBuilder.Entity<AgencyAccountLevel>()
                .Property(aal => aal.MonthlyRevenue)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<AgencyAccountLevel>()
                .Property(aal => aal.OrderDiscount)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<AgencyAccountLevel>()
                .Property(aal => aal.OrderRevenue)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<AgencyAccountLevel>()
                .Property(aal => aal.TotalDebtValue)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<AgencyLevel>()
                .Property(al => al.CreditLimit)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<AgencyLevel>()
                .Property(al => al.DiscountPercentage)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<TaxConfig>()
                .Property(tc => tc.TaxRate)
                .HasColumnType("decimal(18, 4)");

            modelBuilder.Entity<User>()
                .Property(u => u.Status)
                .HasDefaultValue(false);

            // Configurations if needed
            modelBuilder.Entity<ImportTransaction>()
                .HasMany(it => it.ImportTransactionDetails)
                .WithOne(itd => itd.ImportTransaction)
                .HasForeignKey(itd => itd.ImportTransactionId);

            modelBuilder.Entity<ImportTransactionDetail>()
                .HasMany(itd => itd.Batches)
                .WithOne(b => b.ImportTransactionDetail)
                .HasForeignKey(b => b.ImportTransactionDetailId);

            modelBuilder.Entity<WarehouseProduct>()
                .HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.NoAction); // Fixing cascade issue

            modelBuilder.Entity<WarehouseProduct>()
                .HasOne(i => i.Warehouse)
                .WithMany()
                .HasForeignKey(i => i.WarehouseId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<WarehouseProduct>()
                .HasOne(i => i.Batch)
                .WithMany()
                .HasForeignKey(i => i.BatchId)
                .OnDelete(DeleteBehavior.Cascade);

            /*modelBuilder.Entity<Order>()
                .HasOne(o => o.RequestProduct)
                .WithMany()
                .HasForeignKey(o => o.RequestId);*/

            //request product

            modelBuilder.Entity<RequestProduct>()
                .HasOne(r => r.AgencyAccount)
                .WithMany()
                .HasForeignKey(r => r.AgencyId)
                .OnDelete(DeleteBehavior.NoAction);

            
            // Cấu hình quan hệ 1-N giữa Product và ProductDetail
            modelBuilder.Entity<RequestProductDetail>()
                .HasOne(pd => pd.Product)
                .WithMany(p => p.RequestProductDetail)
                .HasForeignKey(pd => pd.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // Khi xóa Product, xóa luôn ProductDetail

            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.UnitPrice)
                .HasPrecision(18, 2); // 18 chữ số, 2 số thập phân

            

            //done
            modelBuilder.Entity<Batch>()
                .HasOne(b => b.ImportTransactionDetail)
                .WithMany(d => d.Batches)
                .HasForeignKey(b => b.ImportTransactionDetailId)
                .OnDelete(DeleteBehavior.Cascade); // ✅ Chỉ dùng cascade khi thực sự cần

            // Explicitly define precision and scale for decimal fields to avoid truncation issues
            modelBuilder.Entity<Batch>()
                .Property(b => b.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Batch>()
                .Property(b => b.UnitCost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ImportTransactionDetail>()
                .Property(itd => itd.TotalPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.Discount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.FinalPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Image>()
            .HasOne(i => i.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.ProductId);



            // Định dạng cột decimal (18,2) cho TotalPrice
            modelBuilder.Entity<WarehouseReceipt>()
                .Property(w => w.TotalPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Batch>()
                .Property(b => b.UnitCost)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Batch>()
                .Property(b => b.TotalAmount)
                .HasColumnType("decimal(18,2)");

            //Moi Quan He Warehouse, WarehouseProduct, ExportTransaction, ExportTransactionDetail
            // 🔹 1. ExportTransaction ↔ Warehouse (1-Nhiều)
            modelBuilder.Entity<ExportTransaction>()
                .HasOne(et => et.Warehouse)
                .WithMany(w => w.ExportTransactions)
                .HasForeignKey(et => et.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔹 2. ExportTransaction ↔ ExportTransactionDetail (1-Nhiều)
            modelBuilder.Entity<ExportTransactionDetail>()
                .HasOne(etd => etd.ExportTransaction)
                .WithMany(et => et.ExportTransactionDetail)
                .HasForeignKey(etd => etd.ExportTransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔹 3. ExportTransactionDetail ↔ WarehouseProduct (1-Nhiều)
            modelBuilder.Entity<ExportTransactionDetail>()
                .HasOne(etd => etd.WarehouseProduct)
                .WithMany(wp => wp.ExportTransactionDetails) // ✅ Đúng với WarehouseProduct
                .HasForeignKey(etd => etd.WarehouseProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔹 4. ExportWarehouseReceipt ↔ Warehouse (1-Nhiều)
            modelBuilder.Entity<ExportWarehouseReceipt>()
                .HasOne(ewr => ewr.Warehouse)
                .WithMany(w => w.ExportWarehouseReceipts)
                .HasForeignKey(ewr => ewr.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔹 ExportWarehouseReceipt ↔ ExportWarehouseReceiptDetail (1-Nhiều)
            modelBuilder.Entity<ExportWarehouseReceiptDetail>()
                .HasOne(ewrd => ewrd.ExportWarehouseReceipt)
                .WithMany(ewr => ewr.ExportWarehouseReceiptDetails)
                .HasForeignKey(ewrd => ewrd.ExportWarehouseReceiptId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔹 ExportWarehouseReceiptDetail ↔ WarehouseProduct (1-Nhiều)
            modelBuilder.Entity<ExportWarehouseReceiptDetail>()
                .HasOne(ewrd => ewrd.WarehouseProduct)
                .WithMany()
                .HasForeignKey(ewrd => ewrd.WarehouseProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔹 7. WarehouseLedger ↔ Warehouse (1-Nhiều)
            modelBuilder.Entity<WarehouseLedger>()
                .HasOne(wl => wl.Warehouse)
                .WithMany(w => w.WarehouseLedgers)
                .HasForeignKey(wl => wl.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔹 8. WarehouseLedger ↔ ExportTransaction (1-1 hoặc 1-Nhiều, nullable)
            modelBuilder.Entity<WarehouseLedger>()
                .HasOne(wl => wl.ExportTransaction)
                .WithMany()
                .HasForeignKey(wl => wl.ExportTransactionId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔹 9. WarehouseLedger ↔ ImportTransaction (1-1 hoặc 1-Nhiều, nullable)
            modelBuilder.Entity<WarehouseLedger>()
                .HasOne(wl => wl.ImportTransaction)
                .WithMany()
                .HasForeignKey(wl => wl.ImportTransactionId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔹 WarehouseProduct ↔ ExportWarehouseReceiptDetail (1-Nhiều)
            modelBuilder.Entity<ExportWarehouseReceiptDetail>()
                .HasOne(ewrd => ewrd.WarehouseProduct)
                .WithMany(wp => wp.ExportWarehouseReceiptDetails)
                .HasForeignKey(ewrd => ewrd.WarehouseProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔹 WarehouseProduct ↔ Warehouse (1-Nhiều)
            modelBuilder.Entity<WarehouseProduct>()
                .HasOne(wp => wp.Warehouse)
                .WithMany(w => w.WarehouseProducts) // ✅ Đảm bảo Warehouse có `ICollection<WarehouseProduct>`
                .HasForeignKey(wp => wp.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExportTransactionDetail>()
                .Property(e => e.UnitPrice)
                .HasPrecision(18, 4); // Đảm bảo chính xác đến 4 số lẻ

            modelBuilder.Entity<ExportTransactionDetail>()
                .Property(e => e.TotalProductAmount)
                .HasPrecision(18, 4);

            modelBuilder.Entity<ExportWarehouseReceipt>()
                .Property(e => e.TotalAmount)
                .HasPrecision(18, 4);

            modelBuilder.Entity<ExportWarehouseReceiptDetail>()
                .Property(e => e.UnitPrice)
                .HasPrecision(18, 4);

            modelBuilder.Entity<ExportWarehouseReceiptDetail>()
                .Property(e => e.TotalProductAmount)
                .HasPrecision(18, 4);

            modelBuilder.Entity<WarehouseLedger>()
                .Property(e => e.TotalAmount)
                .HasPrecision(18, 4);
            // 🏷️ Cấu hình bảng PaymentHistory
            modelBuilder.Entity<PaymentHistory>()
                .ToTable("PaymentHistory")
                .HasOne(ph => ph.Order)
                .WithMany(o => o.PaymentHistories)
                .HasForeignKey(ph => ph.OrderId)
                .OnDelete(DeleteBehavior.Cascade); // Khi xóa Order, xóa tất cả PaymentHistory liên quan

            modelBuilder.Entity<PaymentHistory>()
                .HasOne(ph => ph.PrePayment)
                .WithMany()
                .HasForeignKey(ph => ph.PrePaymentId)
                .OnDelete(DeleteBehavior.Restrict); // Không xóa PaymentHistory nếu có liên kết với PrePayment

            modelBuilder.Entity<PaymentHistory>()
                .Property(ph => ph.RemainingDebtAmount)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<PaymentHistory>()
                .Property(ph => ph.PaymentAmount)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<PaymentHistory>()
                .Property(ph => ph.TotalAmountPayment)
                .HasColumnType("decimal(10,2)");

            // 🏷️ Cấu hình bảng PaymentTransaction
            modelBuilder.Entity<PaymentTransaction>()
                .ToTable("PaymentTransaction")
                .HasOne(pt => pt.PaymentHistory)
                .WithMany(ph => ph.PaymentTransactions)
                .HasForeignKey(pt => pt.PaymentHistoryId)
                .OnDelete(DeleteBehavior.Cascade); // Khi xóa PaymentHistory, xóa tất cả PaymentTransaction liên quan

            // **Tạo Unique Index trên RequestProductId và ProductId**
            modelBuilder.Entity<RequestProductDetail>()
                .HasIndex(d => new { d.RequestProductId, d.ProductId })
                .IsUnique();

            modelBuilder.Entity<PaymentTransaction>()
                .Property(pt => pt.Amount)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<PaymentTransaction>()
                .Property(pt => pt.TransactionReference)
                .HasMaxLength(100);

            // 🔥 Cấu hình quan hệ Order
            modelBuilder.Entity<Order>()
                .ToTable("Order")
                .HasMany(o => o.PaymentHistories)
                .WithOne(ph => ph.Order)
                .HasForeignKey(ph => ph.OrderId);


            modelBuilder.Entity<RequestExportDetail>()
                .ToTable("RequestExportDetail")
                .HasOne(red => red.RequestExport)
                .WithMany(re => re.RequestExportDetails)
                .HasForeignKey(red => red.RequestExportId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RequestExportDetail>()
                .HasOne(red => red.Product)
                .WithMany()
                .HasForeignKey(red => red.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExportWarehouseReceiptDetail>()
                .HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // ❌ Không cascade ở đây

            modelBuilder.Entity<ExportWarehouseReceiptDetail>()
                .HasOne(e => e.Batch)
                .WithMany()
                .HasForeignKey(e => e.BatchId)
                .OnDelete(DeleteBehavior.Restrict); // ❌ Không cascade

            modelBuilder.Entity<ExportWarehouseReceiptDetail>()
                .HasOne(e => e.ExportWarehouseReceipt)
                .WithMany(e => e.ExportWarehouseReceiptDetails)
                .HasForeignKey(e => e.ExportWarehouseReceiptId)
                .OnDelete(DeleteBehavior.Cascade); // ✅ Cho phép cascade ở đây

            modelBuilder.Entity<TemporaryStockExport>()
                 .Property(p => p.UnitPrice)
                .HasPrecision(18, 4); // ✅ chính xác để lưu giá tiền


            // 🔹 Thiết lập mối quan hệ 1-1 giữa Order và RequestProduct
            modelBuilder.Entity<Order>()
                .HasOne(o => o.RequestProduct)
                .WithOne(rp => rp.Order)
                .HasForeignKey<Order>(o => o.RequestId) // 🔹 FK trong Order
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Khi xóa Order, RequestProduct cũng bị xóa


            // ✅ Cấu hình quan hệ 1-N giữa Order và OrderDetail
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade); // ✅ Xóa Order sẽ xóa OrderDetail

            modelBuilder.Entity<OrderDetail>()
                .HasOne(o => o.Product)
                .WithMany()
                .HasForeignKey(o => o.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            /*modelBuilder.Entity<Product>()
                .HasOne(p => p.RequestProductDetail)
                .WithOne(d => d.Product)
                .HasForeignKey<RequestProductDetail>(d => d.ProductId);*/
            modelBuilder.Entity<Order>()
                .Property(od => od.TotalPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<RequestProductDetail>()
                .Property(od => od.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Batch>()
                .Property(od => od.SellingPrice)
                .HasPrecision(18, 2);

            // ✅ Quan hệ 1-1 giữa Order và RequestExport
            modelBuilder.Entity<RequestExport>()
                .HasOne(re => re.Order)
                .WithOne(o => o.RequestExport)
                .HasForeignKey<RequestExport>(re => re.OrderId)
                .OnDelete(DeleteBehavior.NoAction);


            // ✅ Quan hệ N-1 giữa AgencyAccount và RequestExport
            modelBuilder.Entity<RequestExport>()
                .HasOne(re => re.RequestedByAgency)
                .WithMany()
                .HasForeignKey(re => re.RequestedByAgencyId)
                .OnDelete(DeleteBehavior.Restrict);


            // Đảm bảo mỗi User chỉ có 1 Warehouse
            modelBuilder.Entity<Warehouse>()
                .HasIndex(w => w.UserId)
                .IsUnique();


            // Tắt cascade delete cho User
            modelBuilder.Entity<PaymentHistory>()
                .HasOne(p => p.User)
                .WithMany(u => u.PaymentHistories)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Tắt cascade delete cho PrePayment (self reference)
            modelBuilder.Entity<PaymentHistory>()
                .HasOne(p => p.PrePayment)
                .WithMany()
                .HasForeignKey(p => p.PrePaymentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<WarehouseTransferRequest>()
                .HasOne(w => w.SourceWarehouse)
                .WithMany()
                .HasForeignKey(w => w.SourceWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseTransferRequest>()
                .HasOne(w => w.DestinationWarehouse)
                .WithMany()
                .HasForeignKey(w => w.DestinationWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔑 Khóa chính
            modelBuilder.Entity<TemporaryStockExport>()
                .HasKey(t => t.TemporaryStockExportId);

            // 🔗 Mối quan hệ với Product (1 Product - nhiều TemporaryStockExport)
            modelBuilder.Entity<TemporaryStockExport>()
                .HasOne(t => t.Product)
                .WithMany(p => p.TemporaryStockExports)
                .HasForeignKey(t => t.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // tránh xóa cascade nếu không mong muốn

            // 🔗 Mối quan hệ với Warehouse (1 Warehouse - nhiều TemporaryStockExport)
            modelBuilder.Entity<TemporaryStockExport>()
                .HasOne(t => t.Warehouse)
                .WithMany(w => w.TemporaryStockExports)
                .HasForeignKey(t => t.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔗 Mối quan hệ với Order (1 Order - nhiều TemporaryStockExport)
            modelBuilder.Entity<TemporaryStockExport>()
                .HasOne(t => t.Order)
                .WithMany(o => o.TemporaryStockExports)
                .HasForeignKey(t => t.OrderId)
                .OnDelete(DeleteBehavior.Cascade); // có thể để Cascade nếu muốn xóa Order thì xóa luôn tạm

            modelBuilder.Entity<AgencyAccount>()
                .HasOne(a => a.ManagedByEmployee)
                .WithMany(e => e.ManagedAgencies)
                .HasForeignKey(a => a.ManagedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReturnRequest>()
                .HasMany(r => r.Details)
                .WithOne(d => d.ReturnRequest)
                .HasForeignKey(d => d.ReturnRequestId);

            modelBuilder.Entity<ReturnWarehouseReceipt>()
                .HasMany(r => r.Details)
                .WithOne(d => d.ReturnWarehouseReceipt)
                .HasForeignKey(d => d.ReturnWarehouseReceiptId);

            modelBuilder.Entity<ReturnRequestDetail>()
                .HasOne(d => d.ReturnRequest)
                .WithMany(r => r.Details)
                .HasForeignKey(d => d.ReturnRequestId)
                .OnDelete(DeleteBehavior.Restrict); // hoặc NoAction

            modelBuilder.Entity<ReturnRequestDetail>()
                .HasOne(d => d.OrderDetail)
                .WithMany()
                .HasForeignKey(d => d.OrderDetailId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReturnRequestDetail>()
                .HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DamagedStock>()
                .HasOne(d => d.Warehouse)
                .WithMany() // hoặc: .WithMany(w => w.DamagedStocks) nếu bạn thêm navigation ở Warehouse
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DamagedStock>()
                .HasOne(d => d.Product)
                .WithMany() // hoặc: .WithMany(p => p.DamagedStocks) nếu bạn thêm navigation ở Product
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AgencyAccount>()
                .HasMany(a => a.Contracts)
                .WithOne(c => c.AgencyAccount)
                .HasForeignKey(c => c.AgencyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.Sender)
                .WithMany()
                .HasForeignKey(cm => cm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.Receiver)
                .WithMany()
                .HasForeignKey(cm => cm.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }

}
