using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace WavesMod;

class DissolveBubble : CosmeticSprite
{
	public float life;
    public float maxScale;
	public int lifeTime;
	public Vector2 originPoint;
    public float angle;
    public float dist;
    private Color color;

	public DissolveBubble(Vector2 pos, float intensity)
	{
        originPoint = pos;
        maxScale = Mathf.Max(0.1f, Random.value * 0.15f + intensity);

        angle = Random.Range(0f, 2f * Mathf.PI);
        dist = Random.Range(0f, 4f);

		lastPos = pos;
		life = 1f;
		lifeTime = 60;

        color = new Color(Random.value, Random.value, Random.value);
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
        color = palette.blackColor;
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

class DissolveBubbler
{
    public readonly Creature creature;
    private readonly Room room;
    private int time = 0;
    private int phase = 0;

    private readonly List<Vector2> curvePositions = new();

    public DissolveBubbler(Creature creature)
    {
        this.creature = creature;
        room = creature.room;
    }

    /// <summary>
    /// Update the DissolveBubbler.
    /// </summary>
    /// <returns>False if the DissolveBubbler is no longer active.</returns>
    public bool Update()
    {
        if (phase == 3) return false;
        if (phase == 0 && creature.room is null)
        {
            creature.abstractCreature.Room?.RemoveEntity(creature.abstractCreature);
            return false;
        }

        if (creature.room is not null)
            UpdateParticleCurve();

        if (phase == 0)
        {
            time++;

            var animationProgress = time / 400f;
            SpawnBubble(animationProgress * 2.5f);

            if (creature.graphicsModule is LizardGraphics lizardGfx)
            {
                lizardGfx.lightSource.setAlpha *= 1f - animationProgress;
            }

            var blackColor = creature.room.game.cameras[0].currentPalette.blackColor;
            WavesMod.Instance.spriteLeaserMod.SetColorData(creature.graphicsModule, blackColor, animationProgress);

            if (animationProgress >= 1f)
            {
                creature.room.RemoveObject(creature);
                phase++;
            }
        }
        else if (phase == 1)
        {
            time -= 4;

            if (time <= 0)
            {
                phase++;
                return false;
            }

            var animationProgress = time / 400f;
            SpawnBubble(animationProgress * 2.5f);
        }

        return true;
    }

    private void SpawnBubble(float scale)
    {
        // spawn a dissolve bubble on a random point along
        // the particle curve
        Vector2 originPoint;
        if (curvePositions.Count <= 1)
        {
            originPoint = curvePositions[0];
        }
        else
        {
            int index = Random.Range(0, curvePositions.Count - 1);
            originPoint = Vector2.Lerp(curvePositions[index], curvePositions[index+1], Random.value);
        }

        room.AddObject(new DissolveBubble(originPoint, scale));
    }

    private void UpdateParticleCurve()
    {
        var graphics = creature.graphicsModule;
        curvePositions.Clear();

        for (int i = 0; i < graphics.bodyParts.Length; i++)
        {
            curvePositions.Add(graphics.bodyParts[i].pos);
        }
    }
}