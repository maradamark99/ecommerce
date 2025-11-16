using System.Text;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Ecommerce.Common;

public class Base64DecodeModelBinder : IModelBinder 
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
            return Task.CompletedTask; 

        var encodedValue = valueProviderResult.FirstValue; 

        if (!string.IsNullOrEmpty(encodedValue))
        {
            try
            {
                var decodedBytes = Convert.FromBase64String(encodedValue);
                var decodedString = Encoding.UTF8.GetString(decodedBytes);

                bindingContext.Result = ModelBindingResult.Success(decodedString);
            }
            catch (FormatException)
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Invalid Base64 string.");
            }
        }

        return Task.CompletedTask;
    }
}