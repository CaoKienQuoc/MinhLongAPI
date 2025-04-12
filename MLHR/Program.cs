using BusinessObject.DTO.PaymentDTO;
using DataAccessLayer;
using DinkToPdf.Contracts;
using DinkToPdf;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Repo.IRepository;
using Repo.Repository;
using Services.IService;
using Services.Service;
using System.Security.Claims;
using System.Text;
using Services.Exceptions;
using Hangfire;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

// Lấy cấu hình JWT từ appsettings.json
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);


// Cấu hình JWT Bearer Authentication cho Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "MLHR API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Nhập JWT Token theo định dạng: Bearer YOUR_TOKEN_HERE",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, new string[] {} }
    });
});

// Thêm Authentication & Authorization
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        RoleClaimType = ClaimTypes.Role, // ✅ Đảm bảo Role đọc đúng
        ClockSkew = TimeSpan.Zero // ✅ Không cho phép thời gian trễ
    };

    // ✅ Cho phép dùng access_token trong SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs/notifications"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ✅ Đọc chuỗi kết nối từ appsettings.json
var connectionString = builder.Configuration.GetConnectionString("ServerConnection");
builder.Services.AddDbContext<MinhLongDbContext>(options =>
    options.UseSqlServer(connectionString));

// 📌 Hangfire Configuration
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(connectionString));
builder.Services.AddHangfireServer();

// ✅ Đăng ký Services và Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<ILocationService, LocationService>();

builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();

builder.Services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();

builder.Services.AddScoped<ITaxConfigRepository, TaxConfigRepository>();
builder.Services.AddScoped<ITaxConfigService, TaxConfigService>();

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRoleService, RoleService>();

builder.Services.AddScoped<IWarehouseReceiptRepository, WarehouseReceiptRepository>();
builder.Services.AddScoped<IWarehouseReceiptService, WarehouseReceiptService>();

builder.Services.AddScoped<IRequestProductRepository, RequestProductRepository>(); 
builder.Services.AddScoped<IRequestProductService, RequestProductService>();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<IExportRepository, ExportRepository>();
builder.Services.AddScoped<IRequestExportService, RequestExportService>();

builder.Services.AddScoped<IExportWarehouseReceiptService, ExportWarehouseReceiptService>();
builder.Services.AddScoped<IExportWarehouseReceiptRepository, ExportWarehouseReceiptRepository>();

builder.Services.AddScoped<IBatchRepository, BatchRepository>();
builder.Services.AddScoped<IBatchService, BatchService>();

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddScoped<IWarehouseRequestExportRepository, WarehouseRequestExportRepository>();
builder.Services.AddScoped<IWarehouseRequestExportService, WarehouseRequestExportService>();

builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IImageService, ImageService>();

builder.Services.AddScoped<IPaymentHistoryRepository, PaymentHistoryRepository>();
builder.Services.AddScoped<IPaymentHistoryService, PaymentHistoryService>();

builder.Services.AddScoped<IWarehouseTransferRepository, WarehouseTransferRepository>();
builder.Services.AddScoped<IWarehouseTransferService, WarehouseTransferService>();

builder.Services.AddScoped<IAgencyLevelRepository, AgencyLevelRepository>();
builder.Services.AddScoped<IAgencyLevelService, AgencyLevelService>();

builder.Services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
builder.Services.AddScoped<IPaymentTransactionService, PaymentTransactionService>();
builder.Services.AddMemoryCache(); // hoặc services.AddMemoryCache() nếu dùng Startup
builder.Services.AddScoped<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<IAgencyAccountRepository, AgencyAccountRepository>();
builder.Services.AddScoped<IAgencyAccountLevelRepository, AgencyAccountLevelRepository>();

builder.Services.AddScoped<IWarehouseProductRepository, WarehouseProductRepository>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<ITemporaryWarehouseExportRepository, TemporaryWarehouseExportRepository>();

builder.Services.AddScoped<JwtService>();
builder.Services.AddHttpContextAccessor();

if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    var path = "/usr/local/lib/libwkhtmltox.so";
    if (File.Exists(path))
    {
        var context = new CustomAssemblyLoadContext();
        context.LoadUnmanagedLibrary(path);
        Console.WriteLine("✅ libwkhtmltox.so loaded in Program.cs");
    }
    else
    {
        Console.WriteLine("❌ Không tìm thấy libwkhtmltox.so tại " + path);
    }
}

// ✅ Sau đó mới tạo PdfTools
var pdfTools = new PdfTools();
var converter = new SynchronizedConverter(pdfTools);
builder.Services.AddSingleton<IConverter>(converter);


/*var pdfTools = new PdfTools();
var converter = new SynchronizedConverter(pdfTools);
builder.Services.AddSingleton<IConverter>(converter);*/

// 🔹 Đăng ký các service khác
builder.Services.AddScoped<PdfService>();


var configuration = builder.Configuration;
builder.Services.Configure<PayOSSettings>(configuration.GetSection("PayOS"));


// ✅ Đăng ký Controllers với JSON Options
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.WriteIndented = true; // Nếu muốn JSON format đẹp hơn
});

// ✅ Đăng ký HttpClient
builder.Services.AddHttpClient();

// ✅ Đăng ký Swagger để test API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/*// ✅ Bật CORS (cho phép gọi API từ các client khác nhau)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});*/
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "https://admin-warehouse-otme.vercel.app", // ✅ domain chính thức
                "http://localhost:5173",                   // ✅ local FE
                "https://clone-ui-user.vercel.app"         // ✅ nếu có clone UI
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // ✅ Cho phép gửi token/cookie
    });
});


builder.Services.AddSignalR();



var app = builder.Build();

// ✅ Cấu hình Middleware cho Swagger (chỉ trong môi trường Development)


//134
app.UseSwagger();
app.UseSwaggerUI();


// ✅ Bật HTTPS
app.UseHttpsRedirection();

app.UseRouting();

/*// ✅ Bật CORS
app.UseCors("AllowAllOrigins");*/

app.UseCors("AllowFrontend");


app.UseAuthentication();
// ✅ Bật Authorization
app.UseAuthorization();

// ✅ Hangfire Dashboard & Recurring Job
app.UseHangfireDashboard("/hangfire"); // Giao diện quản lý job
RecurringJob.AddOrUpdate<IPaymentHistoryService>(
    x => x.SendDebtRemindersAsync(),
    Cron.Daily() // 🕒 Gửi mỗi ngày - bạn có thể test nhanh bằng Cron.Minutely
);

// ✅ Kích hoạt API Controllers
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers(); // 🟢 Cần có dòng này!
    endpoints.MapHub<NotificationHub>("/hubs/notifications"); // Định tuyến cho hub
});

app.Run();
