using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace WavesMod;

class DissolveBubble : CosmeticSprite
{
	public Creature creature;
	public float life;
    public float maxScale;
	public int lifeTime;
	public Vector2 originPoint;
    public float angle;
    public float dist;
    private Color color;

	public DissolveBubble(Creature creature, float intensity)
	{
        List<Vector2> curvePositions = new();

        if (false && creature.graphicsModule is LizardGraphics lizardGfx)
        {
            curvePositions.Add(lizardGfx.head.pos);

            int numBodyCircles = lizardGfx.SpriteBodyCirclesEnd - lizardGfx.SpriteBodyCirclesStart;
            for (int i = 0; i < numBodyCircles; i++)
            {
                curvePositions.Add(lizardGfx.BodyPosition(i, 0f));
            }

            for (int i = 0; i < lizardGfx.tail.Length; i++)
            {
                curvePositions.Add(lizardGfx.tail[i].pos);
            }

            // pick a random point along this "curve"
            int index = Random.Range(0, curvePositions.Count - 1);
            originPoint = Vector2.Lerp(curvePositions[index], curvePositions[index+1], Random.value);
        }
        else
        {
            for (int i = 0; i < creature.graphicsModule.bodyParts.Length; i++)
            {
                curvePositions.Add(creature.graphicsModule.bodyParts[i].pos);
            }
        }

        // pick a random point along this "curve"
        if (curvePositions.Count <= 1)
        {
            originPoint = curvePositions[0];
        }
        else
        {
            int index = Random.Range(0, curvePositions.Count - 1);
            originPoint = Vector2.Lerp(curvePositions[index], curvePositions[index+1], Random.value);
        }

        /*if (graphicsModule.bodyParts.Length == 1)
        {
            originPoint = graphicsModule.bodyParts[0].pos;
        }
        else
        {
            int index = Random.Range(0, graphicsModule.bodyParts.Length - 2);
            originPoint = Vector2.Lerp(graphicsModule.bodyParts[index].pos, graphicsModule.bodyParts[index+1].pos, Random.value * 1.2f - 0.1f);
        }*/
        
        maxScale = Mathf.Max(0.1f, Random.value * 0.3f + intensity);

        angle = Random.Range(0f, 2f * Mathf.PI);
        dist = Random.Range(0f, 1f);

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
		sLeaser.sprites = new FSprite[1];
		sLeaser.sprites[0] = new FSprite("Circle20", true)
        {
            scaleX = maxScale,
            scaleY = maxScale
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
        sprite.scaleX = scale;
        sprite.scaleY = scale;
        
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
    private int nextSpawn = 0;

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
        if (creature.room is null)
        {
            creature.abstractCreature.Room?.RemoveEntity(creature.abstractCreature);
            return false;
        }

        time++;
        /*nextSpawn -= time / 120f;
        if (nextSpawn < -10f) nextSpawn = -10f;

        while (nextSpawn < 0f)
        {
            room.AddObject(new DissolveBubble(graphicsModule, time / 4000f));
            nextSpawn++;
        }

        nextSpawn = 1f;*/
        
        /*if (nextSpawn == 0)
        {
            room.AddObject(new DissolveBubble(creature, 1f));
            nextSpawn = 2;
        }
        else
        {
            nextSpawn--;
        }*/

        var animationProgress = time / 400f;
        var scale = animationProgress * 2f;

        room.AddObject(new DissolveBubble(creature, scale));

        if (creature.graphicsModule is LizardGraphics lizardGfx)
        {
            lizardGfx.lightSource.setAlpha *= 1f - animationProgress;
        }

        var col = 1f - animationProgress;
        WavesMod.Instance.spriteLeaserMod.SetTint(creature.graphicsModule, new Color(col, col, col));

        if (animationProgress >= 1f)
        {
            creature.room.RemoveObject(creature);
            return false;
        }

        return true;
    }
}