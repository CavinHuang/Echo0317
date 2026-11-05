# Echo0317 / 《03:17 · 回声》

Unity 6.3 LTS (6000.3.2f1) narrative FMV game project. Five acts, 55 story nodes, four endings, five evidence deductions.

## Project status
**Internal Alpha — not a released game.** The C# narrative and desktop game runtime exists, but the Windows Unity Player has not yet been successfully compiled and launched. Eight programmatic previs clips are **not final cinematic footage**. Release gating deliberately blocks shipping without approved footage.

## How to build
- Install Unity 6.3 LTS with Windows Build Support; use Unity Hub to open this project.
- Run EditMode/PlayMode tests before building.
- For hosted builds, run the GitHub Actions workflow `Hosted Unity Windows Alpha` after setting Unity license secrets on this repository.
- A compiled Windows player must successfully run its native four-ending QA before packaging.

## Security
Never commit Unity license credentials or generated `Library/`, `Temp/`, `Builds/`, or `Logs/` directories.

## Current milestone
Bring up the first **real Windows x64 Unity Alpha** and attach compilation/test/runtime logs. The hosted build workflow will not be represented as a successful EXE build until it actually runs.
