using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._Planet14.Planets;
using Robust.Shared.Console;

namespace Content.Server._Planet14.Planets;

[AdminCommand(AdminFlags.Mapping)]
public sealed partial class Planet14PlanetCommand : LocalizedEntityCommands
{
    [Dependency] private Planet14PlanetSystem _planets = default!;

    public override string Command => "planet14_planet";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            shell.WriteError(Help);
            return;
        }

        int? seed = null;
        if (args.Length == 2)
        {
            if (!int.TryParse(args[1], out var value) || value < 0)
            {
                shell.WriteError(Loc.GetString("planet14-planet-seed-invalid"));
                return;
            }
            seed = value;
        }

        if (_planets.TryCreate(args[0], seed, out _, out var message))
            shell.WriteLine(message);
        else
            shell.WriteError(message);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<Planet14PlanetPrototype>(),
                Loc.GetString("planet14-planet-prototype-hint"))
            : CompletionResult.Empty;
    }
}
