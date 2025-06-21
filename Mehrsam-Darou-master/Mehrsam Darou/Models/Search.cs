using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mehrsam_Darou.Models
{
    // Search result models
    public class GlobalSearchResult
    {
        public string SearchQuery { get; set; } = "";
        public List<SearchResultItem> Results { get; set; } = new();
        public Dictionary<string, List<SearchResultItem>> GroupedResults { get; set; } = new();
        public int TotalResults { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class SearchResultItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Url { get; set; } = "";
        public string Badge { get; set; } = "";
        public string BadgeClass { get; set; } = "";
    }

  
}