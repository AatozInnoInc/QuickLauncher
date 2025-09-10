namespace QuickLauncher.Engine;

/// <summary>
/// Basic in‑memory implementation of <see cref="ISearchEngine"/>.  It maintains a list of
/// commands and provides simple prefix and substring searching with weighting for hit
/// count and recency.  Replace this implementation with a more sophisticated scorer
/// (e.g. trigram fuzzy matching) as needed.
/// </summary>
public sealed class SearchEngine : ISearchEngine
{
    private readonly List<Command> _commands;
    private readonly Expressions.ExpressionRegistry _expressionRegistry;
    private readonly bool _trustedMode;

    // Weight factors for ranking.  Higher values emphasise the corresponding metric.
    private const double PrefixWeight = 1.0;
    private const double SubstringWeight = 0.5;
    private const double FrequencyWeight = 0.2;
    private const double RecencyWeight = 0.1;

    public SearchEngine(IEnumerable<Command> commands,
                       Expressions.ExpressionRegistry expressionRegistry,
                       bool trustedMode = false)
    {
        _commands = commands?.ToList() ?? throw new ArgumentNullException(nameof(commands));
        _expressionRegistry = expressionRegistry ?? throw new ArgumentNullException(nameof(expressionRegistry));
        _trustedMode = trustedMode;
    }

    public ValueTask<IReadOnlyList<SearchResult>> SearchAsync(string query, int topN, CancellationToken cancellationToken = default)
    {
        var normalized = (query ?? string.Empty).Trim().ToLowerInvariant();
        IEnumerable<Command> candidates = _commands;
        if (!string.IsNullOrEmpty(normalized))
        {
            candidates = _commands.Where(cmd =>
                cmd.Label.AsSpan().StartsWith(normalized, StringComparison.OrdinalIgnoreCase) ||
                cmd.Label.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (!string.IsNullOrEmpty(cmd.Category) && cmd.Category.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        var now = DateTimeOffset.UtcNow;
        var results = new List<SearchResult>();
        foreach (var cmd in candidates)
        {
            double score = 0;
            if (!string.IsNullOrEmpty(normalized))
            {
                var labelLower = cmd.Label.ToLowerInvariant();
                if (labelLower.StartsWith(normalized))
                {
                    score += PrefixWeight * (normalized.Length / (double)labelLower.Length);
                }
                else if (labelLower.Contains(normalized))
                {
                    score += SubstringWeight * (normalized.Length / (double)labelLower.Length);
                }
            }

            // Add frequency component (logarithmic to avoid runaway growth).
            score += FrequencyWeight * Math.Log10(cmd.HitCount + 1);

            // Add recency component (decay over days).
            if (cmd.LastRunUtc.HasValue)
            {
                var daysSinceLast = (now - cmd.LastRunUtc.Value).TotalDays;
                score += RecencyWeight / (1 + daysSinceLast);
            }

            results.Add(new SearchResult { Command = cmd, Score = score });
        }

        var ordered = results.OrderByDescending(r => r.Score).ThenBy(r => r.Command.Label).Take(topN).ToList();
        return ValueTask.FromResult<IReadOnlyList<SearchResult>>(ordered);
    }

    public ValueTask<bool> ExecuteAsync(Guid commandId, CancellationToken cancellationToken = default)
    {
        var cmd = _commands.FirstOrDefault(c => c.Id == commandId);
        if (cmd == null)
            return ValueTask.FromResult(false);

        // Update hit count and recency.
        cmd.HitCount++;
        cmd.LastRunUtc = DateTimeOffset.UtcNow;

        switch (cmd.Type)
        {
            case CommandType.Run:
                // Launch process or open URI.  Use Process.Start; if it fails, return false.
                try
                {
                    System.Diagnostics.Process.Start(cmd.Args);
                    return ValueTask.FromResult(true);
                }
                catch
                {
                    return ValueTask.FromResult(false);
                }
            case CommandType.Expression:
                // Dispatch expression to the registry.  Compose verb + args.
                return new ValueTask<bool>(_expressionRegistry.ExecuteAsync($"{cmd.Verb} {cmd.Args}", _trustedMode, cancellationToken));
            case CommandType.BuiltIn:
                // Built‑ins must be handled by the UI or another service.  For now just log.
                Console.WriteLine($"[BuiltIn] {cmd.Label}: {cmd.Args}");
                return ValueTask.FromResult(true);
            default:
                return ValueTask.FromResult(false);
        }
    }
}