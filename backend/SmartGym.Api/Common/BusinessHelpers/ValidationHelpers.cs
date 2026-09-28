using System.Text.RegularExpressions;
using SmartGym.Api.Exceptions;

namespace SmartGym.Api.Common.BusinessHelpers;

public static class ValidationHelpers
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static void EnsureNotNullOrEmpty(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BadRequestException($"'{fieldName}' cannot be empty.");
        }
    }

    public static void EnsurePositive(decimal value, string fieldName)
    {
        if (value <= 0)
        {
            throw new BadRequestException($"'{fieldName}' must be strictly positive (> 0).");
        }
    }

    public static void EnsureNonNegative(decimal value, string fieldName)
    {
        if (value < 0)
        {
            throw new BadRequestException($"'{fieldName}' cannot be negative.");
        }
    }

    public static void EnsureInRange(int value, int min, int max, string fieldName)
    {
        if (value < min || value > max)
        {
            throw new BadRequestException($"'{fieldName}' must be between {min} and {max}.");
        }
    }

    public static void EnsureValidEmail(string email, string fieldName = "Email")
    {
        if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email))
        {
            throw new BadRequestException($"'{fieldName}' is not a valid email address.");
        }
    }

    public static void EnsureFutureDate(DateTime date, string fieldName)
    {
        if (date <= DateTime.UtcNow.AddMinutes(-5))
        {
            throw new BadRequestException($"'{fieldName}' must be in the future.");
        }
    }

    public static void EnsureDateRangeValid(DateTime start, DateTime end, string startName = "StartDate", string endName = "EndDate")
    {
        if (end <= start)
        {
            throw new BadRequestException($"'{endName}' must be strictly after '{startName}'.");
        }
    }
}
