# Handball

A mobile-first third-person handball game built in Unity (URP), featuring aim-and-hold throwing, ball pickup/physics, jumping, hoop scoring with goal celebrations, and touch (joystick + look area) controls.

## Project structure
- `Assets/Handball game/Scripts/` — gameplay code:
  - `PlayerMovementController.cs` — player movement, jump (coyote time/jump buffering), gravity, aim-movement mode
  - `PlayerAimController.cs` — aim-and-hold targeting
  - `PlayerThrowController.cs` — throw mechanic
  - `PlayerAnimationController.cs` — animation state driving
  - `PlayerAnimationEventRelay.cs` — relays animation events (`AE_AttachBall`, `AE_ReleaseBall`) to ball pickup/throw
  - `PlayerBallPickup.cs` — ball pickup interaction
  - `BallController.cs` — ball physics
  - `TrajectoryPreviewController.cs` — throw trajectory preview
  - `ThirdPersonCameraFollow.cs` — third-person camera (incl. goal-scored camera shake)
  - `MobileLookArea.cs` — mobile touch look input
  - `GameManager.cs` — game/restart flow
  - `HoopGoalDetector.cs` — per-hoop dual-trigger goal detection (arm → confirm → ball reset)
  - `GoalTriggerRelay.cs` — relays a hoop's entry/score trigger colliders to its `HoopGoalDetector`
  - `ScoreManager.cs` — score tracking and TMP score display
  - `GoalCelebrationController.cs` — goal UI pop/fade animation and confetti/camera-shake celebration
  - `Player Scripts/`, `UI Scripts/` — currently empty, reserved for future reorganization
- `Assets/Handball game/Scenes/SampleScene.unity` — main playable scene (player, ball, three `Hoop` targets, boundaries, gameplay UI canvas)
- `Assets/Handball game/3d Character/`, `3d models/`, `Materials/`, `Prefabs/` — character, props, and environment art
- `Assets/Particle/`, `Assets/Hovl Studio/`, `Assets/JMO Assets/` — particle/VFX assets (confetti, hit effects)
- `Assets/Handball game/Joystick Pack/` — mobile virtual joystick UI
- `Assets/TextMesh Pro/` — TextMesh Pro package assets
- `Assets/InputSystem_Actions.inputactions` — Input System action bindings

## Controls
- Move via the on-screen joystick (or standard Input System bindings)
- Aim by holding, release to throw
- Pick up the ball by moving into it
- Jump via the on-screen jump button
- Score by throwing the ball through one of the hoops — triggers a goal celebration (confetti, camera shake, score UI pop) and resets the ball
- Restart via the on-screen restart button

See [CHANGELOG.md](CHANGELOG.md) for the history of changes to this project.