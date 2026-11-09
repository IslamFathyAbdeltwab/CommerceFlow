using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Inventory;

public class Warehouse : AggregateRoot
{
    public const int CodeMaxLength = 50;
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private Warehouse() { } // required by EF Core

    private Warehouse(string code, string name, string? description)
    {
        Code = code;
        Name = name;
        Description = description;
        IsActive = true;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static Warehouse Create(string code, string name, string? description = null)
    {
        var (c, n, d) = Validate(code, name, description);
        return new Warehouse(c, n, d);
    }

    public void UpdateDetails(string code, string name, string? description)
    {
        var (c, n, d) = Validate(code, name, description);

        Code = c;
        Name = n;
        Description = d;
        Touch();
    }

    public void Activate()
    {
        if (IsActive) return;

        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        if (!IsActive) return;

        IsActive = false;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static (string Code, string Name, string? Description) Validate(
        string? code, string? name, string? description)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Warehouse code is required.");
        code = code.Trim().ToUpperInvariant();
        if (code.Length > CodeMaxLength)
            throw new DomainException($"Warehouse code cannot exceed {CodeMaxLength} characters.");
        if (code.Any(char.IsWhiteSpace))
            throw new DomainException("Warehouse code cannot contain spaces.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Warehouse name is required.");
        name = name.Trim();
        if (name.Length > NameMaxLength)
            throw new DomainException($"Warehouse name cannot exceed {NameMaxLength} characters.");

        var desc = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (desc is { Length: > DescriptionMaxLength })
            throw new DomainException($"Warehouse description cannot exceed {DescriptionMaxLength} characters.");

        return (code, name, desc);
    }
}
