# Wave Preset Configuration
Wave spawn data is specified in a JSON format. Below is the format's specifications in a TypeScript-esque format:
```
root: WaveData[]            data for each wave.

type WaveData = {
    min?: int               minimum number of creatures that will spawn. required if "amount" is not used.
    max?: int               maximum number of creatures that will spawn. required if "amount" is not used.
    amount?: int            sets min and max to this value. do not use this with min & max.
    spawns: WaveSpawn[]     list of creatures to spawn.
}

type WaveSpawn = {
    type: string            name of the creature template. ex: PinkLizard, GreenLizard, Scavenger
    ids?: int[]             list of IDs to randomly choose from.
    modifiers?: string[]    list of spawn modifiers to apply. there are currently only two:
                                NoSkyExit:      spawn this creature if and only if there are no sky exits in the level.
                                                note that creatures that require a sky exit (i.e. vultures) will not spawn
                                                if the level has no sky exit, so this modifier is intended to be used for
                                                substituting vultures in enclosed levels.

                                RandomSpawn:    if a wave needs to spawn more creatures than the number of creatures in the
                                                spawn list that do not have the RandomSpawn modifier, the remaining creatures
                                                will be randomly picked from the collection of creatures that have this
                                                modifier.
}
```

# Wave Randomizer Configuration
Creature generation works with two systems: a points system and a probability weight system.

Each wave has a certain number of points assigned to it for the purpose of spawning enemies.
Each Spawn also has a certain number of points assigned to it, which dictates both the minimum
number of points required to spawn it and the number of points deducted from the wave generator's remaining points
upon being spawned. The wave generator will spawn creatures until it runs out of points to spawn creatures with.

Additionally, each Spawn has a probability weight assigned to it per wave. The creature with the highest
probability weight in a given wave has the highest probability of being spawned, and vice versa the creature
with the lowest probability weight has the lowest probability of being spawned. The probability weight is not
an absolute value, and only has meaning relative to the probability weights of the other creatures. Additionally,
these probability weights are not specified in a table per wave, but rather through parameters to a continuous function.

## JSON format
```
root: Spawn[]

type Spawn = {
    creatures?: string[]        array of creatures to spawn. must not be present if "creature" is.
    creature?: string           the singular creature to spawn. must not be present if "creatures" is.

    points: int                 the number of points this Spawn will deduct upon spawning.
    max?: int                   the maximum number of this Spawn allowed in a wave.

    // probability weight fields (read Probability Weight Formula section)
    center: float
    variance: float
    peak?: float

    constant?: float
    constantL?: float
    constantR?: float
}
```

## Probability Weight Formula
$$
p(x) = \begin{cases}
        (m - c_0) e^{-\frac{(x-\mu)^2}{2\sigma^2}} + c_0, & x\leq c \\
        (m - c_1) e^{-\frac{(x-\mu)^2}{2\sigma^2}} + c_1, & x\gt c \\
    \end{cases}
$$

where:
- $x$: wave number starting from 0.
- $\mu$: "center" field.
- $\sigma^2$: "variance" field.
- $m$: "peak" field.
- $c_0$: "constantL" or "constant" field.
- $c_1$: "constantR" or "constant" field.
- $e$: Euler's number.

note that:
- a $p(x)$ value of less than 0.01 (1%) will be flushed to 0.
- probability weights are not absolute values; they only have meaning relative to the probability weights of other creatures.

playground: https://www.desmos.com/calculator/pcjo2sd6f2