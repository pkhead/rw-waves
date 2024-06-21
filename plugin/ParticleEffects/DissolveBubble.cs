using UnityEngine;
namespace WavesMod.ParticleEffects;

class DissolveBubble : CosmeticSprite
{
	public float life;
    public float maxScale;
	public int lifeTime;
	public Vector2 originPoint;
    public float angle;
    public float dist;
    private Color color;

	public DissolveBubble(Vector2 originPt, float intensity, Color color)
	{
        originPoint = originPt;

        maxScale = Mathf.Max(0.1f, Random.value * 0.15f + intensity);

        angle = Random.Range(0f, 2f * Mathf.PI);
        dist = Random.Range(0f, 4f);

        pos.x = originPoint.x + Mathf.Cos(angle) * dist;
        pos.y = originPoint.y + Mathf.Sin(angle) * dist;
		lastPos = pos;

		life = 1f;
		lifeTime = 60;
        this.color = color;
	}

	public override void Update(bool eu)
	{
        lastPos = pos;

		life -= 1f / lifeTime;
		if (life <= 0f)
			Destroy();

        angle += 0.1f;

        pos.x = originPoint.x + Mathf.Cos(angle) * dist;
        pos.y = originPoint.y + Mathf.Sin(angle) * dist;

		base.Update(eu);
	}

	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
        // create circle mesh
		sLeaser.sprites = new FSprite[1];
		sLeaser.sprites[0] = new FSprite("Futile_White", true)
        {
            scaleX = maxScale,
            scaleY = maxScale,
            shader = rCam.game.rainWorld.Shaders["VectorCircle"]
        };

		AddToContainer(sLeaser, rCam, null);
	}

	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
        float scale = maxScale * life;

        var sprite = sLeaser.sprites[0];
		sprite.x = Mathf.Lerp(lastPos.x, pos.x, timeStacker) - camPos.x;
		sprite.y = Mathf.Lerp(lastPos.y, pos.y, timeStacker) - camPos.y;
		sprite.color = color;
        sprite.scaleX = 1f * scale;
        sprite.scaleY = 1f * scale;
        
		base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
	}

    public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        base.ApplyPalette(sLeaser, rCam, palette);
    }

    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
	{
        newContatiner ??= rCam.ReturnFContainer("Midground");

		foreach (FSprite fsprite in sLeaser.sprites)
		{
			fsprite.RemoveFromContainer();
			newContatiner.AddChild(fsprite);
		}
	}
}