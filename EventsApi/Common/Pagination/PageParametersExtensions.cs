namespace EventsApi.Common.Pagination;

public static class PageParametersExtensions
{
    public static int GetNormalizedPage(this PageParameters parameters) =>
        parameters.Page < 1 ? PageParameters.DefaultPage : parameters.Page;

    public static int GetNormalizedPageSize(this PageParameters parameters) =>
        Math.Clamp(parameters.PageSize, 1, PageParameters.MaxPageSize);

    public static int GetSkip(this PageParameters parameters)
    {
        var skip = ((long)parameters.GetNormalizedPage() - 1) * parameters.GetNormalizedPageSize();
        return skip > int.MaxValue ? int.MaxValue : (int)skip;
    }
}
