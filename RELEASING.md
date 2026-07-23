# Publicar una versión

Esta lista evita subir archivos incompletos o datos locales.

## Preparar

1. Actualiza `<Version>` en `YTMusicLite.csproj`.
2. Agrega los cambios de la versión a `CHANGELOG.md`.
3. Ejecuta `dotnet build --configuration Release`.
4. Prueba inicio, bandeja, segunda ejecución y salida completa.

## Crear el paquete

Desde PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Publish-Release.ps1 -Version 0.1.0
```

El script crea dentro de `artifacts/`:

- `YTMusicLite-0.1.0-win-x64-self-contained.zip`
- `YTMusicLite-0.1.0-win-x64-self-contained.zip.sha256`

El ZIP es autocontenido para .NET, pero todavía requiere Microsoft Edge WebView2 Runtime.

## Revisar antes de publicar

1. Extrae el ZIP en una carpeta nueva.
2. Confirma que no contiene `.pdb`, perfiles de WebView2 ni rutas personales.
3. Ejecuta `YTMusicLite.exe` desde la carpeta extraída.
4. Compara el SHA-256 con el archivo `.sha256`.

## GitHub Release

1. Crea una etiqueta con formato `v0.1.0`.
2. Crea una Release a partir de esa etiqueta.
3. Adjunta el ZIP y su archivo SHA-256.
4. Copia las notas correspondientes desde `CHANGELOG.md`.

No incluyas directamente las carpetas `bin`, `obj`, `release` o `artifacts` en Git.
