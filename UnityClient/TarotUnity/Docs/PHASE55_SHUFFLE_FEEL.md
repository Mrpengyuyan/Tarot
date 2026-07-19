# Phase 55 — The Shuffle Moves: Riffle Choreography on the Deck Stack

Date: 2026-07-19

The card's journey now has one motion language: the flip got its weight in
Phase 52, the deal its landing in Phase 54. The shuffle - the *first* beat of
the ritual, the one the player triggers with the draw button - was still sound
and dust over a perfectly still deck. The camera focused it, particles rose,
the cue played, and the Midnight Parlor stack (Phase 38's staggered pile of
nine card shells) sat like a photograph.

## The four beats, on the deck

`DeckShuffleChoreographer` sits on `MP_DeckStack` and plays the established
language (anticipation → action → contact → settle) as a hand shuffling a deck:

1. **Anticipation** — the whole stack presses down (0.015, ease-out), a hand
   squaring the deck before working it.
2. **Riffle** — a ripple runs bottom-to-top through the stacked cards: each
   card pops up (0.055 local, about two card-gaps) with a small yaw shiver
   (4°), rising ease-out and dropping ease-in - it falls back with weight, not
   floating. Neighbours start 0.025 s apart so the pops read as one gesture
   climbing the pile. The stack rises out of its press-down as the ripple runs.
3. **Contact** — the deck squares up: on that frame the camera takes a small
   `Kick` (0.03, quieter than the flip's reveal - the shuffle is a prelude, not
   the payoff) and the stack squashes (−6% height).
4. **Settle** — the squash springs back ease-out to the **exact** rest pose.
   Every card returns to its authored stagger and twist to sub-millimetre
   precision - the PlayMode test asserts it - so Phase 38's composition is
   untouched the moment the shuffle ends.

The whole choreography runs ~0.65 s, matching the Phase 9 shuffle breath
(`shuffleBreathSeconds` 0.65, floor 0.55 - untouched) so the deck settles just
as the deal begins.

## Wiring

`ReadingRoomController` gains a `deckShuffle` field and fires `Play()` on the
same line of the draw flow that plays the ShuffleStarted cue - motion, dust,
and sound land together. The choreographer finds the camera with the same
lazy self-healing lookup the flip and deal use, captures the stack's rest pose
on first play, and ignores re-entrant calls while a shuffle is running.

## Verification and honesty

- EditMode keeps the nine tuning knobs in a tasteful envelope, and proves the
  scene wiring: the stack carries the choreographer and the draw flow points
  at it (a dangling reference would leave the shuffle silent-still again).
- PlayMode plays a real shuffle in the ReadingRoom via the production path
  (the Phase 54 lesson: hand-driving an enumerator only sees its outer yield
  points) and samples every frame: cards visibly pop, the stack squashes on
  contact, and everything restores exactly.
- Whether the riffle *feels* like a shuffle is yours to judge at the table -
  every knob is one Inspector edit away.
