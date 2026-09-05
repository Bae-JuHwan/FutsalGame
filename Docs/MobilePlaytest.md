# Mobile match flow checks

Open `Assets/Scenes/FutsalPrototype.unity` and enter Play mode. The match
manager creates the match HUD and menus at runtime; no scene wiring is required.
UI labels currently use English, matching the existing touch controls.

## Editor / Device Simulator

- Move, sprint and kick with the existing touch controls. Check the timer,
  score and stamina bar. Keyboard controls remain WASD/arrows, Shift and Space.
- A match now starts with a 3-2-1 countdown as 3v3: two field players and one
  goalkeeper per team. The match timer must remain stopped during countdowns.
  PASS selects the teammate closest to the movement/facing direction, including
  a backward pass to the goalkeeper; the keyboard shortcut is E and
  the gamepad shortcut is the west/left face button. Control and the yellow ring
  should transfer only when a home player actually receives or wins the ball.
- Passes travel at 14.5 units per second. Verify short and cross-court passes are
  controllable and reach the receiver without feeling sluggish.
- While a home player owns the ball, change the stick direction. A cyan ring
  should move to the teammate selected for PASS; the yellow ring must remain on
  the controlled player. Backward aim should allow a goalkeeper back-pass.
- Hold KICK and watch the power bar below the button. A quick release should
  produce a controlled low shot; holding for about 0.9 seconds should produce the
  fastest, highest shot. Shot speed now ranges from 16 to 32, and the ball speed
  cap is 34, so minimum and maximum charge should be clearly distinguishable.
- The stick direction at release determines shot direction. Inputs generally
  facing the attacking goal receive a gradual goal/corner aim assist; deliberate
  sideways or backward inputs remain unassisted. With no direction held, the shot
  should aim at the center of the opponent's goal.
- While defending, approach the opponent from the front and press DEF to tackle.
  The keyboard shortcut is F and the gamepad shortcut is the north/top face
  button. Tackles outside the 1.75-unit range or behind the defender should miss,
  and repeated taps should respect the 0.75-second cooldown.
- When the away team has the ball, control should move to the nearest home field
  player. If the home goalkeeper wins the ball, control should move to the keeper
  until a pass is completed. The camera must follow every control transfer.
- Watch off-ball players during attacks and defensive transitions. They should
  preserve useful lanes and move apart instead of overlapping in a single clump.
- While dragging the stick, press PAUSE with another pointer on a touch device.
  The timer, ball, player, defender and goalkeeper should stop. Release all
  touches, then press RESUME: no movement or kick should remain held, and the
  stick should be centered. Repeat while holding RUN and KICK.
- Score in each goal. Each entry should add exactly one point, freeze the scene
  for 1.1 seconds without consuming match time, then reset the ball, player,
  defender and goalkeeper. Scoring at the south goal displays RIVAL GOAL.
- Pause during the goal message. Wait longer than 1.1 seconds and resume:
  the remaining celebration delay should finish before play resumes.
- Let the 60-second timer expire. Verify the result and score, frozen gameplay,
  hidden movement controls and working RESTART button. Repeat several matches
  and check that scores/time/stamina reset and the game runs at normal speed.
- Press Escape to pause/resume and R after the match to verify PC shortcuts.
- Check both landscape orientations and a notched device in Device Simulator.
  HUD, menu and touch controls should stay within the safe area.

## Android / iOS device

- Repeat the multitouch checks on hardware; mouse testing cannot establish them.
- While moving or during a goal message, background the app or lock the screen.
  On returning, it should remain paused until RESUME is tapped, with no lost
  match time and no stuck touch input.
- Rotate between landscape orientations and confirm controls remain reachable.
  Portrait autorotation is disabled for this two-handed control layout.

Compilation alone does not verify rendering, touch behavior or device lifecycle.
These runtime checks must be completed in Unity and on a target phone.
