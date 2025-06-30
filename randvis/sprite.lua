local spritelib = {}
local jsonlib = require("json")

---@class sprite.Frame
---@field quad love.Quad
---@field ox number
---@field oy number

---@class sprite.Sprite
---@field texture love.Image
---@field frames sprite.Frame[]
local Sprite = {}
Sprite.__index = Sprite

function spritelib.load(jsonPath)
    local sprite = setmetatable({}, Sprite)
    sprite.frames = {}

    local json = jsonlib.decode(love.filesystem.read(jsonPath))
    local tex = love.graphics.newImage(json.meta.image) -- todo: don't assume root path?

    sprite.texture = tex
    local tex_w = tex:getWidth()
    local tex_h = tex:getHeight()

    for name, frame in pairs(json.frames) do
        local q = frame.frame
        sprite.frames[name] = {
            quad = love.graphics.newQuad(q.x, q.y, q.w, q.h, tex_w, tex_h),
            ox = frame.spriteSourceSize.x,
            oy = frame.spriteSourceSize.y
        }
    end

    return sprite
end

function Sprite:drawFrame(frame, x, y, ...)
    love.graphics.draw(self.texture, frame.quad, (x or 0) + frame.ox, (y or 0) + frame.oy, ...)
    -- love.graphics.draw(self.texture, frame.quad, x, y, ...)
end

function Sprite:draw(frame_name, x, y, ...)
    return self:drawFrame(self.frames[frame_name], x, y, ...)
end

spritelib.draw = Sprite.draw
spritelib.drawFrame = Sprite.drawFrame

return spritelib