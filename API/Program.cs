var builder = WebApplication.CreateBuilder(args);

// add services to the container

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddCors();
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddSignalR();

// Configure the HTTP request pipeline
string connString;
if (builder.Environment.IsDevelopment()) {
    Console.WriteLine("Dev-------------------------------------------------------------------------------");
    connString = builder.Configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine(connString);
    Console.WriteLine("-------------------------------------------------------------------------------");

}
else 
{
// Use connection string provided at runtime by FlyIO.
        Console.WriteLine("Prod-------------------------------------------------------------------------------");
        var connUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

        Console.WriteLine($"++++++++++++++++++++");
        Console.WriteLine($"connUrl1:{connUrl}");
        connUrl = "secret";
        Console.WriteLine($"connUrl2:{connUrl}");
        Console.WriteLine($"++++++++++++++++++++");

        // Parse connection URL to connection string for Npgsql
        connUrl = connUrl.Replace("postgres://", string.Empty);

        var pgUserPass = connUrl.Split("@")[0];
        var pgHostPortDb = connUrl.Split("@")[1];
        var pgHostPort = pgHostPortDb.Split("/")[0];
        var pgDb = pgHostPortDb.Split("/")[1];
        var pgUser = pgUserPass.Split(":")[0];
        var pgPass = pgUserPass.Split(":")[1];
        var pgHost = pgHostPort.Split(":")[0];
        var pgPort = pgHostPort.Split(":")[1];
        var updatedHost = pgHost.Replace("flycast", "internal");
        // pgHost = "fdaa:2:a97f:0:1::3";

        connString = $"Server={updatedHost};Port={pgPort};User Id={pgUser};Password={pgPass};Database={pgDb};";
        Console.WriteLine($"pgUserPass:{pgUserPass}");
        Console.WriteLine($"pgHostPortDb: {pgHostPortDb}");
        Console.WriteLine($"pgDb: {pgDb}");
        Console.WriteLine($"pgUser: {pgUser}");
        Console.WriteLine($"pgPass: {pgPass}");
        Console.WriteLine($"pgHost: {pgHost}");
        Console.WriteLine($"pgPort: {pgPort}");
        Console.WriteLine($"ConnString: {connString}");
        Console.WriteLine("-------------------------------------------------------------------------------");
}

Console.WriteLine("Started-------------------------------------------------------------------------------");
builder.Services.AddDbContext<DataContext>(opt =>
{
    Console.WriteLine("proc");
    opt.UseNpgsql(connString);
});

Console.WriteLine("Completed-------------------------------------------------------------------------------");
var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseCors(x => x.AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .WithOrigins("https://localhost:4200"));

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<PresenceHub>("hubs/presence");
app.MapHub<MessageHub>("hubs/message");
app.MapFallbackToController("Index", "Fallback");

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
try
{
    var context = services.GetRequiredService<DataContext>();
    var userManager = services.GetRequiredService<UserManager<AppUser>>();
    var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
    await context.Database.MigrateAsync();
    await Seed.ClearConnections(context);
    await Seed.SeedUsers(userManager, roleManager);
}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred during migration");
}

await app.RunAsync();