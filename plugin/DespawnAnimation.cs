using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace WavesMod;

class DespawnAnimation : UpdatableAndDeletable
{
    public readonly Creature creature;
    private int time = 0;
    private int phase = 0;

    private readonly List<Vector2> curvePositions = new();
    private Color dissolveColor = Color.red;

    public DespawnAnimation(Creature creature) : base()
    {
        this.creature = creature;
    }

    public override void Update(bool eu)
    {
        if (phase == 3)
        {
            Destroy();
        }

        if (phase == 0 && creature.room is null)
        {
            creature.abstractCreature.Room?.RemoveEntity(creature.abstractCreature);
            Destroy();
        }

        if (creature.room is not null)
        {
            UpdateParticleCurve();

            // get creature color
            // defaults to black
            dissolveColor = room.game.cameras[0].currentPalette.blackColor;

            /*if (creature is Lizard lizard && lizard.Template.type == CreatureTemplate.Type.WhiteLizard)
            {
                dissolveColor = (lizard.graphicsModule as LizardGraphics).whiteCamoColor;
            }*/
        }

        if (phase == 0)
        {
            time++;

            var animationProgress = time / 400f;
            SpawnBubble(animationProgress * 2.5f, dissolveColor);

            if (creature.graphicsModule is LizardGraphics lizardGfx)
            {
                lizardGfx.lightSource.setAlpha *= 1f - animationProgress;
            }

            SpriteTinter.SetColorData(creature.graphicsModule, dissolveColor, animationProgress);

            if (animationProgress >= 1f)
            {
                creature.AllGraspsLetGoOfThisObject(true);
                creature.LoseAllGrasps();
                creature.Destroy();
                phase++;
            }
        }
        else if (phase == 1)
        {
            time -= 4;

            if (time <= 0)
            {
                phase++;
                Destroy();
            }

            var animationProgress = time / 400f;
            SpawnBubble(animationProgress * 2.5f, dissolveColor);
        }
    }

    private void SpawnBubble(float scale, Color color)
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

        room.AddObject(new ParticleEffects.DissolveBubble(originPoint, scale, color));
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