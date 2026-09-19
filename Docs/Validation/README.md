# Dribble physics regression checks

## Corner pursuit regression (2026-09-19)

The previous chaser target clamped z to +/-16, leaving a grounded corner ball
2.023 units away, outside the 1.45-unit acquisition radius. The target now uses
the field collider bounds minus the player's collider extent and a 0.04-unit
wall margin. Chasers retain reduced lateral separation so crowd spacing cannot
cancel their approach; receiving targets use the same pitch boundary.

Copy `CornerRecoveryPlayCheck.cs` to the isolated project's `Assets`, and run
`GoalImpactPlayCheckBuilder.RunCorners3` / `RunCorners5` in batch play mode.
The old code failed to recover the first corner ball after four seconds. The
fix passed all 16 real-physics cases: four corners, with and without a nearby
teammate, in both match sizes. Each defender recovered possession in 0.24 seconds
from the regression starting position. These checks cover stationary loose balls
with active teams, field walls and player collisions, not every contested tackle.

## Missed edge goals regression (2026-09-19)

The previous full-line check rejected a valid diagonal shot into the side net:
the frame is at z=17.7, while the back edge of the painted line is at z=17.91.
A ball can clear the frame and then move sideways or upward before its trailing
edge clears the line. `GoalTrigger` now records legal entry through the frame
and retains that entry until full line clearance or a return to the field.
The goal-line threshold has no added tolerance. Frame contact allows 0.02 units
for physics contact margins.

`GoalScoringChecks.Run` reproduces the old diagonal rejection and passes 57
checks with the fix, including both side nets, rising shots, pending crossings,
return-to-field reset, illegal side entries, and the earlier scoring checks.
Copy `GoalMissPlayCheck.cs` to the isolated project's `Assets` and execute
`GoalImpactPlayCheckBuilder.RunMissedGoals` for live physics coverage: right/left
diagonals and rising shots at each goal, with 0.02 and 0.01 second physics steps.
The check uses a 5v5 match with player interference disabled to isolate scoring.

## Whole-ball goal scoring and net impact (2026-09-19)

`GoalScoringChecks.Run` passed 43 checks in the isolated Unity project, including
line contact/partial crossings, full crossings in both directions, opening bounds,
high-speed crossings and same-step net rebounds, duplicate prevention, momentum
through celebration, pause/resume, restart, mesh isolation and spring recovery.
Copy `GoalScoringChecks.cs` to `Assets/Editor` with the current runtime and goal
builder scripts, prototype scene, materials and athlete resources.

For live collision validation, copy `GoalImpactPlayCheck.cs` to `Assets` and
`GoalImpactPlayCheckBuilder.cs` to `Assets/Editor`, then execute
`GoalImpactPlayCheckBuilder.Run` in batch mode with GPU rendering enabled.
This starts a 5v5 match, isolates shooting from player interference, and fires
30-unit/s shots into both goals. Both scored once; the net deflected 0.291 units,
the ball continued moving during the two-second celebration, and the north goal
transitioned through countdown to the next kickoff before testing the south goal.
`north-goal-impact.png` and `south-goal-impact.png` were visually inspected.
Previews use the validation project's built-in lighting; the game uses URP.

## Possession recovery regression (2026-09-19)

Copy `PossessionRecoveryChecks.cs` to the isolated project's `Assets/Editor`,
along with current runtime scripts and the existing athlete resources. Run
`-batchmode -nographics -executeMethod PossessionRecoveryChecks.Run`.
The original center-height comparison failed on the first grounded pass with
the 0.3-diameter ball. Comparing ball-bottom and player-foot heights passed
all 135 assertions: three pass/shot/recovery cycles for both 3v3 and 5v5 at
diameters 0.3 and 0.5, plus standalone dribble release/reacquisition. Checks
cover control handoff, release cooldown, AI movement and dribble after recovery,
fast-shot rejection, and airborne-ball rejection. They use deterministic ball
arrivals and manual physics stepping, not complete trajectories or live input.

## Ball and goal appearance (2026-09-19)

`GoalAppearanceChecks.cs` runs with `-executeMethod GoalAppearanceChecks.Run` in
the isolated validation project, with the prototype scene, material assets,
runtime scripts (including their `.meta` files), `FutsalGoalGeometry.cs`,
`FutsalStarterSceneBuilder.cs`, and `FutsalBallSetup.cs` copied in. Include the
cached URP packages to preserve scene component references and omit `-nographics`.
Unity 6000.3.23f1 compiled successfully and all 14 checks passed: ball collider
diameter 0.3, baked nets and post colliders, floor extensions, open goal mouths,
back-net collision on both ends, and repeatable scene updates without duplicates.
`goal-preview.png` was rendered and visually inspected using the validation
project's built-in pipeline. It shows editor capsules; the main game's player
models are initialized at runtime. Final URP lighting and gameplay feel still
need an in-game playtest.

Run from PowerShell with Unity 6000.3.23f1 installed:

```powershell
.\Docs\Validation\RunDribbleChecks.ps1
```

The runner creates a separate temporary Unity project, copies runtime scripts,
and uses the installed Input System and uGUI packages from the game's cache.
It does not close or edit the scene in the main Unity instance. It retains the
temporary project and log for inspection. The C# checks invoke the actual motor
and dribble logic with controlled inputs and simulate real PhysX rigidbodies.

## Verified 2026-09-14

Unity batch compilation and all 14 scenarios passed (7 cases at 0.02 and 0.01
second physics steps). Ball mass is 0.43 and linear damping 0.22, matching the
prototype. Distances below are horizontal player-center to ball-center distances
in Unity units, not distance from a visible foot.

| Scenario | Maximum distance across both steps | Minimum speed during first 0.3s of cut |
| --- | --- | --- |
| Walk straight | 0.950 | 7.000 |
| Sprint straight | 1.158 | 9.500 |
| Sprint, 45-degree cut | 1.158 | 7.754 |
| Sprint, 90-degree cut | 1.158 | 4.892 |
| Sprint, 180-degree reversal | 1.158 | 0.234 |
| Repeated zigzags | 1.158 | 4.266 |
| Sprint then stop | 1.179 | 0.000 |

Checks also assert gradual starts, stopping, reversal braking, valid velocity,
and no dribble velocity change immediately after releasing a pass.

## Limits and remaining playtests

The automated setup constrains vertical motion and omits the floor, walls,
opponents, team possession arbitration, render interpolation and device input.
It checks locomotion/control behavior, not final feel or animation quality.
Repeat the manual checks in `../MobilePlaytest.md` in both match modes, especially
wall contact, tackling, receiving a pass at speed, and shooting mid-turn.
The athlete visual now has locomotion and kick clips. Animation-synchronized ball
contacts have not been implemented.

## Athlete visual validation

`AthleteVisualChecks.cs` runs in the same isolated project with the athlete FBX,
textures and `FutsalAthleteImporter.cs` copied in. Execute
`AthleteVisualChecks.Run` with GPU rendering enabled (omit `-nographics`).
The final check verified the three preview characters, all four animation clips,
hidden original capsules and exactly one existing collider per character.
Team initialization also passed for 3v3 (six players) and 5v5 (ten players),
including two goalkeepers per match, five skinned renderers and one rigidbody
and collider per player. Kick playback and formation reset completed successfully.
The imported character uses 20,925 vertices across five skinned renderers.
Idle height measured 1.871 units and the lowest point was -0.019 units.
The built-in-pipeline preview is `athlete-unity-preview.png`; the main game uses
URP, so final lighting can differ. Mobile GPU performance has not been measured.
