using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApplication2.Infrastructure.Analytics.Dtos;

public record FriendsQuery(
    int Page = 1,
    int PageSize = 20,
    int? MinAge = null,
    int? MaxAge = null
);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    long TotalCount,
    int Page,
    int PageSize
);

public record CursorPage<T>(
    IReadOnlyList<T> Items,
    string? NextCursor
);
