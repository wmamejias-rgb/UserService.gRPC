


using ECommerceGRPC.UserService;
using FluentValidation;

namespace UserService.gRPC.Validators
{
    public class GetUserRequestValidator: AbstractValidator<GetUserRequest>
    {

        public GetUserRequestValidator() {

            RuleFor(x => x.Id).GreaterThan(0).WithMessage("El Id del usuario debe ser mayor a cero");
        }

    }

    public class GetUsersRequestValidator : AbstractValidator<GetUsersRequest>
    {

        public GetUsersRequestValidator()
        {

            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("El número de página debe ser mayor a cero");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("El tamaño de página debe ser mayor a cero")
                .LessThanOrEqualTo(100).WithMessage("El tamaño de página no puede exceder 100 elementos");
        }

    }


    public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
    {
        public CreateUserRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Correo es requerido")
                .EmailAddress().WithMessage("Formato invalido de correo");

            RuleFor(x => x.FirstName)
               .NotEmpty().WithMessage("Nombre es requerido")
               .MaximumLength(100).WithMessage("Nombre no puede ser mayor a 100 caracteres");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Apellido es requerido")
                .MaximumLength(100).WithMessage("Apellido no puede ser mayor a 100 caracteres");

            RuleFor(x => x.Password)
               .NotEmpty().WithMessage("Password es requerido")
               .MinimumLength(8).WithMessage("Password debe ser de al menos 8 caracteres");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Rol es requerido. Valores permitidos :Customer, Premium, Admin");
                
        }

    }


    public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
    {
        public UpdateUserRequestValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("El ID del usuario debe ser mayor a cero");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Correo es requerido")
                .EmailAddress().WithMessage("Formato invalido de correo");

            RuleFor(x => x.FirstName)
               .NotEmpty().WithMessage("Nombre es requerido")
               .MaximumLength(100).WithMessage("Nombre no puede ser mayor a 100 caracteres");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Apellido es requerido")
                .MaximumLength(100).WithMessage("Apellido no puede ser mayor a 100 caracteres");

            
            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Rol es requerido. Valores permitidos :Customer, Premium, Admin");

        }

    }



    public class DeleteUserRequestValidator : AbstractValidator<DeleteUserRequest>
    {

        public DeleteUserRequestValidator()
        {

            RuleFor(x => x.Id).GreaterThan(0).WithMessage("El Id del usuario debe ser mayor a cero");
        }

    }
}
