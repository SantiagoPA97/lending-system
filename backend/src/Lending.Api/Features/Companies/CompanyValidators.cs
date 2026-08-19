using FluentValidation;

namespace Lending.Api.Features.Companies;

public sealed class CompanyRequestValidator : AbstractValidator<CompanyRequest>
{
    public CompanyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.RegistrationNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Country)
            .NotEmpty()
            .Matches("^[A-Za-z]{2}$")
            .WithMessage("Country must be a two-letter ISO 3166-1 alpha-2 code.");
        RuleFor(x => x.Industry).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ContactEmail).NotEmpty().EmailAddress().MaximumLength(320);
    }
}
