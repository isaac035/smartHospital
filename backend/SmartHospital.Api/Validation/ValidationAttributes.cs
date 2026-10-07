using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SmartHospital.Api.Validation;

// Shared input-validation attributes. The same rules and messages are mirrored in
// frontend_React/src/utils/validators.js and frontend_Flutter/lib/core/utils/validators.dart.
// Empty values pass these attributes; use [Required] when a field is mandatory.

public static class FieldLabel
{
    // "FirstName" -> "First name", "DepartmentId" -> "Department"
    public static string For(ValidationContext context) => FromName(context.DisplayName ?? context.MemberName ?? "Value");

    public static string FromName(string name)
    {
        if (name.Length > 2 && name.EndsWith("Id", StringComparison.Ordinal)) name = name[..^2];
        var words = Regex.Replace(name, "(?<=[a-z0-9])([A-Z])", " $1").ToLowerInvariant();
        return char.ToUpperInvariant(words[0]) + words[1..];
    }

    public static ValidationResult Fail(ValidationContext context, string message) =>
        new(message, context.MemberName is null ? null : new[] { context.MemberName });
}

/// <summary>Letters, spaces, hyphens, apostrophes and dots; at least 2 characters.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PersonNameAttribute : ValidationAttribute
{
    private static readonly Regex Pattern = new(@"^\p{L}[\p{L}\p{M} .'\-]*$", RegexOptions.Compiled);

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text)) return ValidationResult.Success;
        var trimmed = text.Trim();
        var label = FieldLabel.For(context);
        if (trimmed.Length < 2) return FieldLabel.Fail(context, $"{label} must be at least 2 characters.");
        if (!Pattern.IsMatch(trimmed))
            return FieldLabel.Fail(context, $"{label} may only contain letters, spaces, hyphens, apostrophes and dots.");
        return ValidationResult.Success;
    }
}

/// <summary>7 to 15 digits; may include a leading +, spaces, hyphens or brackets.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PhoneNumberAttribute : ValidationAttribute
{
    private static readonly Regex Allowed = new(@"^\+?[0-9 ()\-]+$", RegexOptions.Compiled);

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text)) return ValidationResult.Success;
        var trimmed = text.Trim();
        var digits = trimmed.Count(char.IsAsciiDigit);
        if (!Allowed.IsMatch(trimmed) || digits < 7 || digits > 15)
            return FieldLabel.Fail(context,
                "Phone number must contain 7 to 15 digits and may include +, spaces, hyphens or brackets.");
        return ValidationResult.Success;
    }
}

/// <summary>8-128 characters with an uppercase letter, a lowercase letter, a number and a special character.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class StrongPasswordAttribute : ValidationAttribute
{
    public const string Message =
        "Password must be 8 to 128 characters and include an uppercase letter, a lowercase letter, a number and a special character.";

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is not string password || password.Length == 0) return ValidationResult.Success;
        var ok = password.Length is >= 8 and <= 128
                 && password.Any(char.IsUpper)
                 && password.Any(char.IsLower)
                 && password.Any(char.IsDigit)
                 && password.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
        return ok ? ValidationResult.Success : FieldLabel.Fail(context, Message);
    }
}

/// <summary>Rejects HTML/script markup in free text that is displayed back to users.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NoHtmlAttribute : ValidationAttribute
{
    private static readonly Regex Markup = new(@"<\s*/?\s*[A-Za-z!?]|javascript\s*:", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is not string text || text.Length == 0) return ValidationResult.Success;
        return Markup.IsMatch(text)
            ? FieldLabel.Fail(context, $"{FieldLabel.For(context)} must not contain HTML or script tags.")
            : ValidationResult.Success;
    }
}

/// <summary>Free text must contain at least one letter (not only numbers or symbols).</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ContainsLettersAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text)) return ValidationResult.Success;
        return text.Any(char.IsLetter)
            ? ValidationResult.Success
            : FieldLabel.Fail(context, ErrorMessage ?? $"{FieldLabel.For(context)} must contain words, not only numbers or symbols.");
    }
}

/// <summary>A date of birth: not in the future and not before 1900.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DateOfBirthAttribute : ValidationAttribute
{
    private static readonly DateOnly Earliest = new(1900, 1, 1);

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        DateOnly? date = value switch
        {
            DateTime dt => DateOnly.FromDateTime(dt),
            DateOnly d => d,
            _ => null
        };
        if (date is null) return ValidationResult.Success;
        // Allow one day of leeway for time-zone differences between client and server.
        if (date > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            return FieldLabel.Fail(context, "Date of birth cannot be in the future.");
        if (date < Earliest)
            return FieldLabel.Fail(context, "Date of birth must be on or after 1 January 1900.");
        return ValidationResult.Success;
    }
}

/// <summary>An enum-typed value must be one of the defined members (rejects e.g. Priority = 99).</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DefinedEnumAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is not Enum e) return ValidationResult.Success;
        return Enum.IsDefined(e.GetType(), e)
            ? ValidationResult.Success
            : FieldLabel.Fail(context, $"{FieldLabel.For(context)} has an invalid value.");
    }
}

/// <summary>A string must name a member of the given enum (case-insensitive, no numbers).</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EnumNameAttribute : ValidationAttribute
{
    private readonly Type _enumType;

    public EnumNameAttribute(Type enumType) => _enumType = enumType;

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text)) return ValidationResult.Success;
        var names = Enum.GetNames(_enumType);
        return names.Any(n => string.Equals(n, text.Trim(), StringComparison.OrdinalIgnoreCase))
            ? ValidationResult.Success
            : FieldLabel.Fail(context, $"{FieldLabel.For(context)} must be one of: {string.Join(", ", names)}.");
    }
}

/// <summary>A required foreign-key id: must be a positive number ("Please select a department.").</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SelectedIdAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is null) return ValidationResult.Success;
        return value is int id && id > 0
            ? ValidationResult.Success
            : FieldLabel.Fail(context, $"Please select a {FieldLabel.For(context).ToLowerInvariant()}.");
    }
}
