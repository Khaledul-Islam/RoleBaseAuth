namespace RoleBaseAuth.Application.Common.Models;

public class PagedQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SearchTerm { get; set; } = string.Empty;
    public string SortBy { get; set; } = string.Empty;
    public bool SortDescending { get; set; }
}