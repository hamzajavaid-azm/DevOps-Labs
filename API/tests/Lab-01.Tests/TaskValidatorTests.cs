using Lab_01.Validation;

namespace Lab_01.Tests;

// Unit tests for the title validation rules: required + max 100 chars.
public class TaskValidatorTests
{
    [Fact]
    public void Empty_title_is_invalid()
    {
        var errors = TaskValidator.Validate("");

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Whitespace_only_title_is_invalid()
    {
        var errors = TaskValidator.Validate("   ");

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Title_longer_than_100_chars_is_invalid()
    {
        var tooLong = new string('a', 101);

        var errors = TaskValidator.Validate(tooLong);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Title_of_exactly_100_chars_is_valid()
    {
        var atLimit = new string('a', 100);

        var errors = TaskValidator.Validate(atLimit);

        Assert.Empty(errors);
    }

    [Fact]
    public void Normal_title_is_valid()
    {
        var errors = TaskValidator.Validate("Buy milk");

        Assert.Empty(errors);
    }
}
