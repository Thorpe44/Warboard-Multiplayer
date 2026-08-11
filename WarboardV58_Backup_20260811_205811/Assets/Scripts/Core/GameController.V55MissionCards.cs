using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// WARBOARD_V55_MISSION_CARD_TEXT
public partial class GameController : MonoBehaviour
{
    public string WorldUiPrimaryCardText55(
        int playerIndex)
    {
        if (missionSystem == null ||
            playerIndex < 0 ||
            playerIndex >= factions.Count)
        {
            return "PRIMARY\nWaiting for mission...";
        }

        string faction =
            factions[playerIndex];

        MissionPlayerState state =
            missionSystem.State(
                faction
            );

        if (state == null)
        {
            return
                DisplayFactionName(faction) +
                "\nPRIMARY\nNot configured";
        }

        string summary =
            V55PrimarySummary(
                state.PrimaryMission
            );

        return
            DisplayFactionName(faction) +
            "\nPRIMARY\n" +
            state.PrimaryMission +
            "\n" +
            V55WrapCardText(
                summary,
                31
            );
    }

    public string WorldUiSecondaryCardText55(
        int playerIndex)
    {
        if (missionSystem == null ||
            playerIndex < 0 ||
            playerIndex >= factions.Count)
        {
            return "SECONDARY\nWaiting for mission...";
        }

        string faction =
            factions[playerIndex];

        MissionPlayerState state =
            missionSystem.State(
                faction
            );

        if (state == null)
        {
            return
                DisplayFactionName(faction) +
                "\nSECONDARY\nNot configured";
        }

        List<string> cards =
            state.SecondaryMode ==
                MissionSecondaryMode.Fixed
            ? state.FixedSecondaries
                .ToList()
            : state.SecondaryHand
                .ToList();

        StringBuilder text =
            new StringBuilder();

        text.Append(
            DisplayFactionName(faction)
        );

        text.Append(
            "\nSECONDARY - "
        );

        text.Append(
            state.SecondaryMode
                .ToString()
                .ToUpperInvariant()
        );

        if (cards.Count == 0)
        {
            text.Append(
                "\nNo active card."
            );

            if (state.SecondaryMode ==
                MissionSecondaryMode.Tactical)
            {
                text.Append(
                    "\nDraw in MISSION INFO."
                );
            }

            return text.ToString();
        }

        foreach (string card
            in cards.Take(2))
        {
            text.Append("\n\n");
            text.Append(card);
            text.Append("\n");
            text.Append(
                V55WrapCardText(
                    V55SecondarySummary(
                        card,
                        state.SecondaryMode ==
                            MissionSecondaryMode.Fixed
                    ),
                    31
                )
            );
        }

        return text.ToString();
    }

    private string V55SecondarySummary(
        string card,
        bool fixedMode)
    {
        switch (card)
        {
            case "Behind Enemy Lines":
                return
                    "End of your turn: 3VP per eligible unit wholly in the enemy deployment zone, max 5VP.";

            case "Secure No Man's Land":
                return
                    "End of your turn: control at least 2 objectives in No Man's Land for 5VP.";

            case "Engage on All Fronts":
                return fixedMode
                    ? "End of your turn: eligible units in 3 quarters = 2VP; 4 quarters = 4VP."
                    : "End of your turn: eligible units in 3 quarters = 3VP; 4 quarters = 5VP.";

            case "Centre Ground":
                return
                    "End of your turn: have an eligible unit within 3\" of centre. Score 5VP if no enemy is within 6\", or 3VP if none is within 3\".";

            case "No Prisoners":
                return
                    "2VP per enemy unit destroyed this turn, to a maximum of 5VP.";

            case "Assassination":
                return fixedMode
                    ? "3VP per enemy Character model destroyed; +1VP for each such model with 4+ Wounds."
                    : "Score 5VP if an enemy Character was destroyed this turn, or all enemy Characters are destroyed.";

            case "Bring it Down":
                return fixedMode
                    ? "4VP per enemy model with 10+ Wounds destroyed."
                    : "Score 5VP if an enemy model with 10+ Wounds was destroyed this turn.";

            default:
                return
                    "Manual scoring in the current build. Use the official card text; Warboard will not invent unverified scoring.";
        }
    }

    private string V55PrimarySummary(
        string mission)
    {
        switch (mission)
        {
            case "Immovable Object":
                return
                    "From round 2: score 5VP for each non-home objective you control, through round 4.";

            case "Unstoppable Force":
                return
                    "Score from non-home objective control, destroying enemy units and taking new ground. End-battle bonus for controlling a central objective.";

            case "Battlefield Dominance":
                return
                    "Score for objectives controlled; holding your home objective increases the value of non-home objectives.";

            case "Inescapable Dominion":
                return
                    "Score for controlling 2+ objectives and controlling more than the opponent. End-battle bonus for the enemy home objective.";

            case "Meatgrinder":
                return
                    "Score for holding non-home objectives, with additional destruction-based mission scoring.";

            case "Purge and Secure":
                return
                    "Score for non-home objectives plus mission-specific destruction conditions.";

            case "Reconnaissance Sweep":
                return
                    "Spread eligible units across battlefield quarters and destroy enemy units.";

            case "Triangulation":
                return
                    "Complete Triangulate actions on objectives; accumulated markers increase scoring.";

            default:
                return
                    "Warboard tracks this Primary automatically where verified. Open MISSION INFO for live mission state and action controls.";
        }
    }

    private string V55WrapCardText(
        string value,
        int width)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return "";
        }

        string[] words =
            value.Split(
                new[]
                {
                    ' ',
                    '\t',
                    '\r',
                    '\n'
                },
                System.StringSplitOptions
                    .RemoveEmptyEntries
            );

        StringBuilder result =
            new StringBuilder();

        int lineLength = 0;

        foreach (string word
            in words)
        {
            if (lineLength > 0 &&
                lineLength +
                1 +
                word.Length >
                width)
            {
                result.Append('\n');
                lineLength = 0;
            }

            if (lineLength > 0)
            {
                result.Append(' ');
                lineLength++;
            }

            result.Append(word);
            lineLength +=
                word.Length;
        }

        return result.ToString();
    }
}
