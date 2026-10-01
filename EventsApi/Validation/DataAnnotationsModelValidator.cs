using System.ComponentModel.DataAnnotations;

namespace EventsApi.Validation;

public class DataAnnotationsModelValidator : IModelValidator
{
    public ValidationErrors Validate(object model)
    {
        ArgumentNullException.ThrowIfNull(model);

        List<ValidationResult> results = new();
        var context = new ValidationContext(model);

        Validator.TryValidateObject(model, context, results, true);

        var errors = new ValidationErrors();

        foreach (ValidationResult result in results)
        {
            var message = result.ErrorMessage ?? "Некорректное значение";
            var members = result.MemberNames.Any() ? result.MemberNames : [string.Empty];

            foreach (var member in members)
                errors.AddError(member, message);
        }

        return errors;
    }
}
