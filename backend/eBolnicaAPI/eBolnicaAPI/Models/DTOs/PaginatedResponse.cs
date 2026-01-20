using System;
using System.Collections.Generic;

namespace eBolnicaAPI.Models.DTOs
{
    /// <summary>
    /// Generic paginated response wrapper for API endpoints
    /// </summary>
    /// <typeparam name="T">The type of items in the paginated response</typeparam>
    public class PaginatedResponse<T>
    {
        /// <summary>
        /// The paginated list of items
        /// </summary>
        public List<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Total number of records without pagination
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Indicates if there is a next page
        /// </summary>
        public bool HasNext { get; set; }

        /// <summary>
        /// Indicates if there is a previous page
        /// </summary>
        public bool HasPrevious { get; set; }

        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Creates a new PaginatedResponse instance
        /// </summary>
        public PaginatedResponse()
        {
        }

        /// <summary>
        /// Creates a new PaginatedResponse instance with the specified values
        /// </summary>
        public PaginatedResponse(List<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Items = items ?? new List<T>();
            TotalCount = totalCount;
            CurrentPage = pageNumber;
            PageSize = pageSize;
            TotalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
            HasNext = CurrentPage < TotalPages;
            HasPrevious = CurrentPage > 1;
        }
    }
}
