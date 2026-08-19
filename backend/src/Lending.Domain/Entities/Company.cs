namespace Lending.Domain.Entities;

public class Company
{
    private Company()
    {
    }

    public Company(
        string name,
        string legalName,
        string registrationNumber,
        string country,
        string industry,
        string contactEmail)
    {
        Id = Guid.NewGuid();
        Name = name;
        LegalName = legalName;
        RegistrationNumber = registrationNumber;
        Country = country;
        Industry = industry;
        ContactEmail = contactEmail;
        Status = CompanyStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string LegalName { get; private set; } = string.Empty;
    public string RegistrationNumber { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string Industry { get; private set; } = string.Empty;
    public string ContactEmail { get; private set; } = string.Empty;
    public CompanyStatus Status { get; private set; }
    public uint Xmin { get; set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void UpdateDetails(
        string name,
        string legalName,
        string registrationNumber,
        string country,
        string industry,
        string contactEmail)
    {
        Name = name;
        LegalName = legalName;
        RegistrationNumber = registrationNumber;
        Country = country;
        Industry = industry;
        ContactEmail = contactEmail;
        Touch();
    }

    public void Deactivate()
    {
        if (Status == CompanyStatus.Inactive)
            throw new DomainException(DomainErrors.Company.InvalidTransition, "Company is already inactive.");
        Status = CompanyStatus.Inactive;
        Touch();
    }

    public void Activate()
    {
        if (Status == CompanyStatus.Active)
            throw new DomainException(DomainErrors.Company.InvalidTransition, "Company is already active.");
        Status = CompanyStatus.Active;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
