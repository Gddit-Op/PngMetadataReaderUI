# PngMetadataReaderUI
Png Files read metadata with UI Avalonia .NET core

## NativeAOT publish

On Windows, publish through the repository script so NativeAOT can initialize a
clean Visual C++ toolchain even when the shell was started by another Visual
Studio version:

```powershell
.\scripts\publish-aot.ps1
```

The output is written to
`PngMetadataReaderUI\bin\Release\net10.0\win-x64\publish`.
