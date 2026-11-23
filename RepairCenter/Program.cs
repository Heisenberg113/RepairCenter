using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using System.Text;
using System.Security.Claims;
using RepairCenter.Data;
using RepairCenter.Entities;
using RepairCenter.Repositories;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Services;
using RepairCenter.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<RepairCenterDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key must be configured.");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must contain at least 32 UTF-8 bytes.");
var jwtIssuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer must be configured.");
var jwtAudience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience must be configured.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = jwtIssuer,
            ValidateAudience = true, ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                if (!int.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ||
                    !int.TryParse(principal?.FindFirstValue("token_version"), out var tokenVersion))
                {
                    context.Fail("Token is missing account version claims.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<RepairCenterDbContext>();
                var current = await db.Users.AsNoTracking()
                    .Where(user => user.Id == userId)
                    .Select(user => new { user.IsActive, user.TokenVersion, user.MustChangePassword })
                    .SingleOrDefaultAsync(context.HttpContext.RequestAborted);
                if (current is null || !current.IsActive || current.TokenVersion != tokenVersion)
                {
                    context.Fail("Token has been revoked or its account is inactive.");
                    return;
                }

                var isPasswordChangeEndpoint = string.Equals(context.HttpContext.Request.Path.Value,
                    "/api/auth/change-temporary-password", StringComparison.OrdinalIgnoreCase);
                if (current.MustChangePassword && !isPasswordChangeEndpoint)
                {
                    context.Fail("Password change is required before using the system.");
                    return;
                }

                if (principal!.IsInRole("Technician"))
                {
                    var hasActiveTechnicianProfile = await db.Technicians.AsNoTracking()
                        .AnyAsync(technician => technician.UserId == userId && technician.IsActive,
                            context.HttpContext.RequestAborted);
                    if (!hasActiveTechnicianProfile && principal.Identity is ClaimsIdentity identity)
                    {
                        foreach (var roleClaim in identity.FindAll(ClaimTypes.Role)
                                     .Where(claim => claim.Value == "Technician").ToArray())
                            identity.RemoveClaim(roleClaim);
                    }
                }
            }
        };
    });
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser().Build());
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString();
        await context.HttpContext.Response.WriteAsJsonAsync(
            new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Bạn đã đăng nhập quá nhiều lần. Vui lòng thử lại sau."
            }, cancellationToken);
    };
});
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<
    ICustomerRepository,
    CustomerRepository>();

builder.Services.AddScoped<
    ICustomerService,
    CustomerService>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IDeviceTypeRepository, DeviceTypeRepository>();
builder.Services.AddScoped<IDeviceTypeService, DeviceTypeService>();
builder.Services.AddScoped<IRepairServiceRepository, RepairServiceRepository>();
builder.Services.AddScoped<IRepairServiceService, RepairServiceService>();
builder.Services.AddScoped<IRepairTicketRepository, RepairTicketRepository>();
builder.Services.AddScoped<IRepairTicketService, RepairTicketService>();
builder.Services.AddScoped<IRepairTicketDetailRepository, RepairTicketDetailRepository>();
builder.Services.AddScoped<IRepairTicketDetailService, RepairTicketDetailService>();
builder.Services.AddScoped<ITechnicianRepository, TechnicianRepository>();
builder.Services.AddScoped<ITechnicianService, TechnicianService>();
builder.Services.AddScoped<ISparePartRepository, SparePartRepository>();
builder.Services.AddScoped<ISparePartService, SparePartService>();
builder.Services.AddScoped<IInventoryTransactionRepository, InventoryTransactionRepository>();
builder.Services.AddScoped<IInventoryTransactionService, InventoryTransactionService>();
builder.Services.AddScoped<IBillingRepository, BillingRepository>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IWarrantyWorkflowRepository, WarrantyWorkflowRepository>();
builder.Services.AddScoped<IWarrantyWorkflowService, WarrantyWorkflowService>();
builder.Services.AddHostedService<InvoiceOverdueBackgroundService>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();
var app = builder.Build();

await AccountBootstrapper.InitializeAsync(app.Services, app.Configuration);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
