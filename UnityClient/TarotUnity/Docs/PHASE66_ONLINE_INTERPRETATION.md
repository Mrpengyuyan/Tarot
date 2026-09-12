# Phase 66 — Online Interpretation Loop

The reading room now starts a real online reading, and the Result screen waits for
the AI interpretation instead of the reading room blocking on it.

## Why

- The guest token was issued to the persistent Boot `ApiClient`, but the reading room
  used its own scene `ApiClient`, so `CanCreateAuthenticatedReading` was always false
  and every reading silently went offline. Scene code now reads `ApiClient.Shared` first.
- Even with a token, the old `CompleteReading` waited for the AI (often longer than the
  15 s request timeout) before a single card was dealt.

## Flow

1. The shuffle starts while `BackendReadingService.StartReading` creates the record,
   draws and fetches the cards (no AI). A rejected token refreshes the guest, or starts
   a new one, and the start is retried once.
2. The table deals the backend's cards. The snapshot is saved as `Online` / `Pending`
   and handed to `InterpretationPoller`, which lives on the persistent Boot object.
3. `InterpretationPoller` calls `POST /records/{id}/interpret/async`, then polls
   `GET /records/{id}` after 2, 2, 3, 3 and then every 5 s, for up to 330 s. A stored
   interpretation counts as done whatever the record status says.
4. The Result screen shows one of four states: generating (a breathing status line,
   plus a slow notice after 20 s), ready (the reading fades in), failed (重新解读 when
   retryable, and 查看离线解读), or offline.

A failed start deals an offline reading and shows the matching line from
`ReleaseUxCopy`; raw server messages only reach the log. A spread the backend does
not have, or a card-count mismatch, never goes online.

## Known limits

- If the backend process dies mid-generation, the record stays `processing` until the
  300 s stale window passes, so the first 重新解读 may time out before a second one
  succeeds.
- The guest daily quota starts over for a new guest session; real rate limiting belongs
  to deployment.
- The spread-selection copy ("Choose a spread…", "One Card Focus") is still English; it
  belongs to the reading-room clarity sub-project.

## Files

- `Assets/Scripts/Network/ApiClient.cs` — `Shared` and the structured requests.
- `Assets/Scripts/Network/ApiError.cs` — status code, kind and Retry-After.
- `Assets/Scripts/Network/BackendReadingService.cs` — `StartReading`, `RecoverSession`.
- `Assets/Scripts/Network/InterpretationPoller.cs` — the persistent poller.
- `Assets/Scripts/Data/ReadingSessionSnapshot.cs`, `ReadingSessionMapper.cs` — reading
  source and interpretation state.
- `Assets/Scripts/UI/ReleaseUxCopy.cs` — the Chinese copy table.
- `Assets/Scripts/UI/ReadingRoomController.cs` — deal first, interpret in the background.
- `Assets/Scripts/UI/ResultPanelPresenter.cs`, `ResultSceneController.cs` — the four states.
- `Assets/Editor/Phase66ResultInterpretationStateBootstrapper.cs` — lays and wires the
  Result UI.
- `Assets/Editor/Phase66InterpretationStateCaptureBuilder.cs` — review shots.
- `Assets/Tests/EditMode/Phase66OnlineInterpretationTests.cs`,
  `Assets/Tests/PlayMode/Phase66*Tests.cs`, `Assets/Tests/PlayMode/MockTarotBackend.cs` —
  guards and scripted-backend scenarios.
- `Docs/VisualReview/Phase66/` — generating, ready, failed and offline captures.
