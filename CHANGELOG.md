# Changelog

All notable changes to the Handball project are documented in this file.

## [In Progress] Small audio & feedback system
A minimal, non-invasive audio/feedback pass — one ScriptableObject, two `AudioSource`s, small hooks at real gameplay events. No `SoundId` enum, no `AudioEntry`/`AudioLibrary`, no audio pool, no extra managers, no footstep/soft-hard-impact variation system, as explicitly requested. **Implementation complete; not yet manually verified in Play Mode — the user is testing.**

### New backup folder
`.backup/2026-07-29-audio-pass1/` (previous trajectory/camera backup folders untouched) — `GameManager.cs.bak`, `PlayerMovementController.cs.bak`, `PlayerBallPickup.cs.bak`, `PlayerThrowController.cs.bak`, `BallController.cs.bak`, `HoopGoalDetector.cs.bak`, `ThirdPersonCameraFollow.cs.bak`, `SampleScene.unity.bak`, `CHANGELOG.md.bak`.

### Existing audio inventory (checked before assigning anything)
Project-wide search for `.wav`/`.mp3`/`.ogg`/`AudioClip`/`AudioSource`/`AudioMixer` before this pass found: **no `AudioMixer`, no `AudioSource` in the scene, exactly 1 `AudioListener`** (`Main Camera`, standard), and six pre-existing clips in `Assets/Music and SFX/` (none were referenced by any component):

| File | Length | Category | Used as |
|---|---|---|---|
| `ball_Hit_ground.mp3` | 1.88s, stereo | **Suitable** | General ball impact/bounce |
| `Throw whoosh.mp3` | 1.15s, stereo | **Suitable** | Throw/release |
| `Goal chime.mp3` | 3.08s, stereo | **Suitable** | Goal/win |
| `Bubble_Pop.mp3` | 0.19s, stereo | **Possibly suitable** | Repurposed as the jump sound (a quick "pop" reads well as a hop; a proper UI click was sourced separately for the button sound instead) |
| `Victory (Male voince.mp3` (sic) | 2.14s, stereo | **Unclear** | Not used — a male voice cheer would double up with `Goal chime` for the same event; the spec asks for exactly one goal sound, and a chime is more universally reusable than a voice line. Left in the project, untouched. |
| `Sparkle Tiwinkle.mp3` (sic) | 2.46s, stereo | **Unused** | Doesn't map to any of the 8 required sounds; not assigned. |

None of the six had documented licensing anywhere in the project — their original source/license could not be verified this pass, so they're used as-is but flagged as unclear provenance in `AudioLicenses.md`.

### Downloaded audio (missing sounds only — music, button, pickup, hoop impact)
Bash has outbound network access in this environment (confirmed via `curl`), so missing clips were sourced rather than invented. Full details, including exact download URLs, in `Assets/Handball game/Audio/AudioLicenses.md`.

| File | Source | License | Attribution required |
|---|---|---|---|
| `Audio/Music/gameplay_music.mp3` ("Monkeys Spinning Monkeys" by Kevin MacLeod) | incompetech.com | CC BY 4.0 | **Yes** — see `AudioLicenses.md` for the exact credit line |
| `Audio/SFX/ui_button_click.ogg` (`click_001.ogg`) | Kenney "Interface Sounds" (kenney.nl) | CC0 1.0 | No |
| `Audio/SFX/ball_pickup.ogg` (`confirmation_001.ogg`) | Kenney "Interface Sounds" (kenney.nl) | CC0 1.0 | No |
| `Audio/SFX/hoop_impact.ogg` (`impactBell_heavy_000.ogg`) | Kenney "Impact Sounds" (kenney.nl) | CC0 1.0 | No |

Every download was verified before use: the music file's ID3 metadata was read to confirm it's genuinely 144 BPM (within the requested 130–150 range) before import; each Kenney file's presence in its official CC0 zip was confirmed; nothing was assumed or invented.

**Missing**: none — all 8 required sounds are covered (5 reused-existing, 3 newly downloaded, `Bubble_Pop.mp3` repurposed for jump). The music track is a real song rather than a custom-composed seamless loop — see Known Limitations.

### Scripts created
- **`Assets/Handball game/Scripts/Audio/HandballSoundData.cs`** — the one ScriptableObject, structured exactly per the provided reference (same field names, same `PlayOneShot`-based methods, same `StartGameplayMusic` guard against duplicate playback).

### Scripts modified
- **`GameManager.cs`** — added a minimal `static Instance` (none existed before — required for the `GameManager.Instance.PlayX()` pattern used by the hooks below); `soundData`/`musicAudioSource`/`sfxAudioSource` fields + public accessors; `Start()` now calls `StartGameplayMusic()` once; `Restart()` now calls `PlayButtonSound()` first; seven null-safe `PlayXSound()` helpers + `StartGameplayMusic()`, each a one-line `soundData?.PlayX(...)` call — gameplay is unaffected if `soundData`/AudioSources are unassigned.
- **`PlayerMovementController.cs`** — one line, `GameManager.Instance?.PlayJumpSound();`, added immediately after `Jumped?.Invoke()` inside `TryPerformJump()` — i.e. only on the actual successful-jump code path (coyote time + buffer + cooldown already passed), never on a failed jump-button press.
- **`PlayerBallPickup.cs`** — one line, `GameManager.Instance?.PlayPickupSound();`, added in `AttachPendingBall()` right after `HeldBall = pendingBall;` — the exact "ball successfully attaches to the hand" moment, called from `AE_AttachBall`. No separate start/complete sound.
- **`PlayerThrowController.cs`** — added an optional `followCamera` (`ThirdPersonCameraFollow`) reference field; in `ReleasePendingBall()` (the `AE_ReleaseBall` flow), added `GameManager.Instance?.PlayThrowSound();` and `followCamera?.PlayThrowImpulse();` immediately after `pendingThrowBall.Throw(...)` — after the real release, before `ballPickup.CompleteThrow()`. Release timing itself (`Throw()`'s call site) is unchanged.
- **`BallController.cs`** — added `minimumImpactSpeed` (1.5), `impactSoundCooldown` (0.1s), and `hoopImpactLayerMask` (512 = `Hoop` layer 9) fields; added `TryPlayImpactSound(Collision)`, called only from `OnCollisionEnter` (not `OnCollisionStay`, so resting/rolling contact never re-triggers it) after the existing `EvaluateGroundContact` call. Speed-gated, cooldown-gated, and picks hoop-impact vs general-ball-impact by the colliding object's layer. Existing ground-detection/rolling physics untouched.
- **`HoopGoalDetector.cs`** — in `ConfirmGoal()` (called exactly once per confirmed goal — already guarded against `OnTriggerStay` repeats by the existing armed/cooldown state machine), added `GameManager.Instance?.PlayGoalSound();` and a `#if UNITY_ANDROID || UNITY_IOS` `Handheld.Vibrate();` block, both after the existing celebration/score calls.
- **`ThirdPersonCameraFollow.cs`** — added `throwImpulseDuration` (0.12s), `throwImpulsePositionShake` ((0.02, 0.015, 0.01)), `throwImpulseRotationShake` ((0.3, 0.35, 0.15)) fields, and a `PlayThrowImpulse()` public method that calls the existing `PlayCameraShake(...)` with those values — reuses the goal-shake system entirely, no new shake component, much weaker/shorter than `PlayGoalShake()`. Camera composition, aim framing, pitch ranges, and `PlayGoalShake()` itself are untouched.

### ScriptableObject asset
- **`Assets/Handball game/Audio/HandballSoundData.asset`** — created and all 8 clips assigned, verified by reloading from disk after saving: `gameplayMusic`→`gameplay_music`, `buttonSound`→`ui_button_click`, `jumpSound`→`Bubble_Pop`, `pickupSound`→`ball_pickup`, `throwSound`→`Throw whoosh`, `ballImpactSound`→`ball_Hit_ground`, `hoopImpactSound`→`hoop_impact`, `goalSound`→`Goal chime`.

### Hierarchy / component changes
- **`-----Managers------/GameManager`**: added two `AudioSource` components (see settings below); `GameManager`'s `soundData`/`musicAudioSource`/`sfxAudioSource` fields wired to the new asset and the two sources (confirmed via component read-back).
- **`Player`**: `PlayerThrowController.followCamera` wired to `Main Camera`'s `ThirdPersonCameraFollow` (confirmed via component read-back).
- Scene-wide check after all changes: **1 `AudioListener`, exactly 2 `AudioSource`s** (both on `GameManager`) — confirmed via script query.

### AudioSource settings
| | MusicAudioSource | SfxAudioSource |
|---|---|---|
| Play On Awake | Off | Off |
| Loop | On | Off |
| Spatial Blend | 0 | 0 |
| Volume | 0.4 | 0.85 |

### Audio import settings
- **Short SFX** (`ui_button_click.ogg`, `ball_pickup.ogg`, `hoop_impact.ogg`, and the three reused existing clips `Bubble_Pop.mp3`/`Throw whoosh.mp3`/`ball_Hit_ground.mp3`/`Goal chime.mp3`): Force To Mono On, Load Type = Decompress On Load, Compression Format = ADPCM, Preload Audio Data On (via per-platform sample settings).
- **`gameplay_music.mp3`**: kept stereo, Load Type = Streaming, Compression Format = Vorbis, Quality ≈ 0.65 (65%). Looping is handled by `MusicAudioSource.loop = true`, not the clip.
- `Victory (Male voince.mp3` and `Sparkle Tiwinkle.mp3` were **not** touched (unused this pass).

### Event hook locations (summary)
Jump → `PlayerMovementController.TryPerformJump()` after `Jumped?.Invoke()`. Pickup → `PlayerBallPickup.AttachPendingBall()`. Throw → `PlayerThrowController.ReleasePendingBall()`. Ball/hoop impact → `BallController.OnCollisionEnter` → `TryPlayImpactSound`. Goal + vibration → `HoopGoalDetector.ConfirmGoal()`. Button → `GameManager.Restart()` (the only "normal UI button" in the current scene — Throw/Jump buttons already have their own dedicated sounds, so a generic click on them would duplicate/clutter per the "no separate button sounds" constraint; wiring `GameManager.PlayButtonSound()` to any future non-gameplay UI button is a one-line OnClick addition). Music → `GameManager.Start()`, once.

### Feedback added
1. **Throw camera impulse** — `ThirdPersonCameraFollow.PlayThrowImpulse()`, reusing the existing shake system, much weaker/shorter than the goal shake, called at real release.
2. **Existing goal camera shake** — unchanged, still `PlayGoalShake()`.
3. **Goal vibration** — `Handheld.Vibrate()` in `ConfirmGoal()`, mobile-only (`#if UNITY_ANDROID || UNITY_IOS`), fires once per confirmed goal, never on ball collisions.

### Tests performed (static/automated only)
- All 5 modified scripts + the new ScriptableObject compile cleanly (`assets-refresh` + `console-get-logs`, no errors) — checked after an interruption mid-pass specifically to confirm `PlayerMovementController.cs`/`PlayerBallPickup.cs` weren't left partially edited (they weren't; `git diff` showed clean, complete single-line insertions).
- `HandballSoundData.asset`'s 8 clip assignments confirmed by reloading the asset from disk after saving.
- `GameManager`'s 3 audio references and `PlayerThrowController.followCamera` confirmed via component read-back after wiring (the first attempt via one modification surface silently no-opped on component-typed fields; retried via the `componentDiff` surface, which worked and was verified).
- Scene-wide `AudioListener`/`AudioSource` count confirmed via script query: 1 listener, 2 sources, both correctly configured.

### Tests NOT performed — Play Mode is not accessible to me in this session
No sound has been heard, no vibration triggered, no camera impulse observed. **Manual verification required before this is considered complete:**
1. Gameplay music starts once, loops, and does not duplicate after a ball reset.
2. Button sound plays once (Restart).
3. Jump sound plays only after a successful jump (not on a failed jump-button press).
4. Pickup sound plays once when the ball reaches the hand.
5. Throw sound plays at actual release.
6. Ball impact sound doesn't spam while rolling.
7. Hoop impact uses the hoop sound, not the general ball impact sound.
8. Goal sound plays once per confirmed goal; goal vibration fires once (device/mobile build only).
9. Throw camera impulse is small, doesn't drift or accumulate; existing goal shake still works.
10. Pickup, aiming, trajectory, and throwing still work exactly as before.
11. Scoring and ball reset still work.
12. Console stays clean; exactly 1 `AudioListener`; exactly the 2 intended `AudioSource`s.

### Known limitations
- `gameplay_music.mp3` is a real song ("Monkeys Spinning Monkeys"), not a custom-composed seamless loop — there will likely be a small audible seam where it loops back to the start. Trimming/crossfading a proper loop point was not attempted this pass (would need audio editing tools not available here); flag if a tighter loop is wanted.
- The reused existing clips (`ball_Hit_ground.mp3`, `Throw whoosh.mp3`, `Goal chime.mp3`, `Bubble_Pop.mp3`) have no recorded original source/license anywhere in the project; their provenance could not be verified this pass even though they're already in use.
- `Victory (Male voince.mp3` and `Sparkle Tiwinkle.mp3` remain in the project, unused — left as-is per "do not perform unrelated cleanup."

### Rollback instructions
1. **Scripts**: copy the six `.bak` files from `.backup/2026-07-29-audio-pass1/` back over their originals (`GameManager.cs`, `PlayerMovementController.cs`, `PlayerBallPickup.cs`, `PlayerThrowController.cs`, `BallController.cs`, `HoopGoalDetector.cs`, `ThirdPersonCameraFollow.cs`).
2. **Scene**: restore `.backup/2026-07-29-audio-pass1/SampleScene.unity.bak` to remove the two `AudioSource`s and all the new reference wiring in one step (or manually remove the two `AudioSource` components from `GameManager` and clear the three audio fields).
3. **New assets**: delete `Assets/Handball game/Scripts/Audio/HandballSoundData.cs`, `Assets/Handball game/Audio/` (asset + Music/ + SFX/ + AudioLicenses.md) if a full revert is wanted. The reused pre-existing clips in `Assets/Music and SFX/` are untouched by this rollback either way.
4. Reload the scene (or restart the Editor) after any script rollback to force recompilation.

---

## [In Progress] Aim trajectory & camera framing rework
Scoped, in-flight work. Sound effects, camera bob/shake polish, and other demo features are intentionally paused for this pass — only the aim trajectory visuals and aim camera framing are being touched. **Status: four passes in so far — (1) initial chevron-mesh + camera rework, (2) a world/local mesh-space position bug found and fixed, (3) a visual-match correction that turned out to have its own bugs (red material, spike shape), (4) a chevron mesh-topology fix + camera composition rewrite — see "Correction pass 3" below for the latest. Round-4 manual re-verification by the user is still pending; do not consider this task complete until that's confirmed.**

### Root cause (why the old trajectory looked dark and combed)
- `AimTrajectory.mat` was a **Lit** surface (`receiveShadows = true`), so it visibly darkened wherever it faced away from the directional light or fell in shadow.
- Trajectory points were sampled at a constant **time** step, but a ballistic arc slows near the apex, so points bunched together there in world space. With `LineRenderer.textureMode = Tile` and `LineAlignment.View`, the tiled arrow texture compressed at that bunching point, producing the "comb" artifact. `UpdateTextureTiling` also hardcoded `(4,1)` regardless of arc length, so it never compensated.
- `TrajectoryPreviewController.LaunchSpeed` (15) was already the exact same value `PlayerThrowController.ThrowBall()` uses for the real throw velocity — trajectory and real physics were already unified, so throw physics itself was **not** changed.

### Bug fix — trajectory offset from a world/local mesh-space bug (found in manual Play Mode testing)
Manual testing showed the trajectory no longer starting at `ThrowOrigin`/the player — it appeared offset around `TrajectoryPreview`'s own (pre-existing, non-zero) world position `(0, 1.896, -5.906)`.

**Root cause**: the chevron mesh's vertex-building code computed each chevron corner correctly in **world space** (from the world-space sampled trajectory), then wrote those world-space values straight into the array passed to `Mesh.SetVertices`. `Mesh` vertices are always interpreted as **local** to the GameObject's transform — unlike the old `LineRenderer`, which had `useWorldSpace = true` and so ignored the transform entirely. Because `TrajectoryPreview` sits at a non-zero world position, Unity re-applied that transform on top of coordinates that were already world-space, double-offsetting every corner by `(0, 1.896, -5.906)`.

**Fix** (`TrajectoryPreviewController.cs`, `AnimateChevrons()` and `CollapseChevron()`): wrap every corner in `transform.InverseTransformPoint(...)` right before it's stored, converting it back to the local space the mesh actually needs:

```csharp
// Before (wrong — world-space value written directly into a local-space buffer):
chevronVertices[vertexBase + 0] = position + forwardAxis * halfLength;

// After (correct — converted to TrajectoryPreview's local space):
chevronVertices[vertexBase + 0] = transform.InverseTransformPoint(position + forwardAxis * halfLength);
```
Applied to all 4 corners in `AnimateChevrons()` plus the degenerate collapse point in `CollapseChevron()` (also previously stored in raw world space, which would have skewed `RecalculateBounds()`).

This is a pure coordinate-space fix — the ballistic simulation itself (`SimulateTrajectory`) already started from `throwOrigin.position` (never `transform.position`), and `startSkipDistance` was already measured as arc-length distance from that same origin, so neither needed to change. `TrajectoryPreview`'s transform was left exactly as found (rotation identity, scale `(1,1,1)`, not reparented, position untouched) — the conversion is robust to wherever that transform sits, including if it or the player moves.

**Files changed by this fix**: `Assets/Handball game/Scripts/TrajectoryPreviewController.cs` only. No camera, input, or gameplay script touched.

**Manual Play Mode test result (pre-fix)**: camera framing ✅ improved as intended; chevron appearance ✅ improved as intended; trajectory position ❌ offset bug (this fix). **Post-fix re-verification by the user is pending** — required before this task is considered done.

### Scripts changed
- **`Assets/Handball game/Scripts/TrajectoryPreviewController.cs`** — full rewrite. Backed up (pre-change version) at `.backup/2026-07-29-trajectory-camera/TrajectoryPreviewController.cs.bak`.
  - Replaced the `LineRenderer`/tiled-texture approach with a procedurally generated **chevron mesh** (`MeshFilter`/`MeshRenderer`), rebuilt each frame from a small pool of preallocated vertex/triangle arrays (no per-frame `new`/`Instantiate`/`Destroy`).
  - Chevrons are spaced **evenly by arc length** (`totalLength / targetChevronCount`, clamped to `[minChevronSpacing, maxChevronSpacing]`), not by simulation time step — this is what removes the apex "comb" bunching.
  - Each chevron is billboarded to face the camera, then rotated in-plane to match the arc's tangent, so it visually points along the throw direction from any camera angle. `[DefaultExecutionOrder(50)]` makes this run after `ThirdPersonCameraFollow.LateUpdate` in the same frame, so billboarding uses that frame's final camera transform (no lag), without touching the camera script.
  - Continuous "flow toward target" animation via a looping arc-length phase offset (standard scrolling-dash technique) — no wrap-teleport, no allocation.
  - The expensive part (ballistic simulation + `Physics.SphereCast` collision walk) only reruns when aim direction/throw origin/launch speed change beyond a small threshold; the cheap part (chevron placement/animation) runs every frame from the cached result.
  - A configurable `startSkipDistance` keeps the first chevron clear of the player/held ball; the last few chevrons (`endFadeChevronCount`) shrink smoothly near the impact point.
  - **Preserved exactly**: public `LaunchSpeed` property, and every existing serialized reference field (`ballPickup`, `aimController`, `throwOrigin`) and prediction/collision field (`maximumPointCount`, `simulationStep`, `maximumSimulationTime`, `previewBallRadius`, `collisionSkin`) — same names/types/defaults, so prior Inspector values carried over automatically (confirmed via component inspection after the rewrite).
  - **`animateTexture`** and **`scrollSpeed`** — same field names, **repurposed** (flow-animation on/off, and chevron flow speed in world units/sec) rather than renamed, so their existing Inspector values (`true`, `1`) carried over with sensible new meaning.
  - **`tilesPerWorldUnit`** — kept as a serialized field (not deleted), `[HideInInspector]` + `[Obsolete]`, with a comment explaining it's unused since the texture-scroll approach was replaced. Its existing value (`2`) is preserved but has no effect.
  - **`collisionMask`** default changed from `192` to **`896`** (see Collision layers below) — this is a behavior change, not a rename.

### New assets
- **`Assets/Handball game/Materials/AimChevron.mat`** — `Universal Render Pipeline/Unlit`, Surface=Transparent, Blend=Alpha, Cull=Off, ZWrite=Off, `_BaseColor`=`RGBA(1,1,1,0.92)` (bright near-white, unaffected by scene lighting). `AimTrajectory.mat` / `AimTrajectory 1.mat` left untouched.

### Hierarchy / component changes
- **`TrajectoryPreview`** GameObject: removed `LineRenderer`; added `MeshFilter` + `MeshRenderer` (shadow casting off, no light probes/reflection probes); assigned `AimChevron.mat` to both the renderer and the script's `chevronMaterial` field.
- **Collision layers**: added two new project layers — **`Boundaries`** (8) and **`Hoop`** (9) — via `ProjectSettings/TagManager.asset`. Reassigned the `Boundaries` hierarchy to `Boundaries`, and the `Hoop`/`Hoop (1)`/`Hoop (2)` hierarchies (including their trigger colliders — already separately excluded from prediction via `QueryTriggerInteraction.Ignore`, unaffected by layer) to `Hoop`. `Player` and `Ball` are untouched (still `Default`/`Ball` respectively).
  - New `collisionMask` = `Ground`(7,value 128) + `Boundaries`(8,value 256) + `Hoop`(9,value 512) = **896**. Explicitly excludes `Ball`(6,64) and `Default`/`Player`(0,1) — confirmed via script query at setup time.

### Inspector values changed
| Field | Component | Before | After |
|---|---|---|---|
| `collisionMask` | `TrajectoryPreviewController` on `TrajectoryPreview` | `192` (Ball+Ground) | `896` (Ground+Boundaries+Hoop) |
| `chevronMaterial` | `TrajectoryPreviewController` on `TrajectoryPreview` | *(new field)* | `AimChevron.mat` |
| `aimPositionOffset` | `ThirdPersonCameraFollow` on `Main Camera` | `(-1.53, 0.80, 0.00)` | `(0.85, 0.55, 0.00)` |
| `aimDistance` | `ThirdPersonCameraFollow` on `Main Camera` | `2.53` | `2.15` |

Camera values are applied in the scene now but are **provisional** pending the user's manual Play Mode check (see Testing).

### Preserved / untouched (per hard constraints)
- No changes to `BallController.cs`, `PlayerMovementController.cs`, `PlayerBallPickup.cs`, `PlayerThrowController.cs`, `PlayerAnimationController.cs`, `GoalTriggerRelay.cs`, `HoopGoalDetector.cs`, `ScoreManager.cs`, or the `AE_AttachBall`/`AE_ReleaseBall` animation events.
- No code changes to `ThirdPersonCameraFollow.cs` — only two serialized field *values* changed on its `Main Camera` instance.
- `PlayerAimController.AimDirection`/`IsAiming`, `ThirdPersonCameraFollow.Yaw`/`Pitch`/`AimVertical01`/`SetAimMode()`/`AlignYawTo()`/`PlayGoalShake()`, `TrajectoryPreviewController.LaunchSpeed` — all untouched/preserved.
- Mobile joystick (`Fixed Joystick`) and `RightLookArea`/`MobileLookArea` input untouched.
- Real throw physics (`BallController.Throw`, `PlayerThrowController`) untouched — trajectory prediction already used the same `LaunchSpeed`/gravity/direction before this change and still does.

### Testing
**Automated verification performed:** script compiles cleanly (`assets-refresh` + `console-get-logs` show no errors), collision-mask/layer math confirmed via script query (896 = Ground+Boundaries+Hoop, Ball/Player excluded), all component/material references confirmed wired via `gameobject-component-get`. After the world/local space fix: recompiled clean, no console errors.

**Manual Play Mode test — round 1 (pre-fix) result:**
- ✅ Aim camera framing — clearly improved (player lower-left, close over-the-shoulder).
- ✅ Chevron appearance — clearly improved (bright, evenly spaced, no comb).
- ❌ **Blocking bug**: trajectory did not start at `ThrowOrigin`/the player — appeared offset around `TrajectoryPreview`'s old world position. See "Bug fix" section above. Fixed in code; not yet re-verified.

**Manual Play Mode test — round 2 (post-fix), pending user confirmation:**
1. Pick up the ball — first chevrons appear immediately in front of the ball/`ThrowOrigin`.
2. Move the player while aiming — the whole trajectory follows the player.
3. Rotate the camera — the trajectory rotates with the aim direction.
4. Raise/lower aim — arc updates from the same `ThrowOrigin`.
5. Throw the ball — it follows the displayed path closely.
6. Joystick stays visible; player can still move slowly while aiming; right-side swipe still controls yaw/elevation.
7. Chevrons remain evenly spaced at low and high arcs, no bunching at the apex, flow toward the target, don't clip the player/held ball at the start.
8. Throw animation and `AE_ReleaseBall` still fire correctly; normal movement resumes after the throw.
9. Scoring still works; goal camera shake still plays and resets correctly.
10. Console stays clean (no errors/warnings/missing references) throughout.

**This task is not considered complete until the user visually confirms round 2.**

---

### Correction pass 2 — visual match to the football reference
Manual round-2 testing found the trajectory was positionally correct, but visually still didn't match the target reference: the chevrons rendered as **red** 3D "spikes" with sideways tilt/perspective twisting and huge-to-tiny sizing, and the aim camera sat too close over the player's head/shoulder. This pass fixes all three, touching only `TrajectoryPreviewController.cs` (code) and `ThirdPersonCameraFollow` Inspector values on `Main Camera` (data only — no code change). No player model, animation, ball physics, movement, pickup, throw, scoring, animation event, joystick, or mobile-look-control script was touched.

**New backups taken before this pass** (in addition to the pass-1 backup): `.backup/2026-07-29-trajectory-camera-pass2/TrajectoryPreviewController.cs.bak`, `.backup/2026-07-29-trajectory-camera-pass2/ThirdPersonCameraFollow.cs.bak`, `.backup/2026-07-29-trajectory-camera-pass2/SampleScene.unity.bak`.

#### Bug found — chevron material was red, not white
`Assets/Handball game/Materials/AimChevron.mat`'s `_BaseColor`/`_Color` was found to be `RGBA(1, 0, 0.108, 0.92)` (red) instead of the white it was set to when created in pass 1 — it had been changed at some point after (most likely an accidental edit in the Unity Editor Inspector while testing; the material asset has no other automated writer). This, not a shader error, is what the user saw as "red 3D spikes." **Fix**: rebuilt the material via script (`_BaseColor`/`_Color` = pure opaque white `RGBA(1,1,1,1)`, Surface=Transparent, Blend=Alpha, Cull=Off, ZWrite=Off, render queue=3000/Transparent), then reloaded it from disk and re-read every property back to confirm the save stuck. Per-instance transparency is now applied separately at runtime via a `MaterialPropertyBlock` (`chevronOpacity`), so the material asset itself stays a clean, fully-opaque white and can't drift again the same way.

#### Bug found — chevron shape read as 3D spikes with tilt/twist
The pass-1 chevron was a single filled kite/notch quad, billboarded using a **per-chevron** view direction (`position - cameraPosition`), which varies slightly from chevron to chevron along the arc. Combined with the solid-triangle silhouette, this produced visible sideways tilt and perspective twisting — reading as small 3D arrowheads/spikes rather than a flat, UI-style chevron trail.

**Fix** — rewrote chevron construction in `TrajectoryPreviewController.cs` to build every chevron in one **shared** camera-facing plane, exactly per the requested algorithm:
```csharp
// One shared plane normal for ALL chevrons this frame — every chevron is
// coplanar with the screen, so none of them can tilt sideways relative to each other.
Vector3 cameraForward = cameraTransform.forward;
...
// Per chevron: project this point's world tangent onto that shared plane...
Vector3 screenTangent = Vector3.ProjectOnPlane(tangent, cameraForward).normalized;
// ...then build the chevron's local axes purely within the plane.
Vector3 planeUp = rollCorrection * screenTangent;      // "pointing" axis, always ⟂ cameraForward
Vector3 planeRight = Vector3.Cross(cameraForward, planeUp).normalized; // also always ⟂ cameraForward
```
Each chevron is now a **flat double-chevron ("V"/">" bracket)** made of two thin stroke-quads meeting at a forward tip (`BuildChevronGeometry`/`BuildStrokeQuad`) — a hollow bracket shape, not a filled arrowhead — built entirely from `planeUp`/`planeRight`, so every vertex lies exactly in the plane through that chevron's position with normal `cameraForward`. Because the plane *normal* is identical for every chevron in a given frame, none of them can tilt sideways or twist relative to each other; only the in-plane rotation (via `screenTangent`) varies, which is exactly what makes the trail follow the curve on-screen. An optional `chevronRotationOffsetDegrees` field (default `0`) rotates this in-plane basis around `cameraForward`, in case a future sprite-based chevron's authored forward axis doesn't line up — unused for the current procedural geometry.

#### Bug found — huge-to-tiny chevron sizing
Chevrons used a fixed world-space size regardless of distance from the camera, so ordinary perspective made the near ones (close to the player/camera) look oversized and the far ones tiny. **Fix**: each chevron's world size is now scaled by `Mathf.Lerp(1f, distanceFromCamera / referenceDistance, PerspectiveCompensation)`, where `referenceDistance` is the distance from the camera to the trajectory's start point and `PerspectiveCompensation = 0.82` (a private constant). This mostly counteracts natural perspective shrink while intentionally leaving a small residual size reduction toward the far end, per the requested "small perspective reduction is acceptable, avoid huge-to-tiny."

#### `TrajectoryPreviewController.cs` — Inspector field changes
Replaced the pass-1 chevron field set with the exact fields requested. Fields not in this new list (`chevronForwardLength`, `chevronWidth`, `targetChevronCount`, `minChevronSpacing`, `maxChevronSpacing`, `animateTexture`) were removed outright — they were introduced by me one pass ago with values never manually tuned by the user, so there was nothing to preserve. `scrollSpeed` is renamed to `flowSpeed` at the user's explicit request in this pass (its value, `1`, is unchanged — matches the requested default exactly, so nothing was lost). The previously-established rule to *never* delete `tilesPerWorldUnit` (an original, pre-existing project field) still applies — it remains `[HideInInspector]` + `[Obsolete]`, untouched.

| Field | Before (pass 1) | After (pass 2) | Value set |
|---|---|---|---|
| `chevronSize` | *(new)* | added | `0.30` |
| `chevronSpacing` | *(new — was adaptive `min/maxChevronSpacing`)* | added | `0.42` |
| `startSkipDistance` | `0.4` | value updated (field unchanged) | `0.55` |
| `endSkipDistance` | *(new)* | added | `0.10` |
| `chevronOpacity` | *(new — opacity wasn't separately controllable)* | added | `0.85` |
| `flowSpeed` | `scrollSpeed` (renamed), value `1` | renamed | `1.0` |
| `maximumVisibleChevrons` | `targetChevronCount` (replaced), value `22` | replaced | `24` |
| `chevronRotationOffsetDegrees` | *(new)* | added | `0` (no correction needed for procedural geometry) |

Note: `launchSpeed` was found at `20` (changed by the user directly in the Editor since pass 1, was `15`) — left exactly as-is, per "preserve `LaunchSpeed`."

#### New assets
None new — `AimChevron.mat` (created in pass 1) was repaired in place rather than replaced.

#### Camera framing & pitch range — Inspector values changed (`ThirdPersonCameraFollow` on `Main Camera`, data only, no code change)
"Before" values reflect what was actually in the scene at the start of this pass — `aimPositionOffset`/`aimDistance` matched pass 1, but `maximumAimCameraPitch` (`30`) had already been hand-tuned by the user since then.

| Field | Before | After |
|---|---|---|
| `aimDistance` | `2.15` | **`3.4`** |
| `aimPositionOffset` | `(0.85, 0.55, 0.00)` | **`(0.65, 0.45, 0.00)`** |
| `aimFallbackLookOffset` | `(0, 1.15, 4.00)` | **`(0, 1.00, 5.00)`** |
| `cameraModeBlendTime` | `0.15` | **`0.25`** |
| `minimumNormalPitch` | `5` | **`-8`** |
| `maximumNormalPitch` | `45` | **`48`** |
| `minimumAimCameraPitch` | `10` | **`-10`** |
| `maximumAimCameraPitch` | `30` | **`28`** |

These are the requested starting values, applied and confirmed via component read-back — **not yet visually re-tested**, per "treat these as initial values and visually tune them in Play Mode." `PlayerAimController`'s throw-elevation range is untouched and remains independently configured, so the trajectory can still aim higher than the camera looks while staying visible near the top of the screen.

#### Testing (this pass)
**Automated verification performed:** script compiles cleanly (`assets-refresh` + `console-get-logs`, no errors both before and after the material fix); `AimChevron.mat` re-read from disk after saving to confirm `_BaseColor`/`_Color` = `RGBA(1,1,1,1)`, `Surface`=Transparent, `Cull`=Off, `renderQueue`=3000; `MeshRenderer.sharedMaterial` confirmed still pointing at the same (now-fixed) material asset; all new/changed Inspector values confirmed via component read-back.

**Manual Play Mode re-verification — round 3 — pending.** Checklist (from the acceptance test):
1. Chevrons are white, flat and clean.
2. Chevrons never appear sideways or like 3D spikes.
3. Chevron size and spacing remain visually consistent.
4. Trajectory starts from the ball/`ThrowOrigin`.
5. Trajectory follows the moving player.
6. Low and high arcs both remain readable.
7. Camera is farther behind and resembles the football reference.
8. Player remains lower-left and does not block the trajectory.
9. Normal camera can look slightly farther upward and downward.
10. Throw path still matches the preview.
11. Pickup, movement, animation, throw and scoring still work.
12. Console contains no errors.

**Do not mark this trajectory/camera task complete until round 3 is visually confirmed.**

#### Rollback (this pass only)
1. **Script**: copy `.backup/2026-07-29-trajectory-camera-pass2/TrajectoryPreviewController.cs.bak` back over `Assets/Handball game/Scripts/TrajectoryPreviewController.cs` (restores the pass-1 kite-shaped chevron, not the original `LineRenderer` — see the pass-1 rollback further below for that).
2. **Material**: re-run the pass-1 material setup, or manually set `AimChevron.mat`'s `_BaseColor`/`_Color` alpha back to `0.92` if pass-1 translucency (rather than pass-2's opaque-base + property-block opacity) is preferred — cosmetic only, has no functional effect either way.
3. **Camera**: on `Main Camera`'s `ThirdPersonCameraFollow`, restore: `aimDistance=2.15`, `aimPositionOffset=(0.85,0.55,0)`, `aimFallbackLookOffset=(0,1.15,4)`, `cameraModeBlendTime=0.15`, `minimumNormalPitch=5`, `maximumNormalPitch=45`, `minimumAimCameraPitch=10`, `maximumAimCameraPitch=30`.
4. Reload the scene (or restart the Editor) after a script rollback to force recompilation.

---

### Correction pass 3 — chevron mesh-merging bug + camera composition rewrite
Round-3 manual testing found the pass-2 visual correction still didn't match the football reference: a large solid white polygon/connected strip appeared among the chevrons, and the aim camera still felt centred on the player instead of looking into the play area. This pass fixes both root causes properly rather than re-tuning values.

**New backup folder** (previous backups left untouched): `.backup/2026-07-29-trajectory-camera-pass3/` — `TrajectoryPreviewController.cs.bak`, `ThirdPersonCameraFollow.cs.bak`, `SampleScene.unity.bak`, `CHANGELOG.md.bak`.

#### Problem 1 root cause — aim camera looked at the player, not into the play area
Passes 1-2 only ever retuned `aimPositionOffset`/`aimDistance`/pitch values feeding the *same* formula as normal mode: an orbit position (`target.position + orbitRotation * Vector3.back * distance`) plus a look point blended toward `aimLookTarget`/`aimFallbackLookOffset`. Because both the position and the look point were still anchored close to `target.position`, the camera kept rotating to face back toward the player as yaw/pitch changed, instead of holding a fixed shoulder-relative position and looking forward down-range.

**Fix** (`ThirdPersonCameraFollow.cs`) — aim mode now computes position and rotation from two independent points instead of one shared orbit+lookAt pair:
```csharp
// Position: a fixed shoulder offset + height, pulled back by aimDistance — never orbits toward the player.
Vector3 ComputeAimPosition(Quaternion yawRotation) =>
    target.position +
    yawRotation * Vector3.right * aimShoulderOffset +
    Vector3.up * aimCameraHeight -
    yawRotation * Vector3.forward * aimDistance;

// Rotation: looks at a point several metres AHEAD, tilted by pitch — never at the player.
Quaternion ComputeAimRotation(Vector3 cameraPosition, Quaternion yawRotation)
{
    Quaternion pitchRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    Vector3 pitchedForward = yawRotation * pitchRotation * Vector3.forward;
    Vector3 lookPoint = target.position + pitchedForward * aimLookAheadDistance
        + yawRotation * Vector3.right * aimLookRightOffset + Vector3.up * aimLookHeight;
    return Quaternion.LookRotation(lookPoint - cameraPosition, Vector3.up) * Quaternion.Euler(aimRotationOffset);
}
```
Normal mode's own position/rotation formula is untouched (still the original orbit+lookAt). `UpdateCameraTransform()` now computes both modes fully, then blends: position via `Vector3.Lerp(normalPosition, aimPosition, aimBlend)`, rotation via `Quaternion.Slerp(normalRotation, aimRotation, aimBlend)` — using the same `aimBlend` (`SmoothDamp` over `cameraModeBlendTime`) as before, so the smooth transition, orbit smoothing, yaw/pitch system, touch input, and goal-shake application (applied after, on top of the final transform, unchanged) all keep working exactly as before.

**New fields added** to `ThirdPersonCameraFollow.cs` (`aimDistance` and `cameraModeBlendTime` already existed and are reused, not duplicated): `aimShoulderOffset`, `aimCameraHeight`, `aimLookAheadDistance`, `aimLookHeight`, `aimLookRightOffset`.

**Deprecated, not deleted**: `aimPositionOffset` and `aimFallbackLookOffset` are no longer read by the new formula (the old single position-offset/look-offset model doesn't map onto the new two-point composition), so they're now `[HideInInspector]` + `[Obsolete]` — declared and their values preserved, matching the "never rename/delete a serialized field" rule, exactly like `tilesPerWorldUnit` in the trajectory script. `aimLookTarget` (a scene reference, not a tuned value) is left wired but is no longer read by aim-mode rotation — the explicit look-ahead formula supersedes it. `aimRotationOffset` is still actively applied (post-multiplied onto the new aim rotation), so it isn't dead.

#### Problem 2 root cause — chevron mesh was one connected mesh, not independent quads
The pass-2 chevron was a hand-built double-stroke bracket (8 vertices/4 triangles per chevron), and — more importantly — its triangle *index* buffer was built once in `Awake` for the full `ChevronCapacity`, and every frame `Mesh.SetVertices` was called with the **entire** preallocated array regardless of how many chevrons were actually meant to be visible. Inactive slots were "collapsed" to a single point rather than excluded, and the mesh's submitted vertex/index *count* never shrank to match the active count. Between the collapse-point behaviour and the double-stroke geometry, this was fragile — the earlier reports of "sideways spikes" and now a "connected white polygon" both trace back to this same one active mesh being asked to represent a variable number of active chevrons without the index/vertex counts actually being trimmed to match.

**Fix** (`TrajectoryPreviewController.cs`, `AnimateChevrons()`) — rewritten to the exact independent-quad topology:
- Every chevron is now a single flat quad: **4 vertices, 4 UVs, 6 triangle indices**, with `vertexBase = i * 4` / `triangleBase = i * 6`, exactly `(0,1,2,0,2,3)` per quad — no vertex is ever shared or connected between chevrons.
- Vertex/UV/triangle arrays are preallocated **once** at the hard cap (`ChevronCapacity = 20`) and reused every frame — never reallocated.
- Only the **active range** is submitted to the mesh each frame: `chevronMesh.Clear(false)` (explicitly requested, to guarantee no previous frame's larger chevron count leaves anything behind) followed by `SetVertices(chevronVertices, 0, activeCount * 4)`, `SetUVs(0, chevronUVs, 0, activeCount * 4)`, and `SetIndices(chevronTriangles, 0, activeCount * 6, MeshTopology.Triangles, 0, true)` — i.e. exactly `activeChevronCount * 6` indices, never the full preallocated buffer. There is no more "collapse inactive chevrons to a point" step at all — chevrons beyond the active count are simply never written or submitted.
- Because indices are only ever submitted up to `activeCount * 4 - 1`, and vertices are truncated to the same count by the same `SetVertices` call, there is no way for one chevron's geometry to be interpreted as reaching into the next chevron's slot.

#### Chevron visual — texture-based double-chevron replaces procedural stroke geometry
Per the requested "preferred final implementation," the flat double-chevron shape is now a **texture** on a plain quad instead of hand-built stroke geometry:
- **New asset**: `Assets/Handball game/Materials/AimChevronTexture.png` (128×128, RGBA32) — generated procedurally (`Texture2D` + per-pixel distance-to-segment shading + `EncodeToPNG`, no external image tool available) as a clean white double-chevron/">" bracket on a fully transparent background, with a small antialiased edge. Import settings: `alphaIsTransparency = true`, uncompressed, no mipmaps, bilinear, clamp.
- **`AimChevron.mat` rebuilt and re-verified**: `_BaseMap` = the new texture, `_BaseColor`/`_Color` = pure opaque white `RGBA(1,1,1,1)` (per-instance opacity is applied separately via a runtime `MaterialPropertyBlock`, not baked into the material), `Surface` = Transparent, `Blend` = Alpha, `Cull` = Off, `ZWrite` = Off, `renderQueue` = 3000 (Transparent). **Verified by reloading the material from disk after saving** and reading every property back — this directly addresses the pass-2 material color drift (`_BaseColor` had silently become red) by never trusting an unverified write again.
- **Chevron orientation** (Problem 2's "CHEVRON ORIENTATION" section) is unchanged in approach from pass 2 and still correct: one shared camera-facing plane per frame (`cameraForward` = the single plane normal for every chevron), with each chevron's `screenTangent = Vector3.ProjectOnPlane(worldTangent, cameraForward)` giving its in-plane rotation. `chevronRotationOffsetDegrees` (default `0`) remains available to correct the texture's authored forward axis if ever needed — not used to mask tangent-logic bugs.
- **Chevron sizing** now follows the requested screen-space-derived model: `worldHeight = 2 * distanceFromCamera * tan(fieldOfView/2) * chevronScreenHeight`, clamped to `[minimumWorldSize, maximumWorldSize]` — this keeps chevrons a roughly constant apparent screen size from near to far, instead of pass-2's flat-then-perspective-compensated size. Spacing is derived from size: `spacing = referenceWorldHeight * spacingMultiplier`, calculated once per rebuild from the trajectory's start point (kept singular, not recalculated per-chevron, so the flow-animation phase stays stable — see Known Limitations).
- Coordinate space is unchanged from the pass-1 fix and still correct: every corner is computed in world space, then converted via `transform.InverseTransformPoint(...)` immediately before being written into the vertex buffer. The simulation still starts at `throwOrigin.position`, never `transform.position`.
- End-of-trajectory chevrons are **not** scaled down (per "use opacity fading only, do not use strong end-scale reduction") — this pass removed the old end-scale-fade behavior entirely rather than risk it looking like the previous size distortion. True **per-chevron opacity** fade near the end was **not implemented** — see Known Limitations for why.

#### `TrajectoryPreviewController.cs` — Inspector field changes (pass 2 → pass 3)
`chevronSize`/`chevronSpacing` (pass-2 fields, one pass old, no real tuning history) are removed, replaced by the screen-space-derived sizing model's fields. `startSkipDistance`/`endSkipDistance`/`chevronOpacity`/`flowSpeed`/`maximumVisibleChevrons`/`chevronRotationOffsetDegrees` field *names* are unchanged from pass 2 — only their default/actual values changed, per the explicit requirement that the previous incorrect values (`startSkipDistance≈0`, `endSkipDistance≈2.59`, spacing≈`1.41`) must not remain.

| Field | Before (pass 2, as found in-scene) | After (pass 3) |
|---|---|---|
| `chevronSize` | `0.30` | *(removed — superseded)* |
| `chevronSpacing` | `0.42` | *(removed — superseded)* |
| `chevronScreenHeight` | *(new)* | **`0.028`** |
| `minimumWorldSize` | *(new)* | **`0.10`** |
| `maximumWorldSize` | *(new)* | **`0.22`** |
| `spacingMultiplier` | *(new)* | **`1.4`** |
| `maximumVisibleChevrons` | `24` | **`20`** |
| `startSkipDistance` | `0.55` | **`0.7`** |
| `endSkipDistance` | `0.10` | **`0.2`** |
| `chevronOpacity` | `0.85` | **`0.85`** (unchanged) |
| `flowSpeed` | `1` | **`0.8`** |
| `chevronRotationOffsetDegrees` | `0` | **`0`** (unchanged) |

`launchSpeed` remains `20` (the user's own change since pass 1) — still untouched.

#### `ThirdPersonCameraFollow` on `Main Camera` — Inspector values (pass 2/hand-tuned → pass 3)
"Before" reflects what was actually in the scene at the start of this pass, including values the user had hand-tuned since pass 2 (`maximumAimCameraPitch` had drifted to `38`).

| Field | Before | After |
|---|---|---|
| `aimDistance` | `3.4` | **`3.4`** (unchanged — already matched the new target) |
| `aimShoulderOffset` | *(new)* | **`0.9`** |
| `aimCameraHeight` | *(new)* | **`1.35`** |
| `aimLookAheadDistance` | *(new)* | **`6.5`** |
| `aimLookHeight` | *(new)* | **`1.15`** |
| `aimLookRightOffset` | *(new)* | **`0.45`** |
| `cameraModeBlendTime` | `0.25` | **`0.25`** (unchanged) |
| `minimumNormalPitch` | `-8` | **`-10`** |
| `maximumNormalPitch` | `48` | **`48`** (unchanged) |
| `minimumAimCameraPitch` | `-10` | **`-10`** (unchanged) |
| `maximumAimCameraPitch` | `38` (hand-tuned by user since pass 2) | **`28`** |
| `aimPositionOffset` | `(0.65, 0.45, 0)` | *(deprecated, hidden — value preserved, no longer read)* |
| `aimFallbackLookOffset` | `(0, 1.0, 5.0)` | *(deprecated, hidden — value preserved, no longer read)* |

#### Hierarchy / component changes
None beyond the material/texture asset changes above — `TrajectoryPreview`'s `MeshFilter`/`MeshRenderer` and `Main Camera`'s `ThirdPersonCameraFollow` component are the same components from earlier passes, just with new/changed serialized values and (for the trajectory script) new code.

#### Performance approach (unchanged principle, re-verified this pass)
No per-frame `Instantiate`/`Destroy`/`new Material`. Vertex/UV/triangle arrays are allocated exactly once (`Awake`) at the `ChevronCapacity` hard cap and reused every frame; only a leading sub-range is ever written to or submitted. `Mesh.Clear(false)` + partial `SetVertices`/`SetUVs`/`SetIndices` calls are the only per-frame mesh API calls. The expensive ballistic simulation + `Physics.SphereCast` walk still only reruns when aim direction/origin/speed change beyond a small threshold (unchanged from pass 1).

#### Testing (this pass)
**Automated verification performed:** both scripts compile cleanly (`assets-refresh` + `console-get-logs`, no errors) after each edit; the generated chevron texture's alpha channel was read back pixel-by-pixel (`Texture2D.GetPixels32`) and rendered as an ASCII map to confirm it actually forms a chevron bracket shape, not a solid block; `AimChevron.mat` reloaded from disk after saving and every relevant property (`_BaseMap`, `_BaseColor`, `Surface`, `Cull`, `ZWrite`, `renderQueue`, shader) read back and confirmed; all Inspector values on both components confirmed via component read-back after being set.

**Not performed — Play Mode is not accessible to me in this session** (the Unity MCP tools for entering Play Mode and reading the Game View are not exposed here, same limitation as passes 1-2). I have not seen this render. Do not take anything above as visual confirmation.

**Manual Play Mode re-verification — round 4 — required before this task can be marked complete.** Full 31-item acceptance checklist as given, notably:
1-9 (chevrons): white, flat, camera-facing, no spikes, no sideways tilt, consistent size, no merging into a white block, no tiny needles.
10-14 (trajectory): starts beside/above the throwing hand, follows the moving player, rotates with yaw, updates with pitch, matches the thrown ball.
15-18 (camera): player in the lower-left third, camera looks ahead into the play area (not at the player), target/trajectory visible near centre.
19-23 (camera behavior): normal camera's extra vertical range, aim camera's upward look without going top-down, smooth transitions both ways, no drift.
24-31 (regression): goal shake, pickup, slow aim movement, throw animation, `AE_ReleaseBall`, normal movement after throw, scoring, clean console.

**This task remains incomplete until the user manually verifies round 4 in Play Mode and approves.**

#### Rollback (this pass only)
1. **Trajectory script**: copy `.backup/2026-07-29-trajectory-camera-pass3/TrajectoryPreviewController.cs.bak` back over `Assets/Handball game/Scripts/TrajectoryPreviewController.cs` (restores pass-2's double-stroke chevron, not the pass-1 kite or the original `LineRenderer`).
2. **Camera script**: copy `.backup/2026-07-29-trajectory-camera-pass3/ThirdPersonCameraFollow.cs.bak` back over `Assets/Handball game/Scripts/ThirdPersonCameraFollow.cs` (restores the single orbit+lookAt aim formula).
3. **Scene**: restore `.backup/2026-07-29-trajectory-camera-pass3/SampleScene.unity.bak` if the Inspector value changes need reverting wholesale, or manually set the "Before" values from the tables above.
4. **Texture/material**: delete `Assets/Handball game/Materials/AimChevronTexture.png` and clear `AimChevron.mat`'s `_BaseMap` if the textured approach needs reverting to pass-2's procedural geometry (only meaningful together with rollback step 1).
5. Reload the scene (or restart the Editor) after any script rollback to force recompilation.

---

### Known limitations
- The trajectory prediction mask (`Ground|Boundaries|Hoop`) intentionally does **not** include the stray `Cube`/`Cube (1)` leftover objects or the inactive `Basketball Hoop` model in the scene — consistent with the old mask's narrower behavior, but means the preview won't show a bounce off those if they're ever re-enabled/reused.
- **Per-chevron end-of-trajectory opacity fade was not implemented.** The requested "last 2-3 chevrons fade by opacity only" needs per-vertex or per-chevron alpha, but the stock `Universal Render Pipeline/Unlit` shader does not sample mesh vertex colors, and this component renders the whole trajectory as one draw call/one `MaterialPropertyBlock`, so alpha can currently only be set uniformly for the entire trajectory (`chevronOpacity`). Implementing true per-chevron fade would need a small custom shader (to read vertex color) — deliberately not attempted this pass to avoid introducing another new failure mode; flagging for a future pass if wanted.
- Chevron **spacing** is computed once per rebuild from the trajectory start point's on-screen size (for animation-phase stability), while chevron **size** is computed individually per chevron from its own distance to the camera — so size correctly stays near-constant on screen end-to-end, but spacing is a single representative value rather than continuously perspective-correct along the whole arc. This was a deliberate simplification to avoid the added complexity/risk of a variable-spacing marching algorithm; likely imperceptible in practice but noted here.
- Camera composition values (`aimShoulderOffset`, `aimCameraHeight`, `aimLookAheadDistance`, `aimLookHeight`, `aimLookRightOffset`) are the requested starting values, applied and read-back-confirmed but **not yet visually tuned in Play Mode**.

### Rollback instructions
1. **Script**: copy `.backup/2026-07-29-trajectory-camera/TrajectoryPreviewController.cs.bak` back over `Assets/Handball game/Scripts/TrajectoryPreviewController.cs`.
2. **Hierarchy**: on the `TrajectoryPreview` GameObject, remove `MeshFilter`/`MeshRenderer`, re-add a `LineRenderer`, and reassign `AimTrajectory.mat` to it (the original material is untouched and still present).
3. **Material**: delete `Assets/Handball game/Materials/AimChevron.mat` (optional — harmless if left unused).
4. **Layers**: the `Boundaries`/`Hoop` layer additions and reassignments are harmless to leave in place even after a script rollback (a `LineRenderer`-based preview never used them), but to fully revert: rename layers 8/9 back to blank in `ProjectSettings/TagManager.asset` and re-set `Boundaries`/`Hoop`*/`Hoop (1)`/`Hoop (2)` GameObjects' layer back to `0` (Default).
5. **Camera**: on `Main Camera`'s `ThirdPersonCameraFollow`, set `aimPositionOffset` back to `(-1.53, 0.80, 0)` and `aimDistance` back to `2.53`.
6. Reload the scene (or restart the Editor) after a script rollback to force recompilation.

---

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
