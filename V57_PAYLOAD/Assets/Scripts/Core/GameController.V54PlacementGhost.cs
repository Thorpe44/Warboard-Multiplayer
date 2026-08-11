using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// WARBOARD_V54_PLACEMENT_GHOST_SYSTEM
public partial class GameController : MonoBehaviour
{
    private sealed class PlacementGhostEntry54
    {
        public ModelToken Source;
        public GameObject Ghost;
        public Renderer[] Renderers;
    }

    private readonly List<PlacementGhostEntry54>
        placementGhosts54 =
            new List<PlacementGhostEntry54>();

    private void V54UpdatePlacementGhost()
    {
        if (battleSetupMode ||
            armyImportMode ||
            missionSetupMode ||
            battleOver ||
            gameCamera == null ||
            showDatasheet ||
            (showRuleChoiceWindow &&
             !deploymentMode) ||
            showStratagemReaction ||
            showStratagemMenu ||
            interactiveAttack != null ||
            traditionalAttackPending)
        {
            V54ClearPlacementGhosts();
            return;
        }

        Vector3 cursor;

        // WARBOARD_V57_DEPLOYMENT_GHOSTS
        // Undeployed units deliberately have their normal presentation hidden.
        // Deployment therefore uses its own cursor projection + model list,
        // rather than relying on the battlefield-only joined-unit path.
        if (deploymentMode &&
            currentDeploymentSquad != null)
        {
            if (!V57TryCursorOnTabletop(
                    out cursor))
            {
                V54ClearPlacementGhosts();
                return;
            }

            List<ModelToken> models =
                V57DeploymentGhostModels(
                    currentDeploymentSquad
                );

            if (models.Count == 0)
            {
                V54ClearPlacementGhosts();
                return;
            }

            Vector3 centre =
                Vector3.zero;

            foreach (ModelToken model
                in models)
            {
                centre +=
                    model.transform.position;
            }

            centre /=
                models.Count;

            Vector3 delta =
                cursor - centre;

            delta.y = 0f;

            MissionDeploymentZone zone =
                DeploymentZoneForFaction(
                    activeFaction
                );

            bool legal =
                zone != null;

            foreach (ModelToken model
                in models)
            {
                Vector3 destination =
                    model.transform.position +
                    delta;

                destination.y =
                    model.transform.position.y;

                legal =
                    legal &&
                    InsideBoard(destination) &&
                    zone.ContainsBase(
                        destination,
                        model.BaseRadiusInches
                    ) &&
                    !V53ModelBaseOverlapsSolidAreaScenery(
                        model,
                        destination
                    );
            }

            V54ShowGhosts(
                models,
                model =>
                {
                    Vector3 destination =
                        model.transform.position +
                        delta;

                    destination.y =
                        model.transform.position.y;

                    return destination;
                },
                legal
            );

            return;
        }

        if (!TryCursorPointOnBattlefield(
                out cursor))
        {
            V54ClearPlacementGhosts();
            return;
        }

        if (reservePlacementSquad != null)
        {
            List<ModelToken> models =
                reservePlacementSquad
                    .JoinedLivingModelTokens()
                    .Where(model => model != null)
                    .ToList();

            Vector3 centre =
                reservePlacementSquad
                    .CurrentCentre();

            Vector3 delta =
                cursor - centre;

            delta.y = 0f;

            bool legal =
                V54TranslatedGroupLooksLegal(
                    models,
                    delta
                );

            V54ShowTranslatedGroup(
                models,
                delta,
                legal
            );

            return;
        }

        if (specialMoveSquad != null)
        {
            List<ModelToken> models =
                specialMoveSquad
                    .JoinedLivingModelTokens()
                    .Where(model => model != null)
                    .ToList();

            Vector3 centre =
                specialMoveSquad
                    .CurrentCentre();

            Vector3 delta =
                cursor - centre;

            delta.y = 0f;

            bool legal =
                HorizontalDistance(
                    centre,
                    cursor
                ) <=
                specialMoveMaxDistance +
                0.001f &&
                V54TranslatedGroupLooksLegal(
                    models,
                    delta
                );

            V54ShowTranslatedGroup(
                models,
                delta,
                legal
            );

            return;
        }

        if (phase == Phase.Move &&
            selectedSquad != null &&
            selectedSquad.IsAlive &&
            selectedSquad.FactionId ==
                activeFaction)
        {
            if (wholeSquadMoveMode)
            {
                List<ModelToken> models =
                    selectedSquad
                        .JoinedLivingModelTokens()
                        .Where(model => model != null)
                        .ToList();

                Vector3 centre =
                    selectedSquad
                        .CurrentCentre();

                Vector3 delta =
                    cursor - centre;

                delta.y = 0f;

                bool legal =
                    selectedSquad
                        .CanTranslateWithinNormalMove(
                            delta
                        ) &&
                    V54TranslatedGroupLooksLegal(
                        models,
                        delta
                    );

                V54ShowTranslatedGroup(
                    models,
                    delta,
                    legal
                );

                return;
            }

            if (selectedModel != null &&
                selectedModel.IsAlive)
            {
                Vector3 destination =
                    cursor;

                destination.y =
                    selectedModel
                        .transform.position.y;

                float total =
                    selectedModel
                        .DistanceMovedFromTurnStart(
                            destination
                        );

                float allowance =
                    selectedSquad
                        .GetMovementAllowanceFor(
                            selectedModel
                        );

                bool legal =
                    total <=
                        allowance + 0.001f &&
                    InsideBoard(destination) &&
                    CanPlaceModel(
                        selectedModel,
                        destination
                    ) &&
                    !V53ModelBaseOverlapsSolidAreaScenery(
                        selectedModel,
                        destination
                    );

                V54ShowGhosts(
                    new List<ModelToken>
                    {
                        selectedModel
                    },
                    model => destination,
                    legal
                );

                return;
            }
        }

        if (phase == Phase.Fight &&
            fightActivationUnit != null &&
            selectedModel != null &&
            selectedModel.IsAlive &&
            ModelBelongsToFightActivation(
                selectedModel) &&
            (fightActivationStage ==
                FightActivationStage.PileIn ||
             fightActivationStage ==
                FightActivationStage
                    .Consolidate))
        {
            Vector3 destination =
                cursor;

            destination.y =
                selectedModel
                    .transform.position.y;

            string reason;

            bool legal =
                Fight11FightStageDestinationLegal(
                    selectedModel,
                    destination,
                    out reason
                );

            V54ShowGhosts(
                new List<ModelToken>
                {
                    selectedModel
                },
                model => destination,
                legal
            );

            return;
        }

        V54ClearPlacementGhosts();
    }

    private bool V54TranslatedGroupLooksLegal(
        List<ModelToken> models,
        Vector3 delta)
    {
        if (models == null ||
            models.Count == 0)
        {
            return false;
        }

        foreach (ModelToken model
            in models)
        {
            if (model == null)
                continue;

            Vector3 destination =
                model.transform.position +
                delta;

            destination.y =
                model.transform.position.y;

            if (!InsideBoard(destination) ||
                V53ModelBaseOverlapsSolidAreaScenery(
                    model,
                    destination
                ))
            {
                return false;
            }
        }

        return true;
    }

    private bool V57TryCursorOnTabletop(
        out Vector3 point)
    {
        point = Vector3.zero;

        if (gameCamera == null)
            return false;

        Ray ray =
            gameCamera.ScreenPointToRay(
                Input.mousePosition
            );

        Plane tabletop =
            new Plane(
                Vector3.up,
                Vector3.zero
            );

        float distance;

        if (!tabletop.Raycast(
                ray,
                out distance))
        {
            return false;
        }

        point =
            ray.GetPoint(distance);

        point.y = 0f;

        return true;
    }

    private List<ModelToken>
        V57DeploymentGhostModels(
            SquadController squad)
    {
        List<ModelToken> result =
            new List<ModelToken>();

        if (squad == null)
            return result;

        SquadController action =
            squad.JoinedActionController();

        if (action == null)
            action = squad;

        result.AddRange(
            action
                .AllLivingModelTokens()
                .Where(model =>
                    model != null)
        );

        // JoinedLivingModelTokens intentionally excludes an attached Leader
        // until it is actually on the battlefield. Deployment preview must
        // show the complete joined unit before that happens.
        if (action.AttachedLeader != null &&
            action.AttachedLeader.IsAlive &&
            action.AttachedLeader.BattlefieldState ==
                SquadBattlefieldState.Undeployed)
        {
            result.AddRange(
                action.AttachedLeader
                    .AllLivingModelTokens()
                    .Where(model =>
                        model != null)
            );
        }

        if (action.IsAttachedLeader &&
            action.AttachedBodyguard != null)
        {
            SquadController bodyguard =
                action.AttachedBodyguard;

            result.AddRange(
                bodyguard
                    .AllLivingModelTokens()
                    .Where(model =>
                        model != null)
            );

            if (bodyguard.AttachedLeader != null &&
                bodyguard.AttachedLeader.IsAlive)
            {
                result.AddRange(
                    bodyguard.AttachedLeader
                        .AllLivingModelTokens()
                        .Where(model =>
                            model != null)
                );
            }
        }

        return
            result
                .Where(model =>
                    model != null &&
                    model.IsAlive)
                .Distinct()
                .ToList();
    }

    private void V54ShowTranslatedGroup(
        List<ModelToken> models,
        Vector3 delta,
        bool legal)
    {
        V54ShowGhosts(
            models,
            model =>
            {
                Vector3 destination =
                    model.transform.position +
                    delta;

                destination.y =
                    model.transform.position.y;

                return destination;
            },
            legal
        );
    }

    private void V54ShowGhosts(
        List<ModelToken> sources,
        System.Func<ModelToken, Vector3>
            destination,
        bool legal)
    {
        if (sources == null ||
            destination == null)
        {
            V54ClearPlacementGhosts();
            return;
        }

        sources =
            sources
                .Where(model =>
                    model != null &&
                    model.IsAlive)
                .Distinct()
                .ToList();

        bool same =
            placementGhosts54.Count ==
                sources.Count;

        if (same)
        {
            for (int i = 0;
                 i < sources.Count;
                 i++)
            {
                if (placementGhosts54[i]
                        .Source !=
                    sources[i])
                {
                    same = false;
                    break;
                }
            }
        }

        if (!same)
        {
            V54ClearPlacementGhosts();

            foreach (ModelToken source
                in sources)
            {
                GameObject ghost =
                    source
                        .CreatePlacementGhost54();

                if (ghost == null)
                    continue;

                PlacementGhostEntry54 entry =
                    new PlacementGhostEntry54
                    {
                        Source = source,
                        Ghost = ghost,
                        Renderers =
                            ghost
                                .GetComponentsInChildren<
                                    Renderer
                                >(true)
                    };

                V54PrepareGhostMaterials(
                    entry
                );

                placementGhosts54.Add(
                    entry
                );
            }
        }

        Color tint =
            legal
            ? new Color(
                0.20f,
                1.0f,
                0.62f,
                0.42f
              )
            : new Color(
                1.0f,
                0.20f,
                0.18f,
                0.42f
              );

        foreach (PlacementGhostEntry54 entry
            in placementGhosts54)
        {
            if (entry == null ||
                entry.Source == null ||
                entry.Ghost == null)
            {
                continue;
            }

            entry.Ghost.transform.position =
                destination(
                    entry.Source
                );

            entry.Ghost.transform.rotation =
                entry.Source
                    .transform.rotation;

            V54TintGhost(
                entry,
                tint
            );
        }
    }

    private void V54PrepareGhostMaterials(
        PlacementGhostEntry54 entry)
    {
        if (entry == null ||
            entry.Renderers == null)
        {
            return;
        }

        Shader ghostShader =
            Shader.Find("Standard");

        if (ghostShader == null)
        {
            ghostShader =
                Shader.Find(
                    "Sprites/Default"
                );
        }

        foreach (Renderer renderer
            in entry.Renderers)
        {
            if (renderer == null)
                continue;

            // WARBOARD_V57_FORCE_GHOST_RENDERERS
            // Deployment source renderers are intentionally disabled.
            // The preview clone must not inherit that hidden presentation.
            renderer.enabled = true;
            renderer.gameObject.SetActive(true);

            renderer.shadowCastingMode =
                UnityEngine.Rendering
                    .ShadowCastingMode.Off;

            renderer.receiveShadows = false;

            Material[] originals =
                renderer.materials;

            Material[] ghosts =
                new Material[
                    originals.Length
                ];

            for (int i = 0;
                 i < originals.Length;
                 i++)
            {
                Material original =
                    originals[i];

                Material material =
                    ghostShader != null
                    ? new Material(
                        ghostShader
                      )
                    : new Material(
                        original
                      );

                if (original != null)
                {
                    if (original.HasProperty(
                            "_MainTex") &&
                        material.HasProperty(
                            "_MainTex"))
                    {
                        material.SetTexture(
                            "_MainTex",
                            original.GetTexture(
                                "_MainTex"
                            )
                        );
                    }

                    if (original.HasProperty(
                            "_BaseMap") &&
                        material.HasProperty(
                            "_MainTex"))
                    {
                        material.SetTexture(
                            "_MainTex",
                            original.GetTexture(
                                "_BaseMap"
                            )
                        );
                    }
                }

                V54ConfigureTransparentMaterial(
                    material
                );

                ghosts[i] = material;
            }

            renderer.materials = ghosts;
        }
    }

    private void V54ConfigureTransparentMaterial(
        Material material)
    {
        if (material == null)
            return;

        if (material.HasProperty("_Mode"))
            material.SetFloat("_Mode", 3f);

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);

        if (material.HasProperty(
                "_SrcBlend"))
        {
            material.SetInt(
                "_SrcBlend",
                (int)UnityEngine.Rendering
                    .BlendMode.SrcAlpha
            );
        }

        if (material.HasProperty(
                "_DstBlend"))
        {
            material.SetInt(
                "_DstBlend",
                (int)UnityEngine.Rendering
                    .BlendMode.OneMinusSrcAlpha
            );
        }

        if (material.HasProperty("_ZWrite"))
            material.SetInt("_ZWrite", 0);

        material.EnableKeyword(
            "_ALPHABLEND_ON"
        );

        material.renderQueue = 3000;
    }

    private void V54TintGhost(
        PlacementGhostEntry54 entry,
        Color tint)
    {
        if (entry == null ||
            entry.Renderers == null)
        {
            return;
        }

        foreach (Renderer renderer
            in entry.Renderers)
        {
            if (renderer == null)
                continue;

            foreach (Material material
                in renderer.materials)
            {
                if (material == null)
                    continue;

                if (material.HasProperty(
                        "_Color"))
                {
                    material.SetColor(
                        "_Color",
                        tint
                    );
                }

                if (material.HasProperty(
                        "_BaseColor"))
                {
                    material.SetColor(
                        "_BaseColor",
                        tint
                    );
                }
            }
        }
    }

    private void V54ClearPlacementGhosts()
    {
        foreach (PlacementGhostEntry54 entry
            in placementGhosts54)
        {
            if (entry != null &&
                entry.Ghost != null)
            {
                Destroy(entry.Ghost);
            }
        }

        placementGhosts54.Clear();
    }
}
