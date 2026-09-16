# Bird screensaver

Native Windows screensaver: NAudio capture, BirdNET v3 ONNX in-process, Fugleramme plates packed onto paper.

## Commands

```bash
dotnet build
dotnet test
dotnet run --project src/BirdScreensaver -- --window --demo
dotnet run --project src/BirdScreensaver -- --preview out.png --demo
dotnet run --project src/BirdScreensaver -- fetch-assets
dotnet run --project src/BirdScreensaver -- fetch-model
dotnet run --project src/BirdScreensaver -- configure
dotnet run --project src/BirdScreensaver -- install
./installer/package.sh   # self-contained MSI in installer/bin/Release
```

`--demo` rotates species that have artwork, so you can work the collage without a mic.

## Layout

- `src/BirdScreensaver/` - WPF host, `/s` `/c` `/p`
- `Artwork/` - species keys, picks, catalog
- `Detection/` - hearing window, live ONNX, demo garden
- `Render/` - paper collage, adapted from Fugleramme
- `Tools/` - fetch plates, download model, install `.scr`

Settings live in `%APPDATA%\BirdScreensaver\settings.json`. Artwork and models are not committed. A still for the README lives in `docs/demo.png`.

## Style

- Flat functions, early returns
- Comments only for non-obvious decisions
- Sentence case headers
- No em dashes; use spaced hyphens
