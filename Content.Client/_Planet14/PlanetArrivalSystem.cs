using Content.Client.Resources;
using Content.Shared._Planet14;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using System.Numerics;

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
        _title = resources.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 26);
        _body = resources.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 15);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (Message == null || Age > 7 || args.ViewportControl == null)
            return;
        var alpha = Math.Clamp(Math.Min(Age / 0.5f, (7 - Age) / 1.5f), 0, 1);
        var width = args.Viewport.Size.X;
        var left = Math.Max(16, width * 0.15f);
        var top = 65 - (1 - alpha) * 15;
        var handle = args.ScreenHandle;
        handle.DrawRect(new UIBox2(left - 16, top - 30, width - left + 16, top + 75),
            Color.Black.WithAlpha(alpha * 0.7f));
        handle.DrawString(_title, new Vector2(left, top), Message.Title, 1, Color.White.WithAlpha(alpha));
        handle.DrawString(_body, new Vector2(left, top + 28), Message.Subtitle, 1, Color.LightSkyBlue.WithAlpha(alpha));
        handle.DrawString(_body, new Vector2(left, top + 56), Message.Description, 1, Color.White.WithAlpha(alpha));
    }
}
