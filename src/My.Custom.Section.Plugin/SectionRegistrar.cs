using System.Text.Json;

namespace My.Custom.Section.Plugin
{
    public static class SectionRegistrar
    {
        public static object BuildPayload(string id, string displayText, string resultsAssembly, string resultsClass, string resultsMethod)
        {
            var payload = new {
                id = id,
                displayText = displayText,
                limit = 1,
                route = "",
                additionalData = "",
                resultsAssembly = resultsAssembly,
                resultsClass = resultsClass,
                resultsMethod = resultsMethod
            };

            return JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(payload));
        }
    }
}
