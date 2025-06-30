--[[
public static Color ColorOfCreature(IconSymbolData iconData)
{
    if (iconData.critType.Index == -1)
    {
        return global::Menu.Menu.MenuRGB(global::Menu.Menu.MenuColors.MediumGrey);
    }
    if (iconData.critType == CreatureTemplate.Type.Slugcat)
    {
        return PlayerGraphics.DefaultSlugcatColor(SlugcatStats.Name.ArenaColor(iconData.intData));
    }
    if (iconData.critType == CreatureTemplate.Type.GreenLizard)
    {
        return (StaticWorld.GetCreatureTemplate(iconData.critType).breedParameters as LizardBreedParams).standardColor;
    }
    if (iconData.critType == CreatureTemplate.Type.PinkLizard)
    {
        return (StaticWorld.GetCreatureTemplate(iconData.critType).breedParameters as LizardBreedParams).standardColor;
    }
    if (iconData.critType == CreatureTemplate.Type.BlueLizard)
    {
        return (StaticWorld.GetCreatureTemplate(iconData.critType).breedParameters as LizardBreedParams).standardColor;
    }
    if (iconData.critType == CreatureTemplate.Type.WhiteLizard)
    {
        return (StaticWorld.GetCreatureTemplate(iconData.critType).breedParameters as LizardBreedParams).standardColor;
    }
    if (iconData.critType == CreatureTemplate.Type.RedLizard)
    {
        return new Color(46f / 51f, 0.05490196f, 0.05490196f);
    }
    if (iconData.critType == CreatureTemplate.Type.BlackLizard)
    {
        return new Color(0.36862746f, 0.36862746f, 37f / 85f);
    }
    if (iconData.critType == CreatureTemplate.Type.YellowLizard || iconData.critType == CreatureTemplate.Type.SmallCentipede || iconData.critType == CreatureTemplate.Type.Centipede)
    {
        return new Color(1f, 0.6f, 0f);
    }
    if (iconData.critType == CreatureTemplate.Type.RedCentipede)
    {
        return new Color(46f / 51f, 0.05490196f, 0.05490196f);
    }
    if (iconData.critType == CreatureTemplate.Type.CyanLizard || iconData.critType == CreatureTemplate.Type.Overseer)
    {
        return new Color(0f, 0.9098039f, 46f / 51f);
    }
    if (iconData.critType == CreatureTemplate.Type.Salamander)
    {
        return new Color(14f / 15f, 0.78039217f, 76f / 85f);
    }
    if (iconData.critType == CreatureTemplate.Type.CicadaB)
    {
        return new Color(0.36862746f, 0.36862746f, 37f / 85f);
    }
    if (iconData.critType == CreatureTemplate.Type.CicadaA)
    {
        return new Color(1f, 1f, 1f);
    }
    if (iconData.critType == CreatureTemplate.Type.SpitterSpider || iconData.critType == CreatureTemplate.Type.Leech)
    {
        return new Color(58f / 85f, 8f / 51f, 0.11764706f);
    }
    if (iconData.critType == CreatureTemplate.Type.SeaLeech || iconData.critType == CreatureTemplate.Type.TubeWorm)
    {
        return new Color(0.05f, 0.3f, 0.7f);
    }
    if (iconData.critType == CreatureTemplate.Type.Centiwing)
    {
        return new Color(0.05490196f, 0.69803923f, 0.23529412f);
    }
    if (iconData.critType == CreatureTemplate.Type.BrotherLongLegs)
    {
        return new Color(0.45490196f, 0.5254902f, 26f / 85f);
    }
    if (iconData.critType == CreatureTemplate.Type.DaddyLongLegs)
    {
        return new Color(0f, 0f, 1f);
    }
    if (iconData.critType == CreatureTemplate.Type.VultureGrub)
    {
        return new Color(0.83137256f, 0.7921569f, 37f / 85f);
    }
    if (iconData.critType == CreatureTemplate.Type.EggBug)
    {
        return new Color(0f, 1f, 0.47058824f);
    }
    if (iconData.critType == CreatureTemplate.Type.BigNeedleWorm || iconData.critType == CreatureTemplate.Type.SmallNeedleWorm)
    {
        return new Color(1f, 0.59607846f, 0.59607846f);
    }
    if (iconData.critType == CreatureTemplate.Type.Hazer)
    {
        return new Color(18f / 85f, 0.7921569f, 33f / 85f);
    }
    Color? color = MoreSlugcatsCreatures.ColorOfCreature(iconData);
    if (color.HasValue)
    {
        return color.Value;
    }
    if (ModManager.Watcher)
    {
        Color? color2 = WatcherCreatures.ColorOfCreature(iconData);
        if (color2.HasValue)
        {
            return color2.Value;
        }
    }
    return global::Menu.Menu.MenuRGB(global::Menu.Menu.MenuColors.MediumGrey);
}
--]]

local module = {}

local creature_colors = {
    PinkLizard = {1, 0, 1},
    GreenLizard = {0.2, 1, 0},
    BlueLizard = {0, 0.5, 1},
    YellowLizard = {1, 0.6, 0},
    SmallCentipede = {1, 0.6, 0},
    Centipede = {1, 0.6, 0},
    WhiteLizard = {1, 1, 1},
    RedLizard = {46 / 51, 0.05490196, 0.05490196},
    BlackLizard = {0.1, 0.1, 0.1},
    Salamander = {14 / 15, 0.78039217, 76 / 85},
    CyanLizard = {0, 0.9098039, 46 / 51},
    SpitLizard = {0.55, 0.4, 0.2},
    ZoopLizard = {0.95, 0.73, 0.73},
    Overseer = {0, 0.9098039, 46 / 51},
    CicadaB = {0.36862746, 0.36862746, 37 / 85},
    CicadaA = {1, 1, 1},
    SpitterSpider = {58 / 85, 8 / 51, 0.11764706},
    Leech = {58 / 85, 8 / 51, 0.11764706},
    RedCentipede = {46 / 51, 0.05490196, 0.05490196},
    SeaLeach = {0.05, 0.3, 0.7},
    TubeWorm = {0.05, 0.3, 0.7},
    Centiwing = {0.05490196, 0.69803923, 0.23529412},
    BrotherLongLegs = {0.45490196, 0.5254902, 26 / 85},
    DaddyLongLegs = {0, 0, 1},
    VultureGrub = {0.83137256, 0.7921569, 37 / 85},
    EggBug = {0, 1, 0.47058824},
    BigNeedleWorm = {1, 0.59607846, 0.59607846},
    SmallNeedleWorm = {1, 0.59607846, 0.59607846},
    Hazer = {18 / 85, 0.7921569, 33 / 85}
}

local name_mappings = {
    DaddyLongLegs = "Daddy",
    Centipede = "Centipede1",
    GarbageWorm = "Garbageworm",
    GreenLizard = "Green_Lizard",
    YellowLizard = "Yellow_Lizard",
    BlackLizard = "Black_Lizard",
    WhiteLizard = "White_Lizard",
    LanternMouse = "Mouse",
    TubeWorm = "Tubeworm",
    Fly = "Bat",
    CicadaA = "Cicada",
    CicadaB = "Cicada",
}

function module.get_sprite_name(name)
    local mapped_name = name_mappings[name]
    if not mapped_name then
        local strlen = string.len(name)
        if strlen >= 6 and string.sub(name, strlen - 5) == "Lizard" then
            -- mapped_name = string.sub(name, 1, strlen - 6) .. "_Lizard"
            mapped_name = "Standard_Lizard"
        end

        if not mapped_name then
            print(string.format("no mapping for " .. name))
            return "Sandbox_QuestionMark.png"
        end
    end

    return "Kill_" .. mapped_name .. ".png"
end

function module.get_creature_color(name)
    local col = creature_colors[name]
    if col then
        return col[1], col[2], col[3]
    end

    return 0.5, 0.5, 0.5
end

return module