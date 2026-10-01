namespace EventsApi.Validation;

public class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = new();
    public bool HasErrors => _errors.Count > 0;

    public void AddError(string key, string message)
    {
        if (!_errors.TryGetValue(key, out var list))
            _errors[key] = list = new List<string>();
        
        list.Add(message);
    }

    public Dictionary<string, string[]> ToDictionary()
    {
        return _errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
    }
}
