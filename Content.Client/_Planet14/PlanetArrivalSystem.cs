using Content.Client.Resources;
using Content.Shared._Planet14;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using System.Numerics;
using System.Collections.Generic;
using System.Text;

namespace Content.Client._Planet14;

public sealed partial class PlanetArrivalSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlays = default!;
    [Dependency] private IResourceCache _resources = default!;
    private PlanetArrivalOverlay _overlay = default!;
    public PlanetArrivalEvent? CurrentArrival => _overlay.Message;

    public override void Initialize()
    {
        _overlay = new PlanetArrivalOverlay(_resources);
        _overlays.AddOverlay(_overlay);
        _overlays.AddOverlay(new PlanetBackdropOverlay(EntityManager));
        SubscribeNetworkEvent<PlanetArrivalEvent>(OnArrival);
    }

    private void OnArrival(PlanetArrivalEvent args)
    {
        _overlay.Message = args;
        _overlay.Age = 0;
    }

    public override void FrameUpdate(float frameTime) => _overlay.Age += frameTime;

    public override void Shutdown()
    {
        _overlays.RemoveOverlay<PlanetArrivalOverlay>();
        _overlays.RemoveOverlay<PlanetBackdropOverlay>();
        base.Shutdown();
    }
}

public sealed class PlanetArrivalOverlay : Overlay
{
    private readonly Font _title;
    private readonly Font _body;
    public PlanetArrivalEvent? Message;
    public float Age = 10;
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public PlanetArrivalOverlay(IResourceCache resources)
    {
        _title = resources.GetFont("/Fonts/PressStart2P/PressStart2P-Regular.ttf", 16);
        _body = resources.GetFont("/Fonts/PressStart2P/PressStart2P-Regular.ttf", 9);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (Message == null || Age > 7 || args.ViewportControl == null)
            return;
        if (args.Viewport.Size.X < 100 || args.Viewport.Size.Y < 100)
            return;
        var alpha = Math.Clamp(Math.Min(Age / 0.5f, (7 - Age) / 1.5f), 0, 1);
        var width = Math.Min(900, args.Viewport.Size.X - 40);
        var left = (args.Viewport.Size.X - width) / 2f;
        var title = Wrap(_title, Message.Title.ToUpperInvariant(), width - 48);
        var subtitle = Wrap(_body, Message.Subtitle, width - 48);
        var description = Wrap(_body, Message.Description, width - 48);
        var titleHeight = _title.GetLineHeight(1) + 7;
        var bodyHeight = _body.GetLineHeight(1) + 7;
        var height = 56 + title.Count * titleHeight + (subtitle.Count + description.Count) * bodyHeight + 24;
        var top = (args.Viewport.Size.Y - height) / 2f + (1 - alpha) * 15;
        var handle = args.ScreenHandle;
        handle.DrawRect(new UIBox2(left, top, left + width, top + height), Color.Black.WithAlpha(alpha * 0.88f));
        handle.DrawRect(new UIBox2(left, top, left + width, top + 3), Color.LightSkyBlue.WithAlpha(alpha));
        handle.DrawRect(new UIBox2(left, top + height - 3, left + width, top + height), Color.LightSkyBlue.WithAlpha(alpha));
        var y = top + 24 + _title.GetAscent(1);
        foreach (var line in title)
        {
            handle.DrawString(_title, new Vector2(left + (width - Measure(_title, line)) / 2, y), line, 1, Color.White.WithAlpha(alpha));
            y += titleHeight;
        }
        y += 12;
        foreach (var line in subtitle)
        {
            handle.DrawString(_body, new Vector2(left + (width - Measure(_body, line)) / 2, y), line, 1, Color.LightSkyBlue.WithAlpha(alpha));
            y += bodyHeight;
        }
        y += 12;
        foreach (var line in description)
        {
            handle.DrawString(_body, new Vector2(left + (width - Measure(_body, line)) / 2, y), line, 1, Color.White.WithAlpha(alpha));
            y += bodyHeight;
        }
    }

    private static float Measure(Font font, string text)
    {
        var width = 0f;
        foreach (var rune in text.EnumerateRunes())
            width += font.GetCharMetrics(rune, 1)?.Advance ?? 0;
        return width;
    }

    private static List<string> Wrap(Font font, string text, float width)
    {
        var lines = new List<string>();
        var line = string.Empty;
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var proposed = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && Measure(font, proposed) > width)
            {
                lines.Add(line);
                line = word;
            }
            else
                line = proposed;
        }
        if (line.Length > 0)
            lines.Add(line);
        return lines;
    }
}
