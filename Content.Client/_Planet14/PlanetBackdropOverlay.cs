using System.Numerics;
using Content.Shared._Planet14;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Utility;

namespace Content.Client._Planet14;

/// <summary>Drawn after parallax, before every grid and sprite. Camera rotation stays world-relative.</summary>
public sealed class PlanetBackdropOverlay : Overlay
{
    private readonly IEntityManager _entities;
    private readonly SharedTransformSystem _transform;
    private readonly Texture _texture;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;

    public PlanetBackdropOverlay(IEntityManager entities)
    {
        ZIndex = 1;
        _entities = entities;
        _transform = entities.System<SharedTransformSystem>();
        _texture = entities.System<SpriteSystem>().Frame0(new SpriteSpecifier.Rsi(
            new ResPath("/Textures/_Planet14/Space/planet.rsi"), "planet"));
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var planets = _entities.EntityQueryEnumerator<PlanetBackdropComponent, TransformComponent>();
        while (planets.MoveNext(out var uid, out _, out var transform))
        {
            if (transform.MapID != args.MapId)
                continue;
            args.WorldHandle.SetTransform(_transform.GetWorldMatrix(uid));
            args.WorldHandle.DrawTextureRect(_texture, new Box2(-5, -5, 5, 5));
        }
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }
}
