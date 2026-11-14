# StagePad Live - actualizaciones portátiles

Cierra la app y sustituye `StagePad.exe` por el de una release estable más reciente. Ajustes, shows, recuperación y referencias de audio permanecen en `%LOCALAPPDATA%\StagePad` o el directorio elegido. Runtime incluido.

Ajustes > Actualizaciones consulta `Alessio278/StagePad-Live`. Requiere el asset `StagePad.exe` y SHA-256 o `SHA256.txt`. Instalación con audio/secuencias detenidos, comprobación de producto/versión, copia anterior y restauración si falla el arranque.

Conserva shows y audio antes de una versión principal. Exportar show incluye medios; copiar solo JSON no. Migración sin sobrescribir shows; conserva `backups\legacy-data` si contiene audio referenciado.

Build sin firma. SmartScreen es independiente de la integridad de la descarga. La red se necesita para buscar actualizaciones, no para reproducir.

[Guía](README.es.md)
