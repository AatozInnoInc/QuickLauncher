namespace QuickLauncher.Engine;

/// <summary>
/// Abstraction for the search engine used by the QuickLauncher.  A concrete
/// implementation should maintain an in‑memory index of commands, update hit counts
/// and recency upon execution, and expose methods for searching and executing commands.
/// </summary>
public interface ISearchEngine
{
    /// <summary>
    /// Returns a list of search results matching the provided query.  The
    /// implementation is free to apply ranking logic (prefix, fuzzy, frequency,
    /// recency, etc.) and may return fewer than <paramref name="topN"/> if there are
    /// insufficient matches.
    /// </summary>
    /// <param name="query">The user’s query string.  May be empty.</param>
    /// <param name="topN">The maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of <see cref="SearchResult"/> objects.</returns>
    ValueTask<IReadOnlyList<SearchResult>> SearchAsync(string query, int topN, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the specified command.  The engine should locate the command by its
    /// identifier, invoke the appropriate handler based on its type, update hit count
    /// and recency, and return a boolean indicating whether execution succeeded.  For
    /// commands requiring elevation or other side effects, the UI may need to prompt
    /// the user.
    /// </summary>
    /// <param name="commandId">The identifier of the command to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the command executed successfully; otherwise false.</returns>
    ValueTask<bool> ExecuteAsync(Guid commandId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Describes a ranked match returned by the search engine.  Contains the command and
/// its calculated score.  Scores are relative; higher scores should appear earlier in
/// the result set.
/// </summary>
public sealed class SearchResult
{
    public Command Command { get; init; } = null!;
    public double Score { get; init; }
}