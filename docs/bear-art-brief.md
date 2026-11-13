# Enemy art brief — polar bear guard (ChatGPT sprite sheet)

Done 2026-11-11: `bear_sheet.png` (idle/taunt/dizzy/ko) and `bear_sheet_b.png` (eat/sleep), villain look (angry brows, fangs,
scar, big belly). Redraw with the same prompts if the style must change; keep the villain traits.

Same pipeline as the seal pup (`docs/hero-art-brief.md`, `Tools/slice_sheets.py`): one 4×4 sheet made in ChatGPT,
dropped into `Assets/Art/Reference/bear_sheet.png`, cut by `python3 -I Tools/slice_sheets.py`, then
**Puffer Launch → 2. Build Project**. Keep the seal's drawing style: start from the Edit tool on `pup_sheet_a.png`
(same line weight, shading and palette) and ask for the content below, instead of a fresh image.

## Prompt (paste into ChatGPT, attach `Assets/Art/Reference/pup_sheet_a.png` as the style reference)

> Draw a 4×4 sprite sheet of a cute cartoon POLAR BEAR in exactly the same style as the attached seal sheet:
> medium navy outline, soft gradient shading, small white highlights, white background, every cell the same size,
> the character the same size in every cell, feet on the same baseline in every cell, front/three-quarter view,
> standing upright on its hind legs. Chubby body, round head, small round ears, pale cream belly patch, black nose,
> pink cheeks, big glossy eyes. No clothes, no props. Original design, not from any existing game.
> Row 1 — idle: 4 frames breathing (chest rises and falls), frame 4 blinks.
> Row 2 — taunt: 4 frames, both paws up, waving left and right, mouth open teasing.
> Row 3 — dizzy: 4 frames, spiral eyes, head wobbling left-right, little stars circling above the head.
> Row 4 — knocked out: 4 frames lying on its back, X eyes, tongue out, paws twitching.

Second sheet (optional, `bear_sheet_b.png`, 4×2): Row 1 — EAT: the bear holding a bowl of fish in front of its belly, scooping
with one paw to its mouth and back (4 frames). Row 2 — SLEEP: the bear standing but slumped, eyes closed, mouth open, a small
"z" rising (4 frames with the z at different heights). Same style and size as sheet 1.

Then: "Also draw a small ice-block helmet on a white background, same style, 2:1.4 ratio" → save as
`Assets/Art/Reference/bear_hat_sheet.png` (optional; the code-drawn `bear_hat` stays otherwise).

## Checks before slicing
- Grid is an even 4×4 (the slicer cuts cells by dividing the image); no cell empty; no character touches a cell border.
- Standing height roughly the same in rows 1–3 (the slicer scales the idle row to 260 px and applies that scale to all).
- Move the downloaded PNG out of `~/Downloads` into `Assets/Art/Reference/` (repo rule), never leave it in Downloads.
