using System;
using HUD;
using UnityEngine;

namespace WavesMod;

class HudLifeTracker : PlayerSpecificMultiplayerHud.Part
{
    private FSprite karmaSymbol;
    private FSprite darkGradient;

    private int playerNumber;
    private int lastLives;

    // ranges from 0-1, controls symbol transparency
    private float highlight = 0f;
    private float lastHighlight = 0f;
    private int highlightSustain = 0;

    private float scale = 1f;
    private float lastScale = 1f;

    private Color symbolColor;
    private float colorFlash = 0f;
    private float lastColorFlash = 0f;

    private uint lastLifeFlashTicker = 0;

    private static readonly string[] karmaSymbols = new string[]
    {
        "smallKarmaNoRing0",
        "smallKarmaNoRing1",
        "smallKarmaNoRing2",
        "smallKarmaNoRing3",
        "smallKarmaNoRing4",
    };

    private static FAtlasElement GetKarmaSymbol(int i)
    {
        if (i < 0 || i >= karmaSymbols.Length) return Futile.atlasManager.GetElementWithName("smallKarmaNoRingD");
        return Futile.atlasManager.GetElementWithName(karmaSymbols[i]);
    }

    public HudLifeTracker(PlayerSpecificMultiplayerHud owner) : base(owner)
    {
        // dark gradient behind karma symbol
        darkGradient = new FSprite("Futile_White")
        {
            color = Color.black,
            shader = owner.hud.rainWorld.Shaders["FlatLight"],
            scaleX = 6f,
            scaleY = 6f
        };
        owner.hud.fContainers[0].AddChild(darkGradient);

        // karma symbol representing the number of lives the player has
        symbolColor = PlayerGraphics.DefaultSlugcatColor((owner.abstractPlayer.state as PlayerState).slugcatCharacter);
        karmaSymbol = new FSprite("smallKarmaNoRing0")
        {
            color = symbolColor,
            alpha = 0.4f,
            scale = 0.75f
        };
        owner.hud.fContainers[0].AddChild(karmaSymbol);

        pos = owner.cornerPos + new Vector2(owner.flip * 20f + 0.01f, 0.01f);

        if (!ArenaSittingHooks.TryGetData(owner.session.arenaSitting, out var sittingData))
        {
            throw new System.Exception("HudLifeTracker created, but game session is not a waves game session");
        }

        playerNumber = (owner.abstractPlayer.state as PlayerState).playerNumber;
        lastLives = sittingData.playerLives[playerNumber];
        karmaSymbol.element = GetKarmaSymbol(lastLives);

        // "smallKarma9-9" (cross)
        // "smallKarma[NoRing][0-4]"
    }

    public override void Update()
    {
        base.Update();
        lastHighlight = highlight;
        lastScale = scale;
        lastColorFlash = colorFlash;
        
        if (!ArenaSittingHooks.TryGetData(owner.session.arenaSitting, out var sittingData))
        {
            throw new System.Exception("HudLifeTracker created, but game session is not a waves game session");
        }

        var lifeCount = sittingData.playerLives[playerNumber];

        // the life count actually decreases whenever a new player spawns
        // but i want to show the life count decreasing when they are dead
        // so, artifically decrease the life count whenever they are dead
        if (owner.abstractPlayer.realizedCreature is not Player realizedPlayer
            || ((realizedPlayer.dead || realizedPlayer.grabbedBy.Count > 0) && owner.session.Players.Contains(owner.abstractPlayer))
        )
        {
            lifeCount--;
        }
        
        if (lifeCount != lastLives)
        {
            Debug.Log($"Player #{playerNumber} lives changed");
            karmaSymbol.element = GetKarmaSymbol(lifeCount);
            highlight = 1f;
            highlightSustain = 120;
            if (lifeCount < lastLives) colorFlash = 1f;

            lastLives = lifeCount;
            scale = 1.4f;
        }

        // stay highlighted and flash red every second
        // when the player is on their last life
        if (lifeCount == 0)
        {
            highlight = 1f;
            if (++lastLifeFlashTicker % 40 == 0)
            {
                colorFlash = 0.5f;
                scale = 1.1f;
            }
        }

        // highlight fade animation
        // ticks down highlightSustain until is 0
        // then, slowly decreases highlight alpha until it reaches 0
        // if the player has no more lives, the highlight value will continue decreasing
        // so that the karma symbol fades to invisibility.
        if (highlight > 0f || lifeCount < 0)
        {
            if (highlightSustain == 0)
            {
                highlight -= 0.008f;
                if (lifeCount >= 0) highlight = Math.Max(highlight, 0f);
            }
            else
            {
                highlightSustain--;
            }
        }

        if (colorFlash > 0f)
        {
            colorFlash = Math.Max(colorFlash - 0.01f, 0f);
        }

        scale += (1f - scale) * 0.2f;
    }

    public override void Draw(float timeStacker)
    {
        base.Draw(timeStacker);
        var drawPos = Vector2.Lerp(lastPos, pos, timeStacker);
        var drawHighlight = Mathf.Lerp(lastHighlight, highlight, timeStacker);
        var drawScale = Mathf.Lerp(lastScale, scale, timeStacker);
        var drawColorFlash = Mathf.Lerp(lastColorFlash, colorFlash, timeStacker);

        karmaSymbol.x = drawPos.x;
        karmaSymbol.y = drawPos.y;
        darkGradient.x = drawPos.x;
        darkGradient.y = drawPos.y;

        karmaSymbol.scaleX = drawScale * 0.75f;
        karmaSymbol.scaleY = drawScale * 0.7f;
        karmaSymbol.color = Color.Lerp(symbolColor, Color.red, drawColorFlash);
        karmaSymbol.alpha = Math.Max(Mathf.LerpUnclamped(0.4f, 1f, drawHighlight), 0f);
        darkGradient.alpha = Math.Max(Mathf.LerpUnclamped(0.05f, 0.15f, drawHighlight), 0f);
    }

    public override void ClearSprites()
    {
        base.ClearSprites();
        karmaSymbol.RemoveFromContainer();
        darkGradient.RemoveFromContainer();
    }
}