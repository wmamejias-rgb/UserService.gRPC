using ECommerceGRPC.UserService;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using UserService.gRPC.Data;
using UserService.gRPC.Services;
using UserService.gRPC.Validators;


Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/userservice-.log", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();


try
{
    Log.Information("Iniciando UserService gRPC...");

    var builder = WebApplication.CreateBuilder(args);

    // Configurar Serilog como proveedor de logging
    builder.Host.UseSerilog();

    // Configurar Entity Framework Core con SQL Server
    builder.Services.AddDbContext<UserDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
    });

    // Registrar validadores de FluentValidation
    
    
    builder.Services.AddScoped<IValidator<GetUserRequest>, GetUserRequestValidator>();
    builder.Services.AddScoped<IValidator<GetUsersRequest>, GetUsersRequestValidator>();
    builder.Services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
    builder.Services.AddScoped<IValidator<UpdateUserRequest>, UpdateUserRequestValidator>();
    builder.Services.AddScoped<IValidator<DeleteUserRequest>, DeleteUserRequestValidator>();
    builder.Services.AddScoped<IValidator<SearchUsersRequest>, SearchUsersRequestValidator>();


    // Registrar servicios gRPC
    builder.Services.AddGrpc(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4 MB
        options.MaxSendMessageSize = 4 * 1024 * 1024;    // 4 MB
    });

    // Configurar reflexión de gRPC para herramientas de desarrollo
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddGrpcReflection();
    }

    var app = builder.Build();

    // Inicializar base de datos y datos de prueba
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<UserDbContext>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            await DbInitializer.InitializeAsync(context, logger);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al inicializar la base de datos");
            throw;
        }
    }

    // Configurar pipeline HTTP
    if (app.Environment.IsDevelopment())
    {
        app.MapGrpcReflectionService();
    }

    // Mapear servicio gRPC
    app.MapGrpcService<UserGrpcService>();

    // Endpoint de salud básico
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "Healthy",
        service = "UserService.gRPC",
        timestamp = DateTime.UtcNow
    }));

    // Endpoint de información del servicio
    app.MapGet("/", () => Results.Ok(new
    {
        service = "UserService gRPC",
        version = "1.0.0",
        description = "Servicio de gestión de usuarios con comunicación gRPC",
        endpoints = new[]
        {
            "GetUser - Obtiene un usuario por ID",   
            "GetUsers - Obtiene un listado de usuarios con paginación (streaming) ",
            "CreateUser - Crea un nuevo usuario",
            "UpdateUser - Actualiza un usuario existente",
            "DeleteUser - Elimina un usuario (lógico)",
            "SearchUsers - Busqueda por término en email y firstname"
        },
        grpcPort = 7002,
        healthCheck = "/health"
    }));

    Log.Information("UserService gRPC iniciado exitosamente en puerto 7002");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación falló al iniciar");
    throw;
}
finally
{
    Log.CloseAndFlush();
}