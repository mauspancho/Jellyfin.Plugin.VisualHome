# Jellyfin Visual Home

Jellyfin Visual Home es un plugin ligero para Jellyfin Server que sirve secciones visuales para Jellyfin Web: hero principal, carruseles, top 10, recomendaciones basicas, colecciones de estudios y filas configurables.

El plugin no modifica archivos de Jellyfin Web, no parchea `index.html`, no descarga binarios, no ejecuta procesos externos y no usa APIs externas. Todo sale de la biblioteca local de Jellyfin.

## Estado

Esta es una primera version funcional y conservadora. El backend, la pagina de configuracion y los assets web estan incluidos. La carga automatica de JavaScript dentro de Jellyfin Web depende de capacidades que Jellyfin no expone de forma universal para plugins; por eso el plugin sirve `visualhome.js` y `visualhome.css`, y documenta el modo fallback no destructivo.

## Estructura

- `Jellyfin.Plugin.VisualHome/VisualHomePlugin.cs`: entrada del plugin y pagina de dashboard.
- `Jellyfin.Plugin.VisualHome/Configuration/VisualHomeConfiguration.cs`: configuracion persistente y secciones predeterminadas.
- `Jellyfin.Plugin.VisualHome/Controllers/VisualHomeConfigController.cs`: endpoints de administracion.
- `Jellyfin.Plugin.VisualHome/Controllers/VisualHomeSectionsController.cs`: endpoints de secciones y assets.
- `Jellyfin.Plugin.VisualHome/Services/SectionQueryService.cs`: consulta y filtrado de secciones.
- `Jellyfin.Plugin.VisualHome/Services/RecommendationService.cs`: recomendaciones locales basicas.
- `Jellyfin.Plugin.VisualHome/Services/SectionCacheService.cs`: cache por seccion.
- `Jellyfin.Plugin.VisualHome/Models/*.cs`: modelos de configuracion y respuesta.
- `Jellyfin.Plugin.VisualHome/Web/visualhome.js`: frontend vanilla.
- `Jellyfin.Plugin.VisualHome/Web/visualhome.css`: estilos aislados con prefijo `vh-`.
- `manifest.json`: ejemplo de manifest para repositorio de plugins.

## Compatibilidad

El proyecto esta preparado para Jellyfin `10.11.x` usando `Jellyfin.Controller` y `Jellyfin.Model` `10.11.11`, que apuntan a `net9.0`.

Para Jellyfin `10.10.x`, ajusta:

```xml
<TargetFramework>net8.0</TargetFramework>
<PackageReference Include="Jellyfin.Controller" Version="10.10.x" />
<PackageReference Include="Jellyfin.Model" Version="10.10.x" />
```

La recomendacion de Jellyfin es que las versiones de paquetes coincidan con la version del servidor. Si una API cambia entre `10.10.x` y `10.11.x`, el punto mas probable de ajuste esta en `Services/SectionQueryService.cs` y los controladores.

## Compilacion

Requisitos:

- .NET SDK 9 para Jellyfin 10.11.x.
- Acceso a NuGet para restaurar `Jellyfin.Controller` y `Jellyfin.Model`.

Comando:

```powershell
dotnet build .\Jellyfin.Plugin.VisualHome\Jellyfin.Plugin.VisualHome.csproj -c Release
```

El binario queda en:

```text
Jellyfin.Plugin.VisualHome\bin\Release\net9.0\Jellyfin.Plugin.VisualHome.dll
```

## Instalacion manual

1. Deten Jellyfin.
2. Crea una carpeta para el plugin dentro del directorio de plugins de Jellyfin.
   - Windows: `%LOCALAPPDATA%\jellyfin\plugins\Jellyfin Visual Home`
   - Linux: `/var/lib/jellyfin/plugins/Jellyfin Visual Home`
3. Copia `Jellyfin.Plugin.VisualHome.dll` a esa carpeta.
4. Inicia Jellyfin.
5. Entra al Dashboard y abre la pagina `Jellyfin Visual Home`.
6. Verifica que el plugin este activo y guarda la configuracion.

## Carga en Jellyfin Web

El plugin sirve los assets en:

```text
/VisualHome/assets/visualhome.css
/VisualHome/assets/visualhome.js
```

CSS fallback no destructivo:

```css
@import url("/VisualHome/assets/visualhome.css");
```

JavaScript fallback:

Jellyfin Web no garantiza una API estable para que un plugin de servidor inyecte JavaScript en todas las versiones sin tocar archivos core. Si tu instalacion no carga el JS automaticamente mediante una personalizacion no destructiva existente, agrega una referencia a `/VisualHome/assets/visualhome.js` con el mecanismo seguro que uses para personalizar Jellyfin Web. No edites ni reemplaces `index.html` del servidor.

## Pruebas manuales

1. Compila e instala el plugin.
2. Reinicia Jellyfin.
3. Confirma que aparece la pagina de configuracion.
4. Guarda la configuracion predeterminada.
5. Abre `/VisualHome/sections` autenticado y confirma que devuelve secciones.
6. Carga `visualhome.js` en Jellyfin Web con el modo fallback y entra a Home.
7. Verifica que rendericen al menos:
   - Hero principal
   - Peliculas anadidas recientemente
   - Series anadidas recientemente
   - Top 10 peliculas
   - Top 10 series
8. Desactiva una seccion y guarda.
9. Pulsa `Limpiar cache`.

## Desinstalacion

1. Desactiva el plugin desde su pagina de configuracion.
2. Deten Jellyfin.
3. Elimina la carpeta del plugin dentro del directorio de plugins.
4. Inicia Jellyfin.

## Seguridad

- Los endpoints de configuracion requieren usuario autenticado y administrador.
- Los endpoints de lectura usan el usuario actual para consultar la biblioteca.
- No se aceptan rutas arbitrarias para assets; solo `visualhome.js` y `visualhome.css`.
- No se guardan secretos ni claves.
- No hay ejecucion de comandos ni llamadas a APIs externas.

## Limitaciones conocidas

- La inyeccion automatica de JavaScript en Jellyfin Web no esta garantizada por la API de plugins del servidor.
- Las recomendaciones son basicas: historial reproducido, generos frecuentes y rating local.
- El listado de metadatos del panel limita la inspeccion a 5000 items para evitar consultas enormes.
- Los logos de estudios son manuales o fallback visual.

## Roadmap

- Integracion TMDb opcional.
- Logos de estudios automaticos.
- Mejor algoritmo de recomendaciones.
- Temas visuales intercambiables.
- Editor visual drag and drop.
- Integracion opcional con Jellyseerr.
- Modo "Porque viste...".
- Secciones por actor/director.
