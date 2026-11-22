namespace Lab_01.Validation;

// Plain static validation with no dependencies, so it is trivial to unit test
// without spinning up a web host or a database.
public static class TaskValidator
{
    public const int MaxTitleLength = 100;

    // Returns an empty list when the title is valid, otherwise a list of error messages.
    public static IReadOnlyList<string> Validate(string? title)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add("Title is required.");
        }
        else if (title.Length > MaxTitleLength)
        {
            errors.Add($"Title must be {MaxTitleLength} characters or fewer.");
        }

        return errors;
    }
}
