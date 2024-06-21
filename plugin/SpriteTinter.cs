using UnityEngine;
using System.Runtime.CompilerServices;

namespace WavesMod;

/// <summary>
/// Used to darken sprites during the despawn animation.
/// </summary>
static class SpriteTinter
{
    class TintData
    {
        public Color targetColor;
        public float alpha;

        public TintData()
        {
            targetColor = Color.white;
            alpha = 0f;
        }
    }

    private static ConditionalWeakTable<FSprite, TintData> fSpriteCwt = new();
    private static ConditionalWeakTable<IDrawable, TintData> drawableCwt = new();

    delegate void UpdateOrig(RoomCamera.SpriteLeaser self, float timeStacker, RoomCamera rCam, Vector2 camPos);

    private static void UpdateHook(
            UpdateOrig orig, RoomCamera.SpriteLeaser self,
            float timeStacker, RoomCamera rCam, Vector2 camPos
    )
    {
        if (!drawableCwt.TryGetValue(self.drawableObject, out var extraData))
        {
            orig(self, timeStacker, rCam, camPos);
            return;
        }

        orig(self, timeStacker, rCam, camPos);
        
        for (int i = 0; i < self.sprites.Length; i++)
        {
            var tintData = fSpriteCwt.GetOrCreateValue(self.sprites[i]);
            tintData.targetColor = extraData.targetColor;
            tintData.alpha = extraData.alpha;            
        }
    }

    public static void InitHooks()
    {
        // sprite leaser update hooks
        On.RoomCamera.SpriteLeaser.Update += (
                On.RoomCamera.SpriteLeaser.orig_Update orig, RoomCamera.SpriteLeaser self,
                float timeStacker, RoomCamera rCam, Vector2 camPos
        ) => UpdateHook(new UpdateOrig(orig), self, timeStacker, rCam, camPos);

        On.RoomCamera.SpriteLeaser.PausedUpdate += (
                On.RoomCamera.SpriteLeaser.orig_PausedUpdate orig, RoomCamera.SpriteLeaser self,
                float timeStacker, RoomCamera rCam, Vector2 camPos
        ) => UpdateHook(new UpdateOrig(orig), self, timeStacker, rCam, camPos);

        // hook into FSprite and TriangleMesh in order to tint the mesh
        // by the extra tint value
        On.FSprite.PopulateRenderLayer += (On.FSprite.orig_PopulateRenderLayer orig, FSprite self) =>
        {
            orig(self);

            if (self._isOnStage && self._firstFacetIndex != -1)
            {
                if (!fSpriteCwt.TryGetValue(self, out var extraData))
                    return;
                
                var colors = self._renderLayer.colors;
                var alpha = extraData.alpha;
                var targetColor = extraData.targetColor;

                int numVerts, offset;

                if (self._facetTypeQuad)
                {
                    numVerts = 4;
                    offset = self._firstFacetIndex * 4;
                }
                else
                {
                    numVerts = 6;
                    offset = self._firstFacetIndex * 3;
                }

                for (int i = 0; i < numVerts; i++)
                {
                    ref var color = ref colors[offset + i];
                    color.r = color.r * (1f - alpha) + targetColor.r * alpha;
                    color.g = color.g * (1f - alpha) + targetColor.g * alpha;
                    color.b = color.b * (1f - alpha) + targetColor.b * alpha;
                    color.a = color.a * (1f - alpha) + targetColor.a * alpha;
                }
            }
        };
        
        On.TriangleMesh.PopulateRenderLayer += (On.TriangleMesh.orig_PopulateRenderLayer orig, TriangleMesh self) =>
        {
            orig(self);

            if (self._isOnStage && self._firstFacetIndex != -1)
            {
                if (!fSpriteCwt.TryGetValue(self, out var extraData))
                    return;
                
                var colors = self._renderLayer.colors;
                var alpha = extraData.alpha;
                var targetColor = extraData.targetColor;
                var offset = self._firstFacetIndex * 3;

                if (self.customColor)
                {
                    for (int i = 0; i < self.triangles.Length; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            ref var color = ref colors[offset + i * 3 + j];
                            color.r = color.r * (1f - alpha) + targetColor.r * alpha;
                            color.g = color.g * (1f - alpha) + targetColor.g * alpha;
                            color.b = color.b * (1f - alpha) + targetColor.b * alpha;
                            color.a = color.a * (1f - alpha) + targetColor.a * alpha;
                        }
                    }
                }
                else
                {
                    for (int k = 0; k < self.triangles.Length * 3; k++)
                    {
                        ref var color = ref colors[offset + k];
                        color.r = color.r * (1f - alpha) + targetColor.r * alpha;
                        color.g = color.g * (1f - alpha) + targetColor.g * alpha;
                        color.b = color.b * (1f - alpha) + targetColor.b * alpha;
                        color.a = color.a * (1f - alpha) + targetColor.a * alpha;
                    }
                }
            }
        };
    }

    public static void Reset()
    {
        fSpriteCwt = new();
        drawableCwt = new();
    }

    public static void SetColorData(IDrawable drawable, Color color, float blend)
    {
        var colorData = drawableCwt.GetOrCreateValue(drawable);
        colorData.targetColor = color;
        colorData.alpha = blend;
    }
}