# Changelog

All notable changes to the Handball project are documented in this file.

## [Unreleased]
Pending changes in the working tree, not yet committed.

### Added
- New animation assets: `Animator Back up.controller`, `UpperBodyMask.mask` (upper-body animation layer support).
- New art/model assets: `Low Poly Red Hoop.fbx`, `dance_pole.fbx`, `3d models/ice-gun/`, `3d models/red-hoop/`, additional UI arrow PNGs.

### Changed
- `PlayerMovementController.cs`, `PlayerAnimationController.cs`, `PlayerBallPickup.cs` — further tuning.
- `Animator.controller`, `Materials/AimTrajectory.mat`, `Assets/Handball game/Scenes/SampleScene.unity` — updated.

### Removed
- `CameraShake.cs` — camera shake responsibility folded into `ThirdPersonCameraFollow.cs`.
- Unused URP `Settings/` profile assets and the `SkySeries Freebie/` HDRI sky pack (no longer referenced by the scene).

---

## [999a81c] Jump and Goal feedback
### Added
- `GoalCelebrationController.cs` — goal UI pop/fade animation, confetti playback, and triggers a camera shake on score.
- Jump support in `PlayerMovementController.cs` (renamed from `PlayerController.cs`): coyote time, jump buffering, jump cooldown, air control multiplier.
- `Hoop.prefab`, hit/confetti VFX (`Particle/Hit 5.prefab`, `Particle/CFXM2_Expression_Stun.prefab`), and new UI icons (jump, throw, ball, crosshair).
- `Jump.fbx` / `Breathing Idle.fbx` animations; character rig reorganized under `3d Character/Player/AnimAndController/` and `Player/NewPlayer/`.

### Changed
- `PlayerAnimationController.cs`, `ThirdPersonCameraFollow.cs` — reworked to support jump animation state and goal-scored camera shake.
- `HoopGoalDetector.cs` — scoring flow extended.
- `Assets/Handball game/Scenes/SampleScene.unity` — large rebuild for the new hoop/jump/celebration setup.

---

## [eaab089] animation perfected, added score system and goal system
### Added
- `ScoreManager.cs` — score tracking with TMP score display.
- `HoopGoalDetector.cs` — dual-trigger goal detection per hoop (arm → confirm → ball reset flow).
- `GoalTriggerRelay.cs` — relays a hoop's entry/score BoxCollider triggers to its `HoopGoalDetector`.

### Changed
- `PlayerAimController.cs`, `ThirdPersonCameraFollow.cs`, `TrajectoryPreviewController.cs`, `BallController.cs` — reworked to support the scoring/goal flow.
- `Assets/Handball game/Scenes/SampleScene.unity` — large rebuild wiring up hoops, triggers, and score UI (3,182 lines changed).

---

## [0bd4af5] working on movement, added walk, fixed camera
### Added
- `PlayerAnimationEventRelay.cs` — relays animation-clip events (`AE_AttachBall`, `AE_ReleaseBall`) to ball pickup/throw.
- `Walking.fbx`, `Start to walk.fbx` animations.

### Changed
- `PlayerController.cs`, `BallController.cs`, `PlayerBallPickup.cs`, `PlayerThrowController.cs`, `ThirdPersonCameraFollow.cs`, `PlayerAnimationController.cs` — added walk movement state and camera fixes.
- `Animator.controller` reworked for the new walk state.
- URP settings tuning (`Mobile_RPAsset.asset`, `UniversalRenderPipelineGlobalSettings.asset`, `ProjectSettings/GraphicsSettings.asset`).

---

## [17d282b] Claude added, Fixed Trajectory, Throw, Added Trajectory preview
### Added
- `.claude/` Unity MCP skills for AI-assisted editor workflows (asset, GameObject, scene, profiler, and script tools).

### Changed
- Fixed and extended the throw trajectory preview mechanic.

---

## [3310e08] ThrowBall
### Added
- `PlayerThrowController.cs` — throw mechanic for the ball.

### Changed
- `PlayerAimController.cs` — reworked aiming logic to support the throw flow.
- `Assets/Handball game/Scenes/SampleScene.unity` — wired up the new throw controller.

---

## [a7b9e41] Player Movement, UI Movement, Animations, Ball physics, Ball pick up, Restart button, Aim and hold
Initial gameplay build — the bulk of the project's assets and core scripts landed in this commit.

### Added
- **Core gameplay scripts** (`Assets/Handball game/Scripts/`):
  - `PlayerController.cs` — player movement.
  - `PlayerAimController.cs` — aim-and-hold targeting.
  - `PlayerAnimationController.cs` — animation state driving.
  - `PlayerBallPickup.cs` — ball pickup interaction.
  - `BallController.cs` — ball physics.
  - `ThirdPersonCameraFollow.cs` — third-person camera.
  - `MobileLookArea.cs` — mobile touch look input.
  - `GameManager.cs` — game/restart flow.
  - `TrajectoryPreviewController.cs` — throw trajectory preview.
- **Scene**: `Assets/Handball game/Scenes/SampleScene.unity` with full level setup.
- **Character assets**: rigged character (`Character.fbx`) with animations (`Idle`, `Run`, `Throw`, `Throw Prepare`, `AimHold`, `pick up`, `Running and pick up`, `dance`) and an `Animator.controller`.
- **Environment/materials**: grass materials and textures, basketball material/texture, sky assets (`SkySeries Freebie` HDRIs and materials), URP render pipeline settings (Mobile + PC).
- **UI**: restart button sprite, Joystick Pack (mobile virtual joystick asset with prefabs/scripts/sprites) for on-screen movement input.
- **Third-party packages**: TextMesh Pro, Input System actions asset.
- **Misc**: `.vsconfig`, `.github/copilot-instructions.md`, Unity default `TutorialInfo`/`Readme` assets.

---

## [13ae1d3] Initial commit
### Added
- `.gitignore` (Unity defaults).
- `README.md`.
