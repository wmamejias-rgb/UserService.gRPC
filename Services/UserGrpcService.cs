
using ECommerceGRPC.UserService;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using System;
using UserService.gRPC.Data;
using UserService.gRPC.Models;
using BC = BCrypt.Net.BCrypt;

namespace UserService.gRPC.Services
{
    public class UserGrpcService : global::ECommerceGRPC.UserService.UserService.UserServiceBase
    {

        private readonly UserDbContext _context;
        private readonly ILogger<UserGrpcService> _logger;
        private readonly IValidator<GetUserRequest> _getUserValidator;
        private readonly IValidator<GetUsersRequest> _getUsersValidator;
        private readonly IValidator<CreateUserRequest> _createUserValidator;
        private readonly IValidator<UpdateUserRequest> _updateUserValidator;
        private readonly IValidator<DeleteUserRequest> _deleteUserValidator;
        private readonly IValidator<SearchUsersRequest> _searchUserValidator;


        public UserGrpcService(
            UserDbContext context,
            ILogger<UserGrpcService> logger,
            IValidator<GetUserRequest> getUserValidator,
            IValidator<GetUsersRequest> getUsersValidator,
            IValidator<CreateUserRequest> createUserValidator,
            IValidator<UpdateUserRequest> updateUserValidator,
            IValidator<DeleteUserRequest> deleteUserValidator
            )
        {

            _context = context;
            _logger = logger;
            _getUserValidator = getUserValidator;
            _getUsersValidator = getUsersValidator;
            _createUserValidator = createUserValidator;
            _updateUserValidator = updateUserValidator; 
            _deleteUserValidator = deleteUserValidator; 


        }
        /// <summary>
        /// Obtiene un usuario por su ID/ Unary
        /// </summary>        
        public override async Task<UserResponse> GetUser(GetUserRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"GetUser llamado para ID: {request.Id}");

                // Validar solicitud mayor a cero
                var validationResult = await _getUserValidator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning($"Validación fallida: {errors}");
                    throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
                }

                //Busca si usuario existe en base de datos
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.Id);
                if (user == null)
                {
                    _logger.LogWarning($"User con {request.Id} no se encontró");
                    throw new RpcException(new Status(StatusCode.NotFound,
                        $"Usuario con ID {request.Id} no existe"));
                }

                _logger.LogInformation($"Usuario {user.FirstName} encontrado exitosamente");

                return MapToUserResponse(user);

            }
            catch (RpcException)
            {
                throw;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener usuario con ID {request.Id}");
                throw new RpcException(new Status(StatusCode.Internal,
                    "Error interno al procesar la solicitud"));
            }


        }

        /// <summary>
        /// Obtiene una lista de usuarios con paginación (Server Streaming)
        /// </summary>
        public override async Task GetUsers(GetUsersRequest request, IServerStreamWriter<UserResponse> responseStream, 
            ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("GetUser llamado con paginación: " +
                       $"Página={request.PageNumber}, Tamaño={request.PageSize}");

                // Validar solicitud
                var validationResult = await _getUsersValidator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning($"Validación fallida: {errors}");
                    throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
                }

                // Construir query base
                var query = _context.Users.AsQueryable();

                // Aplicar filtro de activos si se solicita
                if (request.ActiveOnly)
                {
                    query = query.Where(p => p.IsActive);
                }

                // Aplicar filtro de rol si se proporciona
                if (!string.IsNullOrWhiteSpace(request.Rol))
                {
                    query = query.Where(p => p.Role == request.Rol);
                }

                // Calcular paginación
                var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
                var pageSize = request.PageSize > 0 ? request.PageSize : 10;
                var skip = (pageNumber - 1) * pageSize;


                // Obtener usuarios con paginación
                var usuarios = await query
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation($"Se encontraron {usuarios.Count} usuarios");

                // Enviar usuarios en streaming
                foreach (var product in usuarios)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("GetUsuarios cancelado por el cliente");
                        break;
                    }

                    var response = MapToUserResponse(product);
                    await responseStream.WriteAsync(response);
                }

                _logger.LogInformation($"GetUsers completado - {usuarios.Count} usuarios enviados");

            }

            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener lista de usuarios");
                throw new RpcException(new Status(StatusCode.Internal,
                    "Error interno al procesar la solicitud"));
            }


        }


        /// <summary>
        /// Crea un usuario / Unary
        /// </summary>        
        public override async Task<UserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
        {

            try
            {
                _logger.LogInformation($"CreateUser llamado para: {request.Email}");

                // Validar solicitud
                var validationResult = await _createUserValidator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning($"Validación fallida: {errors}");
                    throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
                }

                // Verificar si ya existe un usuario con el mismo email
                var existingUsuario = await _context.Users
                    .FirstOrDefaultAsync(p => p.Email == request.Email);

                if (existingUsuario != null)
                {
                    _logger.LogWarning($"Usuario con email '{request.Email}' ya existe");
                    throw new RpcException(new Status(StatusCode.AlreadyExists,
                        $"Ya existe un usuario con el email '{request.Email}'"));
                }

                // Crear nueva entidad
                var usuario = new User
                {
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,    
                    Role = request.Role,
                    PasswordHash = BC.HashPassword(request.Password),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                // Guardar en la base de datos
                _context.Users.Add(usuario);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Usuario creado exitosamente con ID: {usuario.Id}");
                return MapToUserResponse(usuario);

            }
            catch (RpcException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario.");
                throw new RpcException(new Status(StatusCode.Internal,
                    "Error interno al procesar la solicitud"));
            }

            
            
        }

        /// <summary>
        /// Actualiza un usuario existente / Unary
        /// </summary>        
        public override async Task<UserResponse> UpdateUser(UpdateUserRequest request, ServerCallContext context)
        {

            try
            {
                _logger.LogInformation($"UpdateUser llamado para ID: {request.Id}");

                // Validar solicitud
                var validationResult = await _updateUserValidator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning($"Validación fallida: {errors}");
                    throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
                }

                // Buscar usuario existente
                var usuario = await _context.Users
                    .FirstOrDefaultAsync(p => p.Id == request.Id);

                if (usuario == null)
                {
                    _logger.LogWarning($"Usuario con ID {request.Id} no encontrado");
                    throw new RpcException(new Status(StatusCode.NotFound,
                        $"Usuario con ID {request.Id} no existe"));
                }

                // Actualizar propiedades
                usuario.Email = request.Email;
                usuario.FirstName = request.FirstName;
                usuario.LastName = request.LastName;
                usuario.Role = request.Role;
                
                // Guardar cambios
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Usuario {usuario.Id} actualizado exitosamente");
                return MapToUserResponse(usuario);

            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar usuario con ID {request.Id}");
                throw new RpcException(new Status(StatusCode.Internal,
                    "Error interno al procesar la solicitud"));
            }


        }

        /// <summary>
        /// Eliminado lógico de un usuario por su ID / Unary
        /// </summary>        
        public override async Task<DeleteUserResponse> DeleteUser(DeleteUserRequest request, ServerCallContext context)
        {
            
            try
            {
                _logger.LogInformation($"DeleteUser llamado para ID: {request.Id}");

                // Validar solicitud
                var validationResult = await _deleteUserValidator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning($"Validación fallida: {errors}");
                    throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
                }

                // Buscar usuario
                var usuario = await _context.Users
                    .FirstOrDefaultAsync(p => p.Id == request.Id);

                if (usuario == null)
                {
                    _logger.LogWarning($"Usuario con ID {request.Id} no encontrado");
                    throw new RpcException(new Status(StatusCode.NotFound,
                        $"Usuario con ID {request.Id} no existe"));
                }
                // Eliminación lógica
                usuario.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Usuario {usuario.Id} eliminado exitosamente (lógico)");

                return new DeleteUserResponse
                {
                    Success = true,
                    Message = $"Usuario '{usuario.Email}' eliminado exitosamente"
                };

            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar usuario con ID {request.Id}");
                throw new RpcException(new Status(StatusCode.Internal,
                    "Error interno al procesar la solicitud"));
            }

        }


        /// <summary>
        /// Mapea un Usuario del modelo a un mensaje Protobuffer  UserResponse
        /// </summary>        
        private UserResponse MapToUserResponse(User usuario)
        {
            return new UserResponse
            {
                Id = usuario.Id,
                Email = usuario.Email,
                FirstName = usuario.FirstName,
                LastName = usuario.LastName,
                Role = usuario.Role,
                CreatedAt = usuario.CreatedAt.ToString("o"),
                LastLogin = usuario.LastLogin?.ToString("o") ?? string.Empty,
                IsActive = usuario.IsActive
            };
        }

    }
}
