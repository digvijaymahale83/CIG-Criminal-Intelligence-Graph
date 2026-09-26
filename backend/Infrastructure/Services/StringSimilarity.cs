using System.Text.RegularExpressions;

namespace Infrastructure.Services;

/// <summary>
/// Deterministic, explainable string similarity utilities for entity resolution.
/// Implements Jaro-Winkler, Levenshtein, and Token-Set algorithms without external dependencies.
/// </summary>
public static class StringSimilarity
{
    private static readonly Regex PrefixRegex = new(
        @"^(Shri|Smt|Mr|Mrs|Ms|Dr|Insp|Inspector|Sub-Insp|PSI|API|ACP|DCP)\.?\s+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Computes deterministic Levenshtein distance between two strings.
    /// </summary>
    public static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t)) return s.Length;

        int n = s.Length;
        int m = t.Length;
        var d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (char.ToLowerInvariant(s[i - 1]) == char.ToLowerInvariant(t[j - 1])) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    /// <summary>
    /// Computes Levenshtein similarity ratio between 0.0 and 1.0.
    /// </summary>
    public static double LevenshteinRatio(string s, string t)
    {
        if (string.IsNullOrEmpty(s) && string.IsNullOrEmpty(t)) return 1.0;
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(t)) return 0.0;

        int maxLen = Math.Max(s.Length, t.Length);
        if (maxLen == 0) return 1.0;

        int dist = LevenshteinDistance(s, t);
        return Math.Max(0.0, 1.0 - ((double)dist / maxLen));
    }

    /// <summary>
    /// Computes Jaro similarity between two strings (0.0 to 1.0).
    /// </summary>
    public static double JaroDistance(string s1, string s2)
    {
        if (s1 == s2) return 1.0;
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0.0;

        int len1 = s1.Length;
        int len2 = s2.Length;
        int matchDistance = (Math.Max(len1, len2) / 2) - 1;
        if (matchDistance < 0) matchDistance = 0;

        var s1Matches = new bool[len1];
        var s2Matches = new bool[len2];

        int matches = 0;
        for (int i = 0; i < len1; i++)
        {
            int start = Math.Max(0, i - matchDistance);
            int end = Math.Min(i + matchDistance + 1, len2);

            for (int j = start; j < end; j++)
            {
                if (s2Matches[j]) continue;
                if (char.ToLowerInvariant(s1[i]) != char.ToLowerInvariant(s2[j])) continue;

                s1Matches[i] = true;
                s2Matches[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0) return 0.0;

        // Transpositions
        int k = 0;
        int transpositions = 0;
        for (int i = 0; i < len1; i++)
        {
            if (!s1Matches[i]) continue;
            while (!s2Matches[k]) k++;
            if (char.ToLowerInvariant(s1[i]) != char.ToLowerInvariant(s2[k])) transpositions++;
            k++;
        }

        double m = matches;
        return ((m / len1) + (m / len2) + ((m - (transpositions / 2.0)) / m)) / 3.0;
    }

    /// <summary>
    /// Computes Jaro-Winkler similarity with common prefix bonus (0.0 to 1.0).
    /// Standard scaling factor p = 0.1, max prefix = 4.
    /// </summary>
    public static double JaroWinklerSimilarity(string s1, string s2, double prefixScale = 0.1)
    {
        double jaro = JaroDistance(s1, s2);
        if (jaro < 0.7) return jaro;

        int prefixLen = 0;
        int maxPrefix = Math.Min(4, Math.Min(s1.Length, s2.Length));
        for (int i = 0; i < maxPrefix; i++)
        {
            if (char.ToLowerInvariant(s1[i]) == char.ToLowerInvariant(s2[i]))
                prefixLen++;
            else
                break;
        }

        return jaro + (prefixLen * prefixScale * (1.0 - jaro));
    }

    /// <summary>
    /// Computes token-aware name similarity combining Jaro-Winkler, token matching, and initial matching.
    /// Perfectly handles name variations like "Rahul Sharma" vs "R. Sharma", "Patil Sameer" vs "Sameer Patil".
    /// </summary>
    public static double ComparePersonNames(string name1, string name2)
    {
        if (string.IsNullOrWhiteSpace(name1) || string.IsNullOrWhiteSpace(name2))
            return 0.0;

        string clean1 = PrefixRegex.Replace(name1.Trim(), "").Trim();
        string clean2 = PrefixRegex.Replace(name2.Trim(), "").Trim();

        if (string.Equals(clean1, clean2, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        var tokens1 = clean1.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var tokens2 = clean2.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens1.Length == 0 || tokens2.Length == 0) return 0.0;

        // Check surname match (usually last token in Indian names)
        string surname1 = tokens1[^1];
        string surname2 = tokens2[^1];
        bool surnameMatch = string.Equals(surname1, surname2, StringComparison.OrdinalIgnoreCase);

        // Check initial match (e.g. "R." or "R" vs "Rahul")
        bool initialMatch = false;
        if (tokens1.Length >= 2 && tokens2.Length >= 2)
        {
            string first1 = tokens1[0].TrimEnd('.');
            string first2 = tokens2[0].TrimEnd('.');

            if (first1.Length == 1 && first2.Length > 1 && char.ToLowerInvariant(first1[0]) == char.ToLowerInvariant(first2[0]))
                initialMatch = true;
            else if (first2.Length == 1 && first1.Length > 1 && char.ToLowerInvariant(first2[0]) == char.ToLowerInvariant(first1[0]))
                initialMatch = true;
        }

        // Token intersection ratio
        int sharedTokens = 0;
        foreach (var t1 in tokens1)
        {
            foreach (var t2 in tokens2)
            {
                if (string.Equals(t1, t2, StringComparison.OrdinalIgnoreCase) ||
                    JaroWinklerSimilarity(t1, t2) >= 0.92)
                {
                    sharedTokens++;
                    break;
                }
            }
        }

        double tokenJaccard = (double)sharedTokens / (tokens1.Length + tokens2.Length - sharedTokens);
        double jwOverall = JaroWinklerSimilarity(clean1, clean2);

        // If surname matches and first name initial matches (e.g. "Rahul Sharma" vs "R. Sharma")
        if (surnameMatch && initialMatch)
        {
            return Math.Max(0.88, Math.Max(tokenJaccard, jwOverall));
        }

        // Weighted combination
        return Math.Max(tokenJaccard, jwOverall);
    }
}
