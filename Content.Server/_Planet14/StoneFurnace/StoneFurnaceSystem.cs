using System.Linq;
using Content.Server.Audio;
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
    [Dependency] private AmbientSoundSystem _ambient = default!;
    [Dependency] private SharedPointLightSystem _lights = default!;
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

        var space = fuel ? comp.FuelCapacity - comp.Coal : comp.OreCapacity - comp.Ores.Values.Sum() - comp.BatchCount;
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
            ("ore", ent.Comp.Ores.Values.Sum()), ("heat", ent.Comp.RemainingHeat), ("batch", ent.Comp.BatchCount)));
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<StoneFurnaceComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var comp, out var meta))
        {
            if (meta.EntityPaused)
                continue;

            if (comp.BatchCount == 0)
            {
                var ore = comp.Ores.FirstOrDefault(pair => pair.Value > 0 && comp.Recipes.ContainsKey(pair.Key));
                var perCoal = Math.Max(1, comp.ItemsPerCoal);
                var count = Math.Min(Math.Max(1, comp.BatchSize), Math.Min(ore.Value,
                    comp.RemainingHeat + comp.Coal * perCoal));
                if (count > 0)
                {
                    // Reserve the entire batch and its fuel before starting the timer.
                    var needed = Math.Max(0, count - comp.RemainingHeat);
                    var coal = (needed + perCoal - 1) / perCoal;
                    comp.Coal -= coal;
                    comp.RemainingHeat += coal * perCoal - count;
                    comp.BatchCount = count;
                    comp.BatchOutput = comp.Recipes[ore.Key];
                    if (ore.Value == count)
                        comp.Ores.Remove(ore.Key);
                    else
                        comp.Ores[ore.Key] -= count;
                }
            }

            SetBurning(uid, comp.BatchCount > 0);
            if (comp.BatchCount == 0)
                continue;
            comp.Progress += frameTime;
            if (comp.Progress < Math.Max(1f, comp.SmeltSeconds))
                continue;

            // Complete at most one batch per update, including after a slow frame.
            comp.Progress = 0;
            for (var i = 0; i < comp.BatchCount; i++)
                SpawnAtPosition(comp.BatchOutput, Transform(uid).Coordinates);
            comp.BatchCount = 0;
            var canContinue = comp.Ores.Count > 0 && (comp.RemainingHeat > 0 || comp.Coal > 0);
            SetBurning(uid, canContinue);
        }
    }

    private void SetBurning(EntityUid uid, bool burning)
    {
        _appearance.SetData(uid, StoneFurnaceVisuals.Burning, burning);
        _lights.SetEnabled(uid, burning);
        _ambient.SetAmbience(uid, burning);
    }
}
