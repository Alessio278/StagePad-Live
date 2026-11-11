# Actualizar StagePad sin reinstalar

1. Cierra la versión anterior y espera unos segundos.
2. Extrae el nuevo paquete Windows y sustituye el antiguo `StagePad.exe` en tu carpeta de la aplicación.
3. Inicia el nuevo `StagePad.exe`.

El EXE incluye su runtime: no hacen falta instalador ni reinstalación de dependencias.

Proyectos, ajustes, asignaciones y último show permanecen en `%LOCALAPPDATA%\StagePad` y en la carpeta de proyectos configurada. Conserva estas carpetas. Los audios originales mantienen sus rutas.

Si una instancia anterior invisible impide sustituir el archivo, abre el nuevo EXE desde otra carpeta. La versión 0.12.0 intenta recuperar instancias bloqueadas del mismo usuario/sesión. Si hay una ventana visible se activa: ciérrala antes de actualizar.

El paquete GitHub contiene los fuentes; para usar o actualizar basta el paquete Windows.

**Exportar show con audio** crea `.stagepad.zip` con audio original; **Importar show** acepta una ruta completa o un paquete en la carpeta de proyectos. Importa con otro nombre sin sobrescribir. Detén audio/secuencias antes. Límite: 8 GiB de medios.
