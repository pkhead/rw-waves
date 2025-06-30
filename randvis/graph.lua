require("table.clear")

local CreatureSymbol = require("creature_symbol")

local Graph = {}
Graph.__index = Graph

local font = love.graphics.newFont("ProggyClean.ttf", 16)

local function calc_probability_weight(spawn_data, x)
    if x <= spawn_data.curvePeak then
       local p = (spawn_data.curvePeak - x) / (spawn_data.curvePeak - spawn_data.curveStart)
       return (spawn_data.peakWeight - spawn_data.startWeight) * math.pow(0.01, p*p) + spawn_data.startWeight
    else
        local p = (x - spawn_data.curvePeak) / (spawn_data.curveEnd - spawn_data.curvePeak)
       return (spawn_data.peakWeight - spawn_data.endWeight) * math.pow(0.01, p*p) + spawn_data.endWeight
    end
end

function Graph.new(w, h, data)
    local self = setmetatable({}, Graph)

    self.canvas = love.graphics.newCanvas(w, h)
    self.data = data
    self.width = w
    self.height = h

    self.view_x = 0
    self.view_y = 0
    self.scale_x = 40
    self.scale_y = self.height - 20

    self.active_index = nil
    self._points_tbl = {}

    return self
end

function Graph:resize(w, h)
    if self.width == w and self.height == h then return end
    
    self.canvas:release()
    self.canvas = love.graphics.newCanvas(w, h)
    self.width = w
    self.height = h
end

function Graph:update(dt)
    local pan_speed = 6
    if love.keyboard.isDown("lshift", "rshift") then
        pan_speed = 24
    end

    if love.keyboard.isDown("right") then
        self.view_x = self.view_x + pan_speed * dt
    end

    if love.keyboard.isDown("left") then
        self.view_x = self.view_x - pan_speed * dt
    end

    if self.view_x < 0 then
        self.view_x = 0
    end
end

function Graph:draw()
    love.graphics.push("all")
    love.graphics.reset()
    love.graphics.setCanvas(self.canvas)
    love.graphics.setFont(font)

    local view_l = math.floor(self.view_x)
    local view_r = math.ceil(self.view_x + self.width / self.scale_x)

    love.graphics.clear(0, 0, 0, 0)
    love.graphics.setColor(1, 1, 1)
    love.graphics.rectangle("line", 0, 0, self.width, self.height - 20)

    love.graphics.setColor(0.5, 0.5, 0.5)
    for c=view_l, view_r do
        local draw_x = (c - self.view_x) * self.scale_x
        love.graphics.line(draw_x, 0, draw_x, self.height - 20)
    end

    for i, spawn_data in ipairs(self.data) do
        local creature_name = spawn_data.creature
        if not creature_name then
            creature_name = spawn_data.creatures[1]
        end

        local cr, cg, cb = CreatureSymbol.get_creature_color(creature_name)
        love.graphics.setColor(cr, cg, cb, i == self.active_index and 1 or 0.3)

        local pts = self._points_tbl
        table.clear(pts)
        for x=view_l, view_r, 0.1 do
            local y = 1 - calc_probability_weight(spawn_data, x)
            pts[#pts+1] = (x - self.view_x) * self.scale_x
            pts[#pts+1] = (y - self.view_y) * self.scale_y
            -- if last_y then
            --     love.graphics.line(
            --         (last_x - self.view_x) * self.scale_x, (last_y - self.view_y) * self.scale_y,
            --         (x - self.view_x) * self.scale_x, (y - self.view_y) * self.scale_y
            --     )
            -- end

            -- last_x = x
            -- last_y = y
        end
        love.graphics.line(pts)
    end

    love.graphics.setColor(1, 1, 1)
    for c=view_l, view_r do
        love.graphics.print(tostring(c), (c - self.view_x) * self.scale_x, self.height - 20)
    end

    love.graphics.pop()
end

return Graph