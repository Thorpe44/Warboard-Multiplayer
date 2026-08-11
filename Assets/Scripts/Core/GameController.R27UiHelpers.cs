using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

// WARBOARD_R27_DIRECT_UI_HELPERS
//
// R26 attempted to create a second Canvas on top of the real HUD. R27 instead
// draws player information from GameController's existing IMGUI top bar, using
// the actual roster labels / metadata already loaded for Player 1 and Player 2.

public partial class GameController
{
    private void R27DrawDeploymentPlayerSummaryBar(
        Rect topBar)
    {
        if (factions == null ||
            factions.Count < 2)
        {
            return;
        }

        Rect strip =
            new Rect(
                topBar.x + 10f,
                topBar.y + 46f,
                topBar.width - 20f,
                24f
            );

        DrawTintedBox(
            strip,
            new Color(
                0.020f,
                0.030f,
                0.045f,
                0.98f
            )
        );

        GUIStyle left =
            new GUIStyle(
                GUI.skin.label
            );

        left.fontSize = 11;
        left.fontStyle =
            FontStyle.Bold;
        left.alignment =
            TextAnchor.MiddleLeft;
        left.normal.textColor =
            new Color(
                0.88f,
                0.92f,
                0.96f,
                1f
            );

        GUIStyle right =
            new GUIStyle(left);

        right.alignment =
            TextAnchor.MiddleRight;

        float half =
            strip.width *
            0.5f;

        GUI.Label(
            new Rect(
                strip.x + 10f,
                strip.y,
                half - 18f,
                strip.height
            ),
            R27PlayerRosterSummary(
                0,
                factions[0]
            ),
            left
        );

        GUI.Label(
            new Rect(
                strip.x + half + 8f,
                strip.y,
                half - 18f,
                strip.height
            ),
            R27PlayerRosterSummary(
                1,
                factions[1]
            ),
            right
        );
    }

    private string R27PlayerRosterSummary(
        int playerIndex,
        string runtimeFaction)
    {
        string player =
            playerIndex == 0
            ? "P1"
            : "P2";

        string rosterLabel =
            playerIndex == 0
            ? playerOneRosterLabel
            : playerTwoRosterLabel;

        RosterImportMetadata metadata =
            RosterImportMetadataStore.Get(
                runtimeFaction
            );

        string sourceFaction =
            metadata != null &&
            !string.IsNullOrWhiteSpace(
                metadata.SourceFaction)
            ? metadata.SourceFaction
            : rosterLabel;

        if (string.IsNullOrWhiteSpace(
                sourceFaction))
        {
            sourceFaction =
                DisplayFactionName(
                    runtimeFaction
                );
        }

        string detachment =
            "";

        if (metadata != null &&
            metadata.ExplicitDetachmentValues != null)
        {
            string[] clean =
                metadata
                    .ExplicitDetachmentValues
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value
                            )
                    )
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase
                    )
                    .ToArray();

            if (clean.Length == 1)
            {
                detachment =
                    clean[0].Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(
                detachment) &&
            aeldariRules != null &&
            aeldariRules.IsAeldariFaction(
                runtimeFaction))
        {
            detachment =
                aeldariRules.DetachmentName(
                    runtimeFaction
                );
        }

        string disposition =
            playerIndex == 0
            ? R27HumanizeIdentifier(
                missionDispositionPlayerOne
                    .ToString()
              )
            : R27HumanizeIdentifier(
                missionDispositionPlayerTwo
                    .ToString()
              );

        string secondary =
            playerIndex == 0
            ? R27HumanizeIdentifier(
                missionSecondaryPlayerOne
                    .ToString()
              )
            : R27HumanizeIdentifier(
                missionSecondaryPlayerTwo
                    .ToString()
              );

        List<string> pieces =
            new List<string>();

        pieces.Add(
            player
        );

        pieces.Add(
            sourceFaction
                .ToUpperInvariant()
        );

        if (!string.IsNullOrWhiteSpace(
                detachment))
        {
            pieces.Add(
                detachment
            );
        }

        if (!string.IsNullOrWhiteSpace(
                disposition))
        {
            pieces.Add(
                disposition
            );
        }

        if (!string.IsNullOrWhiteSpace(
                secondary))
        {
            pieces.Add(
                secondary
            );
        }

        return
            string.Join(
                "  •  ",
                pieces.ToArray()
            );
    }

    private static string R27HumanizeIdentifier(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return "";
        }

        string result =
            value
                .Replace("_", " ")
                .Replace("-", " ");

        result =
            Regex.Replace(
                result,
                @"(?<=[a-z0-9])(?=[A-Z])",
                " "
            );

        result =
            Regex.Replace(
                result,
                @"\s+",
                " "
            )
            .Trim();

        if (result.Length == 0)
            return result;

        return
            char.ToUpperInvariant(
                result[0]
            ) +
            result.Substring(1);
    }

    private static string R27WeaponRulesForUi(
        string keywordText,
        string rawText)
    {
        string keywords =
            keywordText ?? "";

        string raw =
            rawText ?? "";

        bool keywordsInternal =
            R27LooksLikeInternalRuleText(
                keywords
            );

        bool rawInternal =
            R27LooksLikeInternalRuleText(
                raw
            );

        // YellowScribe commonly supplies:
        //   keywordText = "devastating_wounds, anti_infantry_2"
        //   raw         = "Anti-Infantry 2+, Devastating Wounds, Psychic"
        // Prefer the human-readable source and never print both versions.
        if (!string.IsNullOrWhiteSpace(
                raw) &&
            !rawInternal)
        {
            return raw.Trim();
        }

        if (!string.IsNullOrWhiteSpace(
                keywords) &&
            !keywordsInternal)
        {
            return keywords.Trim();
        }

        string internalValue =
            !string.IsNullOrWhiteSpace(
                keywords)
            ? keywords
            : raw;

        if (string.IsNullOrWhiteSpace(
                internalValue))
        {
            return "";
        }

        string[] tokens =
            internalValue
                .Split(
                    new[] { ',', '|' },
                    StringSplitOptions
                        .RemoveEmptyEntries
                );

        return
            string.Join(
                ", ",
                tokens
                    .Select(
                        token =>
                            R27HumanizeRuleToken(
                                token.Trim()
                            )
                    )
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value
                            )
                    )
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase
                    )
                    .ToArray()
            );
    }

    private static bool R27LooksLikeInternalRuleText(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return false;
        }

        return
            value.Contains("_") &&
            Regex.IsMatch(
                value.Trim(),
                @"^[a-z0-9_+,\-\s|]+$"
            );
    }

    private static string R27HumanizeRuleToken(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return "";
        }

        string lower =
            value
                .Trim()
                .ToLowerInvariant();

        switch (lower)
        {
            case "devastating_wounds":
                return "Devastating Wounds";

            case "lethal_hits":
                return "Lethal Hits";

            case "deep_strike":
                return "Deep Strike";

            case "fights_first":
                return "Fights First";

            case "torrent":
                return "Torrent";

            case "psychic":
                return "Psychic";
        }

        Match anti =
            Regex.Match(
                lower,
                @"^anti_([a-z0-9_]+)_([0-9]+)$"
            );

        if (anti.Success)
        {
            return
                "Anti-" +
                R27HumanizeIdentifier(
                    anti.Groups[1]
                        .Value
                ) +
                " " +
                anti.Groups[2]
                    .Value +
                "+";
        }

        Match sustained =
            Regex.Match(
                lower,
                @"^sustained_hits_(.+)$"
            );

        if (sustained.Success)
        {
            return
                "Sustained Hits " +
                sustained.Groups[1]
                    .Value
                    .ToUpperInvariant();
        }

        Match fnp =
            Regex.Match(
                lower,
                @"^feel_no_pain_([0-9]+)$"
            );

        if (fnp.Success)
        {
            return
                "Feel No Pain " +
                fnp.Groups[1]
                    .Value +
                "+";
        }

        return
            R27HumanizeIdentifier(
                lower
            );
    }
}
