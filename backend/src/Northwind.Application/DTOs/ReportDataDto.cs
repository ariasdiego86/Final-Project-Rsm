namespace Northwind.Application.DTOs;

public class TimeSeriesPointDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RegionShipmentDto
{
    public string Region { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class PagedResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class LookupDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
