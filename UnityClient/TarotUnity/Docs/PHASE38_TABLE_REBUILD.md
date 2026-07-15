# Phase 38 Table Rebuild — Midnight Parlor Stage

Date: 2026-07-15

## What changed

The ReadingRoom world layer was rebuilt in the Midnight Parlor language defined
by `PHASE37_VISUAL_REDESIGN_BLUEPRINT.md`. One coherent stage replaces eight
generations of overlapping flat-color planes:

- **MP_TableStage** — oxblood velvet cloth (ambientCG felt PBR, 24x15 so no
  camera pose sees void), walnut rim beams on the far/left/right frame edges
  (Wood051 PBR), and a near-black parlor backdrop quad behind the table edge.
- **MP_CardSockets** — four gold socket decals (composed inset texture with an
  inner shadow) printed on the cloth under each card slot; the old beige slot
  slabs keep their transforms as gameplay anchors but no longer render.
- **MP_DeckStack** — eight staggered thin card bodies with seeded jitter/twist
  plus a top quad wearing the composed celestial card back; the purple graybox
  deck box keeps its transform, renderer off.
- **Card prefab** — the seven primitive back-ornament blocks were replaced by a
  single `MP_CardBackFace` quad carrying the composed 180°-symmetric back
  texture; the Phase 15 shell underside plane wears the same material so the
  flip shows a real back at every angle. The always-on Phase 14 "2.5D" dressing
  quads (edge/glass/rim/shadow) were removed; the animated reveal glow and the
  Phase 15 drop shadow remain.
- **Lights** — key light trimmed to 0.85, cool moon fill to 0.4 with a violet
  shift, and the back material renders without specular highlights so point
  lights no longer smear a blue glare across face-down cards.

## Deleted (user-authorized teardown)

Scene objects: `Graybox Tarot Table`, `Phase8_ReadingVisualRoot` (cloth+ring),
`Phase12_CardRevealStage`, `Phase12_RevealBackdrop`, `Phase14_TableDepthPlane`,
`Phase14_CardRevealPool`, the stray empty world `Phase7_TableVignette` root,
and `Phase15_RitualTableSurface` / `Phase15_TableDepthRing` under the table
root. Prefab children: `BackSigil`, `BackConstellation`, `Phase7_MoonSigil`,
`Phase7_BackVeil`, `Phase8_BackPatternTop/Bottom`, `Phase8_CenterGem`,
`Phase14_CardEdge`, `Phase14_CastShadow`, `Phase14_FaceRimLight`,
`Phase14_ArtworkGlass`. The era tests that pinned these now assert the
opposite direction (they must stay deleted), so a stale bootstrapper re-run
cannot silently resurrect them. Orphaned legacy material assets stay on disk
until the Phase 41 sweep.

## Verification

- `Phase38CaptureBuilder` renders `Docs/VisualReview/Phase38/ReadingRoom.png`
  (2560x1440, face-down card proxies on the three-card slots) after every
  bootstrap iteration; five iterations were reviewed pixel-by-pixel.
- `Phase38TableRebuildTests` guard the stage structure, socket count, deck
  stack, prefab back, and this document. Era tests from Phases 5/7/8/12/14/15
  were updated in-phase to guard the teardown.
