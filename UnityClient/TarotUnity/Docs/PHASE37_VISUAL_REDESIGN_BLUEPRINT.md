# Phase 37 Visual Redesign Blueprint — "The Midnight Parlor" (深夜占卜室)

Date: 2026-07-14

## Why a redesign

The user played the current build and judged the frontend far below expectation.
The benchmark is Hearthstone-level tactile 3D presentation. A review of the
Phase 31 HD archive confirms the gap is structural, not incremental:

1. **Zero textures.** Every surface is an untextured flat-color primitive
   (brown disc "table", bare cylinder "candles", beige slab "card pads",
   purple box "deck"). Hearthstone is built almost entirely from hand-crafted
   surface texture; we have none.
2. **No sense of place.** Screens float in a pure black void. Hearthstone
   never shows void: the board box fills the frame edge-to-edge.
3. **Abstract UI chrome.** Panels are flat translucent dark rectangles, the
   step tracker reads as debug UI, buttons are flat orange fills. Hearthstone
   UI is skeuomorphic: wooden frames, stone plaques, brass rivets.
4. **The deck has no identity.** A tarot game's card back is its face; ours is
   a featureless purple box.

Incremental patching cannot cross this gap, so this blueprint authorizes a
teardown of the *visual composition layer* while keeping the healthy invisible
architecture (scene flow, controllers, tests, audio, camera choreography,
holographic face shader, HD RWS 1909 faces, LXGW WenKai type system).

## Research inputs (Hearthstone method, translated to tarot)

From Derek Sakamoto's GDC 2015 talk "Hearthstone: How to Create an Immersive
User Interface" and published analyses of the game's art direction:

- **One physical seed.** Hearthstone's every screen lives inside one physical
  object (the tavern box). Ours: **one continuous velvet tabletop seen from a
  seated lean-in view**. Every screen is a different framing of that table.
- **Physicality over efficiency.** UI elements are objects: buttons are things
  lying on the table, trackers are brass medallions, text sits on parchment.
- **Juice everywhere.** Every interactive element has hover, press, and glow
  states; cards tilt, swoosh, and flutter; decks have real staggered heights.
- **The card is the hero.** The environment stays dark and low-frequency; the
  brightest, sharpest, most saturated pixels always belong to a card.

## Art direction

Concept sentence: *深夜，你俯身在占卜师的丝绒桌前；烛光是唯一的光源，牌是唯一的主角。*

Palette:

- Surround falloff: deep aubergine-black `#14090F`
- Tablecloth: oxblood velvet around `#4A1520` (PBR fabric, tinted)
- Table rim: dark walnut wood `#2A1B12` (PBR wood)
- Metal/accents: antique gold `#C9A227`, highlight `#E8CE79`
- Text/card fronts: ivory `#F2E8CE` (existing theme colors stay compatible)
- Fill light: faint cool moon-violet rim against the warm candle key

## Asset strategy (all CC0 / public domain, recorded in THIRD_PARTY_ASSETS.md)

- **PBR surfaces** — ambientCG (CC0): fabric material for the velvet cloth,
  wood material for the table rim (1K JPG sets: color/normal/roughness).
  Verified downloadable (`https://ambientcg.com/get?file=<Id>_1K-JPG.zip`).
- **Ornate UI frames** — Kenney "Fantasy UI Borders" (CC0, 140 sprites,
  nine-slice ready). Verified direct zip on kenney.nl. Gold-tinted at runtime
  via Image color so sprites stay reusable.
- **Card back + parchment** — composed locally with Python/PIL at high
  resolution: aubergine field, fine double gold filigree border, 180°-symmetric
  central sun/moon emblem, corner flourishes, subtle mottle. (The Phase 26
  failure was an emission-material hack; this is a real composed texture.)
- Existing assets kept: HD RWS 1909 faces, LXGW WenKai font, Phase 33 audio.

## Screen-by-screen target

- **ReadingRoom (core screen).** Velvet cloth fills the frame; beveled walnut
  rim visible along screen edges (no void). Card sockets become gold-line
  insets printed on the cloth. Deck becomes a staggered stack of thin card
  meshes wearing the new back. Step tracker becomes a brass medallion strip on
  the top rim. Action tray becomes one parchment plaque with a gold nine-slice
  frame; buttons become framed plaques with hover glow + press sink.
- **MainMenu.** Same table, tighter crop: deck stack, real-shaped candle with
  emissive flame, one scattered face-down card; title keeps its glow; buttons
  re-skinned as plaques. The brown disc and bare cylinders are deleted.
- **Result.** The table continues behind the reading (dim, defocused) instead
  of black void. Hero card sits in an ornate gold frame; the interpretation
  scroll sits on parchment with a nine-slice border; footer button re-skinned.

## Phase plan

- **Phase 37 — Asset foundation.** Download CC0 packs, compose card back +
  parchment, import via editor bootstrapper (texture/sprite settings, nine-slice
  borders, URP Lit materials), write THIRD_PARTY_ASSETS.md. Tests: assets
  exist, import settings correct, license doc present.
- **Phase 38 — Table rebuild.** ReadingRoom: cloth/rim/sockets/deck-stagger +
  card-back material on the card prefab + lighting rebalance. Screenshot loop
  after every step; the card must stay the brightest object.
- **Phase 39 — UI reskin.** Nine-slice plaque panels, framed buttons with
  hover/press feedback component, medallion step tracker, input field skin —
  across all three screens.
- **Phase 40 — MainMenu + Result re-composition.** Rebuild both framings on
  the shared table language; delete superseded flat-era and blob-era objects
  (each deletion listed explicitly in the reply and commit).
- **Phase 41 — Final polish.** Full HD archive regeneration, pixel scans,
  EditMode + PlayMode green, macOS build, baseline re-measure.

Each phase is one commit. Tests pinned to the old look are updated or deleted
in the same phase (deletions user-authorized for this redesign, scoped strictly
to `/Users/maochuandou/BUPT/Game`).

## Risks

- Nine-slice gold tinting can look muddy → keep sprites near-white, tint down.
- Velvet normal maps can shimmer at glancing angles → high roughness, moderate
  normal strength; SMAA already enabled.
- Batch-mode captures undersell GPU-lit materials → final judgment on the
  user's real build playthrough, as established in Phase 36.
