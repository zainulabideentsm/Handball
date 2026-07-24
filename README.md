# Handball

A mobile-first third-person handball game built in Unity (URP), featuring aim-and-hold throwing, ball pickup/physics, and touch (joystick + look area) controls.

## Project structure
- `Assets/Handball game/Scripts/` — gameplay code:
  - `PlayerController.cs` — player movement
  - `PlayerAimController.cs` — aim-and-hold targeting
  - `PlayerThrowController.cs` — throw mechanic
  - `PlayerAnimationController.cs` — animation state driving
  - `PlayerBallPickup.cs` — ball pickup interaction
  - `BallController.cs` — ball physics
  - `TrajectoryPreviewController.cs` — throw trajectory preview
  - `ThirdPersonCameraFollow.cs` — third-person camera
  - `MobileLookArea.cs` — mobile touch look input
  - `GameManager.cs` — game/restart flow
- `Assets/Handball game/Scenes/SampleScene.unity` — main playable scene
- `Assets/Handball game/3d Character/`, `3d models/`, `Materials/`, `SkySeries Freebie/` — character, props, and environment art
- `Assets/Handball game/Joystick Pack/` — mobile virtual joystick UI
- `Assets/TextMesh Pro/` — TextMesh Pro package assets
- `Assets/InputSystem_Actions.inputactions` — Input System action bindings

## Controls
- Move via the on-screen joystick (or standard Input System bindings)
- Aim by holding, release to throw
- Pick up the ball by moving into it
- Restart via the on-screen restart button

See [CHANGELOG.md](CHANGELOG.md) for the history of changes to this project.