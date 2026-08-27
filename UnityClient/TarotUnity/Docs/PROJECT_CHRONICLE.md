# Project Chronicle — Phases 1–64

Date consolidated: 2026-08-27

This is the single, condensed history of the Tarot Unity desktop client, from the
first bootstrap through the current Phase 64 presentation baseline. Historical
per-phase documents are summarized here under their phase headings; the full
original text of deleted phase notes remains available in git history.

The following phase documents are **kept standalone** because they are the
visual north-star or the latest implementation details:

- `PHASE37_VISUAL_REDESIGN_BLUEPRINT.md` — the Midnight Parlor design north-star.
- `PHASE60_RESULT_SPREAD.md` — the multi-card Result presentation.
- `PHASE61_READING_ROOM_SLOT_STEP.md` — reading-room sockets and ritual progress.
- `PHASE62_RESULT_DYNAMIC_ROW.md` — data-driven Result card rows.
- `PHASE63_SPREAD_DEFINITION.md` — the data-driven spread catalog.
- `PHASE64_RESULT_BACKDROP.md` — the current Result parlor composition.

Cross-cutting references also live outside this chronicle: `UI_COMPLETION_MAINLINE.md`
(the completion/next-steps registry) and `THIRD_PARTY_ASSETS.md` (asset + license
provenance).

Every phase followed the same discipline: an idempotent editor bootstrapper under
`Tools/Tarot Unity/…`, EditMode/PlayMode guard tests, and screenshot verification
before any visual change. Phases 11–33 repeatedly labelled themselves as prototype
passes and **not final high-end VFX** — that caveat is theirs, recorded here once
for all of them.

---

## Era 1 — Foundations and the vertical slice (Phases 1–10)

### Phase 1 — Bootstrap
Turned the generated URP project into a stable workspace for the first vertical
slice: the folder layout, the four scenes (`Boot → MainMenu → ReadingRoom →
Result`), the core prefabs (`PF_TarotCard`, `PF_DeckStack`, `PF_SpreadSlot`,
`PF_ResultPanel`), the first script inventory, naming rules, and the Unity V1
backend API scope against `/api/v1`. No gameplay yet — structure only.

### Phase 2 — Graybox vertical slice
The first locally playable flow: start → choose one/three cards → enter a question
→ draw → deal into slots → click to flip → reveal result with placeholder
interpretation. Intentionally simple, replaceable graybox assets. Locked by the
core transition test (selected card count → all flipped → `ResultReady`).

### Phase 6 — Desktop build
Desktop prototype build support (macOS + Windows) via `Phase6DesktopBuildBuilder`,
plus runtime backend config from `StreamingAssets/tarot_desktop_config.json` with a
`TAROT_BACKEND_URL` override. This builder became the single source of truth for
player settings — product name, bundle id, scripting backend, and the presentation
window. (Its Phase-6-era `1280x720` windowed default was the latent cause of the
blurry-text regression fixed in Phases 42/43; the value now lives here as
`FullScreenWindow` / `1920x1080`.)

### Phase 7 — Immersive UI (design + plan)
Chose direction **A. Immersive Ritual Desktop** and re-styled the three scenes to
read as a real downloadable game rather than a URP sample: atmospheric main menu,
a ritual control band in the ReadingRoom, an oracle-style reading frame on Result,
Chinese-first copy, demo wording removed. Implemented in place via a bootstrapper so
the tested flow stayed intact.

### Phase 8 — Visual identity
Gave the interface a coherent palette instead of a single purple family: deep
velvet shadows, dark-green cloth, moon-gold accents, candle amber, ivory card
faces. Added `TarotUiTheme` and the first set of `MAT_Phase8_*` materials.

### Phase 9 — Motion and audio rhythm
Established the rhythm layer a 3D card game needs — paced shuffle, heavier card
travel, deliberate flip timing, staged reveal — via `RitualRhythmDirector` and
tuned `DeckController` / `CardFlipController` values. These pacing floors (e.g. the
0.65 s shuffle breath) are treated as protected by every later game-feel phase.

### Phase 10 — Release UX hardening
Made the downloadable prototype easy to run and diagnose: a release `README_FIRST`,
a `tarot_desktop_config.example.json`, and player-readable backend/offline status
copy so a player can unzip, start, and edit backend config without reading source.

---

## Era 2 — Cards, the 3D table, and ritual VFX (Phases 11–19)

### Phase 11 — Visual review and final adjustment
Created a repeatable screenshot baseline (`MainMenu.png`, `ReadingRoom.png`,
`Result.png` under `Docs/VisualReview/Phase11/`) so visual problems became concrete
instead of remembered. The review set the long-term target — a **Hearthstone**-like
3D card table — and flagged that flipped faces should later use
**real tarot card artwork** after a source and **license** review.
Separated the **Main Menu** title/
crest/action/table for depth, gave the **Reading Room** a table focus frame, and
gave the **Result** screen a card-presence panel.

### Phase 12 — Card-first reveal
Approved **Direction B** (card face first): durable face-art slot and reveal staging
on the prefab, a `Phase12_CardRevealStage` in the ReadingRoom, and a result card
showcase beside the interpretation text — without importing final deck art yet.

### Phase 13 — Tarot artwork pipeline
Wired an authentic public-domain deck. Deck id `RWS1909`, sourced from
**Wikimedia Commons** (`Rider-Waite tarot deck (Roses & Lilies)`), whose file pages carry the
Creative Commons **Public Domain Mark** (published before 1931). Staged under
`Assets/Art/Tarot/RWS1909/` with a `rws1909_sources.json` manifest, stable card keys
(`major_00`…`wands_14`), and a `CardArtworkCatalog` resolving sprites from draw data.

### Phase 14 — Dimensional card reveal
A 2.5D dimensional card reveal layer: `DimensionalCardRevealController` adds card
lift/settle/glow, and the prefab gains depth anchors under a `Phase14_DimensionalRoot`
(edge, cast shadow, rim light, artwork glass, reveal glow). All references optional
and null-safe.

### Phase 15 — 3D table foundation
The approved A2 direction: a stronger **3D table foundation**.
`ThreeDCardPresentationController` owns optional card-shell visibility; the
ReadingRoom gains a `Phase15_ThreeDTableRoot` (ritual surface, depth ring, warm key
and cool rim lights), and the card prefab gains a mesh shell (face/back/side planes,
drop shadow).

### Phase 16 — Ritual aura VFX
A controlled **Ritual Aura** rune layer. `RitualAuraController` owns optional aura/
rune/particle visibility under a `Phase16_RitualAuraRoot` (glow pool, two rune rings,
four particle anchors); transparent URP materials for light atmosphere, **not final
high-end VFX**.

### Phase 17 — Ritual Aura Runtime Motion
`RitualAuraMotionController` gives the Phase 16 layer life — rotating rune rings,
pulsing glow, floating particle anchors — wired onto the `Phase16_RitualAuraRoot`.
Optional and null-safe; **not final high-end VFX**.

### Phase 18 — Ritual Particle Systems
The first true Unity `ParticleSystem` layer. `RitualParticleSystemController` drives
optional ambient/focus/reveal arrays with null-safe methods, creating real particle
children (e.g. `Phase18_AmbientDustParticles`) under the Phase 16 anchors — kept
low-emission so table and text stay readable. **Not final high-end VFX**.

### Phase 19 — Card Action VFX Integration
`RitualActionVfxController` maps existing `PresentationCueId` values (shuffle, deal,
flip, result) to Phase 18 particle behaviour, forwarded from `RitualFeedbackController`
(the single cue entry point). Forwarding is independent of legacy cue particles.
**Not final high-end VFX**.

---

## Era 3 — Cinematic rendering to a shippable look (Phases 20–36)

### Phase 20 — Cinematic Rendering
The first true rendering-quality pass. A shared post-processing volume
(`Phase20_CinematicVolume` per scene) with bloom, **ACES** filmic tonemapping, warm
color adjustments, white balance, and vignette; cameras get HDR + SMAA + dithering;
glow materials boosted into HDR range so bloom picks them up. **Not final high-end
VFX** — the pipeline pass under all prior layers.

### Phase 21 — Cinematic Camera
Replaced the "surveillance" camera with **seated**, cinematic framing, research-driven
(Hearthstone's stable board + reactions; Inscryption's low seated table; narrow-FOV
intimacy). Five fixed ReadingRoom poses at 30–38° FOV, idle breathing, and a flip
`PunchToward(card)` lean-in with a reveal `Kick`. Also fixed a latent fontless-card-
label defect (deactivated, not deleted). Chose an in-house ~200-line controller over
Cinemachine. **Not final high-end VFX**.

### Phase 22 — UI Composition
Re-composed the ReadingRoom around Hearthstone board language
("**information far, actions near**") and HUD zone theory: a read-only top info zone, a clear card-stage
band with no UI, and a bottom **action tray** holding the question input and buttons.
Deactivated flat-era fake-table overlays. **Not final high-end VFX**.

### Phase 23 — Card Feel
Made the cards physical. `CardHoverTiltController` lifts and tilts a face-down card
toward the pointer, settling near **100ms** (research-backed: NN/G duration, Val
Head), with a `Suspend()` handshake so flip and hover never fight the transform.
Tuned the flip aftermath and deal interval. **Not final high-end VFX**.

### Phase 24 — Typography
Bundled a real CJK font instead of borrowing the OS's: **LXGW WenKai** (霞鹜文楷), a
calligraphic Kai under **SIL OFL**, free to ship. Two cuts (Regular/Medium) as
dynamic fonts, a role system in `TarotUiTheme` (display/emphasis/body), and an
editor bake so the screenshot pipeline renders the bundled font. Deferred full TMP
(revisited in Phase 43). **Not final high-end VFX**.

### Phase 25 — Result composition
Recomposed the **Result** payoff screen to one focal point using game-UI rules
(focal points, alignment, **rule of thirds**, meaning beside the card): full-width
header, card hero in the left third, reading column in the right two-thirds, footer.
Added the `TarotUiAccentText` marker so gold headers survive runtime restyle.
Redundant legacy overlays deactivated, not deleted. **Not final high-end VFX**.

### Phase 26 — Result text robustness
Guarded the **Result** reading against variable-length backend AI text: the four
body texts use Unity **best-fit** (cap at the design size 19, shrink toward a
readable floor) so a section can never overflow into the next, whatever the backend
returns. Also recorded a reverted "moonlit glow" experiment (emissive flat back
planes either do nothing or bloom badly). **Not final high-end VFX**.

### Phase 27 — HD card artwork
Re-sourced all 78 faces in **HD** (~1100×1920 **public domain** RWS1909 scans,
downscaled to height 1280) under `Assets/Art/Tarot/RWS1909_HD/` with an HD manifest,
and repointed the catalog. The old low-res folder was left for the user (since
deleted). **Not final high-end VFX**.

### Phase 28 — Holographic card face
A **holographic** foil **sheen** on the flipped 3D face: shader
`TarotUnity/HolographicCard` (URP sprite, unlit) adds a diagonal glare band by
object-space view direction, so it sweeps as the card tilts or camera breathes —
masked by the card's own alpha. `Fallback "Sprites/Default"` keeps the art safe if
the pass fails to compile.

### Phase 29 — Result reading scroll panel
Delivered the ideal Phase 26 flagged: a real scrollable reading panel
(`ResultReadingScroll`: `ScrollRect` + `RectMask2D` viewport + `VerticalLayoutGroup`
/ `ContentSizeFitter`). Every section renders at full size; overflow **scroll**s
instead of shrinking. A few older **Result** tests were updated to the new nested
structure while preserving intent. **Not final high-end VFX**.

### Phase 30 — Cross-screen consistency
A read-only audit found the three screens already share a `1280x720`, match-0.5
`CanvasScaler` and symmetric centering, so this phase **codifies and guards** that
**consistency** (`TarotUiSpacing` + regression tests) rather than moving fine
layouts — honouring the project's "don't build blind" rule. **Not final high-end VFX**.

### Phase 31 — HD visual archive
A complete `2560x1440` visual **archive** of every faithful screen under
`Docs/VisualReview/Phase31_HDArchive/`. Flagged (then fixed in Phase 32) an oversized
face-up card sizing regression from the HD swap. **Not final high-end VFX**.

### Phase 32 — Card face fit (oversized flip-art bugfix)
Root-caused the oversized flip art: `SetFaceArtwork` never sized the sprite, and HD
sprites at PPU 100 rendered ~3.4× too wide. `CardView.FitFaceArtwork` now scales the
face to a target world footprint (resolution-independent), and the prefab lifts the
face renderer clear of the cream front planes. Verified failing-before / passing-after.

### Phase 33 — Ambient audio
Gave the game actual sound. The wired `AudioManager` had an empty `cueMap`; this
added seven synthesized SFX (bells, riffle, thud, whoosh, pad, cascade) plus a
looping ambient bed, all pure-Python synthesis so it is 100% original and
**royalty-free** (no third-party **license** to track). Bound on the persistent Boot
`AudioManager`. **Not final high-end VFX**.

### Phase 34 — Main-menu quit button
Added an in-game exit (desktop builds only had Cmd+Q / Alt+F4). `MainMenuController`
gains a **quit** handler and a `QuitButton` (labelled 退出占卜, later 离席), cloned
from the start button for styling, placed as a stacked secondary action. Not a taste
change — a UX-gap fix. **Not final high-end VFX**.

### Phase 35 — Result overlay and menu polish
Fixed three objective defects a fresh archive review surfaced: a stray 3D result
stage projecting behind the reading (renderers disabled, objects kept active),
`ResultReadingFrame` seam over the hero slot (deactivated), and the menu status line
sitting on the start plate. `QuitButton` became a **quiet secondary action** (trim
off, smaller, muted). Defect repair, not composition.

### Phase 36 — Performance pass
Removed the per-flip `FindFirstObjectByType` scan (cached, self-healing accessors)
and established the measurement baseline: a PlayMode probe showing an **allocation-
free** steady state and trivial main-thread cost. Documented the real-GPU workflow —
run the macOS player with `MTL_HUD_ENABLED=1` for live FPS/GPU time.

---

## Era 4 — The Midnight Parlor redesign (Phases 38–51)

This era executes the redesign defined in the kept `PHASE37_VISUAL_REDESIGN_BLUEPRINT.md`
(ambientCG CC0 PBR + a PIL-generated gold UI kit). Phase 37 itself has its own doc.

### Phase 38 — Table rebuild (Midnight Parlor stage)
Rebuilt the ReadingRoom world in the Midnight Parlor language: one coherent
`MP_TableStage` (oxblood velvet, walnut rim beams, parlor backdrop) with gold
`MP_CardSockets`, an `MP_DeckStack` of staggered card shells, and a single composed
card-back quad on the prefab. Eight generations of overlapping flat planes were
**deleted** under user-authorized teardown, and the era tests that pinned those
objects were flipped to assert they stay **deleted** so a stale re-run cannot
resurrect them.

### Phase 39 — UI reskin (gold plaques on the velvet)
Moved the ReadingRoom canvas chrome onto the Midnight Parlor **nine-slice** kit: the
step tracker as a brass rail, the action dock as a `TarotPanel` plaque, buttons on
the `TarotButton` plaque with reset ColorBlocks, the question input as an inset slot.
Redundant flat-era frames deactivated.

### Phase 40 — Menu + Result recomposition
Put the menu and result on the shared stage. The menu (`MP_MenuStage`) became a
candlelit tabletop vignette — the brown disc halo and green slab **deleted**, a
staggered deck and scattered backs added, UI on the plaques. Result (`MP_ResultStage`)
got velvet + backdrop (no rim beam at that shallow pitch), `TarotPanel` frames, and a
real radial candle glow behind the hero card.

### Phase 41 — Final polish (redesign close-out)
Closed out the redesign: regenerated the HD archive on the four Midnight Parlor
screens, and swept 14 orphaned legacy materials (GUID-verified unreferenced, then
deleted) — the "orphaned material **sweep**". Candle modelling, hero tint, holographic
intensity, and motion were explicitly held as feedback-gated.

### Phase 42 — Text sharpness + main-menu elevation
Driven by the first real playthrough. Diagnosed the blurry built text by elimination
(two wrong hypotheses recorded): the true cause was presentation resolution — a fixed
`1280x720` windowed player stretched across a 1440p display. Fixed to `FullScreenWindow`
at native resolution. Also fixed the menu reading low-end: the room was lit by a
daylight **skybox** (ambient dropped to a near-black floor), the camera was wrong
(now seated, pitch 27°, FOV 36°), the void became a composed parlor haze, and the
candles became real tapered wax. Found and fixed two of my own staging bugs (see
Phase 45's `light.transform` note; and `GetComponent ?? AddComponent` fake-null).

### Phase 43 — TextMeshPro SDF text
Shipped the sharpness fix Phase 42 only claimed: the `FullScreenWindow` / `1920x1080`
values were being silently reverted on every build by `Phase6DesktopBuildBuilder.RunSetup`,
so they moved into the builder itself. Migrated the MainMenu to TextMeshPro SDF
(dynamic atlas, so Phase 24's "arbitrary runtime CJK" objection is obsolete), with a
LiberationSans fallback chain and a gilded gradient title. Fixed three of my own
migration bugs (asmdef TMP reference, non-idempotent gilding, async essentials import).

### Phase 44 — Menu atmosphere, ritual copy, corner exit
Fixed a coplanar loose-card clip (my staging), rewrote the copy from instructions to
ritual (入席问牌 / 离席; "你尚未开口，牌已听见"), moved the status line to the foot and
the quit to a bare bottom-right corner link, and filled the table (faint arcane
circle at alpha 0.17 — ground, not subject; dust motes; trinkets).

### Phase 45 — Menu depth, mid-ground props, type block
Filled the empty mid-ground band (drapery in the backdrop, a scrying orb in cool
violet, a censer with glowing coals, two more candles at a second depth) and set the
title/subtitle/status as a real type block. Root-caused the recurring bug that a
`Light` added to a prop root shares its Transform, so writing `light.transform.localPosition`
drags the whole prop — now `EnsureChildLight` puts prop lights on children and a test
asserts the rule so it can't return a third time.

### Phase 46 — Candles as wax, and lit
The candles were the loudest remaining tell — plastic tubing. `gen_wax.py` composes a
color map (poured grain, drips, soot, grubby base) and a **translucency** emission map
so the flame lights the wax it sits in. The translucency map needed a floor (0.24, not
zero) or the candle bodies went black at N·L≈0. `CandleFlickerController` gives each
flame layered Perlin flicker (own noise position, allocation-free). The flicker needs
the build/Play mode to see.

### Phase 47 — The orb becomes glass; button and deck craft
The scrying orb read as a plastic marble not because of geometry but shading: buying
an asset would not have fixed it. `TarotScryingOrb.shader` adds **Fresnel** rim, a
parallax equirectangular interior (`gen_orb.py`), and tight speculars; the fake halo
quad retired. The `TarotButton` gained an inner shadow and bevel; the deck became aged
paper so you can count it. A card fan and wax spills were added and then removed in-code.

### Phase 48 — The backdrop's geometric ceiling
Asked to hang a lamp/window/picture on the backdrop; the real output is the measurement
that says none can exist. A read-only diagnostic projects world points against the live
camera: at the backdrop depth the visible ceiling is only world Y 0.34, so anything at
wall height is out of shot. This exposed that Phase 45's drapery was never on screen
(regenerated to run full height). An attempted moonlight-through-a-window rig was
**dropped** because its response to intensity couldn't be explained — shipping an
effect I can't account for is worse than not shipping it. Three wrong measurement
readings recorded; the lesson is measure the pixels, not a proxy.

### Phase 49 — The ReadingRoom gets its own light
First phase on the screen the player sits in, carrying the same daylight-**skybox**
ambient defect Phase 42 fixed on the menu (ambient → near-black floor, reflections
0.06). Nothing in the room emitted light, so four candles now stand in it (same rig/
wax/flicker as the menu) plus one unseen warm fill; the old Phase 12–15 point lights
are dimmed, not deleted. Candle positions are projected against the live camera so
they can't be sliced by the frame edge again.

### Phase 50 — The ReadingRoom gets sharp text
Finished the TMP migration on the ReadingRoom's 15 legacy `UI.Text` components,
converted in place. The one piece the menu never had — the question field — was
rebuilt from `TMP_DefaultControls` into a real `TMP_InputField` (different child
hierarchy), reseated into the old field's transform/sprite/slot, and the caret and
placeholder fixed. `ReadingRoomController`'s serialized fields retyped to TMP.

### Phase 51 — The Result screen joins the world
Migrated the last 13 legacy `UI.Text` on Result and brought its ambient to the shared
Flat contract. Stated plainly: the ambient change is **not visible** here because the
Result canvas is Screen-Space **Overlay** clearing to near-black, so the daylight
ambient only lit off-frame dressing — before/after captures are identical. The value
is data honesty and matching render settings, not a look change. With this, all three
screens are TMP SDF; no legacy `UI.Text` or `InputField` ships.

---

## Era 5 — Game-feel motion chain and release (Phases 52–56)

Phases 57–59 (candle geometry, capture convergence, flame) continue past this era in
their own kept documents.

### Phase 52 — Flip weight and a reveal-synced camera
Reshaped the card flip into five beats — **anticipation** (wind back and dip),
whip to edge-on, **reveal** (face swap + cue), swing in with an **overshoot** and
scale pop, damped settle to an exact rest (asserted sub-millimetre). The camera's
arrival shake moved to fire a second `Kick` on the exact **reveal** frame so the hit
lands with the face. Every value is a serialized knob kept in a tasteful envelope;
the pacing floors are untouched.

### Phase 53 — A holographic foil on the Result hero card
Gave the Result hero card — a UI Image on a Screen-Space **Overlay** canvas with no
view angle — its own **holographic** foil. `TarotUnity/HolographicCardUI` is the UI
counterpart of the Phase 28 shader, positioned by a fed-in `_Sheen` uniform instead of
view direction; `HolographicHeroCard` drives an idle figure-of-eight drift and a
hover that snaps the band to the pointer and tilts the card. Material instanced at
runtime.

### Phase 54 — The deal lands: touchdown weight
The deal flew a lovely arc and then snapped dead. Phase 54 adds a **landing**: on the
impact frame the camera takes a small `Kick`, the card arrives compressed (**squash**:
−10% height, +6% width) and springs back ease-out to the exact base scale (asserted to
four decimals). The same motion language as the flip, on the other half of the card's
journey; the Phase 9 arc pacing is untouched.

### Phase 55 — Riffle choreography on the deck stack
Completed the motion chain on the **shuffle**, the ritual's first beat.
`DeckShuffleChoreographer` on `MP_DeckStack` plays the four beats: press-down
anticipation, a **riffle** ripple bottom-to-top (each card pops with a yaw shiver),
a square-up contact with a camera `Kick` and squash, and a settle to the exact
authored rest pose (sub-millimetre). Runs ~0.65 s to match the Phase 9 shuffle
breath. The PlayMode test drives the production path (the Phase 54 nested-coroutine
lesson).

### Phase 56 — Release 0.9.0 (presentation-complete)
Bumped the version — unmoved since Phase 6 — to `0.9.0`, marking presentation
complete (all three screens redesigned + TMP SDF; the full shuffle → deal → flip →
result motion chain). Deliberately not 1.0: the backend reading path is unverified
end-to-end and Phases 52–55 are unjudged by a player. `Phase6DesktopBuildBuilder.BundleVersion`
is the single source of truth, written into `PlayerSettings.bundleVersion` on every
build. Also corrected the stale `1280×720` line in the Phase 6 doc — the exact setting
behind the blurry-text regression.

---

## Era 6 — Final presentation passes (Phases 57–64)

The last presentation passes are summarized here so the project keeps one
historical entry point instead of one document per small visual adjustment.

### Phase 57 — Candle geometry

Replaced the stacked low-resolution candle primitives with lathed wax geometry:
pooled bases, tapered bodies, melted shoulders, rolled rims, burn craters and
drips. The flame, light positions and wax-to-flame spacing remained stable.

### Phase 58 — Capture convergence

Found that the apparent red candle defect came from screenshots taken before
realtime GI converged. `CaptureRig.RenderConverged` now warms the scene before
capture, and the capture test prevents a new single-render regression.

### Phase 59 — Flame quality

Rebuilt the flame as a reproducible 512px gradient and imported it uncompressed.
This removes compression banding while keeping the candle flame silhouette and
the existing lighting contract.

### Phase 60 — Result spread

The Result screen now renders every card in a multi-card spread while preserving
the single-card hero presentation for one-card readings.

### Phase 61 — Reading-room slots and ritual steps

Reading-room sockets became recessed glowing slots, and the ritual step indicator
now follows the live flow state instead of being a static row of labels.

### Phase 62 — Dynamic Result row

The Result card row became data-driven so the visual presentation follows the
number of cards in the selected spread rather than a fixed three-card layout.

### Phase 63 — Spread definitions

Spread metadata moved into reusable definitions, including the ten-card Celtic
Cross layout, position names and position meanings used by both local and online
reading flows.

### Phase 64 — Result backdrop

The Result screen received the current parlor backdrop and a readable quiet exit
link, completing the Phase 64 visual baseline for the next product-closure work.
