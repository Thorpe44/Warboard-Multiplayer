using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class LeaderCompatibilityOverrideEntry
{
    public string leaderName;
    public string[] bodyguardNames;
}

[Serializable]
public class LeaderCompatibilityOverrideCollection
{
    public LeaderCompatibilityOverrideEntry[] entries;
}

public static class LeaderCompatibilityRegistry
{
    private const string ResourcePath =
        "Core/LeaderCompatibilityOverrides";

    private static LeaderCompatibilityOverrideCollection cached;

    public static IReadOnlyList<string>
        GetBodyguardNames(
            string leaderName)
    {
        EnsureLoaded();

        if (cached == null ||
            cached.entries == null ||
            string.IsNullOrWhiteSpace(
                leaderName))
        {
            return new string[0];
        }

        string wanted =
            NormalizeComparable(
                leaderName
            );

        foreach (
            LeaderCompatibilityOverrideEntry entry
            in cached.entries)
        {
            if (entry == null ||
                NormalizeComparable(
                    entry.leaderName
                ) != wanted)
            {
                continue;
            }

            return entry.bodyguardNames ??
                new string[0];
        }

        return new string[0];
    }

    public static bool HasOverride(
        string leaderName)
    {
        return
            GetBodyguardNames(
                leaderName
            ).Count > 0;
    }

    // WARBOARD_R27_LEADER_COMPATIBILITY
    //
    // Imported roster UnitIds are generated UUID IDs. The YellowScribe Leader
    // description is not guaranteed to contain its legal bodyguard list, so an
    // exact UnitId-only test is not sufficient. This method gives the runtime
    // a conservative display-name fallback through the compatibility table.
    public static bool AllowsBodyguard(
        string leaderName,
        string bodyguardName)
    {
        if (string.IsNullOrWhiteSpace(
                leaderName) ||
            string.IsNullOrWhiteSpace(
                bodyguardName))
        {
            return false;
        }

        foreach (string legalName
            in GetBodyguardNames(
                leaderName))
        {
            if (NamesEquivalent(
                    legalName,
                    bodyguardName))
            {
                return true;
            }
        }

        return false;
    }

    public static bool NamesEquivalent(
        string first,
        string second)
    {
        string a =
            NormalizeComparable(
                first
            );

        string b =
            NormalizeComparable(
                second
            );

        return
            !string.IsNullOrWhiteSpace(a) &&
            !string.IsNullOrWhiteSpace(b) &&
            string.Equals(
                a,
                b,
                StringComparison.OrdinalIgnoreCase
            );
    }

    public static string Normalize(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return "";
        }

        char[] chars =
            value
                .ToLowerInvariant()
                .Select(
                    c =>
                        char.IsLetterOrDigit(c)
                        ? c
                        : ' '
                )
                .ToArray();

        return string.Join(
            " ",
            new string(chars)
                .Split(
                    new[] { ' ' },
                    StringSplitOptions
                        .RemoveEmptyEntries
                )
        );
    }

    private static string NormalizeComparable(
        string value)
    {
        string normalized =
            Normalize(value);

        if (string.IsNullOrWhiteSpace(
                normalized))
        {
            return "";
        }

        string[] removablePrefixes =
        {
            "ynnari ",
            "aeldari ",
            "asuryani ",
            "drukhari ",
            "orks ",
            "ork ",
            "necrons ",
            "necron ",
            "tyranids ",
            "tyranid ",
            "adeptus custodes "
        };

        bool changed = true;

        while (changed)
        {
            changed = false;

            foreach (string prefix
                in removablePrefixes)
            {
                if (normalized.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase) &&
                    normalized.Length >
                        prefix.Length)
                {
                    normalized =
                        normalized.Substring(
                            prefix.Length
                        );

                    changed = true;
                    break;
                }
            }
        }

        return normalized.Trim();
    }

    private static void EnsureLoaded()
    {
        if (cached != null)
            return;

        TextAsset asset =
            Resources.Load<TextAsset>(
                ResourcePath
            );

        if (asset == null)
        {
            cached =
                new LeaderCompatibilityOverrideCollection
                {
                    entries =
                        new LeaderCompatibilityOverrideEntry[0]
                };

            return;
        }

        cached =
            JsonUtility.FromJson<
                LeaderCompatibilityOverrideCollection
            >(asset.text);

        if (cached == null)
        {
            cached =
                new LeaderCompatibilityOverrideCollection
                {
                    entries =
                        new LeaderCompatibilityOverrideEntry[0]
                };
        }
    }
}
