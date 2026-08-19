using FluentValidation;

namespace Lending.Api.Features.Facilities;

public static class MoneyRules
{
    public const decimal MaxAmount = 999_999_999_999.99m;

    public static bool HasAtMostTwoDecimals(decimal value) => decimal.Round(value, 2) == value;
}

public abstract class FacilityTermsValidator<T> : AbstractValidator<T> where T : IFacilityTermsRequest
{
    protected FacilityTermsValidator()
    {
        RuleFor(x => x.CommitmentAmount)
            .InclusiveBetween(0.01m, MoneyRules.MaxAmount)
            .Must(MoneyRules.HasAtMostTwoDecimals)
            .WithMessage("Commitment amount must have at most two decimal places.");
        RuleFor(x => x.Currency).IsInEnum();
        RuleFor(x => x.AnnualInterestRate)
            .InclusiveBetween(0m, 100m)
            .WithMessage("Interest rate must be between 0 and 100 percent.")
            .Must(r => decimal.Round(r, 4) == r)
            .WithMessage("Interest rate must have at most four decimal places.");
        RuleFor(x => x.TermMonths).InclusiveBetween(1, 480);
        RuleFor(x => x.StartDate)
            .NotEqual(default(DateOnly))
            .WithMessage("Start date is required.");
        RuleFor(x => x.RepaymentType).IsInEnum();
    }
}

public sealed class CreateFacilityRequestValidator : FacilityTermsValidator<CreateFacilityRequest>
{
    public CreateFacilityRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}

public sealed class UpdateFacilityRequestValidator : FacilityTermsValidator<UpdateFacilityRequest>;

public sealed class SchedulePreviewRequestValidator : FacilityTermsValidator<SchedulePreviewRequest>;
