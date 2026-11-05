using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Planet14;

/// <summary>Maps planetary surface coordinates to their fixed position in sector space.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Planet14OrbitComponent : Component
{
    [DataField, AutoNetworkedField] public NetEntity SpaceMap;
    [DataField, AutoNetworkedField] public Vector2 Position;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class Planet14OrbitMarkerComponent : Component;
