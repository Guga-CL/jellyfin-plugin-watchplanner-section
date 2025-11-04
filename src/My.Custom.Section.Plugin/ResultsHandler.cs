using System.Collections.Generic;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Entities;

namespace My.Custom.Section.Plugin
{
    public class ResultsHandler
    {
        // Entry point called by HomeScreenSections. Must return QueryResult<BaseItemDto>.
        public static QueryResult<BaseItemDto> GetSectionResults(object request)
        {
            var result = new QueryResult<BaseItemDto>
            {
                Items = new List<BaseItemDto>(),
                TotalRecordCount = 0
            };
            return result;
        }
    }
}
