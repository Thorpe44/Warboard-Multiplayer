using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// WARBOARD_V55_SQUAD_WEAPON_SHOOTING
public partial class GameController : MonoBehaviour
{
    private void V55OpenSquadWeaponChoice(
        SquadController attacker,
        SquadController target,
        List<WeaponAttackSelection> choices,
        bool engaged)
    {
        if (attacker == null ||
            target == null ||
            choices == null ||
            choices.Count == 0)
        {
            return;
        }

        List<IGrouping<string, WeaponAttackSelection>>
            groups =
                choices
                    .Where(selection =>
                        selection != null &&
                        selection.model != null &&
                        selection.weapon != null)
                    .GroupBy(selection =>
                        !string.IsNullOrWhiteSpace(
                            selection.weapon.id)
                        ? selection.weapon.id
                        : selection.weapon.displayName)
                    .OrderBy(group =>
                        group.First()
                            .weapon.displayName)
                    .ToList();

        if (groups.Count == 0)
        {
            status =
                attacker.DisplayName +
                " has no unused ranged weapons that can legally target " +
                target.DisplayName +
                ".";

            return;
        }

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (IGrouping<
            string,
            WeaponAttackSelection> group
            in groups)
        {
            List<WeaponAttackSelection>
                captured =
                    group.ToList();

            WeaponData weapon =
                captured[0].weapon;

            int weaponInstances =
                captured.Count;

            int firingModels =
                captured
                    .Select(selection =>
                        selection.model)
                    .Distinct()
                    .Count();

            string attacks =
                string.IsNullOrWhiteSpace(
                    weapon.attacksExpression)
                ? weapon.attacksPerModel
                    .ToString()
                : weapon.attacksExpression;

            string profile =
                "FIRE " +
                weaponInstances +
                "x " +
                weapon.displayName +
                "   |   " +
                firingModels +
                " model" +
                (firingModels == 1
                    ? ""
                    : "s") +
                "   |   " +
                weapon.range.ToString(
                    "0.#") +
                "\"  A " +
                attacks +
                "  S " +
                weapon.strength +
                "  AP " +
                weapon.ap;

            options.Add(
                new RuleChoiceOption(
                    profile,
                    () =>
                    {
                        CloseRuleChoice();

                        V55StartSquadWeaponGroupAttack(
                            attacker,
                            target,
                            captured,
                            engaged
                        );
                    }
                )
            );
        }

        bool selectedModelBelongs =
            selectedModel != null &&
            selectedModel.Squad != null &&
            selectedModel.Squad
                .JoinedActionController() ==
            attacker.JoinedActionController();

        if (selectedModelBelongs)
        {
            List<WeaponAttackSelection>
                modelChoices =
                    choices
                        .Where(selection =>
                            selection != null &&
                            selection.model ==
                                selectedModel)
                        .ToList();

            if (modelChoices.Count > 0)
            {
                options.Add(
                    new RuleChoiceOption(
                        "ADVANCED / SPLIT FIRE - " +
                        selectedModel.RoleName,
                        () =>
                        {
                            CloseRuleChoice();

                            OpenModelWeaponChoice(
                                attacker,
                                selectedModel,
                                target,
                                modelChoices,
                                engaged
                            );
                        }
                    )
                );
            }
        }

        OpenRuleChoice(
            "SQUAD WEAPONS - " +
            attacker.DisplayName,
            "Choose a weapon pool to fire at " +
            target.DisplayName +
            ". Warboard groups every currently eligible copy of the same weapon across the joined unit. Select a model first and use ADVANCED / SPLIT FIRE when you want model-level allocation instead.",
            options.ToArray()
        );
    }

    private void V55StartSquadWeaponGroupAttack(
        SquadController attacker,
        SquadController target,
        List<WeaponAttackSelection> selections,
        bool engaged)
    {
        if (attacker == null ||
            target == null ||
            selections == null ||
            selections.Count == 0)
        {
            return;
        }

        selections =
            selections
                .Where(selection =>
                    selection != null &&
                    selection.model != null &&
                    selection.weapon != null)
                .ToList();

        if (selections.Count == 0)
            return;

        if (OfferAeldariPathOfWarrior(
                attacker,
                () =>
                    V55StartSquadWeaponGroupAttack(
                        attacker,
                        target,
                        selections,
                        engaged
                    )))
        {
            return;
        }

        interactiveAttackConsumesNormalAction =
            false;

        // The attack still records weapon use per physical model. This flag
        // keeps the unit available until every firing model is actually done.
        interactiveAttackModelLevelShooting =
            true;

        BeginInteractiveAttack(
            attacker,
            target,
            selections,
            AttackMode.Ranged,
            engaged
        );
    }
}
