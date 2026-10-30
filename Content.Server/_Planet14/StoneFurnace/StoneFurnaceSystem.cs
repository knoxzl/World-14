using System.Linq;
using Content.Shared._Planet14;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Server._Planet14.StoneFurnace;

public sealed partial class StoneFurnaceSystem : EntitySystem
{
    [Dependency] private SharedStackSystem _stacks = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    private static readonly ProtoId<StackPrototype> CoalStack = "Coal";

    public override void Initialize()
    {
        SubscribeLocalEvent<StoneFurnaceComponent, AfterInteractUsingEvent>(OnInsert);
        SubscribeLocalEvent<StoneFurnaceComponent, ExaminedEvent>(OnExamine);
    }

    private void OnInsert(Entity<StoneFurnaceComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach || !TryComp<StackComponent>(args.Used, out var stack))
            return;

        if (!TryInsert(ent, (args.Used, stack), out var amount))
        {
            _popup.PopupEntity(Loc.GetString("planet14-furnace-rejected"), ent, args.User);
            return;
        }

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("planet14-furnace-loaded", ("amount", amount)), ent, args.User);
    }

    /// <summary>Consumes only accepted stack units; excess remains in the player's stack.</summary>
    public bool TryInsert(Entity<StoneFurnaceComponent> ent, Entity<StackComponent> item, out int amount)
    {
        var comp = ent.Comp;
        amount = 0;
        var fuel = item.Comp.StackTypeId == CoalStack;
        if (!fuel && !comp.Recipes.ContainsKey(item.Comp.StackTypeId))
            return false;

        var space = fuel ? comp.FuelCapacity - comp.Coal : comp.OreCapacity - comp.Ores.Values.Sum();
        amount = Math.Min(item.Comp.Count, Math.Max(0, space));
        if (amount == 0 || !_stacks.TryUse((item.Owner, item.Comp), amount))
            return false;

        if (fuel)
            comp.Coal += amount;
        else
            comp.Ores[item.Comp.StackTypeId] = comp.Ores.GetValueOrDefault(item.Comp.StackTypeId) + amount;
        return true;
    }

    private void OnExamine(Entity<StoneFurnaceComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;
        args.PushMarkup(Loc.GetString("planet14-furnace-status", ("coal", ent.Comp.Coal),
            ("ore", ent.Comp.Ores.Values.Sum()), ("heat", ent.Comp.RemainingHeat)));
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<StoneFurnaceComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var comp, out var meta))
        {
            if (meta.EntityPaused)
                continue;

            var ore = comp.Ores.FirstOrDefault(pair => pair.Value > 0 && comp.Recipes.ContainsKey(pair.Key));
            var burning = ore.Value > 0 && (comp.RemainingHeat > 0 || comp.Coal > 0);
            _appearance.SetData(uid, StoneFurnaceVisuals.Burning, burning);
            if (!burning)
                continue;

            if (comp.RemainingHeat <= 0)
            {
                comp.Coal--;
                comp.RemainingHeat = Math.Max(1, comp.ItemsPerCoal);
            }

            comp.Progress += frameTime;
            if (comp.Progress < Math.Max(1f, comp.SmeltSeconds))
                continue;

            // No catch-up burst after a slow frame: at most one item completes per update.
            comp.Progress = 0;
            comp.RemainingHeat--;
            if (ore.Value == 1)
                comp.Ores.Remove(ore.Key);
            else
                comp.Ores[ore.Key]--;
            SpawnAtPosition(comp.Recipes[ore.Key], Transform(uid).Coordinates);
            if (comp.Ores.Count == 0 || comp.RemainingHeat == 0 && comp.Coal == 0)
                _appearance.SetData(uid, StoneFurnaceVisuals.Burning, false);
        }
    }
}
