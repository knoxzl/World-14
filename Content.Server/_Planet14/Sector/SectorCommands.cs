using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Planet14.Sector;

[AnyCommand]
public sealed partial class SectorStatusCommand : LocalizedEntityCommands
{
    [Dependency] private SectorCycleSystem _sector = default!;

    public override string Command => "planet14_status";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!_sector.TryGetSnapshot(out var snapshot))
        {
            shell.WriteLine(Loc.GetString("planet14-clock-unavailable"));
            return;
        }

        shell.WriteLine(Loc.GetString("planet14-clock-status",
            ("day", snapshot.Day),
            ("blackSpace", Loc.GetString($"planet14-blackspace-{snapshot.BlackSpace.ToString().ToLowerInvariant()}")),
            ("phase", Loc.GetString($"planet14-phase-{snapshot.Phase.ToString().ToLowerInvariant()}")),
            ("remaining", snapshot.UntilCollapse.ToString("c"))));
    }
}

[AdminCommand(AdminFlags.Server)]
public sealed partial class SectorStartCommand : LocalizedEntityCommands
{
    [Dependency] private SectorCycleSystem _sector = default!;

    public override string Command => "planet14_start";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (_sector.TryStart(out var message))
            shell.WriteLine(message);
        else
            shell.WriteError(message);
    }
}
