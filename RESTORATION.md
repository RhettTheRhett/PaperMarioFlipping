# Restored flipping prototype

The foundation is commit `c1efc5d` (December 30, 2025), which merges the
`no-cinemachine` branch ending at `f9fb825`. The original movement states,
jumping, gravity, camera methods, enemies, coins and curve code were recovered
from that snapshot. The older February controller is still in
`Assets/Scripts/OLD/PlayerController.cs`; its historical location at `5a39b88`
was `Assets/Scripts/PlayerScripts/PlayerController.cs`.

Before editing, the uncommitted scripts and scene were copied to
`RecoveryBackups/before-restoration-20260916-162104/`. That directory also
contains a Git patch and status listing. It is excluded from Git, so keep a
separate copy if you want to retain that rewrite after deleting this checkout.

## Fixes on top of the committed code

- `PlayerStateManager` reads E once in Update, before movement decisions.
  Both idle and movement now use the same flip, with one press per transition.
- `PlayerFlippingState` changes to the opposite dimension, suspends motion
  for the animation and restores gravity/constraints afterward. Facing changes
  no longer use the dimension-flip flag. Space cannot interrupt a flip.
- `PlayerDimension` adds depth/pane handling without replacing the movement
  states. Returning to 2D recenters on the current pane only when the entire
  depth corridor is clear. If blocked, it keeps the player's Z and retries
  after walking clear in 2D. It temporarily ignores overlapping
  walls created by projection, then restores collision once the player leaves.
  Supporting floors and ordinary walls remain solid.
- `HitboxExtender` applies changes synchronously when a flip completes, before
  the next physics step. It keeps its authored collider dimensions.
- The original camera methods now measure their deadzones relative to the
  desired offset, avoiding accumulated offset drift. The flip animation blends
  between their positions and rotations.
- Coin pickup uses the active state-machine controller rather than the old
  unused controller. Bat chase safely handles a missing target.

The pane scripts and world transition events are retained additions from the
uncommitted version, with fixes. Health and flip-meter scripts remain available
but are no longer automatically attached by the player controller.

## Try it

Open `Assets/Scenes/SampleScene.unity` to play the original level. E flips,
Space jumps, and WASD/arrow keys move. Flipping requires being grounded.
In 3D, W/S move along world X and A/D along world Z, as in the committed code.

Open `Assets/Scenes/DepthPaneDemo.unity` for a separate two-area example.
The green front area is at Z=0; the blue back area is at Z=12. Start on the
shared crossing at X=-7. Press E, hold A to move deeper toward the blue area,
then press E again. The selected area remains visible and collidable in 2D.
You can walk behind the blocks in 3D and flip back to test escaping their
projected hitboxes.

For another area, create a GameObject with `Pane`, put it at the desired world
Z, and parent its geometry under it. Set its thickness to cover that area's
depth. `Pane` automatically assigns child geometry to `PaneMember` at runtime.
Use distinct area centers. Nearest containing pane wins where slices overlap.
Keep connecting floors outside the pane roots if they should exist everywhere.
Set `PaneManager.graphicsRange` to zero to hide other areas in 2D; larger values
leave nearby areas visible as non-colliding scenery. Axis-aligned boxes project
through their pane; rotated objects keep their authored collision shapes.

## Verification

`Assets/Editor/FlipRegression.cs` runs the real player prefab in Unity Play Mode
inside an isolated copy of the project. It checks idle/moving flips, repeated
requests, depth constraints, projected-wall escape and re-entry collision,
pane visibility/collision, and scene startup. `DepthPaneDemo.Create` can rebuild
the demo using **Tools > Paper Mario > Create Depth Pane Demo**.

Batch entry point (run with Unity 2022.3.6f1, without `-quit`):

```text
Unity.exe -batchmode -projectPath <isolated-project-copy> -executeMethod FlipRegression.Run -logFile <log-path>
```

The runner creates the demo, performs checks, writes
`Logs/flip-regression-result.txt`, and exits with a nonzero code on failure.
With graphics enabled it also saves front/3D/back camera captures in `Logs`.
This verifies mechanics; matching the Wii game's presentation exactly still
requires an art/camera tuning pass and hands-on playtesting.

## Setting up your own scenes

**PlayerHolder:** this prefab was already in commit `6a63e42` (December 20,
2025). It packages the state-machine Player and CameraV3. Use one instance in
each playable scene, outside any Pane hierarchy. Avoid a second active camera,
AudioListener, or the older `player.prefab` controller alongside it.

**Shared crossing:** this is an ordinary floor cube added for the demo. It has
no pane membership, so it remains available in both dimensions. Replace it
with your own bridge/path, or omit it if the level needs no connection.

**Separated areas:** for example, put pane roots at Z=0 and Z=40 and leave each
thickness at 12. Their slices cover -6..6 and 34..46. Move the whole root so its
child geometry moves with it. Bridge the gap with actual floor geometry; the
pane thickness setting does not create ground. A connector centered at Z=20
with a Z size of 40 joins the centers. Keep that connector outside the pane
roots to make it shared. In gaps, the current implementation chooses the
nearest pane. Returning to 2D can recenter across the gap if the corridor is
clear; it does not create a special third area for the gap.

**Static objects:** put each object root directly under its Pane, with
`autoAssignChildren` enabled. A PaneMember will be added at startup, covering
its child renderers and colliders. Put walkable/blocking solids on the `ground`
layer. Box colliders aligned to world depth can project across the pane.
Spheres, meshes, and rotated colliders keep their original shape. Existing
HitboxExtender children still manage their own authored extended boxes.

**Pickups:** use trigger colliders and the existing CoinFlat/CoinFlipped tags
for this project's coin script. A depth-aligned box trigger is the simplest
projected pickup volume. Keep a rotating visual on a child separate from the
stable collider. Trigger volumes do not block recentering.

**Enemies:** PaneMember manages rendering and collision, not arbitrary AI.
Parent/assign an enemy to its pane, but its movement/attack code must also
respect the dimension and current pane if it should pause while hidden or
restrict depth movement in 2D. Existing bat AI has not been converted to a
general pane-aware enemy framework. Disable `extrudeHitbox` on its PaneMember
if its authored collider shape should be retained.

**Runtime spawning / nested groups:** auto-assignment runs once at startup.
For spawned actors, put PaneMember on their prefab and assign the owning pane.
For manual membership on nested actors, turn off autoAssignChildren on the
pane and assign one member per actor root; avoid overlapping members that both
control the same descendants. Disabling rendering/colliders does not disable
scripts or stop their coroutines.
