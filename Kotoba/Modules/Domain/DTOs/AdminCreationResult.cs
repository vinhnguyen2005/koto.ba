namespace Kotoba.Modules.Domain.DTOs;

public class AdminCreationResult
{
    public bool Succeeded { get; init; }
    public string? CreatedAdminId { get; init; }
    public List<string> Errors { get; init; } = new();

    public static AdminCreationResult Success(string createdAdminId)
    {
        return new AdminCreationResult
        {
            Succeeded = true,
            CreatedAdminId = createdAdminId
        };
    }

    public static AdminCreationResult Failure(IEnumerable<string> errors)
    {
        return new AdminCreationResult
        {
            Succeeded = false,
            Errors = errors
                .Where(error => !string.IsNullOrWhiteSpace(error))
                .Select(error => error.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList()
        };
    }
}
