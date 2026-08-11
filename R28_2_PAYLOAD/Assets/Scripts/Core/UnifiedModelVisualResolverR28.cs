using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// WARBOARD_R28_UNIFIED_MODEL_VISUAL_RESOLVER
//
// One resolver owns faction-pack selection, model matching, TTS transform
// cleanup and visual construction for every currently installed model pack.
//
// Important behaviour:
// - An explicit faction only searches its own pack.
// - Generic "Player 1/2" faction ids may search all packs, but only accept
//   exact / singular-exact matches.
// - A missing model is a normal capsule fallback and is intentionally silent.
// - Broken matched assets still warn, because that indicates an actual pack
//   problem rather than a harmless missing miniature.
// - SquadController resolves each model once and reuses that result for both
//   formation spacing and visual attachment.

[Serializable]
internal sealed class UnifiedModelPackIndexR28
{
    public int formatVersion;
    public string packId;
    public string displayName;
    public string[] sources;
    public UnifiedModelPackUnitR28[] units;
}

[Serializable]
internal sealed class UnifiedModelPackUnitR28
{
    public string source;
    public string name;
    public string canonicalName;
    public string guid;
    public string description;
    public string gmnotes;
    public string[] tags;
    public ModelPackBaseData @base;
    public ModelPackComponentData[] components;
}

internal sealed class UnifiedModelPackCacheR28
{
    public bool Attempted;
    public UnifiedModelPackIndexR28 Pack;
}

public static class UnifiedModelVisualResolverR28
{
    private enum TransformMode
    {
        KeepAuthoredLocal,
        RootRelative
    }

    private sealed class PackSpec
    {
        public readonly string Folder;
        public readonly TransformMode Transform;
        public readonly string[] FactionNeedles;

        public PackSpec(
            string folder,
            TransformMode transform,
            params string[] factionNeedles)
        {
            Folder = folder;
            Transform = transform;
            FactionNeedles =
                factionNeedles ??
                new string[0];
        }
    }

    private sealed class Match
    {
        public PackSpec Spec;
        public UnifiedModelPackIndexR28 Pack;
        public UnifiedModelPackUnitR28 Unit;
        public int BaseScore;
        public int TotalScore;
    }

    private static readonly PackSpec[] Specs =
    {
        new PackSpec(
            "Aeldari",
            TransformMode.KeepAuthoredLocal,
            "aeldari",
            "asuryani",
            "ynnari",
            "eldar",
            "harlequin",
            "drukhari"
        ),
        new PackSpec(
            "Custodes",
            TransformMode.KeepAuthoredLocal,
            "adeptuscustodes",
            "custodes",
            "talonsoftheemperor",
            "talons"
        ),
        new PackSpec(
            "Necrons",
            TransformMode.RootRelative,
            "necron",
            "necrons"
        ),
        new PackSpec(
            "Orks",
            TransformMode.RootRelative,
            "ork",
            "orks"
        ),
        new PackSpec(
            "Tyranids",
            TransformMode.RootRelative,
            "tyranid",
            "tyranids"
        )
    };

    private static readonly Dictionary<string, UnifiedModelPackCacheR28>
        Caches =
            new Dictionary<string, UnifiedModelPackCacheR28>(
                StringComparer.OrdinalIgnoreCase
            );

    private static readonly HashSet<string>
        BrokenAssetWarnings =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

    private static readonly Dictionary<string, ModelVisualDefinition>
        ResolutionCache =
            new Dictionary<string, ModelVisualDefinition>(
                StringComparer.OrdinalIgnoreCase
            );

    private static readonly HashSet<string>
        ResolutionMisses =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

    public static ModelVisualDefinition TryResolve(
        string factionId,
        string unitName,
        string roleName,
        int modelIndex)
    {
        string cacheKey =
            (factionId ?? "") + "|" +
            (unitName ?? "") + "|" +
            (roleName ?? "") + "|" +
            modelIndex;

        ModelVisualDefinition cached;

        if (ResolutionCache.TryGetValue(
                cacheKey,
                out cached))
        {
            return cached;
        }

        if (ResolutionMisses.Contains(
                cacheKey))
        {
            return null;
        }

        ModelVisualDefinition resolved =
            ResolveUncached(
                factionId,
                unitName,
                roleName,
                modelIndex
            );

        if (resolved != null)
        {
            ResolutionCache[
                cacheKey
            ] =
                resolved;
        }
        else
        {
            ResolutionMisses.Add(
                cacheKey
            );
        }

        return resolved;
    }

    private static ModelVisualDefinition ResolveUncached(
        string factionId,
        string unitName,
        string roleName,
        int modelIndex)
    {
        PackSpec explicitSpec =
            FindExplicitFactionSpec(
                factionId
            );

        if (explicitSpec != null)
        {
            Match match =
                FindBestMatch(
                    explicitSpec,
                    unitName,
                    roleName,
                    modelIndex
                );

            return
                match != null
                ? BuildVisual(
                    match.Spec,
                    match.Unit,
                    unitName
                  )
                : null;
        }

        // A real unsupported faction (for example Space Marines) should not
        // probe unrelated Xenos/Custodes packs. Only generic player ids use the
        // conservative cross-pack inference path.
        if (!IsGenericFactionId(
                factionId))
        {
            return null;
        }

        Match best =
            null;

        foreach (PackSpec spec
            in Specs)
        {
            Match candidate =
                FindBestMatch(
                    spec,
                    unitName,
                    roleName,
                    modelIndex
                );

            if (candidate == null)
                continue;

            // Cross-faction inference is intentionally strict. Prefix/family/
            // substring matches are useful once the faction is known, but are
            // not safe enough to infer the faction by themselves.
            if (candidate.BaseScore < 4600)
                continue;

            if (best == null ||
                candidate.TotalScore >
                    best.TotalScore)
            {
                best =
                    candidate;
            }
        }

        return
            best != null
            ? BuildVisual(
                best.Spec,
                best.Unit,
                unitName
              )
            : null;
    }

    private static PackSpec FindExplicitFactionSpec(
        string factionId)
    {
        string faction =
            N(
                factionId
            );

        if (string.IsNullOrWhiteSpace(
                faction))
        {
            return null;
        }

        foreach (PackSpec spec
            in Specs)
        {
            foreach (string needle
                in spec.FactionNeedles)
            {
                if (!string.IsNullOrWhiteSpace(
                        needle) &&
                    faction.Contains(
                        N(
                            needle
                        )))
                {
                    return spec;
                }
            }
        }

        return null;
    }

    private static bool IsGenericFactionId(
        string factionId)
    {
        string faction =
            N(
                factionId
            );

        if (string.IsNullOrWhiteSpace(
                faction))
        {
            return true;
        }

        return
            faction == "player" ||
            faction == "player1" ||
            faction == "player2" ||
            faction == "p1" ||
            faction == "p2" ||
            faction == "army" ||
            faction == "unknown" ||
            faction.StartsWith(
                "player",
                StringComparison.OrdinalIgnoreCase
            );
    }

    private static Match FindBestMatch(
        PackSpec spec,
        string unitName,
        string roleName,
        int modelIndex)
    {
        UnifiedModelPackIndexR28 pack =
            EnsureLoaded(
                spec
            );

        if (pack == null ||
            pack.units == null ||
            pack.units.Length == 0)
        {
            return null;
        }

        string unit =
            Alias(
                spec,
                unitName
            );

        string role =
            Alias(
                spec,
                roleName
            );

        string unitSingular =
            SimpleSingular(
                unit
            );

        string roleSingular =
            SimpleSingular(
                role
            );

        bool genericRole =
            IsGenericRole(
                role
            );

        string requestedFamily =
            FamilyKey(
                unit
            );

        List<UnifiedModelPackUnitR28>
            bestUnits =
                new List<
                    UnifiedModelPackUnitR28
                >();

        int bestBaseScore =
            int.MinValue;

        int bestTotalScore =
            int.MinValue;

        foreach (UnifiedModelPackUnitR28 entry
            in pack.units)
        {
            if (entry == null ||
                entry.components == null ||
                entry.components.Length == 0)
            {
                continue;
            }

            string rawEntry =
                !string.IsNullOrWhiteSpace(
                    entry.canonicalName)
                ? entry.canonicalName
                : entry.name;

            string canonical =
                Alias(
                    spec,
                    rawEntry
                );

            if (string.IsNullOrWhiteSpace(
                    canonical))
            {
                continue;
            }

            string canonicalSingular =
                SimpleSingular(
                    canonical
                );

            string entryFamily =
                FamilyKey(
                    canonical
                );

            bool conflictingKnownFamily =
                !string.IsNullOrWhiteSpace(
                    requestedFamily) &&
                !string.IsNullOrWhiteSpace(
                    entryFamily) &&
                requestedFamily !=
                    entryFamily;

            int baseScore =
                ScoreMatch(
                    unit,
                    unitSingular,
                    role,
                    roleSingular,
                    genericRole,
                    canonical,
                    canonicalSingular,
                    conflictingKnownFamily
                );

            if (baseScore <= 0)
                continue;

            string entryNormalized =
                N(
                    entry.name
                );

            // Specialist variants should only win when New Recruit actually
            // requested that specialist role.
            if (entryNormalized.Contains(
                    "exarch") &&
                !N(roleName).Contains(
                    "exarch") &&
                !N(unitName).Contains(
                    "exarch"))
            {
                baseScore -=
                    500;
            }

            if ((entryNormalized.Contains(
                     "vexil") ||
                 entryNormalized.Contains(
                     "vexilla")) &&
                !N(roleName).Contains(
                    "vexil") &&
                !N(unitName).Contains(
                    "vexil"))
            {
                baseScore -=
                    500;
            }

            if (baseScore <= 0)
                continue;

            int totalScore =
                baseScore *
                1000;

            totalScore +=
                SourcePreferenceBonus(
                    pack,
                    entry.source
                );

            if (baseScore >
                    bestBaseScore ||
                (baseScore ==
                    bestBaseScore &&
                 totalScore >
                    bestTotalScore))
            {
                bestBaseScore =
                    baseScore;

                bestTotalScore =
                    totalScore;

                bestUnits.Clear();
                bestUnits.Add(
                    entry
                );
            }
            else if (baseScore ==
                         bestBaseScore &&
                     totalScore ==
                         bestTotalScore)
            {
                bestUnits.Add(
                    entry
                );
            }
        }

        if (bestUnits.Count == 0)
            return null;

        int safeIndex =
            modelIndex == int.MinValue
            ? 0
            : Mathf.Abs(
                modelIndex
              );

        return
            new Match
            {
                Spec = spec,
                Pack = pack,
                Unit =
                    bestUnits[
                        safeIndex %
                        bestUnits.Count
                    ],
                BaseScore =
                    bestBaseScore,
                TotalScore =
                    bestTotalScore
            };
    }

    private static int ScoreMatch(
        string unit,
        string unitSingular,
        string role,
        string roleSingular,
        bool genericRole,
        string canonical,
        string canonicalSingular,
        bool conflictingKnownFamily)
    {
        if (!genericRole &&
            canonical == role)
        {
            return 5000;
        }

        if (canonical == unit)
        {
            return 4900;
        }

        if (!genericRole &&
            canonicalSingular ==
                roleSingular)
        {
            return 4700;
        }

        if (canonicalSingular ==
                unitSingular)
        {
            return 4600;
        }

        if (conflictingKnownFamily)
            return 0;

        // Specialist roles such as Exarchs are frequently their own TTS object
        // while the roster unit is the parent squad.
        if (!genericRole &&
            role.Contains(
                "exarch") &&
            canonical.Contains(
                "exarch"))
        {
            string roleWithoutExarch =
                role.Replace(
                    "exarch",
                    ""
                );

            string canonicalWithoutExarch =
                canonical.Replace(
                    "exarch",
                    ""
                );

            if (string.IsNullOrWhiteSpace(
                    roleWithoutExarch) ||
                canonicalWithoutExarch.Contains(
                    roleWithoutExarch) ||
                roleWithoutExarch.Contains(
                    canonicalWithoutExarch))
            {
                return 4300;
            }
        }

        if (!genericRole &&
            !string.IsNullOrWhiteSpace(
                roleSingular) &&
            (canonicalSingular.StartsWith(
                 roleSingular,
                 StringComparison.OrdinalIgnoreCase) ||
             roleSingular.StartsWith(
                 canonicalSingular,
                 StringComparison.OrdinalIgnoreCase)))
        {
            return 3800;
        }

        if (!string.IsNullOrWhiteSpace(
                unitSingular) &&
            (canonicalSingular.StartsWith(
                 unitSingular,
                 StringComparison.OrdinalIgnoreCase) ||
             unitSingular.StartsWith(
                 canonicalSingular,
                 StringComparison.OrdinalIgnoreCase)))
        {
            return 3700;
        }

        string unitFamily =
            FamilyKey(
                unit
            );

        string canonicalFamily =
            FamilyKey(
                canonical
            );

        if (!string.IsNullOrWhiteSpace(
                unitFamily) &&
            unitFamily ==
                canonicalFamily)
        {
            return 3500;
        }

        if (!genericRole &&
            !string.IsNullOrWhiteSpace(
                roleSingular) &&
            (canonicalSingular.Contains(
                 roleSingular) ||
             roleSingular.Contains(
                 canonicalSingular)))
        {
            return 3200;
        }

        if (!string.IsNullOrWhiteSpace(
                unitSingular) &&
            (canonicalSingular.Contains(
                 unitSingular) ||
             unitSingular.Contains(
                 canonicalSingular)))
        {
            return 3000;
        }

        return 0;
    }

    private static ModelVisualDefinition BuildVisual(
        PackSpec spec,
        UnifiedModelPackUnitR28 selected,
        string requestedUnit)
    {
        if (selected == null ||
            selected.components == null ||
            selected.components.Length == 0)
        {
            return null;
        }

        List<ModelPackComponentData>
            loadable =
                new List<
                    ModelPackComponentData
                >();

        foreach (ModelPackComponentData component
            in selected.components)
        {
            if (component == null ||
                string.IsNullOrWhiteSpace(
                    component.meshResource))
            {
                continue;
            }

            GameObject imported =
                Resources.Load<GameObject>(
                    component.meshResource
                );

            if (imported != null)
            {
                loadable.Add(
                    component
                );
            }
        }

        if (loadable.Count == 0)
        {
            WarnBrokenAssetOnce(
                spec,
                selected,
                requestedUnit,
                "matched the unit, but none of its OBJ resources could be loaded"
            );

            return null;
        }

        bool hasChildComponents =
            false;

        Vector3 firstChildPosition =
            Vector3.zero;

        bool haveFirstChildPosition =
            false;

        foreach (ModelPackComponentData component
            in loadable)
        {
            if (!string.IsNullOrWhiteSpace(
                    component.childPath))
            {
                hasChildComponents =
                    true;

                if (!haveFirstChildPosition)
                {
                    firstChildPosition =
                        V(
                            component.position,
                            Vector3.zero
                        );

                    haveFirstChildPosition =
                        true;
                }
            }
        }

        Vector3 rootAnchor =
            Vector3.zero;

        bool haveRootAnchor =
            false;

        if (!hasChildComponents &&
            loadable.Count > 0)
        {
            rootAnchor =
                V(
                    loadable[0].position,
                    Vector3.zero
                );

            haveRootAnchor =
                true;
        }

        List<ModelVisualComponentDefinition>
            visuals =
                new List<
                    ModelVisualComponentDefinition
                >();

        foreach (ModelPackComponentData component
            in loadable)
        {
            bool rootComponent =
                string.IsNullOrWhiteSpace(
                    component.childPath
                );

            Vector3 rawPosition =
                V(
                    component.position,
                    Vector3.zero
                );

            if (hasChildComponents &&
                rootComponent &&
                LooksLikeTtsParentWrapper(
                    component,
                    rawPosition))
            {
                continue;
            }

            if (hasChildComponents &&
                !rootComponent &&
                haveFirstChildPosition)
            {
                Vector2 delta =
                    new Vector2(
                        rawPosition.x -
                            firstChildPosition.x,
                        rawPosition.z -
                            firstChildPosition.z
                    );

                if (delta.magnitude >
                    8.0f)
                {
                    WarnBrokenAssetOnce(
                        spec,
                        selected,
                        requestedUnit,
                        "contained a detached child component '" +
                        component.nickname +
                        "' at local offset " +
                        delta.magnitude.ToString("F2")
                    );

                    continue;
                }
            }

            Vector3 localPosition =
                rawPosition;

            if (!hasChildComponents &&
                haveRootAnchor)
            {
                if (spec.Transform ==
                    TransformMode.RootRelative)
                {
                    // Necron/Ork/Tyranid extraction records tabletop/world
                    // coordinates on root-only objects. Convert them to LOCAL.
                    localPosition =
                        rawPosition -
                        rootAnchor;
                }
                else
                {
                    // Aeldari/Custodes are generally already local. If an
                    // isolated root still carries a large tabletop X/Z offset,
                    // remove only that horizontal offset and preserve authored Y.
                    Vector2 rootHorizontal =
                        new Vector2(
                            rootAnchor.x,
                            rootAnchor.z
                        );

                    if (rootHorizontal.magnitude >
                        4.0f)
                    {
                        localPosition =
                            new Vector3(
                                rawPosition.x -
                                    rootAnchor.x,
                                rawPosition.y,
                                rawPosition.z -
                                    rootAnchor.z
                            );
                    }
                }
            }

            visuals.Add(
                new ModelVisualComponentDefinition(
                    component.meshResource,
                    component.diffuseResource ?? "",
                    component.normalResource ?? "",
                    localPosition,
                    V(
                        component.rotation,
                        Vector3.zero
                    ),
                    V(
                        component.scale,
                        Vector3.one
                    )
                )
            );
        }

        if (visuals.Count == 0)
            return null;

        float diameterInches =
            selected.@base != null &&
            selected.@base.diameterMm >
                0.1f
            ? selected.@base.diameterMm /
              25.4f
            : 1.0f;

        string baseResource =
            selected.@base != null
            ? selected.@base.resource ?? ""
            : "";

        return
            new ModelVisualDefinition(
                diameterInches,
                baseResource,
                visuals.ToArray()
            );
    }

    private static void WarnBrokenAssetOnce(
        PackSpec spec,
        UnifiedModelPackUnitR28 selected,
        string requestedUnit,
        string problem)
    {
        string key =
            spec.Folder +
            "|" +
            (selected != null
                ? selected.guid
                : "") +
            "|" +
            problem;

        if (!BrokenAssetWarnings.Add(
                key))
        {
            return;
        }

        Debug.LogWarning(
            "Warboard R28 model pack " +
            spec.Folder +
            ": '" +
            requestedUnit +
            "' -> '" +
            (selected != null
                ? selected.name
                : "<null>") +
            "' " +
            problem +
            ". Capsule fallback remains available."
        );
    }

    private static bool LooksLikeTtsParentWrapper(
        ModelPackComponentData component,
        Vector3 rawPosition)
    {
        if (component == null)
            return true;

        if (string.IsNullOrWhiteSpace(
                component.diffuseResource))
        {
            return true;
        }

        Vector2 horizontal =
            new Vector2(
                rawPosition.x,
                rawPosition.z
            );

        return
            horizontal.magnitude >
            4.0f;
    }

    private static UnifiedModelPackIndexR28 EnsureLoaded(
        PackSpec spec)
    {
        UnifiedModelPackCacheR28 cache;

        if (!Caches.TryGetValue(
                spec.Folder,
                out cache))
        {
            cache =
                new UnifiedModelPackCacheR28();

            Caches[
                spec.Folder
            ] =
                cache;
        }

        if (cache.Attempted)
            return cache.Pack;

        cache.Attempted =
            true;

        string resource =
            "Armies/Models/" +
            spec.Folder +
            "/ModelIndex";

        TextAsset asset =
            Resources.Load<TextAsset>(
                resource
            );

        if (asset == null)
        {
            Debug.LogWarning(
                "Warboard R28 model pack index not found at Resources/" +
                resource +
                ".json."
            );

            return null;
        }

        try
        {
            cache.Pack =
                JsonUtility.FromJson<
                    UnifiedModelPackIndexR28
                >(asset.text);

            Debug.Log(
                "Warboard R28 model pack loaded: " +
                spec.Folder +
                " (" +
                (cache.Pack != null &&
                 cache.Pack.units != null
                    ? cache.Pack.units.Length
                    : 0) +
                " indexed objects)."
            );
        }
        catch (Exception exception)
        {
            cache.Pack =
                null;

            Debug.LogError(
                "Warboard R28 could not read " +
                spec.Folder +
                " ModelIndex.json: " +
                exception.Message
            );
        }

        return cache.Pack;
    }

    private static int SourcePreferenceBonus(
        UnifiedModelPackIndexR28 pack,
        string source)
    {
        if (pack == null ||
            pack.sources == null ||
            pack.sources.Length == 0)
        {
            return 0;
        }

        for (int i = 0;
             i < pack.sources.Length;
             i++)
        {
            if (string.Equals(
                    pack.sources[i],
                    source,
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    (pack.sources.Length -
                     i) *
                    10;
            }
        }

        return 0;
    }

    private static bool IsGenericRole(
        string role)
    {
        return
            string.IsNullOrWhiteSpace(role) ||
            role == "model" ||
            role == "trooper" ||
            role == "warrior" ||
            role == "wraith" ||
            role == "ork" ||
            role == "necron" ||
            role == "tyranid" ||
            role == "custodian";
    }

    private static string Alias(
        PackSpec spec,
        string value)
    {
        string n =
            N(
                value
            );

        n =
            n.Replace(
                "armour",
                "armor"
            )
            .Replace(
                "ventari",
                "venatari"
            )
            .Replace(
                "saggitarum",
                "sagittarum"
            )
            .Replace(
                "aquilon",
                "aquillon"
            );

        bool changed =
            true;

        string[] prefixes =
        {
            "adeptuscustodes",
            "talonsoftheemperor",
            "custodes",
            "asuryani",
            "aeldari",
            "ynnari",
            "drukhari",
            "harlequins",
            "harlequin",
            "eldar",
            "necrons",
            "necron",
            "tyranids",
            "tyranid",
            "orks",
            "ork"
        };

        while (changed)
        {
            changed =
                false;

            foreach (string prefix
                in prefixes)
            {
                if (n.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase) &&
                    n.Length >
                        prefix.Length)
                {
                    n =
                        n.Substring(
                            prefix.Length
                        );

                    changed =
                        true;

                    break;
                }
            }
        }

        if (spec != null &&
            spec.Folder ==
                "Aeldari")
        {
            if (n == "wraiths")
                n = "wraithguard";
        }

        if (spec != null &&
            spec.Folder ==
                "Custodes")
        {
            n =
                n.Replace(
                    "captaingeneral",
                    ""
                );

            if (n.Contains(
                    "shieldcaptain") &&
                n.Contains(
                    "allarus"))
            {
                n =
                    "shieldcaptainallarus";
            }
            else if (n.Contains(
                         "shieldcaptain") &&
                     (n.Contains(
                          "dawneagle") ||
                      n.Contains(
                          "jetbike")))
            {
                n =
                    "shieldcaptainjetbike";
            }
            else if (n.Contains(
                         "trajann"))
            {
                n =
                    "trajannvaloris";
            }
        }

        if (spec != null &&
            spec.Folder ==
                "Orks")
        {
            if (n == "gretchin" ||
                n == "grot" ||
                n == "grots")
            {
                n =
                    "grotz";
            }
        }

        return
            SimpleSingular(
                n
            );
    }

    private static string FamilyKey(
        string normalizedName)
    {
        string value =
            SimpleSingular(
                normalizedName
            );

        // Longest / most specific keys first so nearby datasheets do not bleed
        // into one another during fuzzy matching.
        string[] families =
        {
            "shieldcaptainallarus",
            "shieldcaptainjetbike",
            "custodianguard",
            "custodianwarden",
            "wraithknight",
            "wraithlord",
            "wraithblade",
            "wraithguard",
            "guardiandefender",
            "stormguardian",
            "strikingscorpion",
            "howlingbanshee",
            "swoopinghawk",
            "firedragon",
            "darkreaper",
            "warpspider",
            "windrider",
            "shiningpear",
            "shroudrunner",
            "kabalite",
            "direavenger",
            "ranger",
            "warlock",
            "farseer",
            "spiritseer",
            "allarus",
            "aquillon",
            "vertuspraetor",
            "venatari",
            "sagittarum",
            "bladechampion",
            "shieldcaptain",
            "meganob",
            "beastsnaggaboy",
            "breakaboy",
            "tankbusta",
            "mekgun",
            "warbiker",
            "kommando",
            "loota",
            "burnaboy",
            "grotz",
            "gretchin",
            "nob",
            "boyz",
            "boy",
            "barbgaunt",
            "termagant",
            "hormagaunt",
            "genestealer",
            "warrior",
            "immortal",
            "lychguard",
            "deathmark",
            "flayedone",
            "destroyer"
        };

        foreach (string family
            in families)
        {
            if (value.Contains(
                    family))
            {
                return family;
            }
        }

        return "";
    }

    private static string SimpleSingular(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return "";
        }

        if (value.EndsWith(
                "ies",
                StringComparison.OrdinalIgnoreCase) &&
            value.Length > 3)
        {
            return
                value.Substring(
                    0,
                    value.Length - 3
                ) +
                "y";
        }

        if (value.EndsWith(
                "s",
                StringComparison.OrdinalIgnoreCase) &&
            !value.EndsWith(
                "ss",
                StringComparison.OrdinalIgnoreCase) &&
            value.Length > 1)
        {
            return
                value.Substring(
                    0,
                    value.Length - 1
                );
        }

        return value;
    }

    private static string N(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return "";
        }

        StringBuilder builder =
            new StringBuilder();

        foreach (char character
            in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(
                    character))
            {
                builder.Append(
                    character
                );
            }
        }

        return
            builder.ToString();
    }

    private static Vector3 V(
        float[] values,
        Vector3 fallback)
    {
        if (values == null ||
            values.Length < 3)
        {
            return fallback;
        }

        return
            new Vector3(
                values[0],
                values[1],
                values[2]
            );
    }
}
