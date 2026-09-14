# Dribble physics regression checks

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
