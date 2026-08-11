using UnityEngine;

// WARBOARD_R28_2_MODEL_RESOLVER_COMPATIBILITY_SHIM
//
// SquadController already calls ExtendedFactionModelPackResolverR25.TryResolve.
// R28.2 deliberately keeps that public API so locally modified gameplay code
// does not need brittle source-text surgery.
//
// All actual faction selection, matching, TTS cleanup and caching now live in
// UnifiedModelVisualResolverR28.
public static class ExtendedFactionModelPackResolverR25
{
    public static ModelVisualDefinition TryResolve(
        string factionId,
        string unitName,
        string roleName,
        int modelIndex)
    {
        return
            UnifiedModelVisualResolverR28.TryResolve(
                factionId,
                unitName,
                roleName,
                modelIndex
            );
    }
}
