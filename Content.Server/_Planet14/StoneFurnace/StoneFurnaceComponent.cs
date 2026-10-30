using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Server._Planet14.StoneFurnace;

[RegisterComponent]
public sealed partial class StoneFurnaceComponent : Component
{
    [DataField] public float SmeltSeconds = 20f;
    [DataField] public int BatchSize = 15;
    [DataField] public int BatchCount;
    [DataField] public EntProtoId BatchOutput;
    [DataField] public int ItemsPerCoal = 5;
    [DataField] public int FuelCapacity = 50;
    [DataField] public int OreCapacity = 100;
    [DataField] public int Coal;
    [DataField] public int RemainingHeat;
    [DataField] public float Progress;
    [DataField] public Dictionary<ProtoId<StackPrototype>, int> Ores = new();
    [DataField] public Dictionary<ProtoId<StackPrototype>, EntProtoId> Recipes = new();
}
