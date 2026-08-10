using System.Collections.Generic;
using UnityEngine;

// WARBOARD_V54_UI_HELPERS
public partial class GameController : MonoBehaviour
{
    private void V54DrawFightControls()
    {
        if (phase != Phase.Fight ||
            deploymentMode ||
            armyImportMode ||
            missionSetupMode ||
            battleOver)
        {
            return;
        }

        Rect bar =
            new Rect(
                12f,
                84f,
                Screen.width - 24f,
                44f
            );

        DrawTintedBox(
            bar,
            new Color(
                0.025f,
                0.035f,
                0.050f,
                0.985f
            )
        );

        float x =
            bar.x + 12f;

        DrawFight11ContextControls(
            bar,
            ref x
        );
    }

    private string V54FactionRuleSummary(
        string faction)
    {
        IFactionGameController controller =
            FactionControllerRuntime.Get(
                faction
            );

        CustodesGameController custodes =
            controller as
                CustodesGameController;

        if (custodes != null)
        {
            if (!custodes.DetachmentLocked)
            {
                return
                    "ADEPTUS CUSTODES: " +
                    "detachment not locked.";
            }

            List<string> pieces =
                new List<string>();

            foreach (CustodesDetachment
                detachment
                in custodes.LockedDetachments)
            {
                pieces.Add(
                    CustodesDetachmentRuntime
                        .Name(detachment) +
                    " — " +
                    V54CustodesRuleShort(
                        detachment
                    )
                );
            }

            return
                "ADEPTUS CUSTODES: " +
                string.Join(
                    " | ",
                    pieces.ToArray()
                );
        }

        NecronGameController necrons =
            controller as
                NecronGameController;

        if (necrons != null)
        {
            if (!necrons.DetachmentLocked)
            {
                return
                    "NECRONS: " +
                    "detachment not locked.";
            }

            List<string> pieces =
                new List<string>();

            foreach (NecronDetachment
                detachment
                in necrons.LockedDetachments)
            {
                pieces.Add(
                    NecronDetachmentRuntime
                        .Name(detachment) +
                    " — " +
                    V54NecronRuleShort(
                        detachment
                    )
                );
            }

            return
                "NECRONS: " +
                string.Join(
                    " | ",
                    pieces.ToArray()
                );
        }

        StandardFactionGameController standard =
            controller as
                StandardFactionGameController;

        if (standard != null &&
            standard.Pack != null)
        {
            List<string> pieces =
                new List<string>();

            if (!string.IsNullOrWhiteSpace(
                    standard.Pack.armyRuleName))
            {
                pieces.Add(
                    standard.Pack.armyRuleName +
                    ": " +
                    V54TrimRule(
                        standard.Pack.armyRuleText,
                        115
                    )
                );
            }

            foreach (string selected
                in standard.SelectedDetachments)
            {
                StandardFactionDetachment11
                    detachment =
                        StandardFactionPack11
                            .FindDetachment(
                                standard.Pack,
                                selected
                            );

                if (detachment == null)
                    continue;

                pieces.Add(
                    detachment.name +
                    " — " +
                    detachment.ruleName +
                    ": " +
                    V54TrimRule(
                        detachment.ruleText,
                        125
                    )
                );
            }

            return
                standard.DisplayName +
                ": " +
                string.Join(
                    " | ",
                    pieces.ToArray()
                );
        }

        string fallback =
            factionRules != null
            ? factionRules.RuleSummary(
                faction
              )
            : "Generic Core";

        return
            string.IsNullOrWhiteSpace(
                fallback)
            ? "Faction rules loaded."
            : fallback;
    }

    private string V54TrimRule(
        string text,
        int maximum)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        string value =
            text.Trim();

        if (value.Length <= maximum)
            return value;

        return
            value.Substring(
                0,
                Mathf.Max(
                    0,
                    maximum - 1
                )
            ).TrimEnd() +
            "…";
    }

    private string V54CustodesRuleShort(
        CustodesDetachment detachment)
    {
        switch (detachment)
        {
            case CustodesDetachment
                .TalonsOfTheEmperor:
                return "Revered Companions: Custodes/Anathema auras grant FNP vs psychic/mortals or +1 Hit.";

            case CustodesDetachment
                .ShieldHost:
                return "Martial Mastery: choose melee Critical Hits on 5+ or improve melee AP by 1 each battle round.";

            case CustodesDetachment
                .NullMaidenVigil:
                return "Creeping Dread: nearby Psykers/below-strength enemies take Battle-shock tests; Prosecutors are Battleline.";

            case CustodesDetachment
                .AuricChampions:
                return "Assemblage of Might: Custodes Characters get +1 Wound against the selected enemy unit.";

            case CustodesDetachment
                .SolarSpearhead:
                return "Auric Armour/Moritoi Ancients: vehicle OC/rerolls plus faster Walkers and Walker Character choices.";

            case CustodesDetachment
                .LionsOfTheEmperor:
                return "Against All Odds: isolated non-Vehicle Custodes models get +1 Hit and +1 Wound.";

            case CustodesDetachment
                .MightOfTheMoritoi:
                return "March of the Honoured Dead: Walkers gain +2 Move and +1 Advance/Charge.";

            case CustodesDetachment
                .SilentHunters:
                return "Skin-Crawling Disorientation: Anathema Advance/Action support plus Ceaseless Vigilance.";

            case CustodesDetachment
                .TharanatoiHammerblow:
                return "The Hammer Falls: Terminators that made an ingress move this turn can re-roll Charge rolls.";

            default:
                return "Detachment rule active.";
        }
    }

    private string V54NecronRuleShort(
        NecronDetachment detachment)
    {
        switch (detachment)
        {
            case NecronDetachment
                .AwakenedDynasty:
                return "Command Protocols: led Necron units get +1 Hit.";

            case NecronDetachment
                .AnnihilationLegion:
                return "Annihilation Protocol: Destroyer/Flayed charge support and Destroyer ranged AP pressure.";

            case NecronDetachment
                .CanoptekCourt:
                return "Power Matrix: objective control expands the matrix; Cryptek/Canoptek gain Hit re-roll support.";

            case NecronDetachment
                .ObeisancePhalanx:
                return "Worthy Foes: selected enemy suffers +1 Wound from Noble/Lychguard/Triarch units.";

            case NecronDetachment
                .HypercryptLegion:
                return "Hyperphasing: eligible unengaged Necron units can enter Strategic Reserves at opponent turn end.";

            case NecronDetachment
                .StarshatterArsenal:
                return "Relentless Onslaught: +1 Hit near objectives; eligible Vehicle/Mounted ranged weapons gain Assault.";

            case NecronDetachment
                .CryptekConclave:
                return "Technosorcerous Augmentations: Crypteks gain Assault and select a ranged weapon ability.";

            case NecronDetachment
                .CursedLegion:
                return "Cold Fervour: Destroyer Cult Strength boost can empower other qualifying Necron weapons.";

            case NecronDetachment
                .PantheonOfWoe:
                return "Cosmic Distortion: Monster auras make nearby enemies unravelling for improved AP.";

            case NecronDetachment
                .HandOfTheDynasty:
                return "Hypermotility Protocols: Immortals/Warriors gain Assault and can start Actions after Advancing.";

            case NecronDetachment
                .SkyshroudSpearhead:
                return "Transdimensional Deployment: Tomb Blades gain Deep Strike and ingress shooting support.";

            case NecronDetachment
                .ThePhaeronsArmoury:
                return "Empowered Engines: Necron Titanic Fly units gain +6 Move.";

            default:
                return "Detachment rule active.";
        }
    }
}
