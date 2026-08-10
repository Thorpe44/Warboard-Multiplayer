using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// WARBOARD V52
// Local-only placement/movement ghost preview.
// This module never moves an authoritative ModelToken and never sends network
// state. It only redraws the currently selected miniature(s) at the candidate
// cursor position.

public partial class GameController : MonoBehaviour
{
    private enum PlacementGhostValidity52
    {
        Unknown,
        Legal,
        Illegal
    }

    private sealed class PlacementGhostCandidate52
    {
        public ModelToken Model;
        public Vector3 Destination;
    }

    private readonly Dictionary<string, Material>
        placementGhostMaterials52 =
            new Dictionary<string, Material>();

    private Mesh placementGhostBaseRingMesh52;
    private Material placementGhostBaseRingLegal52;
    private Material placementGhostBaseRingIllegal52;
    private Material placementGhostBaseRingUnknown52;

    private static readonly Color
        PlacementGhostLegalColor52 =
            new Color(
                0.16f,
                1.00f,
                0.55f,
                0.40f
            );

    private static readonly Color
        PlacementGhostIllegalColor52 =
            new Color(
                1.00f,
                0.18f,
                0.14f,
                0.44f
            );

    private static readonly Color
        PlacementGhostUnknownColor52 =
            new Color(
                0.16f,
                0.78f,
                1.00f,
                0.40f
            );

    // No other GameController partial currently defines LateUpdate.
    // Keeping the preview here means it is drawn after normal selection/input
    // state has settled for the frame.
    private void LateUpdate()
    {
        DrawPlacementGhostPreview52();
    }

    private void DrawPlacementGhostPreview52()
    {
        if (battleSetupMode ||
            armyImportMode ||
            missionSetupMode ||
            battleOver ||
            showDatasheet ||
            showRuleChoiceWindow ||
            showStratagemReaction ||
            showStratagemMenu ||
            traditionalAttackPending ||
            interactiveAttack != null)
        {
            return;
        }

        if (gameCamera == null)
            gameCamera = Camera.main;

        if (gameCamera == null)
            return;

        Vector3 cursor;

        if (!TryCursorPointOnBattlefield(
                out cursor))
        {
            return;
        }

        List<PlacementGhostCandidate52>
            candidates;

        PlacementGhostValidity52 validity;

        if (!TryBuildPlacementGhostCandidates52(
                cursor,
                out candidates,
                out validity))
        {
            return;
        }

        if (candidates == null ||
            candidates.Count == 0)
        {
            return;
        }

        Color tint =
            PlacementGhostTint52(
                validity
            );

        EnsurePlacementGhostBaseRing52();

        foreach (PlacementGhostCandidate52
            candidate in candidates)
        {
            if (candidate == null ||
                candidate.Model == null ||
                !candidate.Model.IsAlive)
            {
                continue;
            }

            DrawPlacementGhostModel52(
                candidate.Model,
                candidate.Destination,
                tint,
                validity
            );
        }
    }

    private bool TryBuildPlacementGhostCandidates52(
        Vector3 cursor,
        out List<PlacementGhostCandidate52>
            candidates,
        out PlacementGhostValidity52 validity)
    {
        candidates =
            new List<
                PlacementGhostCandidate52
            >();

        validity =
            PlacementGhostValidity52.Unknown;

        // INITIAL DEPLOYMENT
        if (deploymentMode &&
            currentDeploymentSquad != null)
        {
            SquadController squad =
                currentDeploymentSquad;

            SquadController leader =
                squad.AttachedLeader;

            if (leader != null &&
                !leader.IsOnBattlefield)
            {
                BuildJoinedDeploymentGhost52(
                    squad,
                    leader,
                    cursor,
                    candidates
                );
            }
            else
            {
                BuildRootTranslationGhost52(
                    squad,
                    cursor,
                    true,
                    candidates
                );
            }

            validity =
                DeploymentGhostValidity52(
                    squad,
                    candidates
                );

            return
                candidates.Count > 0;
        }

        // RESERVES / REINFORCEMENTS.
        // The ghost is deliberately neutral cyan when the candidate passes
        // basic physical placement. Edge/enemy-distance/special ingress rules
        // remain authoritative in the existing click validator.
        if (reservePlacementSquad != null)
        {
            BuildRootTranslationGhost52(
                reservePlacementSquad,
                cursor,
                true,
                candidates
            );

            bool physicallyLegal =
                CandidateBasicsLegal52(
                    candidates
                );

            validity =
                physicallyLegal
                ? PlacementGhostValidity52
                    .Unknown
                : PlacementGhostValidity52
                    .Illegal;

            return
                candidates.Count > 0;
        }

        // SPECIAL / FACTION WHOLE-UNIT MOVES.
        if (specialMoveSquad != null)
        {
            Vector3 start =
                specialMoveSquad
                    .CurrentCentre();

            Vector3 delta =
                cursor - start;

            delta.y = 0f;

            BuildWorldTranslationGhost52(
                specialMoveSquad,
                delta,
                candidates
            );

            float distance =
                HorizontalMagnitude(
                    delta
                );

            bool basicLegal =
                distance <=
                    specialMoveMaxDistance +
                    0.001f &&
                CandidateBoardLegal52(
                    candidates
                );

            validity =
                basicLegal
                ? PlacementGhostValidity52
                    .Unknown
                : PlacementGhostValidity52
                    .Illegal;

            return
                candidates.Count > 0;
        }

        // NORMAL MOVEMENT.
        if (phase == Phase.Move &&
            selectedSquad != null &&
            selectedSquad.IsAlive &&
            selectedSquad.FactionId ==
                activeFaction)
        {
            if (wholeSquadMoveMode)
            {
                Vector3 start =
                    selectedSquad
                        .CurrentCentre();

                Vector3 delta =
                    cursor - start;

                delta.y = 0f;

                BuildWorldTranslationGhost52(
                    selectedSquad,
                    delta,
                    candidates
                );

                bool legal =
                    CandidateBoardLegal52(
                        candidates) &&
                    selectedSquad
                        .CanTranslateWithinNormalMove(
                            delta
                        );

                validity =
                    legal
                    ? PlacementGhostValidity52.Legal
                    : PlacementGhostValidity52.Illegal;

                return
                    candidates.Count > 0;
            }

            if (selectedModel != null &&
                selectedModel.IsAlive)
            {
                Vector3 destination =
                    cursor;

                destination.y =
                    selectedModel
                        .transform.position.y;

                candidates.Add(
                    new PlacementGhostCandidate52
                    {
                        Model = selectedModel,
                        Destination = destination
                    }
                );

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
                        allowance +
                        0.001f &&
                    InsideBoard(
                        destination
                    ) &&
                    CanPlaceModel(
                        selectedModel,
                        destination
                    );

                validity =
                    legal
                    ? PlacementGhostValidity52.Legal
                    : PlacementGhostValidity52.Illegal;

                return true;
            }
        }

        // PILE-IN / CONSOLIDATE are also actual model placements. Show the
        // selected miniature at the proposed destination and use the existing
        // exact Fight-stage destination validator for the colour.
        if (phase == Phase.Fight &&
            fightActivationUnit != null &&
            selectedModel != null &&
            selectedModel.IsAlive &&
            ModelBelongsToFightActivation(
                selectedModel) &&
            (fightActivationStage ==
                FightActivationStage.PileIn ||
             fightActivationStage ==
                FightActivationStage.Consolidate))
        {
            Vector3 destination =
                cursor;

            destination.y =
                selectedModel
                    .transform.position.y;

            candidates.Add(
                new PlacementGhostCandidate52
                {
                    Model = selectedModel,
                    Destination = destination
                }
            );

            string reason;

            bool legal =
                FightStageDestinationLegal(
                    selectedModel,
                    destination,
                    out reason
                );

            validity =
                legal
                ? PlacementGhostValidity52.Legal
                : PlacementGhostValidity52.Illegal;

            return true;
        }

        return false;
    }

    private void BuildRootTranslationGhost52(
        SquadController squad,
        Vector3 destinationRoot,
        bool includeAttachedLeader,
        List<PlacementGhostCandidate52>
            candidates)
    {
        if (squad == null ||
            candidates == null)
        {
            return;
        }

        List<ModelToken> models =
            squad.AllLivingModelTokens();

        Vector3 sourceRoot =
            squad.transform.position;

        foreach (ModelToken model
            in models)
        {
            if (model == null ||
                !model.IsAlive)
            {
                continue;
            }

            Vector3 offset =
                model.transform.position -
                sourceRoot;

            Vector3 destination =
                destinationRoot +
                offset;

            destination.y =
                model.transform.position.y;

            candidates.Add(
                new PlacementGhostCandidate52
                {
                    Model = model,
                    Destination = destination
                }
            );
        }

        if (!includeAttachedLeader ||
            squad.AttachedLeader == null ||
            squad.AttachedLeader.IsOnBattlefield)
        {
            return;
        }

        SquadController leader =
            squad.AttachedLeader;

        Vector3 leaderRoot =
            leader.transform.position;

        foreach (ModelToken model
            in leader.AllLivingModelTokens())
        {
            if (model == null ||
                !model.IsAlive)
            {
                continue;
            }

            Vector3 offset =
                model.transform.position -
                leaderRoot;

            Vector3 destination =
                destinationRoot +
                offset;

            destination.y =
                model.transform.position.y;

            candidates.Add(
                new PlacementGhostCandidate52
                {
                    Model = model,
                    Destination = destination
                }
            );
        }
    }

    private void BuildWorldTranslationGhost52(
        SquadController squad,
        Vector3 delta,
        List<PlacementGhostCandidate52>
            candidates)
    {
        if (squad == null ||
            candidates == null)
        {
            return;
        }

        foreach (ModelToken model
            in squad.JoinedLivingModelTokens())
        {
            if (model == null ||
                !model.IsAlive)
            {
                continue;
            }

            Vector3 destination =
                model.transform.position +
                delta;

            destination.y =
                model.transform.position.y;

            candidates.Add(
                new PlacementGhostCandidate52
                {
                    Model = model,
                    Destination = destination
                }
            );
        }
    }

    // Mirrors the formation-generation section used by
    // TryDeployJoinedFormation without touching the authoritative unit.
    private void BuildJoinedDeploymentGhost52(
        SquadController bodyguard,
        SquadController leader,
        Vector3 centre,
        List<PlacementGhostCandidate52>
            candidates)
    {
        if (bodyguard == null ||
            leader == null ||
            candidates == null)
        {
            return;
        }

        List<ModelToken> joined =
            new List<ModelToken>();

        joined.AddRange(
            bodyguard.AllLivingModelTokens()
        );

        joined.AddRange(
            leader.AllLivingModelTokens()
        );

        joined =
            joined
                .Where(
                    model =>
                        model != null &&
                        model.IsAlive
                )
                .ToList();

        if (joined.Count == 0)
            return;

        float maxDiameter =
            joined.Max(
                model =>
                    Mathf.Max(
                        0.70f,
                        model.BaseRadiusInches *
                            2f
                    )
            );

        float spacing =
            maxDiameter +
            0.10f;

        int columns =
            Mathf.Clamp(
                Mathf.FloorToInt(
                    Mathf.Sqrt(
                        joined.Count
                    )
                ),
                2,
                Mathf.Max(
                    2,
                    joined.Count
                )
            );

        int rows =
            Mathf.CeilToInt(
                joined.Count /
                (float)columns
            );

        for (int i = 0;
             i < joined.Count;
             i++)
        {
            int row =
                i / columns;

            int column =
                i % columns;

            float x =
                (column -
                 (columns - 1) *
                 0.5f) *
                spacing;

            float z =
                (row -
                 (rows - 1) *
                 0.5f) *
                spacing;

            ModelToken model =
                joined[i];

            Vector3 destination =
                new Vector3(
                    centre.x + x,
                    model.transform.position.y,
                    centre.z + z
                );

            candidates.Add(
                new PlacementGhostCandidate52
                {
                    Model = model,
                    Destination = destination
                }
            );
        }
    }

    private PlacementGhostValidity52
        DeploymentGhostValidity52(
            SquadController squad,
            List<PlacementGhostCandidate52>
                candidates)
    {
        if (squad == null ||
            candidates == null ||
            candidates.Count == 0)
        {
            return
                PlacementGhostValidity52.Illegal;
        }

        if (!CandidateBasicsLegal52(
                candidates))
        {
            return
                PlacementGhostValidity52.Illegal;
        }

        if (activeMissionBattlefield == null ||
            factions == null ||
            missionAttackerIndex < 0 ||
            missionAttackerIndex >=
                factions.Count)
        {
            return
                PlacementGhostValidity52.Unknown;
        }

        // Infiltrators and similar setup rules can legally bypass the normal
        // deployment polygon. Leave those previews neutral rather than
        // incorrectly telling the player they are illegal.
        bool specialSetup =
            squad.SourceData != null &&
            squad.SourceData.abilities != null &&
            squad.SourceData.abilities.Any(
                ability =>
                    !string.IsNullOrWhiteSpace(
                        ability) &&
                    ability.IndexOf(
                        "infiltrat",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0
            );

        if (specialSetup)
        {
            return
                PlacementGhostValidity52.Unknown;
        }

        bool isAttacker =
            string.Equals(
                factions[
                    missionAttackerIndex
                ],
                squad.FactionId,
                StringComparison.OrdinalIgnoreCase
            );

        MissionDeploymentZone zone =
            activeMissionBattlefield
                .ZoneForRole(
                    isAttacker
                );

        if (zone == null)
        {
            return
                PlacementGhostValidity52.Unknown;
        }

        foreach (PlacementGhostCandidate52
            candidate in candidates)
        {
            if (candidate == null ||
                candidate.Model == null)
            {
                return
                    PlacementGhostValidity52.Illegal;
            }

            if (!zone.ContainsBase(
                    candidate.Destination,
                    candidate.Model
                        .BaseRadiusInches))
            {
                return
                    PlacementGhostValidity52.Illegal;
            }
        }

        return
            PlacementGhostValidity52.Legal;
    }

    private bool CandidateBasicsLegal52(
        List<PlacementGhostCandidate52>
            candidates)
    {
        if (!CandidateBoardLegal52(
                candidates))
        {
            return false;
        }

        foreach (PlacementGhostCandidate52
            candidate in candidates)
        {
            if (candidate == null ||
                candidate.Model == null)
            {
                return false;
            }

            if (!CanPlaceModel(
                    candidate.Model,
                    candidate.Destination))
            {
                return false;
            }
        }

        return true;
    }

    private bool CandidateBoardLegal52(
        List<PlacementGhostCandidate52>
            candidates)
    {
        if (candidates == null ||
            candidates.Count == 0)
        {
            return false;
        }

        float halfX =
            BoardWidth *
            0.5f;

        float halfZ =
            BoardDepth *
            0.5f;

        foreach (PlacementGhostCandidate52
            candidate in candidates)
        {
            if (candidate == null ||
                candidate.Model == null)
            {
                return false;
            }

            float radius =
                Mathf.Max(
                    0.05f,
                    candidate.Model
                        .BaseRadiusInches
                );

            Vector3 point =
                candidate.Destination;

            if (Mathf.Abs(point.x) +
                    radius >
                halfX +
                    0.001f ||
                Mathf.Abs(point.z) +
                    radius >
                halfZ +
                    0.001f)
            {
                return false;
            }
        }

        return true;
    }

    private Color PlacementGhostTint52(
        PlacementGhostValidity52 validity)
    {
        switch (validity)
        {
            case PlacementGhostValidity52.Legal:
                return
                    PlacementGhostLegalColor52;

            case PlacementGhostValidity52.Illegal:
                return
                    PlacementGhostIllegalColor52;

            default:
                return
                    PlacementGhostUnknownColor52;
        }
    }

    private void DrawPlacementGhostModel52(
        ModelToken model,
        Vector3 destination,
        Color tint,
        PlacementGhostValidity52 validity)
    {
        if (model == null)
            return;

        Renderer[] renderers =
            model.GetComponentsInChildren<
                Renderer
            >(true);

        Vector3 translation =
            destination -
            model.transform.position;

        Matrix4x4 shift =
            Matrix4x4.Translate(
                translation
            );

        foreach (Renderer renderer
            in renderers)
        {
            if (!PlacementGhostRendererEligible52(
                    model,
                    renderer))
            {
                continue;
            }

            MeshFilter filter =
                renderer.GetComponent<
                    MeshFilter
                >();

            Mesh mesh =
                filter != null
                ? filter.sharedMesh
                : null;

            if (mesh == null)
                continue;

            Matrix4x4 matrix =
                shift *
                renderer.transform
                    .localToWorldMatrix;

            Material[] sourceMaterials =
                renderer.sharedMaterials;

            int subMeshCount =
                Mathf.Max(
                    1,
                    mesh.subMeshCount
                );

            for (int sub = 0;
                 sub < subMeshCount;
                 sub++)
            {
                Material source =
                    sourceMaterials != null &&
                    sourceMaterials.Length > 0
                    ? sourceMaterials[
                        Mathf.Min(
                            sub,
                            sourceMaterials.Length -
                                1
                        )
                      ]
                    : null;

                Material ghost =
                    PlacementGhostMaterial52(
                        source,
                        tint,
                        validity
                    );

                if (ghost == null)
                    continue;

                Graphics.DrawMesh(
                    mesh,
                    matrix,
                    ghost,
                    0,
                    gameCamera,
                    sub
                );
            }
        }

        DrawPlacementGhostBaseRing52(
            model,
            destination,
            validity
        );
    }

    private bool PlacementGhostRendererEligible52(
        ModelToken model,
        Renderer renderer)
    {
        if (model == null ||
            renderer == null)
        {
            return false;
        }

        if (renderer.GetComponent<TextMesh>() !=
            null)
        {
            return false;
        }

        string objectName =
            renderer.gameObject.name ?? "";

        if (objectName.IndexOf(
                "Selection Ring",
                StringComparison.OrdinalIgnoreCase) >= 0 ||
            objectName.IndexOf(
                "Battle-shock",
                StringComparison.OrdinalIgnoreCase) >= 0 ||
            objectName.IndexOf(
                "Wound",
                StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        // A custom miniature keeps the original capsule as an invisible
        // gameplay proxy. Do not draw that capsule over the real ghost.
        if (model.HasCustomVisual &&
            renderer.gameObject ==
                model.gameObject)
        {
            return false;
        }

        return
            renderer.GetComponent<
                MeshFilter
            >() != null;
    }

    private Material PlacementGhostMaterial52(
        Material source,
        Color tint,
        PlacementGhostValidity52 validity)
    {
        int sourceId =
            source != null
            ? source.GetHashCode()
            : 0;

        string key =
            sourceId.ToString() +
            "|" +
            ((int)validity)
                .ToString();

        Material material;

        if (placementGhostMaterials52
            .TryGetValue(
                key,
                out material) &&
            material != null)
        {
            return material;
        }

        Shader shader =
            source != null
            ? source.shader
            : null;

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit"
                );
        }

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Standard"
                );
        }

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Sprites/Default"
                );
        }

        if (shader == null)
            return null;

        material =
            source != null
            ? new Material(source)
            : new Material(shader);

        material.name =
            "Warboard V52 Placement Ghost";

        ConfigurePlacementGhostMaterial52(
            material,
            tint
        );

        placementGhostMaterials52[
            key
        ] =
            material;

        return material;
    }

    private void ConfigurePlacementGhostMaterial52(
        Material material,
        Color tint)
    {
        if (material == null)
            return;

        Color sourceColor =
            Color.white;

        if (material.HasProperty(
                "_BaseColor"))
        {
            sourceColor =
                material.GetColor(
                    "_BaseColor"
                );
        }
        else if (material.HasProperty(
                     "_Color"))
        {
            sourceColor =
                material.GetColor(
                    "_Color"
                );
        }

        Color color =
            Color.Lerp(
                sourceColor,
                tint,
                0.68f
            );

        color.a =
            tint.a;

        if (material.HasProperty(
                "_BaseColor"))
        {
            material.SetColor(
                "_BaseColor",
                color
            );
        }

        if (material.HasProperty(
                "_Color"))
        {
            material.SetColor(
                "_Color",
                color
            );
        }

        // URP Lit transparency.
        if (material.HasProperty(
                "_Surface"))
        {
            material.SetFloat(
                "_Surface",
                1f
            );

            if (material.HasProperty(
                    "_Blend"))
            {
                material.SetFloat(
                    "_Blend",
                    0f
                );
            }

            if (material.HasProperty(
                    "_ZWrite"))
            {
                material.SetFloat(
                    "_ZWrite",
                    0f
                );
            }

            material.SetOverrideTag(
                "RenderType",
                "Transparent"
            );

            material.EnableKeyword(
                "_SURFACE_TYPE_TRANSPARENT"
            );

            material.renderQueue =
                (int)RenderQueue.Transparent;
        }

        // Built-in Standard transparency.
        if (material.HasProperty(
                "_Mode"))
        {
            material.SetFloat(
                "_Mode",
                3f
            );

            material.SetInt(
                "_SrcBlend",
                (int)BlendMode.SrcAlpha
            );

            material.SetInt(
                "_DstBlend",
                (int)BlendMode.OneMinusSrcAlpha
            );

            material.SetInt(
                "_ZWrite",
                0
            );

            material.DisableKeyword(
                "_ALPHATEST_ON"
            );

            material.EnableKeyword(
                "_ALPHABLEND_ON"
            );

            material.DisableKeyword(
                "_ALPHAPREMULTIPLY_ON"
            );

            material.renderQueue =
                (int)RenderQueue.Transparent;
        }

        if (material.HasProperty(
                "_EmissionColor"))
        {
            material.EnableKeyword(
                "_EMISSION"
            );

            material.SetColor(
                "_EmissionColor",
                new Color(
                    tint.r,
                    tint.g,
                    tint.b,
                    1f
                ) *
                0.55f
            );
        }
    }

    private void EnsurePlacementGhostBaseRing52()
    {
        if (placementGhostBaseRingMesh52 ==
            null)
        {
            placementGhostBaseRingMesh52 =
                BuildPlacementGhostRingMesh52();
        }

        if (placementGhostBaseRingLegal52 ==
            null)
        {
            placementGhostBaseRingLegal52 =
                CreatePlacementGhostRingMaterial52(
                    new Color(
                        0.10f,
                        1.00f,
                        0.48f,
                        0.92f
                    )
                );
        }

        if (placementGhostBaseRingIllegal52 ==
            null)
        {
            placementGhostBaseRingIllegal52 =
                CreatePlacementGhostRingMaterial52(
                    new Color(
                        1.00f,
                        0.12f,
                        0.10f,
                        0.94f
                    )
                );
        }

        if (placementGhostBaseRingUnknown52 ==
            null)
        {
            placementGhostBaseRingUnknown52 =
                CreatePlacementGhostRingMaterial52(
                    new Color(
                        0.12f,
                        0.78f,
                        1.00f,
                        0.92f
                    )
                );
        }
    }

    private Mesh BuildPlacementGhostRingMesh52()
    {
        const int segments = 48;
        const float outer = 0.50f;
        const float inner = 0.43f;

        Vector3[] vertices =
            new Vector3[
                segments * 2
            ];

        int[] triangles =
            new int[
                segments * 6
            ];

        for (int i = 0;
             i < segments;
             i++)
        {
            float angle =
                i /
                (float)segments *
                Mathf.PI *
                2f;

            float x =
                Mathf.Cos(angle);

            float z =
                Mathf.Sin(angle);

            vertices[
                i * 2
            ] =
                new Vector3(
                    x * outer,
                    0f,
                    z * outer
                );

            vertices[
                i * 2 + 1
            ] =
                new Vector3(
                    x * inner,
                    0f,
                    z * inner
                );

            int next =
                (i + 1) %
                segments;

            int t =
                i * 6;

            triangles[t + 0] =
                i * 2;

            triangles[t + 1] =
                next * 2;

            triangles[t + 2] =
                i * 2 + 1;

            triangles[t + 3] =
                i * 2 + 1;

            triangles[t + 4] =
                next * 2;

            triangles[t + 5] =
                next * 2 + 1;
        }

        Mesh mesh =
            new Mesh();

        mesh.name =
            "Warboard V52 Ghost Base Ring";

        mesh.vertices =
            vertices;

        mesh.triangles =
            triangles;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }

    private Material
        CreatePlacementGhostRingMaterial52(
            Color color)
    {
        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Unlit"
            );

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Sprites/Default"
                );
        }

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Standard"
                );
        }

        if (shader == null)
            return null;

        Material material =
            new Material(shader);

        material.name =
            "Warboard V52 Ghost Base Ring";

        if (material.HasProperty(
                "_BaseColor"))
        {
            material.SetColor(
                "_BaseColor",
                color
            );
        }

        if (material.HasProperty(
                "_Color"))
        {
            material.SetColor(
                "_Color",
                color
            );
        }

        ConfigurePlacementGhostMaterial52(
            material,
            color
        );

        return material;
    }

    private void DrawPlacementGhostBaseRing52(
        ModelToken model,
        Vector3 destination,
        PlacementGhostValidity52 validity)
    {
        if (model == null ||
            placementGhostBaseRingMesh52 ==
                null)
        {
            return;
        }

        Material material =
            validity ==
                PlacementGhostValidity52.Legal
            ? placementGhostBaseRingLegal52
            : validity ==
                PlacementGhostValidity52.Illegal
                ? placementGhostBaseRingIllegal52
                : placementGhostBaseRingUnknown52;

        if (material == null)
            return;

        float diameter =
            Mathf.Max(
                0.20f,
                model.BaseRadiusInches *
                    2f
            );

        Matrix4x4 matrix =
            Matrix4x4.TRS(
                new Vector3(
                    destination.x,
                    0.075f,
                    destination.z
                ),
                Quaternion.identity,
                new Vector3(
                    diameter,
                    1f,
                    diameter
                )
            );

        Graphics.DrawMesh(
            placementGhostBaseRingMesh52,
            matrix,
            material,
            0,
            gameCamera
        );
    }
}

