# Trazalibre (Pizarra y Marcador sobre Pantalla)

**Trazalibre** es una aplicación de anotación en pantalla y pizarra interactiva inspirada en **IPEVO Annotator**, desarrollada en C# con WPF bajo una arquitectura estrictamente modular y desacoplada con precisión opcional de dibujo técnico CAD.

---

## ✨ ¿Qué es Trazalibre?

Imagina tener un **marcador mágico** con el que puedes dibujar, escribir o señalar directamente sobre el cristal de la pantalla de tu computadora:

* **Dibuja sobre cualquier cosa:** Puedes hacer anotaciones encima de presentaciones de PowerPoint, videos en reproducción, páginas web, documentos PDF, hojas de cálculo o programas de diseño. Tus trazos quedan flotando sobre la pantalla mientras el contenido sigue visible por debajo.
* **Pensada para todos:** Es la herramienta ideal para **docentes** que dictan clases virtuales o presenciales, **estudiantes** que toman notas visuales, **diseñadores** que señalan correcciones y detalles, **profesionales** que realizan presentaciones de trabajo, y cualquier usuario que necesite explicar algo visualmente de manera rápida.
* **Sin configuraciones complicadas:** No necesitas instalar programas pesados ni crear cuentas. Abres la aplicación y ya tienes a tu alcance lápices de colores, resaltadores, texto, flechas, figuras geométricas y herramientas de medición milimétrica.

---

## ⚡ Guía de Inicio en 3 Minutos

¡Aprende a dominar Trazalibre en cuatro sencillos pasos!

1. **Abrir la aplicación:**
   * Ejecuta `bin\Trazalibre.exe` (o compila al instante con doble clic en `build.bat`).
   * Aparecerá una barra de herramientas elegante y compacta en la esquina superior derecha de tu monitor. Puedes arrastrarla a cualquier lugar de la pantalla que prefieras.

2. **Comenzar a dibujar:**
   * Haz clic en el icono del **Lápiz** (o pulsa la tecla **`P`**) para realizar trazos libres.
   * Haz clic en el **Resaltador** (o tecla **`H`**) para subrayar textos o partes importantes sin tapar lo que hay debajo.
   * Selecciona el color que más te guste en la paleta inferior y ajusta el grosor según lo que necesites.

3. **Volver a usar la computadora con normalidad (`ESC` o `F8`):**
   * ¿Necesitas hacer clic en un enlace web, cambiar de diapositiva o abrir un archivo mientras tienes dibujos en pantalla?
   * Presiona la tecla **`ESC`** (o pulsa el atajo global **`F8`** desde cualquier programa).
   * Pasarás al **Modo Ratón**: tus dibujos se mantendrán visibles en pantalla, pero el cursor atravesará el lienzo de forma transparente para que puedas usar tu computadora normalmente.
   * Cuando quieras volver a dibujar, presiona **`F8`** o pulsa la tecla **`P`**.

4. **Tomar una captura rápida y compartirla (`Ctrl + S`):**
   * Presiona **`Ctrl + S`** (o el botón **`📸 Capturar Sector`**).
   * La pantalla se atenuará levemente. Haz clic y arrastra un recuadro sobre la zona que quieres recortar.
   * Al soltar el ratón, la imagen se guardará automáticamente en formato PNG y **se copiará al portapapeles**. ¡Solo presiona **`Ctrl + V`** para pegarla en WhatsApp, Telegram, correo electrónico, Word o tu programa favorito!

> 📚 **¿Buscas el manual exhaustivo y los controles avanzados?**  
> Consulta el [**Manual de Uso Completo**](MANUAL_DE_USO.md) para conocer en detalle cada herramienta, atajos de teclado, instrumentos geométricos graduados, precisión milimétrica, entrada CAD y diagramas vectoriales.

---

## 🏛️ Arquitectura Modular

El proyecto está diseñado para que cualquier funcionalidad o herramienta pueda modificarse, reemplazarse o agregarse **sin riesgo de romper o afectar a los demás módulos**.

```text
G:\Trazalibre\
│   build.bat                    <- Compilador automático recursivo con icono
│   README.md                    <- Documentación general y arquitectura
│   MANUAL_DE_USO.md             <- Manual de usuario exhaustivo paso a paso
│
├───src\
│   │   Program.cs               <- Inyección de dependencias y composición
│   │   app.ico                  <- Icono oficial multirresolución Win32
│   │
│   ├───Core\                    <- Contratos e interfaces base (Sin dependencias externas)
│   │       IAnnotationContext.cs <- Abstracción del lienzo para herramientas
│   │       ITool.cs              <- Contrato independiente para cualquier herramienta
│   │       ToolRegistry.cs       <- Gestor dinámico del ciclo de vida de herramientas
│   │       BackgroundMode.cs     <- Enumeración de modos de pizarra
│   │       ISequenceTool.cs      <- Contrato para herramientas con conteo
│   │
│   ├───Services\                <- Lógica de infraestructura y servicios aislados
│   │       NativeWindowService.cs <- Win32 API (Click-through, WS_EX_TRANSPARENT)
│   │       ScreenshotService.cs   <- Captura de pantalla, PNG y portapapeles
│   │       BackgroundService.cs   <- Fondos de pizarra (blanca, negra, transparente)
│   │       SnapService.cs         <- Captura magnética de extremos (Endpoint Snapping) e indicadores CAD
│   │       MetricDimensionHelper.cs <- Controlador de precisión milimétrica (1 mm, entrada tipo CAD)
│   │       HotkeyService.cs       <- Gestor de teclas rápidas globales
│   │
│   ├───Tools\                   <- Implementaciones aisladas de herramientas
│   │       MouseTool.cs         <- Modo ratón (pasa clics al sistema)
│   │       PenTool.cs           <- Lápiz opaco estándar
│   │       HighlighterTool.cs   <- Marcador translúcido
│   │       EraserTool.cs        <- Borrador de trazos
│   │       TextTool.cs          <- Herramienta de texto en pantalla
│   │       ArrowShapeTool.cs    <- Flecha interactiva
│   │       RectangleShapeTool.cs <- Rectángulo
│   │       EllipseShapeTool.cs  <- Círculo y elipse
│   │       LineShapeTool.cs     <- Línea recta milimetrada
│   │       VectorTool.cs        <- Vector interactivo entre dos puntos con medidas y componentes
│   │       AxesTool.cs          <- Ejes de coordenadas cartesianos con punto de anclaje (0, 0)
│   │       ProtractorTool.cs    <- Transportador interactivo 360° con rueda de ratón y trazado angular
│   │       RulerTool.cs         <- Regla graduada milimetrada con rotación por rueda y medición px/cm
│   │       Triangle45Tool.cs    <- Escuadra isósceles 45°-45°-90° con rotación y trazado técnico
│   │       Triangle30Tool.cs    <- Escuadra escalena 30°-60°-90° (Cartabón) con rotación y trazado
│   │       CompassTool.cs       <- Compás interactivo con aguja, apertura de radio y regulación de arco
│   │       BaseCalloutTool.cs   <- Base para globos de diálogo con flecha y editor de texto
│   │       RectangularCalloutTool.cs <- Globo rectangular con flecha y texto editable
│   │       OvalCalloutTool.cs   <- Globo ovalado con flecha y texto editable
│   │       CloudCalloutTool.cs  <- Globo nube con flecha y texto editable
│   │       CheckStampTool.cs    <- Sello de visto bueno / check
│   │       CrossStampTool.cs    <- Sello de cruz / error
│   │       StarStampTool.cs     <- Sello de estrella
│   │       ArrowStampTool.cs    <- Sello de flecha fija
│   │       NumberStampTool.cs   <- Sello de número secuencial interactivo
│   │
│   └───UI\                      <- Vistas desacopladas
│           OverlayWindow.cs     <- Lienzo de pantalla completa (implementa IAnnotationContext)
│           ToolbarWindow.cs     <- Barra flotante dinámica con icono oficial
│           SectorSelectionWindow.cs <- Selector interactivo de sector de pantalla con arrastre y guía
│
└───bin\
        Trazalibre.exe           <- Binario oficial generado
```

---

## ⌨️ Atajos de Teclado (Hotkeys)

| Tecla | Acción |
| :--- | :--- |
| **`ESC`** | **Salida inmediata:** Cambia al instante a **Modo Ratón** (clics transparentes) para usar tu PC con normalidad. |
| **`F8`** | **Tecla rápida global:** Alterna entre Lápiz y Ratón desde **cualquier aplicación** de Windows (incluso si Trazalibre no tiene el foco). |
| **`P`** | Activar herramienta **Lápiz**. |
| **`H`** | Activar herramienta **Resaltador**. |
| **`E`** | Activar herramienta **Borrador**. |
| **`T`** | Herramienta **🔤 Texto** (clic para escribir; Enter para fijar; Shift+Enter salto de línea). |
| **`F`** | Forma **🏹 Flecha interactiva** (arrastra para apuntar). |
| **`Q`** | Forma **⬜ Rectángulo** (arrastra para encuadrar). |
| **`O`** | Forma **⭕ Círculo / Elipse** (arrastra para rodear). |
| **`L`** | Forma **📏 Línea recta** (arrastra para unir). |
| **`G`** | Geometría **🌐 Ejes Coordenados (0, 0)** (coloca ejes ortogonales graduados con nódulo en el origen; incrementables arrastrando con el ratón, con la rueda, teclas `+`/`-` o botones `[➕]`/`[➖]` en la barra). |
| **`J`** | Geometría **📐 Transportador 360°** (muestra grados en tiempo real; gira la **rueda del ratón** para ajustar el ángulo exacto de 0° a 360° o ±180°; haz clic y arrastra para trazar una línea con dicho ángulo). |
| **`M`** | Geometría **📏 Regla Graduada** (regla milimetrada con rotación por rueda de ratón y medición en px y cm). |
| *Menú* | Geometría **📐 Escuadra 45°** (escuadra isósceles 45°-45°-90° con rotación por rueda para trazar a 45° y 90°). |
| *Menú* | Geometría **📐 Escuadra 30° / Cartabón** (escuadra 30°-60°-90° con rotación por rueda para trazado técnico). |
| **`U`** | Geometría **🧭 Compás** (clava la aguja en el centro, abre el radio y regula el arco con la rueda del ratón; suelta para trazar la circunferencia). |
| **`B`** | Globo **💬 Globo Rectangular con flecha** (arrastra para crear el globo y escribe el enunciado; Enter para fijar). |
| *Menú* | Globo **🗨️ Globo Ovalado con flecha** (viñeta elíptica con flecha y texto editable). |
| *Menú* | Globo **💭 Globo Nube con flecha** (nube de pensamiento con flecha y texto editable). |
| **`K`** | Sello **✔️ Check / Aprobado**. |
| **`X`** | Sello **❌ Cruz / Error**. |
| **`R`** | Sello **⭐ Estrella**. |
| **`A`** | Sello **➔ Flecha fija**. |
| **`N`** | Sello **① Número secuencial** (clic derecho reinicia contador). |
| **`1`** | Grosor **Fino** (2 px). |
| **`2`** | Grosor **Normal** (4 px). |
| **`3`** | Grosor **Grueso** (8 px). |
| **`4`** | Grosor **Extra** (14 px). |
| **`C`** | **Limpiar** todos los trazos de la pantalla. |
| **`Ctrl + Z`** | **Deshacer** el último trazo. |
| **`Ctrl + S`** | **Capturar sector de pantalla:** Activa el selector interactivo para arrastrar con el ratón el sector a capturar, guardar en PNG y copiar al portapapeles (`ESC` para cancelar). |
| **`V`** | Herramienta **🏹 Vector con Medidas:** Traza un vector entre dos puntos con visor en tiempo real de módulo en mm/cm, ángulo trigonométrico y componentes $\Delta X, \Delta Y$, estampando la cota técnica permanente. |
| **`D`** | **Acotación Vectorial (Módulo y Ángulo):** Alterna el estampado automático de la cota técnica en el lienzo con el formato `[módulo mm ∠ ángulo°]` perpendicular al vector. |

---

## 🌐 Ejes de Coordenadas Cartesianos y Vectores con Medidas

Trazalibre incorpora soporte completo para diagramas de física, matemáticas y geometría analítica:

* **🌐 Ejes de Coordenadas Cartesianos (`G`):**
  * Coloca un sistema completo de ejes ortogonales graduados con flechas direccionales en $+X$ e $+Y$.
  * **Punto de anclaje magnético en el origen $(0, 0)$:** Posee un nódulo circular dorado en la intersección exacta que es reconocido de forma prioritaria por `SnapService` (`🎯 Origen (0, 0)`), permitiendo clavar y "pegar el primer punto" con precisión absoluta.
  * **Incremento dinámico:** Los ejes pueden incrementarse arrastrando con el ratón hacia afuera desde el origen, con la rueda del ratón, con las teclas `+` y `-`, o mediante los botones dedicados **`[ ➕ ]`** y **`[ ➖ ]`** en la barra flotante.
  * **Graduación numérica incremental:** Cada centímetro (10 mm / 50 px) incluye marcas métricas con su correspondiente número ($1, 2, 3, 4, 5...$ cm) en los cuatro cuadrantes.

* **🏹 Vector entre Dos Puntos con Medidas (`V`):**
  * Permite hacer clic en el Punto 1 (ej. en el origen `🎯 Origen (0, 0)`) y arrastrar hacia el Punto 2.
  * Muestra una placa flotante HUD en tiempo real con el **Módulo** en mm y cm, el **Ángulo trigonométrico cartesiano** (`∠ θ°`) y las **Componentes ortogonales** ($\Delta X$ y $\Delta Y$).
  * Al soltar, estampa el vector con su punta de flecha y la cota técnica de medidas permanente.

---

## 🏹 Modo Vectorial y Diagramas Técnicos

* Al activar el **Modo Vector** (con la tecla **`V`** o desde el botón conmutador de la barra de herramientas), las herramientas graduadas de trazado (**Regla**, **Escuadra 45°**, **Cartabón 30°/60°**, **Transportador** y **Línea recta**) colocan una **punta de flecha orientada** en el extremo del trazo.
* **🏷️ Acotación de Módulo y Ángulo (`D`):**
  * Al conmutar la acotación técnica con la tecla **`D`** o desde el botón `🏷️ Cota (Módulo/Ángulo): SÍ/NO [D]`, se muestra una **previsualización flotante en tiempo real** con una placa dorada `[45.0 mm ∠ 30°]`.
  * Al soltar el trazo o presionar `Enter`, se estampa automáticamente la cota vectorial en el lienzo como trazo nativo permanente (`mm` de longitud y `∠ θ°` de dirección respecto a la horizontal), calculada perpendicularmente al vector para no entorpecer la lectura.
  * Dado que la cota se genera como trazo vectorial permanente, admite deshacer con `Ctrl + Z`, borrado individual y exportación/captura sin perder fidelidad.
* **Suma gráfica de vectores (*Cabeza a cola*):** La punta de flecha es detectada por el sistema magnético `SnapService` (`🧲 Punta de Vector`), permitiendo encadenar vectores sucesivos a partir del vértice exacto del vector anterior.

---

## 📏 Módulo Geométrico y Control Métrico Graduado (de a 1 mm y CAD)

Todas las herramientas graduadas de dibujo técnico (**Regla**, **Escuadra 45°**, **Cartabón 30°/60°**, **Transportador 360°** y **Compás**) incorporan calibración métrica técnica y doble modalidad de control de longitud:

1. **Ajuste paso a paso de a 1 milímetro:**
   * **Flechas `↑` / `→` o tecla `+`:** Aumenta la longitud en **+1 mm** (+5 px).
   * **Flechas `↓` / `←` o tecla `-`:** Disminuye la longitud en **-1 mm** (-5 px).
   * **`Shift` + Flechas o `+`/`-`:** Ajusta en pasos de **10 mm** (1 cm / 50 px).
   * **`Ctrl` + Rueda del ratón:** Regula la longitud en pasos continuos de **1 mm** (con `Shift`: 10 mm).
   * **Arrastre del ratón:** Se imanta automáticamente a cada milímetro entero (*Metric Snapping* a 5 px/mm; mantén pulsado `Alt` para movimiento continuo píxel a píxel).

2. **Ingreso numérico directo (estilo CAD):**
   * Mientras cualquier herramienta graduada está activa, simplemente **escribe el número en el teclado** (ej. teclea `45` o `12.5`).
   * En pantalla aparece el HUD dorado: `⌨️ Ingresando Longitud: [ 45 ] mm`.
   * Pulsa **`Enter`** para fijar la medida exacta o trazar la línea directamente con esa dimensión.
   * Pulsa **`Backspace`** para corregir o **`ESC`** para cancelar la entrada numérica.

3. **Imantación a extremos (*Endpoint Snapping*):**
   * Al acercar el origen de la regla, escuadra o transportador al extremo o vértice de una línea previa, se ilumina el recuadro verde magnético CAD con la etiqueta `🧲 Punto Final` para continuar dibujando exactamente desde ahí.

---

## ⚡ Compilación, Icono y Ejecución

* **Icono oficial:** Generado en `src\app.ico` (resoluciones de 16px a 256px en 32-bit ARGB con el diseño seleccionado) e incrustado nativamente en el ejecutable Win32.
* **Compilar:** Ejecuta `build.bat` (compila recursivamente todo el código fuente en UTF-8 con `/codepage:65001` e incrusta el icono de la aplicación).
* **Ejecutar:** Ejecuta `bin\Trazalibre.exe`.
