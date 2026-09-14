# Athlete model sources

The character is an original configured athlete derived from MakeHuman's CC0
core assets. It is not a likeness or asset extracted from a commercial football game.

- MakeHuman/MPFB basemesh, macro targets and game_engine rig:
  https://github.com/makehumancommunity/mpfb2
- MakeHuman system asset pack (CC0):
  https://static.makehumancommunity.org/assets/assetpacks/makehuman_system_assets.html
- Core asset license explanation:
  https://static.makehumancommunity.org/makehuman/faq/are_makehuman_files_free.html
- CC0 terms: https://creativecommons.org/publicdomain/zero/1.0/

Used assets: young_caucasian_male skin (young_lightskinned_male_diffuse),
male_casualsuit06 (tailored into shirt and shorts), short04 hair,
low-poly eyes with brown_eye texture, shoes06. System asset source headers
credit Data Collection AB, Joel Palmius and Jonas Hauquier and state that
the assets were released as CC0 in September 2020.

The kit tailoring, socks, material setup and in-place Idle/Jog/Sprint/Kick
animation curves were created for this project. Animations are authored
procedurally, not captured football motion. No third-party application code
is included in the runtime asset. MPFB/Blender are external authoring tools.

Textures are shared across players. Team and goalkeeper kit colors are applied
at runtime. The gameplay capsule remains as an invisible collider; the imported
model does not change the movement or dribble physics.
