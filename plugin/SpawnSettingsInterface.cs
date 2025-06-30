using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace WavesMod;

class SpawnSettingsInterface : PositionedMenuObject, SelectOneButton.SelectOneButtonOwner
{
    readonly Menu.Remix.MenuTabWrapper tabWrapper;
    readonly Menu.Remix.MixedUI.OpScrollBox scrollBox;

    readonly Configurable<string> presetConfig;
    readonly FileSystemWatcher fsWatcher;
    readonly OpLabelLong descLabel;
    readonly OpComboBox comboBox;
    readonly SelectOneButton[] tabButtons;

    //SimpleButton randomTabButton;
    //SimpleButton presetTabButton;

    public WaveGeneratorType generatorType = WaveGeneratorType.Preset;
    public string presetName = WaveSpawnData.SelectedPreset;
    public event Action<SpawnSettingsInterface> SettingsChanged;

    public SpawnSettingsInterface(Menu.Menu menu, MenuObject owner)
        : base(menu, owner, new Vector2(440f, 385f))
    {
        
        const float boundsLeft = 359f + 20f;
        const float boundsRight = 707f - 20f;
        const float boundsTop = 579f;
        const float lineHeight = 30f;
        const float itemHeight = 24f;

        // ReinitInterface();
        // subObjects.Add(new MenuLabel(
        //     menu: menu,
        //     owner: owner,
        //     text: "Lorem ipsum dolor sit amet",
        //     pos: new Vector2(boundsLeft, boundsTop - 11),
        //     size: new Vector2(200f, 20f),
        //     bigText: false
        // ));

        tabButtons = new SelectOneButton[2];
        tabButtons[1] = new SelectOneButton(
            menu, this,
            displayText: "Preset", signalText: "GENERATORMODE",
            pos: new Vector2((boundsLeft + boundsRight) / 2 + 4f, boundsTop - 30f),
            size: new Vector2((boundsRight - boundsLeft) / 2f - 8f, 30f),
            buttonArray: tabButtons,
            buttonArrayIndex: 0
        );

        tabButtons[0] = new SelectOneButton(
            menu, this,
            displayText: "Random", signalText: "GENERATORMODE",
            pos: new Vector2(boundsLeft, boundsTop - 30f),
            size: new Vector2((boundsRight - boundsLeft) / 2f - 4f, 30f),
            buttonArray: tabButtons,
            buttonArrayIndex: 1
        );


        subObjects.Add(tabButtons[0]);
        subObjects.Add(tabButtons[1]);

        var rect = new RoundedRect(
            menu, this,
            pos: new Vector2(boundsLeft, 142),
            size: new Vector2(boundsRight - boundsLeft, boundsTop - 34f - 142f),
            filled: false
        );
        subObjects.Add(rect);

        tabWrapper = new Menu.Remix.MenuTabWrapper(menu, owner);
        subObjects.Add(tabWrapper);

        scrollBox = new OpScrollBox(
            pos: new Vector2(boundsLeft, 142),
            size: new Vector2(boundsRight - boundsLeft, boundsTop - 34f - 142f),
            contentSize: boundsTop - 34f - 142f,
            hasSlideBar: false,
            hasBack: false
        );
        scrollBox.ScrollLocked = true;
        float yPos = scrollBox.size.y - 8f - lineHeight;
        
        new Menu.Remix.UIelementWrapper(tabWrapper, scrollBox);

        var elements = new List<UIelement>();
        
        var label = new OpLabel(10f, yPos + 4f, "Preset: ");

        var presetList = FetchPresetList();
        var config = new Configurable<string>(presetName, new ConfigurableInfo(
            description: "Spawn preset to use.",
            acceptable: new ConfigAcceptableList<string>(presetList))
        );

        var itemList = new List<ListItem>(presetList.Length);
        for (int i = 0; i < presetList.Length; i++)
        {
            itemList.Add(new ListItem(presetList[i], i));
        }

        comboBox = new OpComboBox(config, new Vector2(label.pos.x + LabelTest.GetWidth(label.text) + 8f, yPos), 120f, itemList);

        config.OnChange += () =>
        {
            WavesMod.Instance.logger.LogInfo("selected preset changed to " + config.Value);
            WaveSpawnData.SelectedPreset = presetName = config.Value;
            descLabel.text = WaveSpawnData.Read(presetName).Description;
            SettingsChanged?.Invoke(this);
        };
        comboBox.OnChange += () =>
        {
            WavesMod.Instance.logger.LogInfo("selected preset changed to " + comboBox.value);
            WaveSpawnData.SelectedPreset = presetName = comboBox.value;
            descLabel.text = WaveSpawnData.Read(presetName).Description;
            SettingsChanged?.Invoke(this);
        };

        var button = new OpSimpleButton(
            pos: new Vector2(comboBox.pos.x + comboBox.size.x + 6f, yPos),
            size: new Vector2(LabelTest.GetWidth("Open Folder") + 12f, itemHeight),
            displayText: "Open Folder"
        );
        button.OnClick += (_) =>
        {
            System.Diagnostics.Process.Start(WaveSpawnData.PresetDirectory);
        };

        var descText = WaveSpawnData.Read(presetName).Description;
        yPos -= lineHeight;
        descLabel = new OpLabelLong(new Vector2(10f, yPos), new Vector2(scrollBox.size.x - 20f, 1f), descText, true, FLabelAlignment.Center);

        elements.Add(descLabel);
        elements.Add(label);
        elements.Add(comboBox);
        elements.Add(button);

        foreach (var elem in elements)
        {
            scrollBox.AddItemToWrapped(elem);
        }

        WavesMod.Instance.logger.LogInfo("create fs watcher");
        fsWatcher = new FileSystemWatcher(WaveSpawnData.PresetDirectory, "*.json");
        fsWatcher.NotifyFilter = NotifyFilters.FileName;

        fsWatcher.Created += (object sender, FileSystemEventArgs e) =>
        {
            // PresetDirectoryChanged?.Invoke();
            WavesMod.Instance.logger.LogInfo("preset was created: " + e.FullPath);

            var presetName = Path.GetFileNameWithoutExtension(e.FullPath);
            if (presetName.Equals(WaveSpawnData.DefaultPresetName, StringComparison.OrdinalIgnoreCase))
                return;
            comboBox.AddItems(true, new ListItem(presetName, comboBox._itemList.Length));
        };

        fsWatcher.Deleted += (object sender, FileSystemEventArgs e) =>
        {
            // PresetDirectoryChanged?.Invoke();
            WavesMod.Instance.logger.LogInfo("preset was deleted: " + e.FullPath);

            var presetName = Path.GetFileNameWithoutExtension(e.FullPath);
            if (presetName.Equals(WaveSpawnData.DefaultPresetName, StringComparison.OrdinalIgnoreCase))
                return;
            comboBox.RemoveItems(true, presetName);
        };

        fsWatcher.Renamed += (object sender, RenamedEventArgs e) =>
        {
            WavesMod.Instance.logger.LogInfo("preset was renamed: " + e.FullPath);

            var oldPresetName = Path.GetFileNameWithoutExtension(e.OldFullPath);
            var presetName = Path.GetFileNameWithoutExtension(e.FullPath);

            if (presetName.Equals(WaveSpawnData.DefaultPresetName, StringComparison.OrdinalIgnoreCase) ||
                oldPresetName.Equals(WaveSpawnData.DefaultPresetName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            comboBox.RemoveItems(true, oldPresetName);
            comboBox.AddItems(true, new ListItem(presetName, comboBox._itemList.Length));
        };
        
        fsWatcher.IncludeSubdirectories = false;
        fsWatcher.EnableRaisingEvents = true;
    }

    public int GetCurrentlySelectedOfSeries(string series)
    {
        switch (series)
        {
            case "GENERATORMODE":
                return (int)generatorType;
        }

        return -1;
    }

    public void SetCurrentlySelectedOfSeries(string series, int to)
    {
        switch (series)
        {
            case "GENERATORMODE":
                generatorType = (WaveGeneratorType)to;
                switch (generatorType)
                {
                    case WaveGeneratorType.Preset:
                        scrollBox.Show();
                        break;

                    case WaveGeneratorType.Randomizer:
                        scrollBox.Hide();
                        break;
                }
                SettingsChanged?.Invoke(this);
                break;
        }
    }

    string[] FetchPresetList()
    {
        return Directory.EnumerateFiles(WaveSpawnData.PresetDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .ToArray();
        
        // var listItems = new ListItem[paths.Length];
        // var i = 0;
        // foreach (var p in paths)
        // {
        //     listItems[i] = new ListItem(p, i);
        //     i++;
        // }
        
        // return listItems;
    }

    // public override void Update()
    // {
    //     base.Update();
    //     scrollBox.Update();
    // }

    // public override void GrafUpdate(float timeStacker)
    // {
    //     // base.GrafUpdate(timeStacker);
    //     // scrollBox.GrafUpdate(timeStacker);

    //     // foreach (var itm in scrollBox.items)
    //     // {
    //     //     itm.GrafUpdate()
    //     // }
    // }

    public void Shutdown()
    {
        WavesMod.Instance.logger.LogInfo("shutdown fs watcher");
        fsWatcher.Dispose();
    }
}