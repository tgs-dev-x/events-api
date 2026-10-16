using EventsApi.Exceptions;

namespace EventsApi.Common.Sorting;

public static class SortOrderParser
{
    private const char Separator = ':';
    private const string Asc = "asc";
    private const string Desc = "desc";


    public static IReadOnlyList<SortOrder<TField>> Parse<TField>(IReadOnlyList<string>? raw)
        where TField : struct, Enum
    {
        if (raw is null || raw.Count == 0)
            return [];

        var result = new List<SortOrder<TField>>(raw.Count);
        var seen = new HashSet<TField>();

        foreach (var item in raw)
        {
            if (string.IsNullOrWhiteSpace(item))
                continue;

            var parts = item.Trim().Split(Separator, 2);

            var name = parts[0].Trim();
            var direction = parts.Length > 1 ? parts[1].Trim() : Asc;

            if (!Enum.TryParse<TField>(name, ignoreCase: true, out var field)
                    || !Enum.IsDefined(field))
                throw new DataValidationException($"Неизвестное поле сортировки '{name}'");

            var descending = direction.ToLowerInvariant() switch
            {
                Asc => false,
                Desc => true,
                _ => throw new DataValidationException(
                    $"Неизвестное направление сортировки '{direction}'. Допустимые: asc, desc")
            };

            if (seen.Add(field))
                result.Add(new SortOrder<TField>
                {
                    Field = field,
                    Descending = descending
                });
        }

        return result;
    }
}
