using System.Linq;
using UnityEngine;

public partial class ModelToken
{
    public WarboardModelSnapshot
        CaptureMultiplayerModelSnapshot(
            int index)
    {
        return
            new WarboardModelSnapshot
            {
                index = index,
                name = gameObject.name,
                roleName = RoleName ?? "",
                position =
                    transform.position,
                rotation =
                    transform.rotation,
                turnStartPosition =
                    TurnStartWorldPosition,
                currentWounds =
                    CurrentWounds,
                alive =
                    IsAlive,
                completedShooting =
                    HasCompletedShootingThisTurn,
                oneShotWeaponsUsed =
                    oneShotWeaponsUsed
                        .ToArray(),
                rangedWeaponsFiredThisTurn =
                    rangedWeaponsFiredThisTurn
                        .ToArray(),
                rangedFireGroupThisTurn =
                    rangedFireGroupThisTurn ??
                    ""
            };
    }

    public void ApplyMultiplayerModelSnapshot(
        WarboardModelSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        transform.position =
            snapshot.position;

        transform.rotation =
            snapshot.rotation;

        TurnStartWorldPosition =
            snapshot.turnStartPosition;

        CurrentWounds =
            Mathf.Clamp(
                snapshot.currentWounds,
                0,
                MaxWounds
            );

        oneShotWeaponsUsed.Clear();

        if (snapshot.oneShotWeaponsUsed !=
            null)
        {
            foreach (
                string value
                in snapshot.oneShotWeaponsUsed)
            {
                if (!string.IsNullOrWhiteSpace(
                        value))
                {
                    oneShotWeaponsUsed.Add(
                        value
                    );
                }
            }
        }

        rangedWeaponsFiredThisTurn.Clear();

        if (snapshot
            .rangedWeaponsFiredThisTurn !=
            null)
        {
            foreach (
                string value
                in snapshot
                    .rangedWeaponsFiredThisTurn)
            {
                if (!string.IsNullOrWhiteSpace(
                        value))
                {
                    rangedWeaponsFiredThisTurn
                        .Add(value);
                }
            }
        }

        rangedFireGroupThisTurn =
            snapshot.rangedFireGroupThisTurn ??
            "";

        HasCompletedShootingThisTurn =
            snapshot.completedShooting;

        bool alive =
            snapshot.alive &&
            CurrentWounds > 0;

        gameObject.SetActive(alive);

        RefreshWoundDisplay();

        SetPresentationVisible(
            alive &&
            Squad != null &&
            Squad.IsOnBattlefield
        );

        SetWoundDisplayVisible(
            alive &&
            Squad != null &&
            Squad.IsOnBattlefield
        );
    }
}
