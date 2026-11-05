using Robust.Shared.GameStates;

namespace Content.Shared._Planet14;

/// <summary>Planet artwork drawn below every grid tile.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlanetBackdropComponent : Component
{
    [DataField, AutoNetworkedField] public string State = "planet";
}
