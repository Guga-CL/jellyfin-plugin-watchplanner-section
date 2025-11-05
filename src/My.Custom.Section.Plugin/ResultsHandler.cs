using System.Collections.Generic;

namespace My.Custom.Section.Plugin
{
    public class ResultsHandler
    {
        // Temporary: return an object shaped like QueryResult<BaseItemDto>
        // so the plugin compiles while we confirm the correct Jellyfin DTO namespaces.
        public static object GetSectionResults(object request)
        {
            return new
            {
                Items = new object[0],
                TotalRecordCount = 0
            };
        }
    }
}
