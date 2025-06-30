love.graphics.setDefaultFilter("nearest")

-- setup cpath
local extension = jit.os == "Windows" and "dll" or jit.os == "Linux" and "so" or jit.os == "OSX" and "dylib"
package.cpath = string.format("%s;./?.%s", package.cpath, extension)
print(package.cpath)

local imgui = require("cimgui")
local JSON = require("json")
local Sprite = require("sprite")
local CreatureSymbol = require("creature_symbol")
local ffi = require("ffi")
local Graph = require("graph")
local tmp_int = ffi.new("int[1]", 0)
local tmp_float2 = ffi.new("float[2]", 0)
local tmp_str_len = 128
local tmp_str = ffi.new("char[?]", tmp_str_len)

local selected_creature_idx = 1
local data
local graph
local file_path
local is_new = false
local sprite = Sprite.load("uiSprites.json")

function love.load(args)
    if args[1] == nil then
        error("no input json specified!")
    end

    file_path = args[1]
    local f = io.open(file_path, "r")
    if f then
        is_new = false
        local file_data = f:read("*a")
        f:close()

        data = JSON.decode(file_data)
    else
        is_new = true
        data = {
            {
                -- creature = "CicadaA",
                creatures = {
                    "PinkLizard",
                },

                points = 1,
                max = 4,
    
                startWeight = 0,
                curveStart = 3,
    
                peakWeight = 1,
                curvePeak = 4,
    
                endWeight = 0.5,
                curveEnd = 8
            }
        }
    end

    for _, spawn in ipairs(data) do
        if spawn.creature and not spawn.creatures then
            spawn.creatures = {spawn.creature}
            spawn.creature = nil
        end
    end

    graph = Graph.new(400, 300, data)
    
    imgui.love.Init()
end

function love.quit()
    return imgui.love.Shutdown()
end

function love.update(dt)
    imgui.love.Update(dt)
    imgui.NewFrame()

    graph:update(dt)
end

local function save_file()
    local output = {}
    local function append(...)
        for i=1, select("#", ...) do
            local v = select(i, ...)
            table.insert(output, v)
        end
    end

    append("[\n")
    for i, spawn in ipairs(data) do
        if i > 1 then
            append(",")
        end
        append("\n")

        append("    {\n")

        if #spawn.creatures == 1 then
            append("        \"creature\": \"", spawn.creatures[1], "\",\n")
        else
            append("        \"creatures\": {")
            for j, name in ipairs(spawn.creatures) do
                if j > 1 then
                    append(", ")
                end

                append(name)
            end
            append("},\n")
        end

        append("        \"points\": ", spawn.points, ",\n")
        append("        \"max\": ", spawn.max, ",\n")
        append("        \n")
        append("        \"curveStart\": ", spawn.curveStart, ",\n")
        append("        \"startWeight\": ", spawn.startWeight, ",\n")
        append("        \"curvePeak\": ", spawn.curvePeak, ",\n")
        append("        \"peakWeight\": ", spawn.peakWeight, ",\n")
        append("        \"curveEnd\": ", spawn.curveEnd, ",\n")
        append("        \"endWeight\": ", spawn.endWeight, "\n")

        append("    }")
    end
    append("\n]\n")

    local f = assert(io.open(file_path, "w"), "could not open file")
    f:write(table.concat(output))
    f:close()
end

function love.draw()
    love.graphics.setColor(1, 1, 1)
    
    -- imgui.ShowDemoWindow()
    local viewport = imgui.GetMainViewport()
    imgui.SetNextWindowPos(viewport.WorkPos)
    imgui.SetNextWindowSize(viewport.WorkSize)
    if imgui.Begin("test", nil, bit.bor(imgui.ImGuiWindowFlags_MenuBar, imgui.ImGuiWindowFlags_NoTitleBar, imgui.ImGuiWindowFlags_NoMove, imgui.ImGuiWindowFlags_NoResize)) then
        if imgui.BeginMenuBar() then
            if imgui.MenuItem_Bool("Save") then
                save_file()
                is_new = false
            end

            if is_new then
                imgui.TextDisabled(file_path .. " [New]")
            else
                imgui.TextDisabled(file_path)
            end

            imgui.EndMenuBar()
        end
        imgui.SeparatorText("Probability Weight Graph")

        local avail = imgui.GetContentRegionAvail()
        graph:resize(avail.x, 300)
        graph:draw()
        imgui.Image(graph.canvas, imgui.ImVec2_Float(graph.canvas:getDimensions()))

        imgui.SeparatorText("Config")

        imgui.BeginGroup()
        imgui.Text("Spawns")
        if imgui.BeginListBox("##Spawns", imgui.ImVec2_Float(imgui.GetFontSize() * 12, imgui.GetFontSize() * 20)) then
            for i, spawnData in ipairs(data) do
                imgui.PushID_Int(i)

                local name
                if #spawnData.creatures > 1 then
                    name = spawnData.creatures[1] .. ", et al."
                else
                    name = spawnData.creatures[1] or "?"
                end

                if imgui.Selectable_Bool(name, i == selected_creature_idx) then
                    selected_creature_idx = i
                end

                imgui.PopID()
            end
            imgui.EndListBox()
        end
        
        if imgui.Button("Add", imgui.ImVec2_Float(imgui.GetFontSize() * 12, 0)) then
            table.insert(data, {
                -- creature = "CicadaA",
                creatures = {},

                points = 1,
                max = 4,
    
                startWeight = 0,
                curveStart = 3,
    
                peakWeight = 1,
                curvePeak = 4,
    
                endWeight = 0.5,
                curveEnd = 8
            })

            selected_creature_idx = #data
        end
        imgui.EndGroup()

        if data[selected_creature_idx] then
            imgui.PushItemWidth(imgui.GetFontSize() * 9)
            local drag_speed = 0.05
            local spawn_data = data[selected_creature_idx]
            
            imgui.SameLine()
            imgui.BeginGroup()

            local want_delete = imgui.Button("Delete")

            if imgui.BeginListBox("Creatures", imgui.ImVec2_Float(imgui.GetFontSize() * 9, imgui.GetFontSize() * 5)) then
                local idx_to_remove = nil
                for i, creature_name in ipairs(spawn_data.creatures) do
                    imgui.PushID_Int(i)
                    if imgui.Selectable_Bool(creature_name, false) then
                        idx_to_remove = i
                    end
                    imgui.PopID()
                end

                if imgui.Button("Add", imgui.ImVec2_Float(-0.0001, imgui.GetFontSize())) then
                    imgui.OpenPopup_Str("AddCreature")
                    tmp_str[0] = 0
                end

                if imgui.BeginPopup("AddCreature") then
                    imgui.InputText("##Name", tmp_str, tmp_str_len)
                    if imgui.IsItemDeactivatedAfterEdit() then
                        table.insert(spawn_data.creatures, ffi.string(tmp_str))
                        imgui.CloseCurrentPopup()
                    end

                    imgui.EndPopup()
                end

                if idx_to_remove then
                    table.remove(spawn_data.creatures, idx_to_remove)
                end

                imgui.EndListBox()
            end

            -- for i, creature_name in ipairs(creatures) do
            --     if i > 1 then
            --         imgui.SameLine()
            --     end

            --     local frame = sprite.frames[CreatureSymbol.get_sprite_name(creature_name)]
            --     local tex_w, tex_h = frame.quad:getTextureDimensions()
            --     local x, y, w, h = frame.quad:getViewport()
            --     local cr, cg, cb = CreatureSymbol.get_creature_color(creature_name)
                
            --     imgui.ImageButton(
            --         creature_name,
            --         sprite.texture,
            --         imgui.ImVec2_Float(w, h),
            --         imgui.ImVec2_Float(x / tex_w, y / tex_h),
            --         imgui.ImVec2_Float((x+w) / tex_w, (y+h) / tex_h),
            --         imgui.ImVec4_Float(0, 0, 0, 0),
            --         imgui.ImVec4_Float(cr, cg, cb, 1.0)
            --     )
            -- end

            tmp_int[0] = spawn_data.points
            if imgui.DragInt("Points", tmp_int, drag_speed, 0, 100) then
                spawn_data.points = tmp_int[0]
            end

            tmp_int[0] = spawn_data.max
            if imgui.DragInt("Max", tmp_int, drag_speed, 1, 100) then
                spawn_data.max = tmp_int[0]
            end

            drag_speed = 0.02

            tmp_float2[0] = spawn_data.curveStart
            tmp_float2[1] = spawn_data.startWeight
            if imgui.DragFloat2("Start", tmp_float2, drag_speed) then
                spawn_data.curveStart = tmp_float2[0]
                spawn_data.startWeight = tmp_float2[1]
            end

            tmp_float2[0] = spawn_data.curvePeak
            tmp_float2[1] = spawn_data.peakWeight
            if imgui.DragFloat2("Peak", tmp_float2, drag_speed) then
                spawn_data.curvePeak = tmp_float2[0]
                spawn_data.peakWeight = tmp_float2[1]
            end

            tmp_float2[0] = spawn_data.curveEnd
            tmp_float2[1] = spawn_data.endWeight
            if imgui.DragFloat2("End", tmp_float2, drag_speed) then
                spawn_data.curveEnd = tmp_float2[0]
                spawn_data.endWeight = tmp_float2[1]
            end
            
            imgui.PopItemWidth()
            imgui.EndGroup()

            if want_delete then
                table.remove(data, selected_creature_idx)
                selected_creature_idx = selected_creature_idx - 1
            end
        end
    end
    imgui.End()

    imgui.Render()
    love.graphics.setColor(1, 1, 1)
    imgui.love.RenderDrawLists()
end

function love.mousemoved(x, y, ...)
    imgui.love.MouseMoved(x, y)
end

function love.mousepressed(x, y, btn)
    imgui.love.MousePressed(btn)
end

function love.mousereleased(x, y, btn)
    imgui.love.MouseReleased(btn)
end

function love.wheelmoved(x, y)
    imgui.love.WheelMoved(x, y)
end

function love.keypressed(key)
    imgui.love.KeyPressed(key)
end

function love.keyreleased(key)
    imgui.love.KeyReleased(key)
end

function love.textinput(t)
    imgui.love.TextInput(t)
end