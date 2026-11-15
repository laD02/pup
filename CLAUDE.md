# CLAUDE.md

Guidance for Claude Code in this repo. Pup Launch (code name PufferLaunch): Unity 6.3 LTS, URP 2D,
**Android only, Google Play**. Open, build, test, install: `README.md`. Game design: `GAME_DESIGN.md`.
General working rules below were ported from the one-fill-line repo (2026-11-10); its design and
app-specific rules were NOT ported — this game keeps its own design in `GAME_DESIGN.md`.

## Rules

**Everything learned goes into the .md files, in the same change as the work** (user, 2026-11-10:
"trong thời gian làm việc, học đc cái j thì phải update vào các file .md nhé"). This covers what is
learned while working too: a gotcha, a fix and its cause, a user decision, a command that works. Game design and
visual style → `GAME_DESIGN.md`, how to build/run → `README.md`, working rules → this file. If a change
needs a new pattern, write it into the matching .md first, then code it. Every later change follows
what those files say.

**Landscape only** (user, 2026-11-11, after studying the Crazy Penguin Catapult layout): every screen is designed
for 1920×1080 with 4:3 as the narrowest case; `GAME_DESIGN.md` §3 "Màn hình ngang" has the layout rules (corner
roles, camera size) that every new screen follows.

**Consistency is a hard rule.** Anything new or edited matches the rest of the game in STYLE and DATA:
- Style: follow `GAME_DESIGN.md` and reuse the existing UI kit (`Assets/Scripts/UI/UiKit.cs`), art
  pipeline (`Tools/make_art.py`) and tuning assets instead of one-off variants.
- Data: real values from game state, never placeholder numbers; same wording as neighbouring screens.
- UI text never hardcoded in a component: add the key to `Assets/Scripts/Core/Loc.cs` for every
  language. Never store translated text in state: store a key, so a language switch re-translates it.

**Accessibility is not optional.** Tap targets ≥48dp; colour is never the only signal.

**Android Back works on every screen** (audit 2026-11-15: it only worked inside a level). A new screen or popup gets a
`HandleBack()` called from its scene's root view; `GAME_DESIGN.md` §R9 lists what Back does where.

**Level content is checked by physics, not by eye.** Anything placed in `LevelDesigns.cs` must stay put in the PlayMode
settle test (blocks, bears, TNT): a TNT crate sat buried inside a ramp in level 7 until that test covered it.

**Performance.** Mobile players complain about heat and battery: a change that adds continuous
per-frame work needs a reason. Do not allocate per frame.

**Validate at the boundary.** Saved data is untrusted: check each field and fall back to defaults;
never crash on a bad or old save.

**Downloaded art lives in the project.** Every image pulled from ChatGPT or any other tool is moved (not
copied) out of `~/Downloads` into `Assets/Art/Reference/` under a clear English name (user, 2026-11-10:
"nhớ chuyển các ảnh tải về vào dự án để dễ quản lí"). Game frames are cut from those sheets by a
script in `Tools/`, never by hand.

**No secrets in the repo.** Keystores and their passwords stay out of git (env vars, `README.md`).

## Verifying a change

- A green build and passing tests are not "done" for UI. Run the change in the real app (emulator or
  device) and drive it: fast taps, pause/resume, background/resume, kill and reopen.
- Before reporting something as a bug, prove the state it needs is reachable (grep the writers, not
  only the reader).
- Report which checks ran and which were skipped.

## Ship → push → deploy → QA

Follow `~/.claude/rules/ship-push-deploy-qa.md` (applies to every project). Project specifics:
- Checks: batch-mode build (`ProjectBuilder.BuildApk`) + EditMode + PlayMode tests (`README.md`).
  UI/art/sound: install the APK on the emulator `Pixel_PlayStore_34` and play it.
- Reviewers: `csharp-reviewer` for C#, `python-reviewer` for `Tools/*.py`, `silent-failure-hunter` for
  catch/fallback paths, `performance-optimizer` for per-frame cost.
- Version: `Version` + `VersionCode` in `ProjectBuilder` (`PUFFER_VERSION_CODE` overrides the code for a rebuild).
- Deploy: `ProjectBuilder.BuildAab` signed from env vars, Play internal testing track first.
- QA on the shipped build: one full level (launch + attack), win and lose, retry, pause, level map,
  shop/upgrades, sound/music, EN/VI, kill + reopen (progress kept). Game is landscape: rotate the emulator
  (`adb shell settings put system user_rotation 1`) and check both landscape directions.
- Remote: `origin` = https://github.com/laD02/pup.git, branch `main`. `.utmp/`, `InitTestScene*` and keystores are git-ignored.

## Docs

Update the matching doc in the same change as the code: a new file or command goes in `README.md`; a
design change goes in `GAME_DESIGN.md`; a player-visible change goes under `## [Unreleased]` in
`CHANGELOG.md`.
A feature added or removed also updates the Play listing in `docs/ASO.md` §3 (every language) and `site/`
(regenerate with `site/make_pages.py`, republish); new UI text gets all 7 languages in `Loc.cs`.

## Language

Reply to the user in Vietnamese. Code, comments and commit messages stay in English.
Commits follow `<type>: <description>` (feat, fix, refactor, docs, test, chore, perf, ci).

## Memory

After a non-trivial fix or feature, add a dated memory entry: what broke, why, the fix, and the
invariant to keep.
