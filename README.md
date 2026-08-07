# PngMetadataReaderUI
Png Files read metadata with UI Avalonia .NET core

Neben einzelnen Bildern kann über **Eingabeordner extrahieren...** ein Ordner
ausgewählt werden. Alle unterstützten Bilder (PNG, JPG, JPEG und WebP) direkt in
diesem Ordner werden verarbeitet; die erzeugten TXT- und Workflow-JSON-Dateien
werden neben den jeweiligen Bildern gespeichert. Unterordner werden nicht
durchsucht.

Bei ComfyUI-Bildern werden verbundene `CLIPTextEncode`-Nodes als positive und
negative Prompts ausgewertet. Wenn mindestens ein nicht leerer Prompt vorhanden
ist, wird zusaetzlich `<Bildname>_prompts.txt` neben dem Bild gespeichert.
Bei der Ordneranalyse werden stattdessen die Prompts aller Bilder im Format
`positive:...` / `negative:...` gesammelt in `prompts.txt` geschrieben.

## NativeAOT publish

On Windows, publish through the repository script so NativeAOT can initialize a
clean Visual C++ toolchain even when the shell was started by another Visual
Studio version:

```powershell
.\scripts\publish-aot.ps1
```

The output is written to
`PngMetadataReaderUI\bin\Release\net10.0\win-x64\publish`.
