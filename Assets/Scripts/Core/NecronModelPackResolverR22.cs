using System;
using System.Collections.Generic;
using UnityEngine;

// WARBOARD_NECRON_MODEL_RESOLVER_R2_2
// The Necron pack is installed under Resources but was never connected to
// ModelVisualRegistry/SquadController. This resolver bridges that gap.

[Serializable]
internal sealed class NecronPackIndexR22
{
    public int formatVersion;
    public string packId;
    public string displayName;
    public string[] sources;
    public NecronPackUnitR22[] units;
}

[Serializable]
internal sealed class NecronPackUnitR22
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

public static class NecronModelPackResolverR22
{
    private const string IndexResource =
        "Armies/Models/Necrons/ModelIndex";

    private static bool loadAttempted;
    private static NecronPackIndexR22 pack;

    private static readonly HashSet<string> unmatchedLogged =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static ModelVisualDefinition TryResolve(
        string factionId,
        string unitName,
        string roleName,
        int modelIndex)
    {
        if (!IsNecronFaction(factionId))
            return null;

        EnsureLoaded();

        if (pack == null ||
            pack.units == null ||
            pack.units.Length == 0)
        {
            return null;
        }

        string unit = Alias(unitName);
        string role = Alias(roleName);

        if (string.IsNullOrWhiteSpace(unit))
            return null;

        string unitSingular = SimpleSingular(unit);
        string roleSingular = SimpleSingular(role);
        bool genericRole = IsGenericRole(roleName);

        List<NecronPackUnitR22> best =
            new List<NecronPackUnitR22>();

        int bestScore = int.MinValue;

        foreach (NecronPackUnitR22 entry in pack.units)
        {
            if (entry == null ||
                entry.components == null ||
                entry.components.Length == 0)
            {
                continue;
            }

            string canonical = Alias(
                !string.IsNullOrWhiteSpace(entry.canonicalName)
                    ? entry.canonicalName
                    : entry.name);

            if (string.IsNullOrWhiteSpace(canonical))
                continue;

            string canonicalSingular = SimpleSingular(canonical);
            int score = 0;

            if (!genericRole && canonical == role)
                score = 2200;
            else if (canonical == unit)
                score = 2100;
            else if (!genericRole && canonicalSingular == roleSingular)
                score = 2050;
            else if (canonicalSingular == unitSingular)
                score = 2000;
            else if (!genericRole &&
                     !string.IsNullOrWhiteSpace(roleSingular) &&
                     (canonicalSingular.Contains(roleSingular) ||
                      roleSingular.Contains(canonicalSingular)))
                score = 1750;
            else if (!string.IsNullOrWhiteSpace(unitSingular) &&
                     (canonicalSingular.Contains(unitSingular) ||
                      unitSingular.Contains(canonicalSingular)))
                score = 1650;

            if (score <= 0)
                continue;

            // The user supplied the colourful/main group as the preferred
            // source and the other group as fallback. Honour that order.
            if (string.Equals(
                    entry.source,
                    "Main",
                    StringComparison.OrdinalIgnoreCase))
            {
                score += 300;
            }
            else if (string.Equals(
                         entry.source,
                         "Primary",
                         StringComparison.OrdinalIgnoreCase))
            {
                score += 250;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best.Clear();
                best.Add(entry);
            }
            else if (score == bestScore)
            {
                best.Add(entry);
            }
        }

        if (best.Count == 0)
        {
            string key = unitName + "|" + roleName;
            if (unmatchedLogged.Add(key))
            {
                Debug.LogWarning(
                    "Warboard Necron Model Pack R2.2: no model-pack match for unit '" +
                    unitName + "', role '" + roleName + "'. Gameplay capsule retained.");
            }

            return null;
        }

        NecronPackUnitR22 selected =
            best[Mathf.Abs(modelIndex) % best.Count];

        return BuildVisual(selected, unitName);
    }

    private static void EnsureLoaded()
    {
        if (loadAttempted)
            return;

        loadAttempted = true;

        TextAsset asset = Resources.Load<TextAsset>(IndexResource);

        if (asset == null)
        {
            Debug.LogWarning(
                "Warboard Necron Model Pack R2.2: index not found at Resources/" +
                IndexResource + ".json.");
            return;
        }

        try
        {
            pack = JsonUtility.FromJson<NecronPackIndexR22>(asset.text);

            int count =
                pack != null && pack.units != null
                    ? pack.units.Length
                    : 0;

            Debug.Log(
                "Warboard Necron Model Pack R2.2 loaded: " +
                count + " indexed objects.");
        }
        catch (Exception exception)
        {
            pack = null;
            Debug.LogError(
                "Warboard Necron Model Pack R2.2 could not read ModelIndex.json: " +
                exception.Message);
        }
    }

    private static ModelVisualDefinition BuildVisual(
        NecronPackUnitR22 selected,
        string requestedUnit)
    {
        if (selected == null ||
            selected.components == null ||
            selected.components.Length == 0)
        {
            return null;
        }

        // Necron indexes came straight from the TTS source saves, so component
        // positions include their original table/world placement. Re-anchor all
        // components to the first loadable visual component before handing them
        // to ModelToken. This keeps multipart models together but puts the model
        // itself back on the token rather than tens of units away from it.
        bool haveAnchor = false;
        Vector3 anchor = Vector3.zero;

        List<ModelVisualComponentDefinition> components =
            new List<ModelVisualComponentDefinition>();

        foreach (ModelPackComponentData component in selected.components)
        {
            if (component == null ||
                string.IsNullOrWhiteSpace(component.meshResource))
            {
                continue;
            }

            GameObject imported =
                Resources.Load<GameObject>(component.meshResource);

            if (imported == null)
            {
                Debug.LogWarning(
                    "Warboard Necron Model Pack R2.2: mesh resource could not be loaded: " +
                    component.meshResource);
                continue;
            }

            Vector3 rawPosition = V(component.position, Vector3.zero);

            if (!haveAnchor)
            {
                anchor = rawPosition;
                haveAnchor = true;
            }

            components.Add(
                new ModelVisualComponentDefinition(
                    component.meshResource,
                    component.diffuseResource ?? "",
                    component.normalResource ?? "",
                    rawPosition - anchor,
                    V(component.rotation, Vector3.zero),
                    V(component.scale, Vector3.one)));
        }

        if (components.Count == 0)
        {
            Debug.LogWarning(
                "Warboard Necron Model Pack R2.2 matched '" +
                requestedUnit + "' to '" + selected.name +
                "', but none of its OBJ resources were loadable. Gameplay capsule retained.");
            return null;
        }

        float diameterInches =
            selected.@base != null && selected.@base.diameterMm > 0.1f
                ? selected.@base.diameterMm / 25.4f
                : 32.0f / 25.4f;

        string baseResource =
            selected.@base != null
                ? (selected.@base.resource ?? "")
                : "";

        Debug.Log(
            "Warboard Necron Model Pack R2.2: '" +
            requestedUnit + "' -> '" +
            (!string.IsNullOrWhiteSpace(selected.canonicalName)
                ? selected.canonicalName
                : selected.name) +
            "' [" + selected.source + "].");

        return new ModelVisualDefinition(
            diameterInches,
            baseResource,
            components.ToArray());
    }

    private static bool IsNecronFaction(string value)
    {
        string n = N(value);
        return n == "necron" ||
               n == "necrons" ||
               n.Contains("necron");
    }

    private static bool IsGenericRole(string value)
    {
        string n = N(value);
        return string.IsNullOrWhiteSpace(n) ||
               n == "model" ||
               n == "trooper" ||
               n == "warrior" ||
               n == "necron";
    }

    private static string Alias(string value)
    {
        string n = N(value);

        string[] prefixes =
        {
            "necrons",
            "necron",
            "dynasty",
            "dynastic"
        };

        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (string prefix in prefixes)
            {
                if (n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    n.Length > prefix.Length)
                {
                    n = n.Substring(prefix.Length);
                    changed = true;
                    break;
                }
            }
        }

        return n
            .Replace("skorpekhdestroyers", "skorpekhdestroyer")
            .Replace("lokhustdestroyers", "lokhustdestroyer")
            .Replace("lokhustheavydestroyers", "lokhustheavydestroyer")
            .Replace("ophydiandestroyers", "ophydiandestroyer")
            .Replace("warriors", "warrior")
            .Replace("immortals", "immortal")
            .Replace("deathmarks", "deathmark")
            .Replace("lychguard", "lychguard")
            .Replace("flayedones", "flayedone")
            .Replace("tombblades", "tombblade")
            .Replace("scarabswarms", "scarabswarm")
            .Replace("canoptekwraiths", "canoptekwraith")
            .Replace("cryptothralls", "cryptothrall");
    }

    private static string SimpleSingular(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        if (value.EndsWith("ies", StringComparison.OrdinalIgnoreCase) &&
            value.Length > 3)
        {
            return value.Substring(0, value.Length - 3) + "y";
        }

        if (value.EndsWith("s", StringComparison.OrdinalIgnoreCase) &&
            !value.EndsWith("ss", StringComparison.OrdinalIgnoreCase) &&
            value.Length > 1)
        {
            return value.Substring(0, value.Length - 1);
        }

        return value;
    }

    private static string N(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        foreach (char character in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
                builder.Append(character);
        }

        return builder.ToString();
    }

    private static Vector3 V(float[] values, Vector3 fallback)
    {
        if (values == null || values.Length < 3)
            return fallback;

        return new Vector3(values[0], values[1], values[2]);
    }
}
