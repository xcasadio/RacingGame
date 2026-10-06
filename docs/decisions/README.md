# Architecture Decision Records

This folder records the architecture decisions of this repository: architecture, data formats, public APIs, backends, and the rules that govern the AI agents working on it.

## Rules

- One file per decision, named `NNNN-short-title.md` with an increasing four-digit number. Decisions taken together on the same theme may share one file.
- Written in English, from the template [template.md](template.md): Status, Date, Source, Context, Decision, Consequences.
- Every decision taken during a plan or a discussion is recorded here, at the time it is taken (skill `adr`).
- Backfilled decisions keep a `Source` pointing at the original document. Existing audits are read-only: the decision is copied here, the audit is not edited.
- A decision is never rewritten: a change is a new record that supersedes the old one, whose status becomes `Superseded by ADR-XXXX`.

## Index

| ADR | Title | Status | Date |
|---|---|---|---|
| ADR-0001 | RacingGameCasaEngine screens are CasaEngine screen assets bound to view models | Accepted | 2026-10-05 |
| ADR-0002 | RacingGameCasaEngine's content folder is a CasaEngine editor project | Accepted | 2026-10-05 |
| ADR-0003 | RacingGameCasaEngine plays its sound effects through CasaEngine's audio system | Accepted | 2026-10-05 |
| ADR-0004 | RacingGameCasaEngine saves the user settings only when the player leaves the Options screen | Superseded by ADR-0013 | 2026-10-05 |
| ADR-0005 | RacingGameCasaEngine's main menu reproduces the original XNA menu with MGUI brushes | Superseded by ADR-0006 | 2026-10-05 |
| ADR-0006 | RacingGameCasaEngine's main menu reproduces the original XNA menu, as delivered | Superseded by ADR-0007 | 2026-10-05 |
| ADR-0007 | RacingGameCasaEngine's main menu reproduces the original XNA menu, with a continuous bevel | Accepted | 2026-10-06 |
| ADR-0008 | RacingGameCasaEngine's title screen and car selection reproduce the original XNA screens as captured | Superseded by ADR-0009 | 2026-10-06 |
| ADR-0009 | RacingGameCasaEngine's title screen and car selection reproduce the original XNA screens as captured, as delivered | Accepted | 2026-10-06 |
| ADR-0010 | RacingGameCasaEngine's track selection reproduces the original XNA screen | Superseded by ADR-0011 | 2026-10-06 |
| ADR-0011 | RacingGameCasaEngine's track selection reproduces the original XNA screen, with cards growing around fixed centres | Accepted | 2026-10-06 |
| ADR-0012 | RacingGameCasaEngine's UI images are premultiplied, except the menu background | Accepted | 2026-10-06 |
| ADR-0013 | RacingGameCasaEngine's Highscores, Options and Help screens reproduce the original XNA screens | Accepted | 2026-10-06 |
