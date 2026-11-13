# Pup Launch (Hải Cẩu Bay) — code name PufferLaunch

Unity 6.3 LTS (`6000.3.28f1`) · URP 2D · Android only (Google Play) · **landscape** (both ways up; the orientation is set in
`ProjectBuilder.ConfigurePlayer`, UI reference 1920×1080). Design: [GAME_DESIGN.md](GAME_DESIGN.md).

## Open & play in the Editor

1. Unity Hub → Add project from disk → this folder.
2. Open `Assets/Scenes/Boot.unity` → Play. (Any scene can be played directly.)
3. Controls in the Editor: Space / click = launch & dive, ← → = steer (Fin Steer), Esc = pause. Set the Game view to 16:9
   (or 4:3 to check the narrowest supported screen).

## Where things live

| What | Where |
|---|---|
| Feel numbers (gravity, dive speed, block HP, score…) | `Assets/Data/Tuning.asset` |
| Levels (pups, required bears, terrain, cubes, bears, objects) | `Assets/Data/Level_01..12.asset` (11 = bonus, 12 = boss), generated from `Assets/Editor/LevelDesigns.cs`; run **Rebuild Levels** after editing it |
| Upgrade catalogue (5 upgrades, prices) | code in `ProjectBuilder.CreateData`, rewritten into `Assets/Data/Upgrades.asset` on every build |
| Text EN / VI | `Assets/Scripts/Core/Loc.cs` |
| Painted art (ChatGPT, v10) | paintings in `Assets/Art/Reference/` (moved there from `~/Downloads`). Backgrounds, map, menu, story, arena terrain: `python3 -I Tools/prep_backgrounds.py` (pit widened to 20:9 with `pit_side_walls.png`, 16:9 crops, seamless cobble tile). Single props on a white sheet (catapult, blocks, objects, UI kit): `python3 -I Tools/cut_props.py` (finds each drawing, names them in reading order per its `SHEETS` list). Then **2. Build Project** (painted blocks/tiles get their own pixels-per-unit there) |
| Art stand-ins (SVG → PNG) | `Tools/make_art.py` → `Assets/Art/Source`, `Assets/Resources/Art`. Never overwrites or deletes a sprite whose painting exists (see `painted_names()`) or any name the cut tools produce, nor an existing `pup_*`/`bear_*` frame (`--stand-ins` redraws those placeholders on purpose). Needs `rsvg-convert` |
| Hero + bear frames (ChatGPT sheets → PNG) | sheets in `Assets/Art/Reference/` (`pup_sheet_a..f`, `bear_sheet*.png` per `docs/bear-art-brief.md`), cut by `python3 -I Tools/slice_sheets.py` → `Assets/Resources/Art/pup_<anim>_<i>.png`, `bear_<anim>_<i>.png`, then `ProjectBuilder.BuildAll`. Missing sheets are skipped; `make_art.py` keeps placeholder bear frames |
| Sounds & music | `Tools/make_audio.py` → `Assets/Resources/Audio`; keeps WAVs that already exist (a hand-replaced sound wins), `--force` regenerates all |

Replace a PNG/WAV with the same file name and the game picks it up — no code change.

**Driving ChatGPT for art** (Claude does this in Chrome): attach a finished sheet or painting and ask in one line, for
the same style. A prop sheet must be on plain white with space between drawings. If the share button's "Tải xuống" does
not open, download the newest big `main img` with a `fetch` + `<a download>` from the page console. Pick-one prompts
("Bạn thích ảnh nào hơn?") need a choice before the image can be downloaded.

## Menu: Puffer Launch

- **2. Build Project** – sprite import settings, TMP font, data assets, scenes. Keeps edited data.
- **Rebuild Levels** – rewrites `Level_XX.asset` from `LevelDesigns.cs` (overwrites Inspector edits).
- **3. Build Android APK (test)** – `Builds/Android/PufferLaunch.apk`, debug-signed, for sideloading.
- **4. Build Android AAB (Play Store)** – `Builds/Android/PufferLaunch.aab`, needs a keystore (below).

Command line (Editor closed):

```sh
U=/Applications/Unity/Hub/Editor/6000.3.28f1/Unity.app/Contents/MacOS/Unity
$U -batchmode -quit -projectPath . -buildTarget Android -executeMethod PufferLaunch.EditorTools.ProjectBuilder.BuildApk -logFile Logs/android.log
$U -batchmode -projectPath . -runTests -testPlatform EditMode -testResults "$PWD/Logs/tests.xml"
$U -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults "$PWD/Logs/play.xml"
```

## Install on a phone

Enable *Developer options → USB debugging*, plug in, then:

```sh
/Applications/Unity/Hub/Editor/6000.3.28f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb install -r Builds/Android/PufferLaunch.apk
```

## QA on the emulator

The game is landscape: `adb shell settings put system user_rotation 1` after boot. `adb exec-out screencap` gives a black
frame for this build (Vulkan surface); capture through the emulator console instead:
`adb emu screenrecord screenshot <dir>` (saves a portrait PNG, rotate it 90°). `Pixel_PlayStore_34` is nearly full with
other projects' apps, so installs fail with `INSTALL_FAILED_INSUFFICIENT_STORAGE`; QA uses the fresh AVD `Pixel_QA_34`
(create with `avdmanager create avd -n Pixel_QA_34 -k "system-images;android-34;google_apis_playstore;arm64-v8a" -d pixel_3a`).

**Reaching later levels in QA:** build the debuggable QA APK
(`-executeMethod PufferLaunch.EditorTools.ProjectBuilder.BuildApkQa` → `Builds/Android/PufferLaunch-qa.apk`, never upload
it), launch it once, stop it, then push a save: `adb push save.json /sdcard/Android/data/com.pufferlaunch.game/files/save.json`
(Unity's persistentDataPath on Android is the external files dir, not `files/` under run-as). A level unlocks when the
one before it has ≥ 1 star, e.g. `"stars":[3,3,3,3,0,3,3,0,3,3,3,0]` opens 5, 8 and 12.

## Release on Google Play

1. Create an upload keystore once and **keep it and its passwords safe and out of git** (losing it blocks updates):
   ```sh
   keytool -genkeypair -v -keystore ~/puffer-upload.keystore -alias upload -keyalg RSA -keysize 2048 -validity 10000
   ```
2. Build the AAB with the keystore passed through environment variables:
   ```sh
   export PUFFER_KEYSTORE=~/puffer-upload.keystore PUFFER_KEY_ALIAS=upload
   export PUFFER_KEYSTORE_PASS='…' PUFFER_KEY_PASS='…'
   $U -batchmode -quit -projectPath . -buildTarget Android -executeMethod PufferLaunch.EditorTools.ProjectBuilder.BuildAab -logFile Logs/aab.log
   ```
3. Bump `PlayerSettings.Android.bundleVersionCode` / `bundleVersion` in `ProjectBuilder.ConfigurePlayer` for every upload.
4. Play Console: create the app (package `com.pufferlaunch.game`), upload the AAB to internal testing first.

## Credits

Art and sounds are original, generated by the scripts in `Tools/`. Font: Baloo 2 (SIL Open Font License, `Assets/Fonts/OFL.txt`).
