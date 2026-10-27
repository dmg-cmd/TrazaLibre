# Manual de Uso Oficial de Trazalibre
*Aplicación de Anotación en Pantalla, Pizarra Interactiva y Dibujo Técnico CAD*

---

## 📖 Índice General

1. [Introducción y Concepto](#1-introducción-y-concepto)
2. [Requisitos y Modo de Compilación](#2-requisitos-y-modo-de-compilación)
3. [La Barra de Herramientas Flotante](#3-la-barra-de-herramientas-flotante)
4. [Herramientas Principales de Anotación](#4-herramientas-principales-de-anotación)
5. [Formas Geométricas y Globos de Texto](#5-formas-geométricas-y-globos-de-texto)
6. [Instrumentos de Dibujo Técnico y Geometría](#6-instrumentos-de-dibujo-técnico-y-geometría)
7. [Modo Vectorial y Diagramas Técnicos](#7-modo-vectorial-y-diagramas-técnicos)
8. [Sistema de Precisión Milimétrica y Entrada CAD](#8-sistema-de-precisión-milimétrica-y-entrada-cad)
9. [Imantación a Extremos (Endpoint Snapping)](#9-imantación-a-extremos-endpoint-snapping)
10. [Sellos de Revisión y Numeración Secuencial](#10-sellos-de-revisión-y-numeración-secuencial)
11. [Modos de Fondo (Pizarra y Pantalla Transparente)](#11-modos-de-fondo-pizarra-y-pantalla-transparente)
12. [Herramienta de Captura de Sector (Screenshot)](#12-herramienta-de-captura-de-sector-screenshot)
13. [Tabla Completa de Atajos de Teclado](#13-tabla-completa-de-atajos-de-teclado)
14. [Preguntas Frecuentes y Solución de Problemas](#14-preguntas-frecuentes-y-solución-de-problemas)

---

## 1. Introducción y Concepto

**Trazalibre** es una solución integral y modular para la anotación sobre pantalla, pizarra digital interactiva y dibujo geométrico de precisión inspirada en *IPEVO Annotator*. 

A diferencia de las pizarras convencionales, Trazalibre combina:
- **Anotación libre sobre el escritorio de Windows** sin interferir con las aplicaciones que estés ejecutando.
- **Instrumentos de dibujo técnico graduados** (Regla, Escuadras de 45° y 30°/60°, Transportador 360° y Compás).
- **Entrada milimétrica exacta tipo CAD** (calibrada a escala real de 5 píxeles por milímetro).
- **Diagramas vectoriales asistidos** con cálculo automático de módulos y ángulos de inclinación.
- **Sellos visuales y numeración correlativa** para corrección de planos, código y documentos.

---

## 2. Requisitos y Modo de Compilación

### Requisitos del Sistema
- **Sistema Operativo:** Microsoft Windows 7, 8, 10 o 11 (64 bits o 32 bits).
- **Plataforma:** Microsoft .NET Framework 4.0 o superior (preinstalado de forma nativa en todas las versiones modernas de Windows).
- **Dependencias externas:** Cero dependencias pesadas ni instaladores adicionales.

### Compilación Rápida
1. Abre una consola o haz doble clic sobre el archivo `build.bat` ubicado en la raíz del proyecto.
2. El script detectará el compilador de C# nativo de Windows (`csc.exe`), compilará todos los módulos ubicados en `src/` en codificación UTF-8 e incrustará el icono multirresolución oficial (`src/app.ico`).
3. El archivo ejecutable final se guardará en `bin\Trazalibre.exe`.

---

## 3. La Barra de Herramientas Flotante

Al iniciar Trazalibre, se mostrará en pantalla una barra flotante oscura, compacta y elegante en el margen superior derecho:

- **Desplazamiento:** Puedes hacer clic sobre cualquier sector libre de la barra y arrastrarla a la posición de la pantalla que te resulte más cómoda.
- **Botón de Cierre (✕):** Ubicado en la esquina superior derecha del encabezado, cierra de inmediato la aplicación y libera todos los recursos del sistema.
- **Persistencia Visual:** La barra permanece siempre visible por encima de cualquier ventana (`Topmost`), permitiéndote cambiar de herramienta en cualquier momento.

---

## 4. Herramientas Principales de Anotación

En la fila superior de la barra se sitúan las 5 herramientas de uso más recurrente:

### 🖱️ Modo Ratón / Clic Transparente (`ESC`)
- **Propósito:** Desactiva temporalmente el lienzo de dibujo y convierte la ventana de Trazalibre en transparente a los clics del ratón (`WS_EX_TRANSPARENT`).
- **Uso:** Te permite interactuar directamente con tu escritorio, navegar por Internet, cambiar diapositivas o escribir en otros programas sin perder las anotaciones previas.
- **Atajo Global `F8`:** Puedes presionar la tecla `F8` en cualquier momento y desde cualquier aplicación para conmutar instantáneamente entre el Modo Ratón y el Lápiz.

### ✏️ Lápiz Opaco (`P`)
- Trazo estándar nítido de color sólido.
- Su grosor y color se ajustan en tiempo real desde el deslizador y la paleta.

### 🖍️ Resaltador Translúcido (`H`)
- Diseñado para subrayar textos, documentos PDF o secciones de código fuente.
- Posee un canal alfa semitransparente que resalta el contenido inferior sin taparlo ni oscurecerlo.

### 🧹 Borrador de Trazos (`E`)
- Borrado por trazo completo: basta con hacer clic o rozar cualquier trazo dibujado para eliminarlo limpiamente de la pantalla.

### 🔤 Texto Libre (`T`)
- Permite escribir notas y comentarios tipográficos en cualquier posición de la pantalla.
- **Cómo usarlo:**
  1. Haz clic con el botón izquierdo en el lugar de la pantalla donde deseas escribir.
  2. Escribe tu texto.
  3. Presiona **`Enter`** para fijar el texto en el lienzo.
  4. Si necesitas saltos de línea (párrafos múltiples), presiona **`Shift + Enter`**.
  5. Presiona **`ESC`** si deseas cancelar la edición del texto.

---

## 5. Formas Geométricas y Globos de Texto

Desplegando el menú **`🔷 Formas:`** o usando sus atajos directos:

- **🏹 Flecha Interactiva (`F`):** Haz clic en el punto de origen y arrastra hacia el objetivo; al soltar, se dibuja una flecha recta orientada con punta proporcional.
- **⬜ Rectángulo (`Q`):** Haz clic y arrastra diagonalmente para encuadrar áreas rectangulares.
- **⭕ Círculo / Elipse (`O`):** Haz clic y arrastra para encerrar elementos en óvalos o círculos regulares.
- **📏 Línea Recta (`L`):** Une dos puntos mediante un segmento perfectamente recto.

### 💬 Globos de Diálogo y Explicaciones (Callouts)
- **💬 Globo Rectangular con Flecha (`B`):** Arrastra para situar el globo sobre el elemento a explicar. Se abrirá automáticamente un editor de texto interno para redactar la explicación. Presiona `Enter` para fijarlo.
- **🗨️ Globo Ovalado:** Viñeta redondeada clásica para diálogos o notas descriptivas.
- **💭 Globo Nube:** Estilo visual de pensamiento para sugerencias, ideas o recordatorios.

---

## 6. Instrumentos de Dibujo Técnico y Geometría

Desplegando el menú **`📐 Geometría:`** accedes a una suite completa de instrumentos técnicos graduados con medición en tiempo real:

### 📏 Regla Graduada Milimetrada (`M`)
- Muestra una regla milimétrica realista en pantalla.
- **Rotación:** Gira la **rueda del ratón** para inclinar la regla en ángulos precisos de grado en grado.
- **Trazado:** Haz clic y arrastra a lo largo del borde superior de la regla para trazar líneas perfectamente alineadas y milimetradas.

### 📐 Transportador 360° (`J`)
- Transportador circular completo con divisiones de grado en grado.
- **Rotación angular:** Gira la **rueda del ratón** para fijar el ángulo deseado (con visor digital en pantalla).
- **Trazado de ángulos:** Haz clic y arrastra para trazar el segmento con la inclinación exacta seleccionada.

### 📐 Escuadra Isósceles 45° (`Menú Geometría`)
- Instrumento con ángulos de 45°-45°-90°.
- Permite trazar paralelas, perpendiculares y bisectrices exactas a 45 grados.
- Se orienta dinámicamente con la rueda del ratón.

### 📐 Cartabón Escaleno 30°/60° (`Menú Geometría`)
- Instrumento clásico con ángulos de 30°-60°-90°.
- Ideal para proyecciones axonométricas, isometrías y perspectivas técnicas.
- Se orienta libremente con la rueda del ratón.

### 🧭 Compás Interactivo (`U`)
- Permite trazar circunferencias y arcos con precisión milimétrica.
- **Cómo usarlo:**
  1. Haz clic con el botón izquierdo sobre la pantalla para clavar la **aguja central**.
  2. Arrastra el ratón hacia afuera para regular el **radio** en milímetros o centímetros.
  3. Gira la **rueda del ratón** para definir la amplitud del arco (desde unos pocos grados hasta los 360° completos).
  4. Suelta el botón para dibujar la circunferencia o arco perfecto.

---

## 7. Modo Vectorial y Diagramas Técnicos

Trazalibre cuenta con un motor especializado para física, ingeniería y matemáticas vectoriales:

### 🏹 Activación del Modo Vector (`V`)
- Presiona la tecla **`V`** o pulsa el botón conmutador de la barra de herramientas.
- Al activarse, cualquier línea recta o trazado realizado con la **Regla**, las **Escuadras**, el **Transportador** o la **Línea Recta** incorporará de forma automática una **punta de flecha orientada** en su extremo final.

### 🏷️ Acotación Técnica Automática (`D`)
- Presiona la tecla **`D`** para activar la estampa de cotas.
- Mientras dibujas el vector, verás un indicador dorado en tiempo real:  
  `[ 45.0 mm ∠ 30.0° ]`
- Al soltar el trazo, la cota se graba de forma permanente como dibujo vectorial junto al vector, colocada ortogonalmente para garantizar una visibilidad óptima.
- La cota incluye el **módulo** (en milímetros reales) y el **ángulo de inclinación** (en grados sexagesimales respecto a la horizontal).

### 🧲 Suma de Vectores ("Cabeza a cola")
- Gracias al sistema de imantación a extremos (`SnapService`), al iniciar un nuevo vector cerca de la punta del anterior, el cursor se imantará automáticamente al vértice (`🧲 Punta de Vector`), permitiendo componer polígonos de fuerzas y sumas vectoriales exactas.

---

## 8. Sistema de Precisión Milimétrica y Entrada CAD

Todas las herramientas de dibujo técnico incorporan una escala calibrada:
$$\text{Escala:} \quad 5\text{ píxeles} = 1\text{ milímetro} \quad (50\text{ px} = 1\text{ cm})$$

### Modos de control de longitud:

1. **Entrada Numérica Directa estilo CAD:**
   - Mientras tienes seleccionada una herramienta técnica (Regla, Escuadra, Compás o Línea), simplemente **teclea la medida deseada en tu teclado numérico** (por ejemplo, escribe `65` o `12.5`).
   - Aparecerá en pantalla el panel HUD: `⌨️ Ingresando Longitud: [ 65 ] mm`.
   - Presiona **`Enter`** para fijar la medida o trazar directamente la línea con esa longitud exacta.
   - Si te equivocas, presiona **`Backspace`** para corregir o **`ESC`** para cancelar.

2. **Ajuste Fino Milimétrico por Teclado:**
   - **`↑` / `→` o tecla `+`:** Aumenta la longitud en **+1 mm** (+5 px).
   - **`↓` / `←` o tecla `-`:** Disminuye la longitud en **-1 mm** (-5 px).
   - **`Shift` + Flechas:** Modifica en saltos de **10 mm** (1 centímetro / 50 px).

3. **Ajuste Milimétrico por Ratón:**
   - **`Ctrl` + Rueda del ratón:** Modifica de a 1 mm.
   - **`Ctrl + Shift` + Rueda del ratón:** Modifica de a 10 mm (1 cm).
   - **Arrastre libre:** Imantación automática al milímetro entero más cercano. Mantén presionado **`Alt`** si prefieres moverte libremente píxel a píxel.

---

## 9. Imantación a Extremos (Endpoint Snapping)

El servicio `SnapService` examina continuamente las figuras trazadas en el lienzo para facilitarte la continuidad del dibujo:

- Al aproximar el cursor a una distancia de 20 píxeles del inicio o final de cualquier trazo previo, se activará un recuadro verde con la etiqueta:
  - `🧲 Punto Inicial`: Se adhiere al origen del segmento previo.
  - `🧲 Punto Final`: Se adhiere al extremo final.
  - `🧲 Punta de Vector`: Se adhiere a la punta exacta de la flecha vectorial.
- Esto asegura que tus figuras, triángulos y polígonos cierren de manera geométrica perfecta sin holguras.

---

## 10. Sellos de Revisión y Numeración Secuencial

Para agilizar la corrección de trabajos, diagramas de flujo y revisiones visuales:

- **✔️ Check / Aprobado (`K`):** Estampa un visto bueno verde de verificación.
- **❌ Cruz / Error (`X`):** Estampa una cruz roja de corrección.
- **⭐ Estrella (`R`):** Marca puntos destacados o ítems de atención.
- **➔ Flecha fija (`A`):** Coloca un indicador de dirección rápido con un solo clic.

### ① Numeración Secuencial Correlativa (`N`)
- Estampa un círculo con un número correlativo en su interior: ①, ②, ③, ④...
- **Incremento automático:** Con cada clic que hagas en la pantalla, el número se estampa y el contador aumenta automáticamente en 1 unidad.
- **Cambio de número inicial:** En la barra de herramientas, dentro de la caja `🔢 Nº inicio`, puedes escribir cualquier cifra para comenzar la serie donde gustes.
- **Botón `🔄 Reset`:** Reinicia inmediatamente la cuenta a 1.
- **Atajo con el ratón:** Un **clic derecho** sobre el lienzo mientras el sello de número está activo reinicia el contador a 1.

---

## 11. Modos de Fondo (Pizarra y Pantalla Transparente)

En la sección inferior de la barra puedes alternar el entorno de trabajo:

- **🖥️ Pantalla Transparente:** Modo por defecto. El fondo es completamente invisible, lo que te permite anotar sobre videos, presentaciones, código o páginas web.
- **⬜ Pizarra Blanca:** Cubre toda la pantalla con un fondo blanco impecable, transformando tu monitor en una pizarra de clase o reunión.
- **⬛ Pizarra Negra:** Cubre la pantalla con fondo oscuro tipo pizarra tradicional, ideal para trabajar con colores claros (blanco, oro, lima) reduciendo la fatiga visual.

---

## 12. Herramienta de Captura de Sector (Screenshot)

Trazalibre incluye un capturador interactivo de alta fidelidad:

- **Acceso:** Presiona **`Ctrl + S`** o pulsa el botón **`📸 Capturar Sector`** en la barra.
- **Modo de captura:**
  1. La pantalla se oscurecerá levemente con una mira de precisión.
  2. Haz clic con el botón izquierdo y arrastra un recuadro para definir el sector exacto que deseas recortar.
  3. Al soltar el botón, Trazalibre captura el área seleccionada (incluyendo tanto el contenido de la pantalla como tus anotaciones).
- **Resultado automático:**
  - Se guarda una imagen en formato **PNG** con fecha y hora en el directorio de la aplicación.
  - La imagen se copia **automáticamente al portapapeles de Windows**, lista para pegarla con `Ctrl + V` en WhatsApp, Telegram, Word, Paint o correo electrónico.
- Si decides no capturar nada, presiona la tecla **`ESC`** para salir sin guardar.

---

## 13. Tabla Completa de Atajos de Teclado

### 🎮 Control General y Modos
| Atajo | Acción |
| :--- | :--- |
| **`ESC`** | Cambia de inmediato al **Modo Ratón** (clics transparentes sobre Windows). Si se está escribiendo texto, cancela la entrada. |
| **`F8`** | **Atajo Global:** Alterna entre Lápiz y Ratón desde **cualquier aplicación** sin necesidad de enfocar Trazalibre. |
| **`Ctrl + Z`** | **Deshacer** el último trazo o elemento dibujado. |
| **`C`** | **Limpiar** la totalidad de los trazos del lienzo. |
| **`Ctrl + S`** | Activar el selector interactivo de **Captura de Sector**. |

### 🛠️ Selección de Herramientas
| Atajo | Herramienta |
| :--- | :--- |
| **`P`** | Lápiz libre |
| **`H`** | Resaltador semitransparente |
| **`E`** | Borrador de trazos |
| **`T`** | Texto editable |
| **`F`** | Forma Flecha |
| **`Q`** | Forma Rectángulo |
| **`O`** | Forma Elipse / Círculo |
| **`L`** | Forma Línea recta |
| **`B`** | Globo de diálogo rectangular |
| **`J`** | Transportador 360° |
| **`M`** | Regla graduada milimetrada |
| **`U`** | Compás interactivo |

### ⭐ Sellos
| Atajo | Sello |
| :--- | :--- |
| **`K`** | Sello Check (Visto bueno) |
| **`X`** | Sello Cruz (Error) |
| **`R`** | Sello Estrella |
| **`A`** | Sello Flecha fija |
| **`N`** | Sello de Número secuencial |

### 📐 Modos de Dibujo Técnico y Vectorial
| Atajo | Acción |
| :--- | :--- |
| **`V`** | Alternar **Modo Vector** (agrega punta de flecha a las herramientas lineales). |
| **`D`** | Alternar **Acotación Técnica** (imprime `[módulo mm ∠ ángulo°]` junto al vector). |
| **`0-9` + `Enter`** | Entrada numérica estilo CAD para fijar la longitud exacta en milímetros. |
| **`↑` / `→` o `+`** | Incrementar longitud en **+1 mm**. |
| **`↓` / `←` o `-`** | Disminuir longitud en **-1 mm**. |
| **`Shift` + Flechas** | Modificar longitud en pasos de **10 mm** (1 cm). |
| **Rueda del Ratón** | Rotar herramientas graduadas (Regla, Escuadras, Transportador) o arco del Compás. |
| **`Ctrl` + Rueda** | Regular la longitud en pasos continuos de 1 mm. |
| **`Alt` (sostenido)** | Desactivar la imantación milimétrica para trazado libre píxel a píxel. |

### 📏 Grosores de Trazo
| Atajo | Grosor |
| :--- | :--- |
| **`1`** | Fino (2 píxeles) |
| **`2`** | Normal (4 píxeles) |
| **`3`** | Grueso (8 píxeles) |
| **`4`** | Extra (14 píxeles) |

---

## 14. Preguntas Frecuentes y Solución de Problemas

#### 1. ¿Cómo vuelvo a interactuar con las carpetas o ventanas de Windows sin cerrar Trazalibre?
Simplemente pulsa la tecla **`ESC`** o el atajo global **`F8`**. Entrarás al instante en el **Modo Ratón**, donde el puntero traspasa el lienzo y puedes hacer clic en cualquier programa de tu equipo.

#### 2. ¿Cómo continuo una línea exactamente desde el final de la anterior?
Acerca el puntero del ratón al extremo de la línea anterior. Verás aparecer un recuadro verde con la leyenda `🧲 Punto Final`. Haz clic allí y el nuevo trazo arrancará milimétricamente acoplado al anterior.

#### 3. ¿Dónde se guardan las capturas de pantalla tomadas con `Ctrl + S`?
Se guardan como archivos PNG en la carpeta del programa con la nomenclatura `Captura_Anotacion_YYYYMMDD_HHMMSS.png` y, simultáneamente, quedan listas en tu portapapeles para pegarlas donde desees con `Ctrl + V`.

#### 4. ¿Puedo personalizar los colores y grosores de las herramientas técnicas?
Sí. El deslizador de grosor (1 a 30 px) y los botones de colores de la barra aplican de forma inmediata tanto al lápiz y figuras como a la regla, escuadras y vectores.
