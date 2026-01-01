<p align="center">
  <img src="XiloAdventures.Wpf.Common/Assets/logo.png" alt="XiloAdventures logo" width="200">
</p>

<p align="center">
  <strong>Crea, juega y distribuye aventuras conversacionales sin escribir una sola línea de código</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white" alt=".NET 8">
  <img src="https://img.shields.io/badge/UI-WPF-0d5a8f?logo=windows&logoColor=white" alt="WPF">
  <img src="https://img.shields.io/badge/Language-C%23-239120?logo=csharp&logoColor=white" alt="C#">
  <img src="https://img.shields.io/badge/Estado-Activo-success" alt="Estado">
  <img src="https://img.shields.io/badge/Tests-838%20passing-brightgreen" alt="Tests">
</p>

<p align="center">
  <a href="https://www.paypal.me/xmasmusicsoft">
    <img src="https://img.shields.io/badge/❤️_¡Se_aceptan_donaciones!-PayPal-00457C?style=for-the-badge&logo=paypal&logoColor=white" alt="Donar">
  </a>
</p>

---

## Qué es XiloAdventures

XiloAdventures es un **ecosistema completo** para crear y jugar aventuras conversacionales (también conocidas como aventuras de texto o ficción interactiva). Incluye un potente **editor visual**, un **motor de juego** robusto y la capacidad de **exportar tus creaciones como ejecutables independientes**.

Todo el contenido de tu aventura (salas, objetos, música, imágenes, diálogos, scripts) viaja empaquetado en un único archivo `.xaw` cifrado y comprimido, listo para distribuir.

---

## Características Principales

### Editor de Mapas Visual

El corazón de XiloAdventures es su editor de mapas intuitivo y potente:

| Característica | Descripción |
|----------------|-------------|
| **Mapa interactivo** | Visualiza y edita tu mundo en un canvas con zoom, pan y grid |
| **Drag & Drop** | Arrastra salas para reorganizar tu mapa |
| **Conexiones visuales** | Crea salidas entre salas arrastrando desde los puertos |
| **Crear puertas visual** | Arrastra entre puertos para crear puertas bidireccionales |
| **Selección múltiple** | Selecciona varias salas con Ctrl+Click o rectángulo de selección |
| **Snap to grid** | Alineación automática para mapas ordenados |
| **Iconos informativos** | Visualiza puertas, llaves y estados de un vistazo |
| **Tooltips con imagen** | Previsualiza las imágenes de sala al pasar el ratón |
| **Undo/Redo completo** | Deshaz cualquier cambio con historial ilimitado |
| **Búsqueda integrada** | Encuentra cualquier elemento de tu mundo |
| **Copiar/Pegar** | Duplica salas con todo su contenido |
| **Autoguardado** | Guardado automático periódico para evitar pérdidas |

### Editor de Scripts Visual (Nodos)

Crea lógica compleja para tu aventura **sin programar**, usando un sistema visual de nodos:

**Eventos (35+ tipos)**
- Eventos de juego: inicio, fin, cada minuto/hora
- Eventos de sala: entrar, salir
- Eventos de puerta: abrir, cerrar, bloquear, desbloquear, llamar
- Eventos de NPC: hablar, atacar, muerte, ver al jugador
- Eventos de objeto: coger, soltar, usar, examinar, abrir/cerrar contenedor
- Eventos de misión: iniciar, completar, fallar, completar objetivo
- Eventos de tiempo: cambio de turno, cambio de clima
- **Eventos de combate**: inicio, victoria, derrota, huida, ataque, daño
- **Eventos de comercio**: abrir tienda, cerrar tienda, comprar, vender

**Condiciones (20+ tipos)**
- Verificar inventario, sala actual, estado de misión
- Comprobar flags y contadores
- Verificar hora del día, estado de puertas, visibilidad de NPCs
- Probabilidad aleatoria
- **Condiciones de combate**: salud del jugador/NPC, turno, en combate
- **Condiciones de comercio**: en comercio, oro del jugador/NPC, tiene objeto

**Acciones (35+ tipos)**
- Mostrar mensajes, dar/quitar objetos, gestionar oro
- Teletransportar jugador, mover NPCs
- Abrir/cerrar/bloquear/desbloquear puertas
- Gestionar misiones, flags y contadores
- Reproducir sonidos, iniciar conversaciones
- **Acciones de combate**: curar, dañar, forzar victoria/derrota, huida
- **Acciones de comercio**: abrir/cerrar tienda, modificar precios, añadir/quitar items

**Control de flujo**
- Bifurcaciones condicionales (if-else)
- Secuencias ordenadas
- Retardos temporizados
- Bifurcaciones aleatorias

**Nodos matemáticos y lógicos**
- Operaciones: suma, resta, multiplicación, división, módulo
- Comparaciones: igual, distinto, mayor, menor
- Lógica: AND, OR, NOT, XOR
- Funciones: mínimo, máximo, clamp, aleatorio

### Sistema de Conversaciones

Crea diálogos ramificados y dinámicos con el editor de conversaciones visual:

- **Nodos de diálogo**: El NPC habla con expresiones emocionales (neutral, feliz, triste, enfadado, sorprendido)
- **Opciones del jugador**: Hasta 4 opciones de respuesta por nodo con condiciones
- **Bifurcaciones condicionales**: Diálogos que cambian según el estado del juego
- **Sistema de tiendas**: NPCs comerciantes con compra/venta integrada
- **Conversaciones enlazadas**: Salta entre diferentes conversaciones
- **Acciones en diálogo**: Ejecuta scripts durante la conversación
- **Variables locales**: Datos persistentes dentro de cada conversación
- **Editor visual de nodos**: Crea y edita conversaciones con drag & drop

### Sistema de Misiones

Guía a tus jugadores con un sistema de misiones completo:

- Definición de misiones con nombre, descripción y objetivos
- Estados: No iniciada, En progreso, Completada, Fallada
- Eventos para cada cambio de estado
- Requisitos de misión para acceder a salas o salidas
- Seguimiento de objetivos individuales

### Gestión de NPCs

Da vida a tu mundo con personajes no jugadores:

- Estadísticas de combate: nivel, fuerza, destreza, inteligencia, salud
- Sistema de inventario propio
- Configuración como comerciante con inventario de tienda
- Multiplicadores de precio para compra/venta
- Visibilidad condicional
- Etiquetas para organización y scripts
- **Patrulla automática**: Rutas predefinidas con movimiento ping-pong
- **Seguimiento del jugador**: Los NPCs pueden seguir al jugador entre salas

### Sistema de Objetos Avanzado

Objetos con propiedades ricas y realistas:

- **Tipos**: Arma, Armadura, Casco, Escudo, Comida, Bebida, Llave
- **Físicas**: Peso (gramos) y volumen (cm³)
- **Contenedores**: Objetos dentro de objetos con capacidad limitada
- **Cerraduras**: Contenedores bloqueables con llave
- **Textos legibles**: Documentos, libros, cartas con contenido
- **Género gramatical**: Artículos correctos en español (el/la)
- **Precios**: Para el sistema de comercio
- **Fuentes de luz**: Antorchas, velas con duración limitada
- **Sistema de fabricación**: Recetas para crear objetos combinando ingredientes

### Sistema de Iluminación

Salas oscuras y fuentes de luz:

- **Salas oscuras**: Salas no iluminadas requieren una fuente de luz para ver
- **Fuentes de luz**: Antorchas, velas, lámparas con duración configurable
- **Encender/Apagar**: Comandos para controlar las fuentes de luz
- **Objeto encendedor**: Algunas fuentes requieren un objeto específico para encenderse
- **Duración limitada**: Las fuentes de luz se agotan con el tiempo (turnos)

### Sistema de Equipamiento

Cuatro ranuras de equipamiento para el jugador y NPCs:

| Ranura | Tipos aceptados | Efecto |
|--------|-----------------|--------|
| **Mano derecha** | Arma, Escudo | Ataque principal o defensa |
| **Mano izquierda** | Arma (1 mano), Escudo | Arma secundaria o escudo |
| **Torso** | Armadura | Bonus de defensa |
| **Cabeza** | Casco | Bonus de defensa adicional |

- **Equipar desde sala**: Puedes equipar objetos directamente desde la sala actual
- **Equipar desde contenedores**: También desde contenedores abiertos en la sala
- **Armas de dos manos**: Ocupan ambas ranuras de mano

### Sistema de Combate D20

Sistema de combate por turnos inspirado en D&D:

- **Iniciativa**: Tirada D20 + Destreza para determinar quién ataca primero
- **Ataques físicos**: Tirada D20 + Fuerza vs Defensa enemiga
- **Ataques mágicos**: Tirada D20 + Inteligencia con habilidades especiales
- **Defensas mágicas**: Escudos y contraataques con coste de maná
- **Daño crítico**: 20 natural = daño doble
- **Fallos críticos**: 1 natural = fallo automático
- **Huida**: Posibilidad de escapar del combate
- **Uso de objetos**: Consumibles durante el combate
- **Eventos de scripting**: Scripts que se disparan en inicio, victoria, derrota
- **Botín**: Al derrotar un NPC, se convierte en cadáver saqueeable

### Sistema de Comercio

Compra y vende objetos con NPCs comerciantes:

- **Tiendas de NPC**: Cualquier NPC puede ser configurado como comerciante
- **Inventario de tienda**: Lista personalizable de objetos en venta
- **Multiplicadores de precio**: Compra al 50% y vende al 100% (configurable)
- **Oro del NPC**: Limitado o infinito según configuración
- **Oro del jugador**: Gestión automática del inventario monetario
- **Eventos de scripting**: Scripts al comprar, vender, abrir o cerrar tienda
- **Interfaz visual**: Ventana de comercio integrada en el jugador

### Sistema de Necesidades Básicas

Simula necesidades realistas del personaje:

- **Hambre**: Aumenta con el tiempo, se reduce comiendo
- **Sed**: Aumenta con el tiempo, se reduce bebiendo
- **Sueño**: Aumenta con el tiempo, se reduce durmiendo
- **Velocidad configurable**: Baja, Normal o Alta por necesidad
- **Muerte por necesidad**: El personaje muere si alguna necesidad llega a 100
- **Eventos de scripting**: Scripts al comer, beber, dormir o morir

### Sistema de Puertas y Llaves

Crea puzzles de exploración:

- Puertas físicas bidireccionales entre salas
- Estados: abierta/cerrada, bloqueada/desbloqueada
- Requisitos de llave específica
- Eventos para cada interacción (abrir, cerrar, bloquear, desbloquear, llamar)
- Requisitos de misión opcionales
- **Restricción de lado**: Configura si se puede abrir desde un lado, el otro o ambos
- **Género gramatical**: Mensajes correctos (la puerta, el portón)

### Sistema de Tiempo y Clima

Mundos dinámicos que evolucionan:

- **Hora del juego**: Sistema de 24 horas
- **Conversión de tiempo**: Configura cuántos minutos reales equivalen a una hora de juego
- **Fases del día**: Mañana, tarde, noche, madrugada
- **Clima**: Despejado, lluvioso, nublado, tormenta
- **Eventos temporales**: Scripts que se disparan cada minuto/hora o al cambiar el clima

### Audio Integrado

Ambienta tu aventura con audio inmersivo:

- **Música global**: Banda sonora del mundo
- **Música por sala**: Melodías específicas con transiciones suaves (fade)
- **Efectos de sonido**: Asociados a acciones y eventos
- **Volúmenes independientes**: Master, música, efectos, voz
- **Precarga inteligente**: El audio de salas adyacentes se prepara para transiciones fluidas
- **Talkover automático**: La música se reduce al 50% cuando hay narración de voz
- **Caché de voz**: El audio TTS generado se almacena para reproducción instantánea

---

## Ventana del Jugador

Una experiencia de juego completa y pulida:

| Elemento | Descripción |
|----------|-------------|
| **Historial de comandos** | Scroll con todo el texto de la partida |
| **Imagen de sala** | Visualización de la localización actual |
| **Panel de inventario** | Lista de objetos con iconos |
| **Panel de estadísticas** | Fuerza, Constitución, Inteligencia, Destreza, Carisma, Oro |
| **Registro de misiones** | Misiones activas con su estado |
| **Indicadores de tiempo** | Hora del día y clima actual |
| **Barras de necesidades** | Hambre, sed, sueño con indicadores visuales |
| **Entrada de comandos** | Con historial navegable (flechas arriba/abajo) |
| **Controles de audio** | Ajusta cada canal de sonido |
| **Mapa integrado** | Visualiza las salas visitadas y navega haciendo clic |
| **Brújula de navegación** | Botones direccionales para moverse con el ratón |
| **Ventana de tienda** | Interfaz visual para comprar y vender objetos |

### Colores de Conexiones en el Mapa

| Color | Significado |
|-------|-------------|
| **Verde** | Puerta abierta o salida libre |
| **Naranja** | Puerta cerrada (se puede abrir) |
| **Rojo** | Puerta cerrada con llave |

### Comandos del Jugador

El motor reconoce comandos en español e inglés:

- **Movimiento**: norte, sur, este, oeste, arriba, abajo (y abreviaturas)
- **Exploración**: examinar, mirar, look_in (contenedores)
- **Inventario**: inventario, coger, soltar, coger todo
- **Interacción**: usar, usar con, abrir, cerrar, leer, dar a
- **Equipamiento**: equipar, desequipar (armas, armaduras, cascos, escudos)
- **Combate**: atacar, defender, huir
- **Consumibles**: comer, beber
- **Iluminación**: encender, apagar
- **NPCs**: hablar, decir, comprar, vender
- **Puertas**: bloquear, desbloquear
- **Sistema**: guardar, cargar, ayuda, misiones

---

## Inteligencia Artificial (Opcional)

Potencia tu creatividad con IA local (requiere Docker):

### Generador de Mundos Completos

Crea aventuras enteras con un solo clic:

- **Generación parametrizable**: Configura el tipo de mundo, ambientación, número de salas, objetos y NPCs
- **Mundos coherentes**: La IA genera salas conectadas con lógica, objetos relevantes y personajes con personalidad
- **Personalización del tema**: Define el género (fantasía, ciencia ficción, terror, medieval, etc.)
- **Estructura automática**: Genera automáticamente misiones, puertas con llaves y puzzles
- **Base para expandir**: Usa el mundo generado como punto de partida y personalízalo a tu gusto

### LLM (Modelo de Lenguaje)

- **Interpretación de comandos**: Cuando el parser no entiende, la IA intenta descifrar la intención
- **Generación de contenido**: Crea descripciones para salas, objetos y NPCs
- **Determinación gramatical**: Asigna género automáticamente a los objetos
- **Generación temática**: Usa el tema/ambientación del mundo para coherencia

### TTS (Texto a Voz)

- **Narración de salas**: Las descripciones se convierten en audio
- **Precarga inteligente**: Prepara la voz de salas adyacentes
- **Control de volumen**: Canal independiente

### Generación de Imágenes

- **Imágenes de sala**: Genera ilustraciones con Stable Diffusion
- **Basado en prompts**: Describe lo que quieres ver
- **Coherencia temática**: Respeta la ambientación del mundo

### Ventana de Generación IA por Lotes

- Procesamiento masivo de géneros, descripciones e imágenes para mundos existentes
- Verificación de disponibilidad de Docker
- Monitorización de progreso en tiempo real

---

## Herramientas del Editor

### Modo de Prueba Integrado

Prueba tu aventura sin salir del editor:

- **Play desde el editor**: Botón para iniciar el juego instantáneamente
- **Debug integrado**: Visualiza el estado del juego en tiempo real
- **Prueba de scripts**: Ejecuta nodos individuales para verificar su funcionamiento
- **Selección de sala inicial**: Comienza la prueba desde cualquier sala
- **Configuración de IA/Audio**: Activa o desactiva según necesites

### Editor de Diccionario del Parser

Personaliza cómo el juego interpreta los comandos:

- **Aliases de verbos**: Define sinónimos para comandos (ej: "coger" = "tomar", "agarrar")
- **Aliases de sustantivos**: Sinónimos para objetos del mundo
- **Específico por mundo**: Cada aventura puede tener su propio diccionario
- **Preposiciones personalizables**: Adapta las preposiciones reconocidas

### Gestores de Audio

| Herramienta | Funcionalidad |
|-------------|---------------|
| **Gestor de Música** | Administra la biblioteca de música del mundo |
| **Gestor de Efectos** | Administra los efectos de sonido |
| **Vista previa** | Reproduce audio antes de asignarlo |
| **Asignación rápida** | Asocia audio a salas y eventos |

---

## Exportación y Distribución

### Exportar como EXE

Convierte tu aventura en un ejecutable independiente:

- **Sin dependencias**: Incluye el runtime de .NET 8 embebido
- **Mundo empaquetado**: El archivo `.xaw` va como recurso interno
- **Icono personalizado**: Al exportar, puedes elegir un archivo `.ico` para personalizar el icono del ejecutable
- **Configuración opcional**: Archivo `config.xac` junto al EXE para ajustes de IA

### Player Independiente

Distribuye juegos sin el editor:

- Ejecutable standalone (`XiloAdventures.Wpf.Player`)
- Mundo pre-embebido desde el código fuente
- Experiencia de juego completa

### Instalador MSI (XiloAdventures.Wpf.Installer)

Genera un instalador profesional de Windows (.msi) con WiX Toolset v4:

#### Características del instalador

| Característica | Descripción |
|----------------|-------------|
| **Selección de ruta** | El usuario puede elegir dónde instalar la aplicación |
| **Acceso directo escritorio** | Opcional, seleccionable durante la instalación |
| **Acceso directo menú inicio** | Incluye acceso al juego y opción de desinstalar |
| **Icono personalizado** | Usa el icono de la aplicación (`appicon.ico`) |
| **Idioma español** | Interfaz del instalador en español |
| **Mensaje de finalización** | "¡Gracias aventurero!" con enlace a la wiki |

#### Opciones de desinstalación

Al desinstalar, el usuario puede elegir eliminar:

1. **Partidas guardadas**: Elimina la carpeta `Documentos\xiloadventures\` con todas las partidas
2. **Datos de Docker (IA)**: Limpieza completa de Docker incluyendo:
   - Contenedores de Xilo Adventures (xilo-ollama, xilo-tts, xilo-stablediffusion)
   - Imágenes de Docker descargadas
   - Distros WSL de Docker (docker-desktop, docker-desktop-data)
   - Docker Desktop (desinstalación completa)
   - Carpetas de Docker en LOCALAPPDATA, APPDATA y PROGRAMDATA

#### Compilar el instalador

```bash
# 1. Publicar la aplicación (si no está publicada)
dotnet publish XiloAdventures.Wpf -c Release -r win-x64 --self-contained -o XiloAdventures.Wpf/bin/publish

# 2. Compilar el instalador (genera automáticamente SourceToExportEXE.zip)
dotnet build XiloAdventures.Wpf.Installer -c Release
```

El archivo MSI resultante se genera en:
```
XiloAdventures.Wpf.Installer\bin\Release\es-ES\XiloAdventures.Setup.msi
```

#### Exportación a .exe desde la app instalada

El instalador incluye `SourceToExportEXE.zip` que contiene el código fuente necesario (Engine, Wpf.Common y Wpf.Player) para que el usuario final pueda exportar mundos como ejecutables standalone desde el editor. Este ZIP se genera automáticamente durante la compilación del instalador.

**Requisitos para exportar**: El usuario debe tener instalado el .NET 8 SDK en su equipo.

---

## Formato de Archivos

| Extensión | Descripción |
|-----------|-------------|
| `.xaw` | Mundo de aventura (JSON comprimido + cifrado AES-CBC) |
| `.xas` | Partida guardada (estado del juego cifrado) |
| `.xac` | Configuración de la aplicación |
| `.msi` | Instalador de Windows generado por WiX |

**Seguridad**: Clave de 8 caracteres para cifrado AES-CBC. Clave vacía = sin cifrar.

---

## Estructura del Proyecto

| Proyecto | Descripción |
|----------|-------------|
| `XiloAdventures.Engine` | Motor del juego: modelos, parser, guardado/carga, audio |
| `XiloAdventures.Wpf` | Editor visual completo |
| `XiloAdventures.Wpf.Common` | Componentes UI compartidos |
| `XiloAdventures.Wpf.Player` | Player standalone para distribución |
| `XiloAdventures.Wpf.Installer` | Proyecto WiX para generar el instalador MSI |
| `XiloAdventures.Wpf.Installer.CustomActions` | Acciones personalizadas de desinstalación |
| `XiloAdventures.Tests` | Tests unitarios y de integración (xUnit) |

### Arquitectura del Motor (XiloAdventures.Engine)

```
XiloAdventures.Engine/
├── Engine/                    # Componentes principales del motor
│   ├── GameEngine.cs          # Motor principal del juego
│   ├── CombatEngine.cs        # Sistema de combate D20
│   ├── TradeEngine.cs         # Sistema de comercio
│   ├── ScriptEngine.cs        # Motor de scripts visuales
│   ├── ConversationEngine.cs  # Sistema de diálogos
│   ├── Parser.cs              # Análisis de comandos del jugador
│   ├── DoorService.cs         # Gestión de puertas y llaves
│   ├── SoundManager.cs        # Gestión de audio
│   ├── SaveManager.cs         # Guardado/carga de partidas
│   ├── WorldLoader.cs         # Carga de mundos (.xaw)
│   └── CryptoUtil.cs          # Cifrado AES-CBC
│
├── Interfaces/                # Contratos para componentes principales
│   ├── ICombatEngine.cs       # Interface del sistema de combate
│   ├── ITradeEngine.cs        # Interface del sistema de comercio
│   └── ISoundManager.cs       # Interface del gestor de audio
│
└── Models/                    # Modelos de datos del juego
    ├── Enumerations.cs        # Enums centralizados
    ├── WorldModel.cs          # Contenedor principal del mundo
    ├── GameInfo.cs            # Configuración del juego
    ├── Room.cs                # Salas y salidas
    ├── GameObject.cs          # Objetos interactivos
    ├── Npc.cs                 # NPCs y estadísticas de combate
    ├── Player.cs              # Estadísticas y estado del jugador
    ├── GameState.cs           # Estado de la partida
    ├── Quest.cs               # Definiciones de misiones
    ├── Door.cs                # Puertas y cerraduras
    ├── CombatState.cs         # Estado del combate activo
    ├── CombatAbility.cs       # Habilidades de combate
    ├── DiceRolls.cs           # Resultados de tiradas D20
    ├── TradeModels.cs         # Modelos de comercio
    ├── ConversationModels.cs  # Modelos de diálogos
    ├── ScriptModels.cs        # Modelos de scripts visuales
    ├── NodeTypeRegistry.cs    # Registro de tipos de nodos
    └── Assets.cs              # Recursos multimedia
```

---

## Tests

El proyecto incluye **838 tests** que verifican el correcto funcionamiento de todos los componentes:

### Tests Unitarios

| Componente | Tests | Cobertura |
|------------|-------|-----------|
| GameEngine | 36 | Comandos, movimiento, inventario, contenedores |
| Parser | 41 | Análisis de comandos, aliases, preposiciones, pronombres |
| DoorService | 12 | Puertas, llaves, estados, restricciones de lado |
| NodeTypeRegistry | 27 | Tipos de nodos del editor de scripts |
| ScriptValidator | 10 | Validación de scripts |
| CombatEngine | 80+ | Sistema de combate D20, ataques, defensa, huida, magia |
| TradeEngine | 40+ | Compra/venta, precios, inventarios, oro |
| ScriptEngine (Trade) | 25+ | Nodos de scripting para comercio |
| ScriptEngine (Combat) | 30+ | Nodos de scripting para combate |
| BasicNeeds | 45+ | Sistema de hambre, sed, sueño, muerte |
| WorldLoader | 3 | Carga/guardado de mundos |
| SaveManager | 2 | Guardado/carga de partidas |
| CryptoUtil | 2 | Cifrado/descifrado |
| UiSettingsManager | 2 | Configuración de UI |
| SoundManager | 3 | Gestión de audio |
| AppPaths | 2 | Rutas de la aplicación |
| WorldEditorHelpers | 2 | Utilidades del editor |

### Tests de Integración

| Escenario | Tests |
|-----------|-------|
| Flujo de juego completo | 36 |
| Integración de comercio | 15+ |
| Integración de combate | 20+ |

```bash
# Ejecutar todos los tests
dotnet test

# Con detalle
dotnet test --verbosity normal
```

---

## Requisitos

- **Sistema**: Windows 10/11 con soporte WPF
- **Framework**: .NET 8 SDK
- **Opcional**: Docker Desktop (para funciones de IA)

---

## Inicio Rápido

```bash
# Clonar el repositorio
git clone https://github.com/xilonoide/XiloAdventures.git

# Compilar
dotnet build XiloAdventures.sln

# Ejecutar el editor
dotnet run --project XiloAdventures.Wpf

# Ejecutar los tests
dotnet test
```

### Primeros Pasos

1. Abre la aplicación y crea un nuevo mundo (o genera uno con IA)
2. Usa el editor de mapas para crear salas y conectarlas
3. Añade objetos, NPCs y misiones desde el árbol de contenido
4. Crea scripts visuales para dar vida a tu mundo
5. Pulsa **Play** para probar tu aventura
6. Exporta como EXE para distribuir

---

## Documentación

Consulta la [Wiki](https://github.com/xilonoide/XiloAdventures/wiki) para guías detalladas:

- [Home](https://github.com/xilonoide/XiloAdventures/wiki) - Visión general del proyecto
- [Editor](https://github.com/xilonoide/XiloAdventures/wiki/editor) - Guía completa del editor
- [Player](https://github.com/xilonoide/XiloAdventures/wiki/player) - Manual del jugador
- [Scripting](https://github.com/xilonoide/XiloAdventures/wiki/scripting) - Manual del editor de scripts

---

## Licencia

Consulta el archivo [LICENSE](LICENSE) para términos y condiciones.

---

<p align="center">
  <a href="https://www.paypal.me/xmasmusicsoft">
    <img src="https://img.shields.io/badge/❤️_¡Se_aceptan_donaciones!-PayPal-00457C?style=for-the-badge&logo=paypal&logoColor=white" alt="Donar">
  </a>
</p>

<p align="center">
  <sub>Hecho con ❤️ para la comunidad de aventuras conversacionales</sub>
</p>
