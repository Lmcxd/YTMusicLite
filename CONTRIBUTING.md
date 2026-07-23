# Colaborar con YTMusicLite

## Antes de empezar

1. Busca un issue existente relacionado con el cambio.
2. Para modificaciones grandes, abre primero una propuesta breve.
3. No incluyas cookies, perfiles de WebView2, credenciales ni archivos generados.

## Entorno de desarrollo

- Windows 10 u 11.
- .NET 8 SDK.
- Visual Studio 2022 con Desarrollo de escritorio de .NET, opcional.

## Verificación local

```powershell
dotnet restore
dotnet build --configuration Release
```

Antes de enviar un pull request, confirma también:

- La aplicación abre YouTube Music correctamente.
- Minimizar o cerrar la ventana la envía a la bandeja.
- Ejecutar `YTMusicLite.exe` por segunda vez restaura la instancia existente.
- Los atajos globales se liberan al salir.

## Estilo

- Conserva `nullable` habilitado.
- Prefiere nombres descriptivos y métodos pequeños.
- Mantén los textos visibles en español hasta que exista un sistema de localización.
- Explica cualquier selector de la página de YouTube Music que se agregue o modifique.

## Pull requests

Describe el problema, la solución y cómo verificaste el cambio. Incluye capturas únicamente cuando sean necesarias y elimina información personal antes de publicarlas.
