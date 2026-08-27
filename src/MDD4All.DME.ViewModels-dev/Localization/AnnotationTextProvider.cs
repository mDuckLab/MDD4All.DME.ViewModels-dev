using MDD4All.Localization.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Resources;

namespace MDD4All.DME.ViewModels.Localization
{
    // Turns the key in a [Display] annotation into text, in the language the user picked.
    //
    // The counterpart to AppTextProvider in the Views: that one reads the application's own
    // resources, this one reads whatever the loaded data model brought along.
    //
    // DisplayAttribute.GetName() would do the same, but it resolves through the generated
    // resource class, and that reads CultureInfo.CurrentUICulture - the one value a Blazor
    // Hybrid host cannot reach. Handing the culture over is the only way it arrives.
    public class AnnotationTextProvider
    {
        private readonly ILanguageSetter _languageSetter;

        // One per resource type. The types come out of assemblies that are loaded and dropped
        // at runtime, so this must not outlive the document it was built for.
        private readonly Dictionary<Type, ResourceManager> _resourceManagers
            = new Dictionary<Type, ResourceManager>();

        public AnnotationTextProvider(ILanguageSetter languageSetter)
        {
            _languageSetter = languageSetter;
        }

        // A key the resource file does not know gives back the key itself. GetName() throws in
        // that case; a label reading DisplayName_FirstName says what is missing.
        public string? Resolve(DisplayAttribute displayAttribute)
        {
            string? result = displayAttribute.Name;

            if (displayAttribute.ResourceType != null && !string.IsNullOrEmpty(result))
            {
                string? text = GetResourceManager(displayAttribute.ResourceType)
                                   .GetString(result, _languageSetter.CurrentCulture);

                if (text != null)
                {
                    result = text;
                }
            }

            return result;
        }

        private ResourceManager GetResourceManager(Type resourceType)
        {
            ResourceManager? result;

            if (!_resourceManagers.TryGetValue(resourceType, out result))
            {
                // The generated class builds its own manager from exactly this name.
                result = new ResourceManager(resourceType);
                _resourceManagers.Add(resourceType, result);
            }

            return result;
        }
    }
}
