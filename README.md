# Godot Flow Field Pathfinding (C#)

This project combines **Flow Field Navigation** with **Boids** for a swarm movement system. It allows large amounts of units to efficiently navigate to the target while avoiding/aligning one another.

The approach in this project would be suitable for an RTS or Vampire Survivors game.

![Alt text](/screenshot/screenshot.png?raw=true "Example")

# Features

- Boids implementation for alignment/cohesion/seperation
- Flow-field generation for generating a path from all possible points on a map to a single location
- A simple Camera2D implementation for an RTS game
- Lightweight avoidance of obstacles


# Gotchas

- Because this project is an example, it chooses clarity over performance. When using this implementation for thousands of agents, I've had success by:
    - Minimising interaction with Godot engine. C# glue is a bottleneck at times. Try to read Node variables once per frame (e.g. Position) and store it in a C# variable.
    - Same goes for setting values. Don't set a sprite offset every frame for example, only set it when it changes.
    - Don't use the 2d physics engine (RigidBody2D and Area2D). These are way too heavy when hundreds of agents.
        - Suggestion: Split entire map into sectors and only check if agent is nearby to agents within its own sector.
        - Note: RigidBody2D and Area2D are only used in this example for neighbour detection. We avoid collisions altogether.

- Unit overlap does occur. I consider this acceptable for my use-case, where I prefer sufficient seperation to be visually clear but enough leniency to keep movement fluid.
    - Obstacle overlap can occur as well in some situations, but quickly resolves itself.

- Flow fields are not cheap to dynamically generate for each individual route (~10ms per call). They're only really efficient for large swarms or if precomputed.

# TODO

- Investigate efficient ways to reduce overlap and jitter
- Use floats to have more gradual pathing

# Credits

- Leif Erkenbrach's
    - Excellent explanation and example of the algorithm
    - https://leifnode.com/2013/12/flow-field-pathfinding/
- kyrick
    - Godot Boids
    - https://github.com/kyrick/godot-boids
