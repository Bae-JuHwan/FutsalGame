# Athlete source

`Athlete.blend` is the editable source for `Assets/Resources/Players/Athlete.fbx`.
It contains the clothed, rigged character, four in-place animation actions and
a studio preview setup. Export only the rig and its five mesh children, excluding
the preview camera, floor and lights. Keep the game root's collision capsule.

`BuildAthlete.py` recreates it using Blender 4.4.3 and MPFB. The external authoring
workspace used for this version is `C:\Users\0412j\4_2\FutsalArtWork`. It contains
`mpfb2-master/src/mpfb` and the extracted MakeHuman system assets under `system`.
The external applications and complete asset packs are not part of this repo.

```powershell
& 'C:\Users\0412j\4_2\FutsalArtWork\blender-4.4.3-windows-x64\blender.exe' --background --python .\ArtSource\BuildAthlete.py -- C:\Users\0412j\4_2\FutsalArtWork
```

Outputs go to the workspace's `export` folder. Copy the FBX and PNG textures to
`Assets/Resources/Players` after inspecting `athlete-preview.png`.
Asset sources and CC0 license information are recorded in
`Assets/Resources/Players/SOURCES.md`.

This is one realistic-proportion base character with runtime team/keeper colors.
Animations are procedural authoring curves, not motion capture. Contact timing,
foot placement and more faces/hairstyles remain future visual polish work.
