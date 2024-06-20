using UnityEngine;
using System.Runtime.CompilerServices;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;

namespace WavesMod;

class SpriteLeaserMod
{
    class TintData
    {
        public Color tint;

        public TintData()
        {
            tint = Color.white;
        }
    }

    private readonly ConditionalWeakTable<FSprite, TintData> fSpriteCwt = new();
    private readonly ConditionalWeakTable<IDrawable, TintData> drawableCwt = new();

    public SpriteLeaserMod()
    {}

    delegate void UpdateOrig(RoomCamera.SpriteLeaser self, float timeStacker, RoomCamera rCam, Vector2 camPos);

    private void UpdateHook(
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
            fSpriteCwt.GetOrCreateValue(self.sprites[i]).tint = extraData.tint;
        }
    }

    public void InitHooks()
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
                var tint = extraData.tint;

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
                    colors[offset + i].r *= tint.r;
                    colors[offset + i].g *= tint.g;
                    colors[offset + i].b *= tint.b;
                    colors[offset + i].a *= tint.a;
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
                var tint = extraData.tint;
                var offset = self._firstFacetIndex * 3;

                if (self.customColor)
                {
                    for (int i = 0; i < self.triangles.Length; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            ref var color = ref colors[(offset + i) * 3 + j];
                            color.r *= tint.r;
                            color.g *= tint.g;
                            color.b *= tint.b;
                            color.a *= tint.a;
                        }
                    }
                }
                else
                {
                    for (int k = 0; k < self.triangles.Length * 3; k++)
                    {
                        ref var color = ref colors[offset + k];
                        color.r *= tint.r;
                        color.g *= tint.g;
                        color.b *= tint.b;
                        color.a *= tint.a;
                    }
                }
            }
        };
    }

    public void SetTint(IDrawable drawable, Color tint)
    {
        drawableCwt.GetOrCreateValue(drawable).tint = tint;
    }

    public Color GetTint(IDrawable drawable)
    {
        if (!drawableCwt.TryGetValue(drawable, out var extraData))
            return Color.white;
        
        return extraData.tint;
    }
}