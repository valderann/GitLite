using GitLite.Repositories.Data;
using System;
using System.Linq;

namespace GitLite.Extensions;

public static class CommitExtensions
{
    public static IQueryable<CommitItem> FilterByDate(this IQueryable<CommitItem> query, DateTime? from, DateTime? to)
    {
        if (from.HasValue && to.HasValue)
        {
            return query.Where(t => t.Date >= from && t.Date <= to.Value);
        }
        return query;
    }
}