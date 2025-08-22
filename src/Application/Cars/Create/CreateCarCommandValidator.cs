using FluentValidation;

namespace Application.Cars.Create;

internal class CreateCarCommandValidator: AbstractValidator<CreateCarCommand>
{
    public CreateCarCommandValidator()
    {
        RuleFor(x => x.Make)
            .NotEmpty().WithMessage("Make is required.")
            .MaximumLength(50).WithMessage("Make cannot exceed 50 characters.");
        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Model is required.")
            .MaximumLength(50).WithMessage("Model cannot exceed 50 characters.");
        RuleFor(x => x.Year)
            .InclusiveBetween(1886, DateTime.Now.Year + 1).WithMessage($"Year must be between 1886 and {DateTime.Now.Year + 1}.");
        RuleFor(x => x.Mileage)
            .GreaterThanOrEqualTo(0).WithMessage("Mileage must be a non-negative value.");
        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be a non-negative value.");
    }
}
