using RWCustom;
using UnityEngine;

namespace WavesMod;

class FlyTransformParticle : CosmeticSprite
{
    private float lastLife = 1f;
    private float life = 1f;
    private readonly float initScale;
    private readonly float lifetime;
    private float speed;

    public FlyTransformParticle(Vector2 pos)
    {
        this.pos = pos;
        lastPos = pos;

        lifetime = Random.Range(18, 25);
        initScale = Random.Range(1.4f, 1.6f);

        var angle = Random.Range(0f, 2f * Mathf.PI);
        speed = 1.2f + Random.value * 0.3f;
        vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
    }

    public override void Update(bool eu)
    {
        lastLife = life;
        life -= 1f / lifetime;
        if (life <= 0)
            Destroy();

        vel = Vector3.Slerp(vel, Custom.DegToVec(Random.value * 360f) * speed, 0.5f);
        base.Update(eu);
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[1];
        sLeaser.sprites[0] = new FSprite("Futile_White")
        {
            scaleX = initScale,
            scaleY = initScale,
            shader = rCam.game.rainWorld.Shaders["VectorCircle"]
        };

        AddToContainer(sLeaser, rCam, null);
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        var sprite = sLeaser.sprites[0];

        var drawLife = Mathf.Lerp(lastLife, life, timeStacker);

        sprite.x = Mathf.Lerp(lastPos.x, pos.x, timeStacker);
        sprite.y = Mathf.Lerp(lastPos.y, pos.y, timeStacker);
        sprite.alpha = 1f - (1f - life) * 3f % 1f;
        sprite.scaleX = drawLife * initScale;
        sprite.scaleY = drawLife * initScale;

        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
    }

    public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        sLeaser.sprites[0].color = palette.blackColor;
        base.ApplyPalette(sLeaser, rCam, palette);
    }
}