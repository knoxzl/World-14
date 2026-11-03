using Content.Server.Lathe;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Player;

namespace Content.Server._Planet14.Workbench;

public sealed partial class ScrapWorkbenchSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ScrapWorkbenchComponent, LatheProgressAttemptEvent>(OnProgress);
    }

    private void OnProgress(Entity<ScrapWorkbenchComponent> ent, ref LatheProgressAttemptEvent args)
    {
        if (!IsAttended(ent))
            args.Cancelled = true;
    }

    public bool IsAttended(Entity<ScrapWorkbenchComponent> ent)
    {
        var origin = _transform.GetMapCoordinates(ent.Owner);
        var people = EntityQueryEnumerator<ActorComponent, MobStateComponent, TransformComponent>();
        while (people.MoveNext(out var person, out _, out var mob, out var transform))
        {
            if (mob.CurrentState != MobState.Alive)
                continue;
            var position = _transform.GetMapCoordinates(person, transform);
            if (position.MapId == origin.MapId &&
                (position.Position - origin.Position).LengthSquared() <= ent.Comp.AttendanceRange * ent.Comp.AttendanceRange)
                return true;
        }
        return false;
    }
}
