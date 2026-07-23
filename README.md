<div align="center">
  <img src="Assets/YTMusicLite-icon.png" width="128" alt="Icono de YTMusicLite">
  <h1>YTMusicLite</h1>
  <p>Cliente liviano y no oficial de YouTube Music para Windows.</p>
</div>

> [!IMPORTANT]
> YTMusicLite es un proyecto comunitario no oficial. No está afiliado, patrocinado ni aprobado por YouTube, Google LLC o sus empresas relacionadas. YouTube y YouTube Music son marcas de sus respectivos propietarios.

## Características

- YouTube Music integrado mediante Microsoft Edge WebView2.
- Una sola instancia: abrir la aplicación nuevamente restaura la ventana existente.
- Minimización a la bandeja del sistema con reproducción en segundo plano.
- Atajos globales: `Ctrl+Alt+Espacio`, `Ctrl+Alt+Izquierda` y `Ctrl+Alt+Derecha`.
- Controles desde el menú de la bandeja.
- Reducción automática del uso de memoria cuando la ventana está oculta.
- Temporizador de apagado desde la bandeja.

## Requisitos

- Windows 10 u 11 de 64 bits.
- Conexión a Internet.
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/).
- Una cuenta de Google es opcional y se administra directamente dentro de YouTube Music.

## Instalación

1. Descarga el archivo ZIP para Windows x64 desde la sección **Releases**.
2. Extrae todo el contenido en una carpeta.
3. Ejecuta `YTMusicLite.exe`.

La aplicación todavía no está firmada digitalmente. Windows SmartScreen puede mostrar una advertencia de editor desconocido.

## Datos y privacidad

YTMusicLite no incorpora telemetría propia. El perfil de WebView2, incluidas las cookies de sesión, se guarda localmente en:

```text
%LOCALAPPDATA%\YTMusicLite\WebView2
```

El contenido web se carga directamente desde YouTube Music y queda sujeto a las políticas y condiciones de Google y YouTube. Consulta [PRIVACY.md](PRIVACY.md) para más detalles.

## Compilar desde el código fuente

Requisitos de desarrollo:

- Visual Studio 2022 con la carga **Desarrollo de escritorio de .NET**, o .NET 8 SDK.
- Windows 10 u 11.

```powershell
git clone URL-DEL-REPOSITORIO
cd YTMusicLite
dotnet restore
dotnet build --configuration Release
```

Publicación portable para Windows x64:

```powershell
dotnet publish --configuration Release --runtime win-x64 --self-contained true --output artifacts/win-x64
```

## Limitaciones conocidas

- Los controles se integran con la página web de YouTube Music. Un cambio importante del sitio puede requerir actualizar selectores internos.
- La aplicación requiere WebView2 Runtime aunque el paquete sea autocontenido.
- El ejecutable no cuenta actualmente con firma digital.

## Colaborar

Lee [CONTRIBUTING.md](CONTRIBUTING.md) antes de abrir un issue o pull request. Los reportes deben excluir cookies, credenciales y datos personales del perfil WebView2.

## Licencia

El código de YTMusicLite se distribuye bajo la [licencia MIT](LICENSE). Esta licencia no concede derechos sobre marcas, contenido o servicios de terceros.
