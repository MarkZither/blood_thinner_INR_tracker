using System;
using System.Linq.Expressions;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Data.Shared
{
    /// <summary>
    /// Common named query filters for EF queries across projects.
    /// Use these to avoid repeating `e => !e.IsDeleted` and for consistent intent.
    /// </summary>
    public static class QueryFilters
    {
        /// <summary>
        /// Returns an expression that filters out soft-deleted medical entities.
        /// Usage: `.Where(QueryFilters.NotDeleted&lt;INRTest&gt;())`
        /// </summary>
        public static Expression<Func<T, bool>> NotDeleted<T>() where T : IMedicalEntity
        {
            return e => !e.IsDeleted;
        }
    }
}
