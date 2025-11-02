
using Microsoft.EntityFrameworkCore;
using UserService.gRPC.Models;

namespace UserService.gRPC.Data
{
    public class UserDbContext : DbContext
    {

        public UserDbContext(DbContextOptions<UserDbContext> options)
           : base(options)
        {
        }


        public DbSet<User> Users { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(p => p.Email)
                    .HasDatabaseName("IX_Users_Email");

                entity.HasIndex(p => p.Role)
                   .HasDatabaseName("IX_Users_Role");

                entity.HasIndex(p => p.IsActive)
                 .HasDatabaseName("IX_Users_IsActive");

                // Configuración de restricciones
                entity.Property(p => p.Email)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(p => p.FirstName)
                    .HasMaxLength(100);

                entity.Property(p => p.LastName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(p => p.PasswordHash)
                   .IsRequired()
                   .HasColumnType("nvarchar(max)");

                entity.Property(p => p.Role)
                  .IsRequired()
                  .HasMaxLength(50);

                entity.Property(p => p.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(p => p.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

            });
        }
    }
}
