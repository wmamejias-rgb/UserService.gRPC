# TAREA 1 - UserService.gRPC
	Eduardo González Bustos <egonzalezb@Poder-Judicial.go.cr>
	Erick Walsh Pizarro <ewalsh@Poder-Judicial.go.cr>
	Wendy Mejías Acevedo <wmejias@Poder-Judicial.go.cr>


## Ejecutar proyecto y creación de contenedores
En la carpeta del proyecto UserService.gRPC ejecutar en ventana de PowerShell o Command Prompt:

docker-compose up -d


## Testing de los métodos del servicio usando comando grpcurl

Obtiene un usuario por su id:

grpcurl -plaintext -d '{\"id\": 1}' localhost:7002 userservice.UserService/GetUser

Obtiene un listado de usuario con paginación:

grpcurl -plaintext -d '{\"page_number\": 1, \"page_size\": 10}' localhost:7002 userservice.UserService/GetUsers

Crea un nuevo usuario:

grpcurl -plaintext -d '{\"email\": \"john@gmail.com\", \"first_name\": \"John\", \"last_name\": \"Smith\", \"password\" : \"12345678\" , \"role\": \"Customer\"}' localhost:7002 userservice.UserService/CreateUser

Actualiza un usuario en particular:

UserService.gRPC> grpcurl -plaintext -d '{\"id\":4 , \"email\": \"john@gmail.com\", \"first_name\": \"John\", \"last_name\": \"Smith\", \"role\": \"Premium\"}' localhost:7002 userservice.UserService/UpdateUser

Eliminado lógico de un usuario por su id:

grpcurl -plaintext -d '{\"id\":4}' localhost:7002 userservice.UserService/DeleteUser