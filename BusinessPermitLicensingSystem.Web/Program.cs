using BusinessPermitLicensingSystem.Web.Authentication;
using BusinessPermitLicensingSystem.Web.Profiles;
using BusinessPermitLicensingSystem.Web.Billing;
using BusinessPermitLicensingSystem.Web.Vehicles;
using BusinessPermitLicensingSystem.Web.Archive;
using BusinessPermitLicensingSystem.Web.Transfer;
using BusinessPermitLicensingSystem.Web.Reports;
using BusinessPermitLicensingSystem.Web.Administration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
// The host owns environment selection; database configuration cannot override it.
builder.Configuration["Database:Environment"] = builder.Environment.EnvironmentName;
if (!builder.Environment.IsDevelopment())
{
    BusinessPermitLicensingSystem.Web.Data.DatabaseConnection.Validate(builder.Configuration);
    // Prevent a staging/production cookie from authenticating against another target.
    // Development retains its existing cookie protection scope.
    string scope = string.Join("|", "BPLO", builder.Environment.EnvironmentName,
        builder.Configuration["Database:ApprovedServerIdentity"],
        builder.Configuration["Database:ApprovedDatabase"]).ToUpperInvariant();
    builder.Services.AddDataProtection().SetApplicationName(scope);
}

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/Create");
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.Cookie.Name = "BPLS.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<UserAuthenticationService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<BillingService>();
builder.Services.AddScoped<VehicleService>();
builder.Services.AddScoped<VehicleRecordService>();
builder.Services.AddScoped<ArchiveService>();
builder.Services.AddScoped<TransferService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<VehiclePaymentReportService>();
builder.Services.AddScoped<VehicleMayorPermitService>();
builder.Services.AddScoped<RentalRateService>();
builder.Services.AddScoped<AuditTrailService>();
builder.Services.AddScoped<UserAccountService>();

var app = builder.Build();

app.UseExceptionHandler("/Error");
if (!app.Environment.IsDevelopment()) app.UseHsts();

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
