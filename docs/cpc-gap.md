# CPC vs Seal Catapult: action / state / animation gap list

Sources:
- `docs/cpc-reference-report.md`, all sections §1–§20 (7 earlier play sessions plus frame-by-frame video). Cited as "R§n".
- The `cpc_ref_*.png` images.
- **A new live session today, 16/11** (flashmuseum v1.0.3, Ruffle; title, instructions, map, level 1 both phases, Perfect card, map return). Cited as "L16".
  - Screenshots in L16 came about 0.7–1 s apart, so a sub-second sequence such as the hit frame was never captured.
  - **Sound could not be heard**: no CPC audio has ever been observed.
- Code paths are relative to `/Users/hoang/Documents/MyProject/seal-catapult/Assets/Scripts/`.

Effort scale: S < half a day, M = 1–2 days, L = 3+ days or new art plus code.

New observations from L16 that are not in the report:
1. Queued penguins have varied idles: one waves a flipper, one lifts its helmet off with a flipper, and one had its helmet set down on the bridge planks.
2. The next penguin waits at the bridge tip **crouched, flippers spread forward** before it jumps to the rope.
3. A **swinging penguin's body is not plumb with the rope**. At the ends of the swing it trails nearly horizontal, feet out (centrifugal look). Near the bottom it hangs upright.
4. A stuck (OOPS) penguin **loses its helmet**. It lies belly-up under the sloped ceiling near the exit, flippers spread.
5. Phase transition: a **giant bare-headed penguin hangs from the wavy bottom edge of the water curtain** (both flippers up, feet dangling) as it comes down. A smoke puff shows at the catapult arm.
6. The mission card shows the penguin **bare-headed, bending to the helmet lying on the ground**.
7. On the dive the body turns to **near-vertical head-down**, with 2–3 white speed lines.
8. Hitting ice:
   - The helmet flies off as a separate object.
   - The penguin turns into a black ball with **grey smoke dots**.
   - Small ice squares **hang in the air in a cluster** and stay there for seconds.
   - A **ring of white snow puffs** bursts at the impact.
   - The penguin rebounds the way it came and crosses the whole arena.
   - It hits the left cliff in a **white circular flash**, and also breaks the ice it passes.
9. The camera **drifts horizontally a little** (the left cliff edge moved about 15 px) and drops low when the pup goes low.
10. Clouds drift.
11. Killing all bears ends the level at once with "Perfect!". The Perfect card shows **bare-headed** penguins dancing, flippers up, with sparkle stars around the title.
12. On the map, the hero stands on a green pad at the next node.

---

## 1. Launch phase (pit)

| # | CPC behaviour | This game today | Verdict | What to add/change | New art? | Effort |
|---|---|---|---|---|---|---|
| L1 | Tap/Space fires. Taps during the ~0.5–1 s reload are ignored. | `LaunchPhase.TryFire` / `CanFire` (l.89, 218). Reload in `WhackerMachine.Swing` (l.108). | same | – | no | – |
| L2 | The arm sweeps up over the top in ~0.1–0.25 s. The **snowball fixed on the arm end** smacks the pup. Yellow flash at contact. The arm returns in ~0.5 s with a small overshoot. (R§12, §16.1) | `WhackerMachine.Swing` l.85–111: windup 0.08 s, sweep 0.24 s, spark at contact l.127, return with a −6° overshoot. | same | – | no | – |
| L3 | **Gears turn and the frame jolts** while the arm moves. Smoke/dust puff at the arm end on every shot, hit or miss (R§3.3, §16.1; L16 saw the puff). | Base squash `Kick` l.147. The machine puffs smoke only on a hit (`LaunchPhase.Fly` l.277). No gears. | different | Always puff from the arm tip at the sweep's end, inside `Swing`. Add 2 gear sprites on the base that spin during the sweep and the return. | yes: 2 gear sprites cut from catapult_base, or a separate gear sheet | S |
| L4 | **Operator penguin** stands on the deck holding the lever and pulls it on every shot (R§3.3). | `LeverReady`/`LeverPull` (l.146, 230) with 4+4 frames. | same | – | no | – |
| L5 | Operator idles between shots: looks around and shifts. Not observed in detail; L16 only saw it standing still with the lever. | Breathing + blink only (`PupRig.Update` l.247). | same / not observed | – | no | – |
| L6 | Rope: about ±45–57° swing, period ~1.1–2 s, getting wider with later penguins (R§12, §16.1). | `SwingRope.Carry` 0.80–1.00 rad, widening with queue index (l.183–194). | same | – | no | – |
| L7 | **Penguin body trails the swing**. At the ends it lies nearly horizontal with feet flung outward. At the bottom it hangs upright (L16 #3). | `SwingRope.Hang` l.213 keeps the body straight along the rope. The hang frames only speed up with omega (`PupRig.Update` l.258). | different | Add a lean on top of the rope angle that follows angular speed and gravity (`rotation = angle + k·omega`, about ±35° extra). Better: frame-select by phase, using a hang frame at the extremes and a "flung" frame mid-swing. | ideally yes: 2 "flung horizontal" hang frames; a rotation-only version needs none | S (rotation) / M (frames) |
| L8 | The next penguin walks to the bridge tip, **crouches, flippers forward, looking down**, then jumps onto the rope. The rope **jerks**, and the penguin **swings down to the LEFT first** (R§11.2, §12; L16 #2). | Walk → `Grab` 0.42 s → `rope.Grab` lerps the pup along an arc in 0.3 s (SwingRope l.135–157). | different | Add a 0.3–0.5 s "wait at edge, look down" hold using `Crouch` (pup_leap_0, already loaded, unused) before Grab. Start the carry with negative omega so the first swing goes left. | no | S |
| L9 | Queued penguins each have **their own idle**: sway, wave a flipper, turn the head, lift the helmet, set the helmet down and pick it up (R§10.4, §16.1; L16 #1). | All queued pups play the same `Idle`/`Walk` (LaunchPhase l.170–181). | missing | Give each queued pup a random antic timer. Play one-shot "wave" / "hat tip" / "look around" clips, then return to Idle. | yes: pup_wave (4), pup_hattip (4), pup_look (4) | M |
| L10 | The queue shifts right one slot after each launch, with a waddle walk (R§3.2). | `MoveTowards` slot with the Walk pose (l.178). Hop on `MoveTo` only (l.420). | same | – | no | – |
| L11 | Good hit: the penguin flies **flat, 25–35°, up-right**, rotating, and leaves the screen in ~0.2 s (R§12, §16.1). | `Fly`: rail-pulled angle (`strikeAngle`), 540° spin for the first 0.35 s (l.301), then leans along velocity, with a trail. | same, mostly | Check that `strikeAngle` is at 25–35° (R§16.5 asked for it). The current comment says 48°. | no | S |
| L12 | **"OK" call-out**: big orange text with a thick white outline, tilted, ~25% of the screen, mid-left under the bridge. It **pops ~0.35 s after the hit** and fades by ~1 s (R§12, §16.1; L16 saw it fading). | `CallOut` at `ViewCenter + (0, 1.2)` (l.267), as `Fx.FloatingText` with a back-ease pop and an upward drift (Fx l.147). Shown on escape, not delayed. "GREAT!" / "OK!" / "OOPS!". | different | (a) Tilt the text by −8°. (b) Hold it still: no upward drift (drift velocity 0). (c) Move it left, under the bridge. | no | S |
| L13 | **Gold sparkle ring at the rope end** right after the penguin leaves (R§16.1). | Star burst at the pup's position on escape (l.335). Nothing at the rope end. | missing | Add `Fx.Burst("spark", rope.EndPoint, 8 …)` in a ring (`speed` low, life 0.4) on release. | no | S |
| L14 | **Bad timing**: the penguin flies steep, **sticks belly-up in the icicles or the sloped ceiling**, flippers and feet kicking, **helmet gone**. It stays until the phase ends. Several stick side by side. Red "OOPS" (R§3.2, §11.2, §15.1; L16 #4). | `KnockedOut` l.365–394: sticks along the normal, `Stuck` pose (8 frames), "OOPS!", stars, squash, dizzy orbit 2.5 s. Helmet stays on. | different | Pop the helmet off on the stick: reuse `PufferController.DropHat` logic, or spawn `pup_hat` debris falling to the floor. Use bare-headed stuck frames. | yes: pup_stuck_bare (4) | S code / M art |
| L15 | A miss **whiffs**: no text (or not observed). The penguin keeps swinging. The counter "x0N" **drops on every shot**, misses included (R§3.2). | A whiff costs nothing. Green "grr" text at the operator (l.262). The counter drops only on a launch. | different (deliberate design) | Keep it (it is friendlier). If a whiff should cost something, make the operator flinch/stomp instead of showing text. | maybe: pup_stomp | S |
| L16 | A penguin on the bridge hit by the snowball falls off and lies on the left ledge (R§11.2). It was later found to be a misread (R§12): the snowball does not fly. | Removed (R§12). `Flinch` frames exist but are unused. | same | Delete the `pup_flinch` load or reuse it for L9/L15. | no | S |
| L17 | **Escaped-penguin count** shows only as "x0N" counting down (R§15.1). | `QueueChanged` → HUD `SetPuffers`. | same | – | no | – |
| L18 | Phase end: the last penguin out, then ~0.5 s, then the curtain. | `NextPup` → 0.6 s → `onDone` (l.193). | same | – | no | – |
| L19 | **"FIRE!"** golden-shot praise and a power-up disc in the top-right at level 17+ (R§9.4, §9.11). Reward not known. | "GREAT!" + star burst + haptic + extra shake (l.324–329). No disc. | different | Optional: a hit-able bonus target in the exit channel on later levels. | yes: disc sprite | M |
| L20 | The pit background is a bright outdoor igloo town with pink clouds. The icicle ceiling is flat, then cut diagonally (R§15.1). | `bg_pit` painting (l.124). | same (per R§15.2) | – | no | – |

## 2. Pup / penguin states (all phases)

| # | State | CPC behaviour | This game today | Verdict | What to add/change | New art? | Effort |
|---|---|---|---|---|---|---|---|
| P1 | Queued idle | See L9 | `Idle` | missing variety | see L9 | yes | M |
| P2 | Walk | Waddle to the tip | `Walk` (6 frames, distance-driven) | same | – | no | – |
| P3 | Waiting at the edge | Crouch, look down (L16 #2) | not used (`Crouch` loaded but unused) | missing | see L8 | no | S |
| P4 | Grab / jump onto the rope | Jumps and hugs the rope | `Grab` (3 frames) + arc lerp | same | – | no | – |
| P5 | Hanging / swinging | Leans with the swing (L7) | `Hang` 4 frames, plumb | different | see L7 | yes/opt | S–M |
| P6 | Launched | Spins, then flies flat | `Launched` 4 frames, spin then lean | same | – | no | – |
| P7 | Stuck | Belly-up, kicking, bare-headed | `Stuck` 4 frames, helmet on | different | see L14 | yes | S/M |
| P8 | Phase-2 glide | **Torpedo, belly down, flippers swept back, feet trailing, body quivering slightly**; flat, constant speed, ~3 s per screen; wraps at the same height (R§11.2, §16.2; L16) | `Fly` (pup_fly, 4 frames @ 9 fps, flapping). Height and speed come from the phase-1 exit (`AttackPhase.Entry` l.410). | different | (a) If `pup_fly` flaps, CPC holds the flippers back: use 2-frame tremble frames, or `Glide`, which is loaded and unused. (b) R§16.5 flagged the entry link to phase 1, which GameManager l.292 kept on purpose; the user decided to keep it, so leave it. | maybe | S |
| P9 | Dive | **Turns to ~70–90° head-down in ~1 frame (≤0.1 s), stays a stiff torpedo**, 3 white speed lines, falls ~2–2.5× glide speed (R§14.2, §16.2; L16 #7) | `NoseDown`: `Dive` pose, rotates 0→90° over **0.2 s**, then **curls into a ball and spins −540°/s** before any impact (PufferController l.177–194). Wind streaks (l.117). | different | CPC stays a **stiff head-down penguin until the first impact** and only balls up after the hit. Hold the `Dive` pose rotated at diveAngle until `OnCollisionEnter2D`, then `PuffUp()`. Cut the turn to 0.08 s. | no | S |
| P10 | Hit bear | The bear flips. The penguin **rebounds back and low** (≈ one body height), balls up with a 3-dot grey smoke trail (R§16.2, §18, §20.2) | Reflect + min upward `bearBounce`, `boing`, puff, hat drop (l.292–303) | same | – | no | – |
| P11 | Hit ice while diving | The block bursts. The penguin rebounds ~½ body back, then falls on (R§20.2). L16: a **ring of white snow puffs** at the impact | `IceRebound` 1.4 (l.321–330). Shards + rubble + puff (Block l.165–172). | different | Add the radial ring of 6–8 `snow_puff` at the impact point (Fx.Burst with zero gravity and a short life, 0.25 s). | no | S |
| P12 | Glide through ice | Breaks it and flies straight on (R§20.1) | l.305–310 | same | – | no | – |
| P13 | Hit rock wall | **White round flash** at the contact, then falls to the foot with a round dust ring (R§18; L16 #8) | Squash + thud + shake, a dent on bare rock (l.338–356). No flash. | missing flash | Spawn a white circle sprite (`snow_puff`, size 1.2, life 0.12, no fade-in) at the contact on every solid hit. | no | S |
| P14 | Slope slide | Belly slide downhill with **orange friction fire**, still hits bears (R§20.2) | l.358–368, sparks (l.118). The ball pose is kept. | different | During `sliding`, show a belly-slide pose (not the ball), rotated along the slope. | yes: pup_slide (2) | M |
| P15 | Bounce chain | Ball, tall bounces (~30% screen), several bounces, smoke trail (R§16.2) | `BaseBounces` 2 + upgrade, `Fx.Smoke` trail (l.119, 369–375) | same | Check the bounce height against R§16.5's "~30% screen". | no | S |
| P16 | Helmet off | On the **first** impact the helmet flies off as its own body, falls, and sometimes lands on a bear's head (R§16.2; L16) | `DropHat` physics hat, fades after 2.5 s (l.385–404). Ignores bears. | mostly same | Optional: if the hat lands on a standing bear's head, snap it there (CPC "bears wearing soldier hats", R§9.6). | no | S |
| P17 | Landing / rest | Stands up **on its feet**, **flippers spread**, dizzy (stars or 2–3 snowballs circling), **smoke over the head**. Stays there **until the next penguin arrives**, then vanishes (R§11.2, §14.2, §16.2, §20.2) | `WalkOff`: GetUp + dizzy orbit, 0.55 s, then **walks 1.6 units and fades** (l.435–460) | different | Replace the walk-off with: GetUp → hold the `Dizzy` loop with `Fx.Smoke` puffs over the head → on the next spawn, poof (`Fx.Puff`) and remove. This is CPC's "litter of dazed penguins". | no | S |
| P18 | Celebrate on a bear | Penguin stands on the belly of a downed bear and waves (R§10.5). Bears now **fall out** of the arena (R§18), so there is no belly to stand on. | Removed along with lying bears. `Cheer` is only used by the cage pup. | n/a | Use `Cheer` for a pup at rest when its dive took ≥2 bears (combo pride) instead of `Dizzy`. | no | S |
| P19 | Flail mid-glide bonk | Not observed separately | `Flail` on a non-dive first hit (l.351) | – | – | no | – |
| P20 | Ninja | Java-only bubble + kick (R§10.5). Web not observed. | `NinjaDrop` (l.203–235) | own design | – | – | – |
| P21 | Fire | Orange-yellow trail, smashes ice far (R§9.4) | `Ignite` sparks trail (l.257–263) | same idea | – | – | – |
| P22 | Off-screen fall | A penguin bouncing off the right edge **drops out of view** and the next penguin flies in (L16) | `Finish()` when out of bounds (l.145–147) | same | – | no | – |
| P23 | Bare-headed variants | Every post-impact pose is bare-headed, including stuck, the mission card and the Perfect card (L16) | `GoBare` covers idle/flail/walk/cheer/dizzy/getup. **Missing for stuck, ball-rest and the result card.** | partial | Add `pup_stuck_bare`. The result card should use `pup_cheer_bare_*`. | yes: stuck_bare | S |

## 3. Attack phase (arena flow)

| # | CPC behaviour | This game today | Verdict | What to add/change | New art? | Effort |
|---|---|---|---|---|---|---|
| A1 | A tap anywhere dives. Taps near the left edge or while wrapping are swallowed (R§14.2). | `InputGrace` 0.2 s only (PufferController l.99). Every tap counts. | better (R§8 wanted this) | – | no | – |
| A2 | Never dives by itself. The penguin circles forever (R§9.7). | Auto-dive after `maxLoops×4` laps (l.139–143) plus a hint nudge. | different (own design) | keep | no | – |
| A3 | Hitting **all** bears ends the level **immediately** with "Perfect!" (R§10.2; L16 confirmed: 04/02 with a penguin still left) | `WinDelay` 1.6 s (AttackPhase l.491–495) + a slow-mo last blow (GameManager l.130–140) | same | – | no | – |
| A4 | Quota met but bears left: play on until the last penguin (R§4.6, §16.2) | `WaitForSettleThenJudge` (l.427) | same | – | no | – |
| A5 | Combo "x2", "x3": big brown-gold, tilted, rising **from the impact point** (R§9.3, §10.5) | `×N` at a fixed `ViewCenter + (0, 0.6)` (l.488) | different | Spawn at the bear's position + up 1.2, rotated −10°. | no | S |
| A6 | Each downed bear sends a **star to the HUD counter**. The counter **bumps** (R§10.5, §11.2) | `HudView.StarToCounter` + `Bump` (l.237–260) | same | – | no | – |
| A7 | No mid-level banner. A tutorial strip appears only on boss levels (R§17.3) | `hud.Banner(Loc.T("attack"))` (GameManager l.291) + hint bar | different (own design) | Optional: drop the "attack" banner for CPC purity. | no | S |
| A8 | Leftover ice shards **stay hanging in the air** at the break point for seconds, then drift away (R§16.2; L16 #8). Rubble lies on beams (R§14.2). | `Fx.Burst` shards (life 0.9) + `Fx.Rubble` 4 pieces stay (Block l.168–169) | partial | Add 6–10 tiny shards with zero gravity, drag ~3, and a life of 3–5 s at the break point (the "hanging cloud"). | no | S |

## 4. Bears

| # | State | CPC behaviour | This game today | Verdict | What to add/change | New art? | Effort |
|---|---|---|---|---|---|---|---|
| B1 | Idle signatures | Each bear loops one antic for 1–2 s: shadow-box, eat from a green bowl, sit hugging knees with the head turning, arms crossed and smug, point and laugh "ha ha", pat belly, look at the sky, sit on a beam with legs dangling (R§4.5, §11.2, §16.3) | 15 poses: idle, taunt, laugh, scratch, juggle, flex, punch, smug, sit, bowl, eat, sleep, dangle (BearRig l.14, Bear l.70–74, 90–93) | same / richer | – | no | – |
| B2 | **Head tracks the flying penguin** (R§16.3). L16: the sitting bear's head moved between frames | Bears all face left; a 4° body sway only (Bear l.113–114) | missing | Cheap version: flip `Face(right)` when the pup passes the bear's x (bears turn to follow). Better: a separate head sprite rotating toward the pup. | cheap: no / better: yes, head-split frames | S / L |
| B3 | Alert as a penguin dives toward it | not observed | none | not observed | – | – | – |
| B4 | Hit | **Flips over with X / spiral eyes, tongue out, stars burst**, a small hop back, then **drops through the floor** out of view. The counter goes up at once (R§11.2, §18, §20.2) | `Defeat`: KO frames, hop (`KnockUp` 3.5), 360° spin 0.55 s, collider off so it falls through, stars + puff + shake, `bear_down` (l.150–187) | same | – | no | – |
| B5 | Falls with a broken block | Counted (R§4.4) | `bearFallDistance` check (l.116) | same | – | no | – |
| B6 | Helmet bears | From ~level 11 bears wear **the penguins' green soldier helmets** (R§9.6). Hit behaviour not observed. | Ice helmet, 2 hp: shards, Dizzy, flinch squash (l.130–148, 190–203) | different (own design) | Optional story touch: draw the helmet as a stolen seal hat. | maybe | S |
| B7 | Sleeping / eating | Eating from a bowl is seen. Sleeping is not seen in CPC. | `Sleep`, `Eat`, `Bowl` | superset | – | – | – |
| B8 | Bear on the right-edge shelf, half cut by the screen edge | `EdgePerch` (R§14.4) | same | – | – | – |
| B9 | Bears never attack and never walk (R§4.5) | static | same | – | – | – |
| B10 | Result-card bear | Lies on its back with spiral eyes and tongue out (L16) | `bear_ko_0` static (ResultView l.52) | same | Optionally animate `bear_ko` 4 frames with `FrameLoop`. | no | S |

## 5. Boss

| # | CPC behaviour | This game today | Verdict | What to add/change | New art? | Effort |
|---|---|---|---|---|---|---|
| X1 | Guard-bear size, **arms crossed**, steam curling above the head (angry). No HP bar (R§10.6). | Guard size. Steam jets from the ears (BossBear l.40–57). The pose is the default `Idle`. | different | Set `rig.SetPose(BearRig.Pose.Smug)` (arms crossed) as the boss's resting pose. | no | S |
| X2 | Hit 1: **4 dizzy stars**, the penguin's helmet flies off, the boss steps back but stays up (R§10.6) | Star burst, Dizzy, `bear_down` sfx (l.126–136) | partial | Add `Fx.DizzyOrbit` with stars (4) over the boss head for 1.5 s, plus a small knock-back (`rb.velocity = (1.2, 1)`). | no | S |
| X3 | Button pressed: green → red, snow puff, **crate bridge above the boss vanishes** (R§10.6, §19) | `FloorButton.Press` swaps the sprite, green sparks, `Pressed` → gated blocks break and the boss is thrown off its perch (`Toppled` l.104–116) | different mechanism, same effect | – | no | – |
| X4 | Hit 2: the boss **vanishes / falls out**. 01/01 (R§10.6) | `KnockOut`: KO pose, stays in place, collider off (l.138–145) | different | Reuse `Bear.Defeat`'s hop + spin + fall-through for the boss. | no | S |
| X5 | Rescue: the camera **pans to the cage**, the cage bursts (bars bend out, gold stars swirl), the penguin pops out, lands, and waves both flippers (~1.5 s) (R§10.6) | `Cage.Open`: swell, bars fly, 5 orbit stars, `Spin` → land squash → `Cheer` (ArenaObjects l.231–285). **No camera pan.** | partial | Point `focus` at the cage during `BossFinale` (GameManager has `focus`; AttackPhase would need a callback). | no | S |
| X6 | Boss mission strip: "Hit the Boss bear several times…" at the bottom of the screen (R§9.5) | Hint bar exists | check | Verify that boss levels set the hint text. | no | S |

## 6. Objects

| # | Object | CPC behaviour | This game today | Verdict | What to add/change | New art? | Effort |
|---|---|---|---|---|---|---|---|
| O1 | Ice block | One type. Breaks on **any** touch into 10–15 small squares that spread along the hit and hang. Neighbours stay floating, with no gravity (R§17.1, §19, §20) | Static blocks, `Break` on any pup touch, 10 shards + 4 rubble + puff + shake (Block l.69, 89–94, 147–179) | same | see A8 for the hang | no | – |
| O2 | Wedge blocks | Triangles at beam ends (R§17.1) | `block_wedge` (Block l.38–49) | same | – | – | – |
| O3 | Wooden crates | Boss level only. Unbreakable. Removed by the button (R§19) | `BlockMaterial.Stone` crate, gated by group (Block l.92) | same | – | – | – |
| O4 | Button | Green raised → red pressed, snow puff (R§10.6) | `button_up` / `button_down`, green sparks (l.161–169) | same | Add a white snow puff on the press (`Fx.Puff`). | no | S |
| O5 | Cage (arena) | Hanging brass cage, penguin inside | Swings 4° (l.227). The captive is in `Idle`. | partial | Captive should play a "help" wave/flinch loop. Reuse `Flinch` frames or `Cheer` slowed. | no | S |
| O6 | Cage (map) | On an orange base with a blinking red "HELP!" and a hammer icon. Opens and empties after the win (R§2, §10.7, §17.2) | `cage` + pulsing "help_cry" (MapView l.193–200). After the win the cage stays closed with stars. | partial | After the boss win show `cage_open` with no captive. | no (cage_open exists) | S |
| O7 | TNT, bumper, fish | Not in CPC web levels 1–20 (R§17.3) | `Tnt`, `Bumper`, `Fish` | own additions | – | – | – |
| O8 | Dent in rock | Black dent where the penguin hit the mound side (R§16.2) | `dent` decal (PufferController l.342–348) | same | – | – | – |

## 7. Camera & FX

| # | CPC behaviour | This game today | Verdict | What to add/change | New art? | Effort |
|---|---|---|---|---|---|---|
| C1 | Phase 1 camera fixed | Fixed `LaunchPhase.ViewCenter` | same | – | – | – |
| C2 | Phase 2: the camera **slides down with the diving penguin** (shows the ice under the floor), **stays low while it bounces**, and rises for the next one (R§15.1, §16.2) | `GameManager.Update` l.215–227 (FollowDown 1.0, heldLow) | same | – | – | – |
| C3 | Phase 2: a **slight horizontal drift** following the penguin (R§4.3; L16 #9: the cliff edge moved ~15 px) | x fixed at `ViewCenter.x` (l.226) | missing (small) | `x = c.x + clamp((pup.x - c.x) * 0.08, ±0.5)`. | no | S |
| C4 | Screen shake | Not clearly observed in CPC (frames too sparse) | Cinemachine impulse on breaks, bears, TNT, launch | not observed | Keep. Verify it is not too strong against CPC's calmer look. | – | – |
| C5 | Hit-stop / slow-mo | Not observed in CPC | `TimeDip` 0.06 s per bear, 0.45 s slow-mo on the last | own addition | keep | – | – |
| C6 | Clouds drift slowly (phase 2 and title) (R§15.1, §16.4; L16) | `bg_arena_sky` is static | missing | Add a scrolling cloud layer (one tiled sprite, UV offset; no per-frame allocation). | yes: cloud strip cut from bg_arena_sky | S |
| C7 | Impact flash ring (P11, P13) | none | missing | see P11/P13 | no | S |
| C8 | Dive speed lines: 2–3 white straight lines | `Fx.WindStreak` | same | – | – | – |
| C9 | Dizzy: snowballs or stars circling the head | `Fx.DizzyOrbit` (3 balls) | same | – | – | – |
| C10 | Smoke on the resting penguin's head (R§16.2) | none | missing | see P17 | no | S |

## 8. Audio

CPC audio was **not observed in any session**: the tooling cannot hear, and the videos have no usable sound (R§7, §13). Every row below is "CPC: not observed".

| Event | This game today | Note |
|---|---|---|
| Catapult fire | `click` + `launch` (WhackerMachine l.88–90) | – |
| Whiff | `splash` 0.4 vol (l.106) | – |
| Pup hit by the snowball | `boing` (LaunchPhase l.279) | – |
| Escape | `star` (l.334) | – |
| Stuck | `zap` (l.383) | no "oops" voice |
| Grab rope | `splash` 0.5 (l.207) | odd choice: a rope creak would fit better |
| Curtain / scene wipe | `splash` | – |
| Glide entry | `launch` (AttackPhase l.405) | – |
| Dive | `dive` | – |
| Curl into a ball | `puff` | – |
| Bear bounce / bounce | `boing` | – |
| Bear down | `bear_down` | – |
| Combo | `combo` | – |
| Ice / snow / crate break | `pop` / `sand` / `thud` | – |
| TNT, bumper, button, fish, cage | `explode`, `bumper`, `button`, `fish`, `cage` | – |
| Win / lose | `win` / `lose` (GameManager l.330) | – |
| Music | `music_reef` (pit, map), `music_arena` | – |
| **Missing candidates** | queue penguin chatter, operator lever creak, bear idle grunts / "ha ha" laugh, boss roar, cage creak, rope creak, slide scrape | add only if wanted; there is no CPC evidence |

## 9. UI / flow

| # | Screen | CPC behaviour | This game today | Verdict | What to add/change | New art? | Effort |
|---|---|---|---|---|---|---|---|
| U1 | Intro | ~25 s cartoon: bears, a rolling snowball scoops up penguins, a cage, the bears carry the cage off. "Skip" at the bottom right (R§9.1, §15.1; L16) | `IntroView`: 5 story stills, played once (`seenIntro`) | different (stills vs animation) | OK as is. Optionally add Ken Burns pan/zoom per still. | no | S |
| U2 | Title | Logo top; big green Play with a penguin icon; 3 teal square buttons top-centre; **clouds drift**; ice floes (R§15.1) | `MainMenuView`: painted bg, a flyer pup looping with a star trail, whacker + operator, smug bear on ice (l.33–55, 140–158) | richer | – | – | – |
| U3 | Instructions | Auto-shown after the first Play, 2 columns (Catapult / Diving) with mouse/SPACE icons, Continue (R§10.7; L16) | `InfoView.ShowHowTo` 2×2, `seenHowTo` | same | – | – | – |
| U4 | Map | "?" stones in an S-path, gold star discs for cleared levels, hero on a **green pad at the next node waving/raising its helmet**; after a win the **hero hops to the next node** and the cleared node turns into a star (R§2, §17.2; L16) | `node_stone` / `node_star` / `node_glow`, hero `FrameLoop` idle + `Bob` at the current node (MapView l.207–216). **No hop animation; the hero appears in place.** | different | On entering the map after a first win (pass `justCleared`), start the hero on the old node, pop the star onto it, then hop along the footprint dots (3 jump arcs with `Leap` frames) to the new node. Use `pup_salute`/`cheer` for the idle on the pad instead of `idle`. | no | M |
| U5 | Map extras | Steam puffs on the ice; standing bears; whale; "HELP!" blinking (R§2, §15.1) | painted map + "HELP!" pulse | partial | Optional: occasional steam puffs (UI particle). | no | S |
| U6 | Between phases | **Giant bare-headed penguin hanging from the wavy edge pulls the water curtain down** → logo on blue → power-up panel + red/white "Loading…" bar → Continue (R§16.1; L16 #5) | `HudView.Curtain` wave_cover drops 0.45 s → camera cut → lifts onto the mission card (l.294–324). No character. | different | Add a big `PupRig`-style UI image (pup hanging, flippers up; reuse the `hang` frames, which already show the pup holding up) attached to the curtain's bottom edge, riding it down and back up. | no (hang frames work) / better yes: 2 "hold the curtain" frames | S |
| U7 | Power-up panel | Cross-promo "Check out all 4 power-ups" every level (R§17.2; L16) | none: the shop holds the upgrades | own design | skip | – | – |
| U8 | Mission card | "Mission N", the penguin **bare-headed bending to pick up its helmet**, then saluting with it on (4-frame loop), "Hit 0X bears…", "Total bears: NN", Continue (R§14.3; L16 #6) | Card with `pup_leap_0` (crouch) → `pup_salute` loop, helmet on throughout (HudView l.279–281) | different | Use a bare-headed crouch frame plus a helmet sprite on the ground, then salute. Needs a "bend + pick up hat" frame. | yes: pup_pickhat (2–3, bare) | M |
| U9 | HUD | Pup portrait "x0N"; 4 teal buttons; bear portrait "Total NN" + "00/02"; numbers blue with a white outline (R§15.1) | `HudView` (l.34–95) | same | – | – | – |
| U10 | Pause | Grey dim, "PAUSE", cross-promo button. Press pause again to resume (R§15.1) | "PAUSED" + Resume / Retry / Map + audio (PauseMenu) | superset | – | – | – |
| U11 | Win card, Congratulations | 1 penguin standing on a downed bear; "Bears hit 05/03"; "Total bears"; Continue. The pause button is hidden (R§4.6, §15.1) | Always the 4-pup pile, `pup_cheer_0..3` as **static single frames** bobbing (ResultView l.48–58) | different | Congratulations: 1 pup on the bear. Perfect: 4. Animate each with `FrameLoop("pup_cheer_bare_",4,8)` (bare-headed, like L16), each with its own phase offset. | no | S |
| U12 | Win card, Perfect | "Perfect!" title with **sparkle stars around it**; 4 bare-headed penguins **dancing, flippers up**; bear on its back with spiral eyes (L16 #11) | `perfect_mission` title, `badge_perfect` pop at 1.2 s, confetti | partial | Add 5–6 small twinkling `star_full` sprites around the title (scale pulse, staggered). Use bare cheer frames. | no | S |
| U13 | Lose card | "Game Over …… Or is it?": 2 bears laughing, one penguin flattened in the middle; Play More Games / Play Again → main menu, progress reset (R§10.1). The iOS version: "MISSION FAILED", bear icon, 3 empty stars, OK / More (R§9.9) | "Mission failed": `bear_helmet` static + `pup_stuck_0`, Retry / attack-again / ad / map (l.60–66) | different | Show two bears using the `bear_laugh` frames (FrameLoop, already drawn) with a flattened pup between them (`pup_stuck` or a squashed scale). Keep this game's no-reset design. | no | S |
| U14 | Post-lose cut | iOS: the boss carries the cage away on a plain blue background with a progress bar (R§9.9) | The scene-wipe runner (`ScreenFader` runner l.191) is a generic running figure | similar idea | Optional: make the runner a bear carrying the cage on lose → map. | maybe | M |
| U15 | Scene transition | n/a (the Flash version cuts) | water wipe + runner | own | – | – | – |
| U16 | Stats | iOS "STATISTICS": 2x / 3x / 4x+ combos (R§9.9) | `StatsPanel` + `AddDive` | same | – | – | – |

---

## Top 20 by feel gain (biggest first)

1. **P9 Dive stays a stiff head-down penguin until impact, then balls up.** Today it curls 0.2 s after the tap, which kills the CPC "torpedo" read. `PufferController.NoseDown`, S, no art.
2. **P17 Resting pup stays on the field dazed, with smoke over the head, until the next pup.** It no longer walks off and fades. `WalkOff`, S.
3. **L7 Swing lean.** Add omega-driven tilt so the pup trails near-horizontal at the swing ends. `SwingRope.Hang`, S (rotation only).
4. **L9 / P1 Queue idle variety** (wave, hat tip, look around). M, needs 3 small frame sets.
5. **U6 Giant pup hanging on the water curtain** during the phase change. `HudView.Curtain`, S, reuse the hang frames.
6. **P11 / P13 / C7 Impact flashes**: a white snow-puff ring on ice, a white round flash on rock. `Fx`, S.
7. **A8 Hanging shard cloud** after an ice break: 6–10 zero-gravity shards living 3–5 s. `Block.Break`, S.
8. **L12 OK / GREAT / OOPS call-out styling**: tilted, holding still, left under the bridge. `LaunchPhase.CallOut`, S.
9. **L14 / P23 Stuck pup loses its helmet** and goes bare-headed. S code plus `pup_stuck_bare` art.
10. **U11 / U12 Result cards**: 1 pup for Congratulations and 4 for Perfect, animated bare-headed cheer loops, twinkling stars round the title. S.
11. **U4 Map progress animation**: a star pops on the cleared node, then the hero hops to the next one. M.
12. **B2 Bears turn to watch the passing pup** (flip facing). S cheap version.
13. **A5 Combo ×N at the impact point**, tilted. S.
14. **L8 Edge wait + first swing to the left**: crouch at the bridge tip, then omega negative on grab. S.
15. **X4 + X5 Boss falls out like a bear; the camera pans to the cage burst.** S.
16. **X1 / X2 Boss arms-crossed rest pose and a 4-star dizzy orbit on hit.** S.
17. **U13 Lose card with two laughing bears** (bear_laugh FrameLoop) over a flattened pup. S.
18. **C6 + C3 Drifting clouds and a slight horizontal camera drift** in the arena. S.
19. **L3 Catapult gears spin, and the arm tip puffs on every shot.** S plus 2 gear sprites.
20. **U8 Mission card bare-headed pick-up-the-helmet loop.** M, needs 2–3 frames.

New art needed in total (for a ChatGPT sheet request):
- `pup_wave` (4), `pup_hattip` (4), `pup_look` (4)
- `pup_stuck_bare` (4), `pup_pickhat` (2–3, bare), `pup_slide` (2)
- optional `pup_hang_flung` (2)
- gear sprite(s), cloud strip

Everything else reuses existing frames: Crouch/leap_0, hang, cheer_bare, bear_laugh, bear_ko, smug, snow_puff, cage_open.

## Not observed (still unknown)
- All CPC audio.
- Bear "alert" reaction to a diving penguin.
- Operator idle variety.
- The exact hit frame of the snowball strike, live (the screenshot latency was ~1 s; R§12 covers it from video).
- Whether CPC shakes the screen.
- Boss 2–5 on the web version.
- What the bonus disc rewards.
