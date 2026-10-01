namespace EventsApi.Validation;

public interface IModelValidator
{
    ValidationErrors Validate(object model);
}
