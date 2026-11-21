# Contextual chat and saved schedules

Chat sends the raw question separately from bounded, owned conversation history.
Routine personal-schedule questions use the existing schedule rows without a model
call. Institutional questions never receive the complete profile or grade history.

## Try it

1. In one chat ask `Quiero informacion sobre intercambios academicos`, then
   `y los documentos?`. Reload and repeat a follow-up in that selected chat.
2. Start a new chat: the previous chat must not supply its context.
3. Save classes under `/horario`, then ask `Mi horario`, `Que clase tengo mañana?`,
   `Que clase tengo ahora?`, or `Mi proxima clase`.
4. Ask `Mi horario y requisitos para becas`: the institutional part still goes
   through retrieval, including when no schedule exists.

## Boundaries

- History comes from authenticated user plus owned session, never from a browser
  user ID. Only adjacent completed user/assistant pairs enter the prompt. Failed
  user turns and partial assistant streams are excluded.
- Default limits are 12 messages and 4,000 UTF-8 content bytes. The byte budget is
  a deterministic conservative bound, not an exact Gemini token count. Drop oldest
  whole turns; no persistent summary is created. `Chat__HistoryMaxMessages` and
  `Chat__HistoryMaxBytes` can lower these limits (minimum 2 messages / 128 bytes).
  Python independently enforces its own limits.
- PostgreSQL advisory locks cover the full turn and deletion, across API
  instances. An overlapping request for the same owned session returns HTTP409.
  In-memory tests use a process-local lease; other sessions remain independent.
- Context rows have timestamp plus ID ordering; new rows preserve turn order.
  Deletion removes stored history; private retrieval caches are not retained.
- Schedule reads reuse `PersonalSchedule.Read` with the existing module and always
  filter by authenticated owner. They are fresh on each question.
- Campus time uses `America/Hermosillo`; the controller clock is injectable.
  Current class uses `[start,end)`. Next class searches the next seven local days.
  Overlaps and missing teachers/rooms remain visible.
- The schedule is recurring saved data, not live enrollment, term history,
  holidays or cancellation status. Chat cannot edit or import schedule data.
- Mixed questions send selected typed fields only. Empty schedule data does not
  discard the institutional half. No new migration or frontend route is required.

## Verification

```powershell
dotnet test MiUni.Api.Tests/MiUni.Api.Tests.csproj --no-restore
node --test MiUni.Web/src/services/chatStream.test.mjs MiUni.Web/src/services/horarioUtils.test.js
```

The real PostgreSQL lease probe is opt-in: load local configuration with
`scripts/dev.ps1 -Action check`, set `MIUNI_CHAT_TEST_DB` from the resulting local
connection setting, then run `ChatPostgresLeaseTests`. It changes no stored rows.
Never place credentials in commands or reports.

See the RAG repository's `odd/tasks/chat-context-schedule-rag.md` for task evidence,
and `docs/chat-context-measurements.md` for measurement limitations. No push or
main-branch integration is implied by these local changes.
