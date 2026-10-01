using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using FinControl.Application;
using FinControl.Domain.Entities;
using FinControl.Domain.Exceptions;
using FinControl.Infrastructure;
using FinControl.Infrastructure.Persistence;
using FinControl.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
if (string.IsNullOrWhiteSpace(builder.Configuration["urls"])) builder.WebHost.UseUrls("http://localhost:5078");
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 8 * 1024 * 1024);
var directory = Environment.GetEnvironmentVariable("FINCONTROL_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FinControl");
Directory.CreateDirectory(directory);
var keys = Path.Combine(directory, "keys"); Directory.CreateDirectory(keys);
if (!OperatingSystem.IsWindows()) { File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); File.SetUnixFileMode(keys, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); }
var connection = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = Path.Combine(directory, "fincontrol.db"), ForeignKeys = true, DefaultTimeout = 30 }.ToString();
builder.Services.AddFinControlApplication().AddFinControlInfrastructure(connection);
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keys)).SetApplicationName("FinControl.Web");
builder.Services.AddSingleton<WriteGate>(); builder.Services.AddSingleton<InitialSetup>();
builder.Services.AddScoped<WorkspaceService>(); builder.Services.AddScoped<RecurringService>(); builder.Services.AddScoped<ImportService>(); builder.Services.AddScoped<BackupService>();
builder.Services.AddScoped<IPasswordHasher<AcessoWeb>, PasswordHasher<AcessoWeb>>();
builder.Services.Configure<PasswordHasherOptions>(o => o.IterationCount = 210000);
builder.Services.AddHostedService<RecurringWorker>();
var publicUrl = Environment.GetEnvironmentVariable("FINCONTROL_PUBLIC_URL");
if (string.IsNullOrWhiteSpace(publicUrl)) publicUrl = null;
if (!string.IsNullOrWhiteSpace(publicUrl) && (!Uri.TryCreate(publicUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https")) throw new InvalidOperationException("FINCONTROL_PUBLIC_URL deve ser uma URL HTTPS.");
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.Cookie.Name = "FinControl.Session"; o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = publicUrl is null ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromHours(12); o.SlidingExpiration = false;
    o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 403; return Task.CompletedTask; };
    o.Events.OnValidatePrincipal = async c =>
    {
        var db = c.HttpContext.RequestServices.GetRequiredService<FinControlDbContext>();
        if (!int.TryParse(c.Principal?.FindFirstValue(ClaimTypes.NameIdentifier),out var id)) { c.RejectPrincipal(); return; }
        var user = await db.AcessosWeb.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);
        if(user is null || user.SeloSeguranca!=c.Principal?.FindFirstValue("stamp")) {c.RejectPrincipal();await c.HttpContext.SignOutAsync();}
    };
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("auth", h => RateLimitPartition.GetFixedWindowLimiter(h.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = 15, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
var trustedProxy = Environment.GetEnvironmentVariable("FINCONTROL_TRUSTED_PROXY");
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    if(IPAddress.TryParse(trustedProxy, out var ip))o.KnownProxies.Add(ip);
});
var app = builder.Build();
using(var scope=app.Services.CreateScope())
{
    var db=scope.ServiceProvider.GetRequiredService<FinControlDbContext>();
    BancoDadosInicializador.Inicializar(db);
    if(!db.AcessosWeb.Any()&&db.Contas.Any()) Console.WriteLine($"\nCódigo para proteger os dados anteriores no primeiro cadastro: {app.Services.GetRequiredService<InitialSetup>().Code}\n");
}
app.UseForwardedHeaders();
if(publicUrl is not null)app.UseHsts();
app.Use(async(h,next)=>
{
    h.Response.Headers["X-Content-Type-Options"]="nosniff";
    h.Response.Headers["Referrer-Policy"]="same-origin";
    h.Response.Headers["Content-Security-Policy"]="default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'";
    h.Response.Headers["Permissions-Policy"]="camera=(), microphone=(), geolocation=()";
    if(h.Request.Path.StartsWithSegments("/api"))h.Response.Headers.CacheControl="no-store";
    try { await next(); }
    catch(Exception ex) when(!h.Response.HasStarted)
    {
        var status=ex switch {ApiException a=>a.Status,RegraDeNegocioException=>400,BadHttpRequestException b=>b.StatusCode,System.Text.Json.JsonException=>400,DbUpdateException=>409,_=>500};
        var message=ex switch {ApiException or RegraDeNegocioException=>ex.Message,DbUpdateException=>"Não foi possível salvar. Verifique registros duplicados ou referências utilizadas.",BadHttpRequestException or System.Text.Json.JsonException=>"Dados inválidos ou arquivo muito grande.",_=>"Não foi possível concluir a operação. Tente novamente."};
        if(status==500)app.Logger.LogError(ex,"Falha em {Path}",h.Request.Path);
        h.Response.StatusCode=status;await h.Response.WriteAsJsonAsync(new {message});
    }
});
app.Use(async(h,next)=>
{
    bool write=h.Request.Path.StartsWithSegments("/api")&&!HttpMethods.IsGet(h.Request.Method)&&!HttpMethods.IsHead(h.Request.Method);
    if(write&&h.Request.Headers["X-FinControl"]!="1")throw new ApiException(403,"Requisição não autorizada. Atualize a página.");
    if(write&&h.Request.Headers.TryGetValue("Origin",out var origin)&&origin.ToString()!=$"{h.Request.Scheme}://{h.Request.Host}")throw new ApiException(403,"Origem não autorizada.");
    await next();
});
app.UseDefaultFiles(); app.UseStaticFiles(new StaticFileOptions { OnPrepareResponse=c=>c.Context.Response.Headers.CacheControl="no-cache" });
app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter();
// Serializa gravações no SQLite e protege importações, restaurações e geração de recorrências.
app.Use(async(h,next)=>
{
    if(!h.Request.Path.StartsWithSegments("/api")||HttpMethods.IsGet(h.Request.Method)||HttpMethods.IsHead(h.Request.Method)){await next();return;}
    var gate=h.RequestServices.GetRequiredService<WriteGate>();await gate.Semaphore.WaitAsync(h.RequestAborted);
    try{await next();}finally{gate.Semaphore.Release();}
});
app.MapAuth(); app.MapFinance();
app.MapGet("/health",()=>Results.Ok(new {status="ok"}));

app.Logger.LogInformation("FinControl pronto. Abra http://localhost:5078 no navegador. Os dados ficam em {Directory}.",directory);
app.Run();
