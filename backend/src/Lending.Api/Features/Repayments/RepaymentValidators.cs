using FluentValidation;
using Lending.Api.Features.Facilities;

namespace Lending.Api.Features.Repayments;

public sealed class RecordRepaymentRequestValidator : AbstractValidator<RecordRepaymentRequest>
{
    public RecordRepaymentRequestValidator()
    {
        RuleFor(x => x.Amount)
            .InclusiveBetween(0.01m, MoneyRules.MaxAmount)
            .Must(MoneyRules.HasAtMostTwoDecimals)
            .WithMessage("Amount must have at most two decimal places.");
        RuleFor(x => x.Currency).IsInEnum();
        RuleFor(x => x.PaymentDate)
            .NotEqual(default(DateOnly))
            .WithMessage("Payment date is required.");
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
