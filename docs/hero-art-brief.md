# Hero art brief — harp seal pup (frame-by-frame sprites)

Design the hero character for the Unity game in this repo and export frame-by-frame sprites.

## Character
- Cute harp seal pup: white fluffy fur, big dark glossy eyes, small dark nose, whisker dots, pink cheeks.
- Red knit beanie with a white pom-pom; aviator goggles (brass rim, light-blue lenses) resting on the beanie.
- Teal scarf around the neck with a short tail that flutters.
- Stands and walks upright on its two rear flippers (they act as feet); front flippers act as hands.
- Chibi proportions: big head, round body, head and body read as ONE connected shape (no visible neck seam).
- Side view, facing right.
- Original design. Not a penguin, and not in the style of any existing game.

## Style
- Clean cartoon vector: medium navy outline `#16304f`, soft gradient shading, small white highlights.
- Match the existing art in `Tools/make_art.py` (palette, outline width ~5px at this size).

## Output
- One complete drawing per frame (no separate limb sprites). Transparent background. 300×340 px.
- Feet touch the bottom at y ≈ 320 in standing frames; body centre around x = 150.
- Save PNGs to `Assets/Resources/Art/`, SVG sources to `Assets/Art/Source/`.

| Animation | Files | Notes |
|---|---|---|
| Idle | `pup_idle_0..3` | breathing, one blink frame |
| Walk | `pup_walk_0..5` | alternating steps, feet lift, flippers swing opposite, slight bob |
| Hang | `pup_hang_0..3` | hugging a vertical rope in front of the belly with both flippers, feet on a knot at the rope end; rope centre at x = 214; leaning/kicking to pump the swing |
| Flail | `pup_flail_0..3` | knocked into the air, flippers flapping, scared face (rotation is done in code) |
| Cheer | `pup_cheer_0..3` | flippers up, little jump, happy open mouth |
| Glide | `pup_glide_0..3` | flippers spread like wings, scarf streaming back |
| Ball | `pup_ball` | curled into a round ball, eyes squeezed shut |
| Dizzy | `pup_dizzy_0..3` | spiral eyes, swaying |

## Quality bar
- No overlapping outlines across the face, no limbs covering the eyes, no gaps where limbs meet the body.
- Consistent size and proportions across all frames (no popping between frames).
- The previous attempt `Tools/make_pup_frames.py` was rejected as ugly with drawing errors; do not reuse its shapes.
