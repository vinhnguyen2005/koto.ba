namespace Kotoba.Modules.Domain.DTOs;

public class RegistrationResult
{
    public bool Succeeded { get; init; }
    public List<string> Errors { get; init; } = new();

    public static RegistrationResult Success()
    {
        return new RegistrationResult { Succeeded = true };
    }

    public static RegistrationResult Failure(IEnumerable<string> errors)
    {
        return new RegistrationResult
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
