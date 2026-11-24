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
    options.Conventions.AuthorizeFolder("/UserAccounts", "StaffAdministration");
    options.Conventions.AuthorizePage("/AuditTrail", "StaffAdministration");
    options.Conventions.AuthorizePage("/DatabaseHealth", "StaffAdministration");
    options.Conventions.AuthorizeFolder("/RentalRates", "StaffAdministration");
    options.Conventions.AuthorizeFolder("/Archive", "StaffAdministration");
    options.Conventions.AuthorizeFolder("/Payments", "StaffPayments");
    options.Conventions.AuthorizePage("/Profiles/Billing", "StaffPayments");
    options.Conventions.AuthorizePage("/Profiles/Transfer", "StaffAdministration");
    options.Conventions.AuthorizePage("/Vehicles/Transfer", "StaffAdministration");
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Events.OnValidatePrincipal = async context =>
        {
            var principal = context.Principal;
            var version = principal?.FindFirst("SessionVersion")?.Value;
            if (int.TryParse(principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out int id) &&
                !string.IsNullOrEmpty(version) &&
                await context.HttpContext.RequestServices.GetRequiredService<UserAuthenticationService>()
                    .IsSessionCurrentAsync(id, version, context.HttpContext.RequestAborted))
                return;

            context.RejectPrincipal();
            context.Response.Cookies.Delete(context.Options.Cookie.Name!, context.Options.Cookie.Build(context.HttpContext));
        };
        options.Cookie.Name = "BPLS.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization(options =>
{
    bool IsListed(System.Security.Claims.ClaimsPrincipal user, string configSection)
    {
        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userId, out int id) && id > 0 &&
            builder.Configuration.GetSection(configSection).GetChildren()
                .Any(entry => int.TryParse(entry.Value, out int allowedId) && allowedId == id);
    }

    options.AddPolicy("StaffPayments", policy => policy.RequireAuthenticatedUser()
        .RequireAssertion(context => IsListed(context.User, "StaffAccess:AdministratorIds") ||
            IsListed(context.User, "StaffAccess:PaymentOperatorIds")));
    options.AddPolicy("StaffAdministration", policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context => IsListed(context.User, "StaffAccess:AdministratorIds")));
});
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
