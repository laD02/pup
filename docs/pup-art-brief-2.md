# Hero art brief 2 — seal pup sheet E (arena animations, ChatGPT)

Done 2026-11-11: `pup_sheet_e.png` sliced into pup_fly_0..7, pup_dive_0..1, pup_ninja_0, pup_getup_0..3.

Same pipeline as the other pup sheets: 4×4 sheet on white → `Assets/Art/Reference/pup_sheet_e.png` →
`python3 -I Tools/slice_sheets.py` → **Seal Catapult → 2. Build Project**. Use **Edit** on `pup_sheet_a.png` in ChatGPT and
ask it to replace the rows (a fresh image drifts in style). Until this sheet exists the game uses older sets instead
(fly = glide, dive = launch, getup = dizzy, ninja = flail), so nothing breaks.

## Prompt (attach `Assets/Art/Reference/pup_sheet_a.png`, use Edit)

> Keep exactly this character, style, size and white background. Replace the four rows with:
> Row 1 — FLY (frames 1–4): the pup flying horizontally to the right like a torpedo, body level, front flippers
> flapping (up / mid / down / mid), scarf streaming straight back, happy determined face.
> Row 2 — FLY (frames 5–8): same flight, four more in-between frames so the flap loops smoothly.
> Row 3 — cell 1 and 2: NOSE-DOWN turn, the pup tucking its head down and curling (body tilting from horizontal to
> 45° down, then head down with flippers tucked); cell 3: NINJA HANG, the pup frozen mid-air in a crouched ninja pose with a
> red headband, arms crossed; cell 4: empty (white).
> Row 4 — GET UP (frames 1–4): the pup flat on its belly after a fall, pushing up on its flippers, sitting, then standing
> upright and dusting snow off. Feet on the same baseline in all four.

## Checks before slicing
- Even 4×4 grid, nothing touching a cell border, row 4 feet on one baseline (the slicer scales to the get-up row).
- Move the PNG out of `~/Downloads` into `Assets/Art/Reference/`.
