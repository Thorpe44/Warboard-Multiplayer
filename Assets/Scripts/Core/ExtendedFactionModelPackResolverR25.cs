using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// WARBOARD_EXTENDED_FACTION_MODEL_RESOLVER_R25
//
// Necrons, Orks and Tyranids now use one model-pack resolver that produces the
// same LOCAL ModelVisualDefinition contract already consumed successfully by
// ModelToken.AttachVisual for Aeldari/Custodes.
//
// Important TTS extraction rule:
// - root-only objects: subtract the first component's TTS table/world position
// - parent + child objects: discard the TTS parent/wrapper and keep child LOCAL
//   transforms exactly as extracted
//
// This is done BEFORE the miniature is spawned. There is no post-spawn
// renderer-bounds recenter hack.

[Serializable]
internal sealed class ExtendedFactionPackIndexR25
{
    public int formatVersion;
    public string packId;
    public string displayName;
    public string[] sources;
    public ExtendedFactionPackUnitR25[] units;
}

[Serializable]
internal sealed class ExtendedFactionPackUnitR25
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

internal sealed class ExtendedFactionPackCacheR25
{
    public bool Attempted;
    public ExtendedFactionPackIndexR25 Pack;
}

public static class ExtendedFactionModelPackResolverR25
{
    private sealed class PackSpec
    {
        public string Folder;
        public string FactionNeedle;

        public PackSpec(
            string folder,
            string factionNeedle)
        {
            Folder = folder;
            FactionNeedle = factionNeedle;
        }
    }

    private sealed class Match
    {
        public PackSpec Spec;
        public ExtendedFactionPackIndexR25 Pack;
        public ExtendedFactionPackUnitR25 Unit;
        public int Score;
    }

    private static readonly PackSpec[] Specs =
    {
        new PackSpec("Necrons", "necron"),
        new PackSpec("Orks", "ork"),
        new PackSpec("Tyranids", "tyranid")
    };

    private static readonly Dictionary<string, ExtendedFactionPackCacheR25>
        Caches =
            new Dictionary<string, ExtendedFactionPackCacheR25>(
                StringComparer.OrdinalIgnoreCase
            );

    private static readonly HashSet<string>
        UnmatchedLogged =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

    public static ModelVisualDefinition TryResolve(
        string factionId,
        string unitName,
        string roleName,
        int modelIndex)
    {
        string faction =
            N(factionId);

        bool explicitSupportedFaction =
            faction.Contains("necron") ||
            faction.Contains("ork") ||
            faction.Contains("tyranid");

        Match best =
            null;

        foreach (PackSpec spec
            in Specs)
        {
            bool explicitThisPack =
                faction.Contains(
                    spec.FactionNeedle
                );

            if (explicitSupportedFaction &&
                !explicitThisPack)
            {
                continue;
            }

            ExtendedFactionPackIndexR25 pack =
                EnsureLoaded(
                    spec
                );

            if (pack == null ||
                pack.units == null ||
                pack.units.Length == 0)
            {
                continue;
            }

            Match candidate =
                FindBestMatch(
                    spec,
                    pack,
                    unitName,
                    roleName,
                    modelIndex,
                    explicitThisPack
                );

            if (candidate == null)
                continue;

            // If YellowScribe has left FactionId as "Player 1/2", only accept
            // exact/singular model-name matches. This lets Boyz resolve as Orks
            // without allowing loose cross-faction substring guesses.
            if (!explicitSupportedFaction &&
                candidate.Score < 2000)
            {
                continue;
            }

            if (best == null ||
                candidate.Score >
                    best.Score)
            {
                best =
                    candidate;
            }
        }

        if (best == null)
        {
            string logKey =
                factionId +
                "|" +
                unitName +
                "|" +
                roleName;

            if (UnmatchedLogged.Add(
                    logKey))
            {
                Debug.LogWarning(
                    "Warboard extended model packs R25: no strong Necron/Ork/Tyranid match for '" +
                    unitName +
                    "' / role '" +
                    roleName +
                    "' / faction '" +
                    factionId +
                    "'. Existing Aeldari/Custodes/capsule fallback remains active."
                );
            }

            return null;
        }

        return
            BuildVisual(
                best.Spec,
                best.Unit,
                unitName
            );
    }

    private static Match FindBestMatch(
        PackSpec spec,
        ExtendedFactionPackIndexR25 pack,
        string unitName,
        string roleName,
        int modelIndex,
        bool explicitFaction)
    {
        string unit =
            Alias(
                unitName
            );

        string role =
            Alias(
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

        List<ExtendedFactionPackUnitR25>
            bestUnits =
                new List<
                    ExtendedFactionPackUnitR25
                >();

        int bestScore =
            int.MinValue;

        foreach (ExtendedFactionPackUnitR25 entry
            in pack.units)
        {
            if (entry == null ||
                entry.components == null ||
                entry.components.Length == 0)
            {
                continue;
            }

            string canonical =
                Alias(
                    !string.IsNullOrWhiteSpace(
                        entry.canonicalName)
                    ? entry.canonicalName
                    : entry.name
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

            int score = 0;

            if (!genericRole &&
                canonical == role)
            {
                score = 2200;
            }
            else if (canonical == unit)
            {
                score = 2100;
            }
            else if (!genericRole &&
                     canonicalSingular ==
                        roleSingular)
            {
                score = 2050;
            }
            else if (canonicalSingular ==
                     unitSingular)
            {
                score = 2000;
            }
            else if (!genericRole &&
                     !string.IsNullOrWhiteSpace(
                         roleSingular) &&
                     (canonicalSingular.Contains(
                          roleSingular) ||
                      roleSingular.Contains(
                          canonicalSingular)))
            {
                score = 1750;
            }
            else if (!string.IsNullOrWhiteSpace(
                         unitSingular) &&
                     (canonicalSingular.Contains(
                          unitSingular) ||
                      unitSingular.Contains(
                          canonicalSingular)))
            {
                score = 1650;
            }

            if (score <= 0)
                continue;

            if (explicitFaction)
                score += 5000;

            score +=
                SourcePreferenceBonus(
                    pack,
                    entry.source
                );

            if (score > bestScore)
            {
                bestScore = score;
                bestUnits.Clear();
                bestUnits.Add(entry);
            }
            else if (score == bestScore)
            {
                bestUnits.Add(entry);
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
                Score = bestScore
            };
    }

    private static ModelVisualDefinition BuildVisual(
        PackSpec spec,
        ExtendedFactionPackUnitR25 selected,
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
            Debug.LogWarning(
                "Warboard " +
                spec.Folder +
                " pack R25 matched '" +
                requestedUnit +
                "' to '" +
                selected.name +
                "', but no OBJ resource loaded."
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

        if (!hasChildComponents)
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

                // A child tens of inches away from every other child is TTS
                // extraction debris, not part of one Warhammer miniature.
                if (delta.magnitude >
                    8.0f)
                {
                    Debug.LogWarning(
                        "Warboard " +
                        spec.Folder +
                        " R25 skipped an outlier child component '" +
                        component.nickname +
                        "' from '" +
                        selected.name +
                        "' (local offset " +
                        delta.magnitude.ToString("F2") +
                        ")."
                    );

                    continue;
                }
            }

            Vector3 localPosition =
                rawPosition;

            if (!hasChildComponents &&
                haveRootAnchor)
            {
                // This is the exact principle that made the Necron pack work:
                // original TTS table position becomes local zero, while any
                // additional root pieces retain their relative offset.
                localPosition =
                    rawPosition -
                    rootAnchor;
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
            selected.@base.diameterMm > 0.1f
            ? selected.@base.diameterMm /
              25.4f
            : 1.0f;

        string baseResource =
            selected.@base != null
            ? selected.@base.resource ?? ""
            : "";

        Debug.Log(
            "Warboard " +
            spec.Folder +
            " R25: '" +
            requestedUnit +
            "' -> '" +
            selected.name +
            "' [" +
            selected.source +
            "] with " +
            visuals.Count +
            " local visual component(s)."
        );

        return
            new ModelVisualDefinition(
                diameterInches,
                baseResource,
                visuals.ToArray()
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

    private static ExtendedFactionPackIndexR25 EnsureLoaded(
        PackSpec spec)
    {
        ExtendedFactionPackCacheR25 cache;

        if (!Caches.TryGetValue(
                spec.Folder,
                out cache))
        {
            cache =
                new ExtendedFactionPackCacheR25();

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
                "Warboard " +
                spec.Folder +
                " R25 index not found at Resources/" +
                resource +
                ".json."
            );

            return null;
        }

        try
        {
            cache.Pack =
                JsonUtility.FromJson<
                    ExtendedFactionPackIndexR25
                >(asset.text);

            Debug.Log(
                "Warboard " +
                spec.Folder +
                " R25 loaded: " +
                (cache.Pack != null &&
                 cache.Pack.units != null
                    ? cache.Pack.units.Length
                    : 0) +
                " indexed objects."
            );
        }
        catch (Exception exception)
        {
            cache.Pack = null;

            Debug.LogError(
                "Warboard " +
                spec.Folder +
                " R25 could not read ModelIndex.json: " +
                exception.Message
            );
        }

        return cache.Pack;
    }

    private static int SourcePreferenceBonus(
        ExtendedFactionPackIndexR25 pack,
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
                // Preserves the user's intended order:
                // Necrons Main > Backup
                // Tyranids Group1Colour > Group2Fallback
                // Orks New1 > New2 > Old1 > Old2
                return
                    (pack.sources.Length -
                     i) *
                    100;
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
            role == "ork" ||
            role == "necron" ||
            role == "tyranid";
    }

    private static string Alias(
        string value)
    {
        string n =
            N(
                value
            );

        bool changed =
            true;

        string[] prefixes =
        {
            "necrons",
            "necron",
            "orks",
            "ork",
            "tyranids",
            "tyranid",
            "ynnari",
            "aeldari"
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

        return
            SimpleSingular(
                n
            );
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
