using EventsApi.Common.Sorting;
using EventsApi.Exceptions;
using EventsApi.Filtering;

namespace EventsApi.Tests.Sorting;

public class SortOrderParserTests
{
    private List<SortOrder<EventSortField>> Parse(params string[]? raw) =>
        SortOrderParser.Parse<EventSortField>(raw).ToList();

    [Fact]
    public void Parse_NullInput_ReturnsEmpty()
    {
        var result = Parse(null);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsEmpty()
    {
        var result = Parse();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Parse_SingleFieldWithoutDirection_DefaultToAsc()
    {
        var result = Parse("Title");

        result.Should().ContainSingle();
        result[0].Field.Should().Be(EventSortField.Title);
        result[0].Descending.Should().BeFalse();
    }

    [Fact]
    public void Parse_MultipleFields_PreservesOrder()
    {
        var result = Parse("Title:asc", "StartAt:desc");

        result.Should().HaveCount(2);
        result[0].Field.Should().Be(EventSortField.Title);
        result[0].Descending.Should().BeFalse();
        result[1].Field.Should().Be(EventSortField.StartAt);
        result[1].Descending.Should().BeTrue();
    }

    [Fact]
    public void Parse_WhitespaceAround_IsTrimmed()
    {
        var result = Parse("  Title  :  asc  ");

        result.Should().ContainSingle();
        result[0].Field.Should().Be(EventSortField.Title);
        result[0].Descending.Should().BeFalse();
    }

    [Fact]
    public void Parse_EmptyEntries_Skipped()
    {
        var result = Parse("", "   ", "Title");

        result.Should().ContainSingle();
        result[0].Field.Should().Be(EventSortField.Title);
    }

    [Fact]
    public void Parse_DuplicateField_KeepsFirst()
    {
        var result = Parse("Title:desc", "Title:asc");

        result.Should().ContainSingle();
        result[0].Descending.Should().BeTrue();
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("Title:updown")]
    [InlineData("Title:asc:extra")]
    public void Parse_InvalidInput_ThrowsDataValidation(string input)
    {
        var action = () => Parse(input);

        action.Should().Throw<DataValidationException>();
    }
}
