# THIS IS AI SLOP
generated with gpt.

## Controls

- Drag a colored sticker, then release to turn that layer.
- Right-drag a colored sticker to make a double turn like `R2`.
- Hold `Shift` while dragging or clicking `CW`/`CCW` to make a 2-layer wide move.
- Diagonal drags on front/back/left/right faces turn `F`/`B`/`L`/`R`.
- Drag empty space, or right-drag empty space, to orbit the cube.
- Use the mouse wheel to zoom.
- Use the sidebar for exact face/layer turns, scramble, and reset.

## Verify

```powershell
.\dist\VibeBetterCube.exe --self-test
```

The self-test exits with code `0` when every move inverse and four-turn cycle passes for sizes `2` through `7`.

Share the `dist\VibeBetterCube.exe` file with another Windows machine that has .NET Framework 4.x installed.
