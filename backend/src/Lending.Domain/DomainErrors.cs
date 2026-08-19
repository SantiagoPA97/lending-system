namespace Lending.Domain;

/// <summary>
/// Domain error codes surfaced to clients via the RFC7807 "errorCode" extension.
/// The string values are part of the API contract and must not change.
/// </summary>
public static class DomainErrors
{
    public static class Company
    {
        public const string InvalidTransition = "company.invalid_transition";
        public const string Inactive = "company.inactive";
        public const string DuplicateName = "company.duplicate_name";
    }

    public static class Facility
    {
        public const string InvalidTransition = "facility.invalid_transition";
        public const string InvalidTerms = "facility.invalid_terms";
        public const string NotEditable = "facility.not_editable";
        public const string NotActive = "facility.not_active";
    }

    public static class Repayment
    {
        public const string InvalidAmount = "repayment.invalid_amount";
        public const string CurrencyMismatch = "repayment.currency_mismatch";
        public const string ExceedsOutstanding = "repayment.exceeds_outstanding";
        public const string NoteTooLong = "repayment.note_too_long";
        public const string NotFound = "repayment.not_found";
        public const string AlreadyReversed = "repayment.already_reversed";
        public const string CannotReverseReversal = "repayment.cannot_reverse_reversal";
    }

    public static class Money
    {
        public const string CurrencyMismatch = "money.currency_mismatch";
    }

    public static class Schedule
    {
        public const string InvalidTerms = "schedule.invalid_terms";
    }
}
