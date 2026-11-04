# Subagentes Oficiales del Proyecto Trazalibre

Este repositorio cuenta con tres subagentes especializados ubicados en `.agents/agents/` para automatizar el mantenimiento, versionado, compilación y publicación del proyecto en cualquier instalación de Antigravity:

1. **`version_agent`** (`.agents/agents/version_agent/agent.md`):
   - **Rol:** Agente de Versionado y Compilación.
   - **Responsabilidades:** Incrementar y sincronizar la versión semántica (`X.Y.Z`) en `src/AssemblyInfo.cs`, recompilar el binario `bin\Trazalibre.exe` ejecutando `.\build.bat`, verificar los metadatos de versión del ejecutable y empaquetar `bin\Trazalibre-vX.Y.Z-Windows.zip`.

2. **`trazalibre_maintainer`** (`.agents/agents/trazalibre_maintainer/agent.md`):
   - **Rol:** Mantenedor de Documentación y Arquitectura.
   - **Responsabilidades:** Mantener actualizados `MANUAL_DE_USO.md` y `README.md` ante cada nueva funcionalidad, herramienta o atajo de teclado, preservando estrictamente el idioma español neutro y el principio de cero dependencias externas.

3. **`git_publisher`** (`.agents/agents/git_publisher/agent.md`):
   - **Rol:** Auditor de Seguridad y Publicador Git/GitHub.
   - **Responsabilidades:** Auditar `git status` y `.gitignore` para impedir que se suban binarios compilados (`bin/`) o recursos locales (`Imagenes/`), realizar commits semánticos en español neutro, crear etiquetas de versión (`git tag -a vX.Y.Z`) y publicar los cambios en `origin/main`.
