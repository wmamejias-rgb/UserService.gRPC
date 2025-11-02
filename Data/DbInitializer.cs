

using Microsoft.EntityFrameworkCore;
using UserService.gRPC.Models;

namespace UserService.gRPC.Data
{
    public class DbInitializer
    {


        public static async Task InitializeAsync(UserDbContext context, ILogger logger)
        {
            try
            {
                // Asegurar que la base de datos esté creada
                logger.LogInformation("Verificando existencia de base de datos...");
                await context.Database.EnsureCreatedAsync();

                // Aplicar migraciones pendientes
                if (context.Database.GetPendingMigrations().Any())
                {
                    logger.LogInformation("Aplicando migraciones pendientes...");
                    await context.Database.MigrateAsync();
                }

                // Verificar si ya existen Users
                if (await context.Users.AnyAsync())
                {
                    logger.LogInformation("Base de datos ya contiene usuarios. Omitiendo inicialización.");
                    return;
                }

                logger.LogInformation("Inicializando base de datos con datos de prueba...");

                var usuarios = new List<User>
                {
                    new User
                    {
                        Email  = "wmejias@poder-judicial.go.cr",
                        FirstName = "Wendy",
                        LastName = "Mejías Acevedo",
                        Role = "Customer",
                        PasswordHash = "12345678",
                        CreatedAt = DateTime.UtcNow,
                        LastLogin = DateTime.UtcNow,
                        IsActive = true
                    },
                     new User
                    {
                        Email  = "egonzalezb@poder-judicial.go.cr",
                        FirstName = "Eduardo",
                        LastName = "Gonzalez Bustos",
                        Role = "Admin",
                        PasswordHash = "12345678",
                        CreatedAt = DateTime.UtcNow,
                        LastLogin = DateTime.UtcNow,
                        IsActive = true
                    },
                     new User
                    {
                        Email  = "ewalsh@poder-judicial.go.cr",
                        FirstName = "Erick",
                        LastName = "Walsh Pizarro",
                        Role = "Premium",
                        PasswordHash = "12345678",
                        CreatedAt = DateTime.UtcNow,
                        LastLogin = DateTime.UtcNow,
                        IsActive = true
                    }
                };
                // Agregar usuarios a la base de datos
                await context.Users.AddRangeAsync(usuarios);
                await context.SaveChangesAsync();

                logger.LogInformation($"Base de datos inicializada exitosamente con {usuarios.Count} usuarios.");
            }

            catch (Exception ex)
            {
                logger.LogError(ex, "Error al inicializar la base de datos.");
                throw;
            }

        }
    }
}
