# Changelog

All notable changes to the Handball project are documented in this file.

## [Unreleased]
Pending changes in the working tree, not yet committed.

### Added
- Unity MCP tooling integration for AI-assisted editor workflows (`.mcp.json`, `Packages/manifest.json` scoped registry for `com.ivanmurzak.unity.mcp`, `Assets/Plugins/NuGet`).
- `.claude/` project configuration for Claude Code.

### Changed
- `Assets/Handball game/Scenes/SampleScene.unity` — scene updates (216 lines changed).
- `ProjectSettings/PackageManagerSettings.asset`, `ProjectSettings/ProjectSettings.asset` — updated to reflect the new package/registry.
- `Packages/packages-lock.json` — lockfile refreshed for the new MCP package dependency.

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
