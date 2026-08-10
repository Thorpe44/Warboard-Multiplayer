Warboard V52 Unity 6000.5 compile fix

Fixes:
GameController.V52PlacementGhost.cs CS0619
'Object.GetInstanceID()' is obsolete: 'Use GetEntityId instead.'

Warboard only used the value as an in-memory material-cache key, so the fix
uses the normal object hash code instead. No gameplay, placement, model, or
network behaviour changes.

Run FIX_WARBOARD_V52_UNITY6000_ENTITYID.bat from the Warboard project root.
