# PandaGame 🐼

Un juego educativo e interactivo donde controlas a un panda escalador usando tu capacidad pulmonar. ¡Sopla en el inspirómetro y ayuda al panda a conquistar la montaña!

![Gameplay](./PandaGame_gameplay.gif)

## 📋 Descripción

PandaGame es una experiencia educativa y divertida diseñada para todo público. El jugador utiliza un inspirómetro casero para controlar a un simpático panda que debe escalar una montaña. Cuanto más fuerte y constante sea tu soplido, más rápido escalará el panda. ¡Tienes 10 segundos para llegar a la cima!

### Características principales:
- 🎮 Control mediante respiración usando inspirómetro
- ⏱️ Desafío contra el tiempo (10 segundos)
- 🏔️ Diseño simple y accesible
- 🎉 Animación de celebración al completar el nivel
- 🔄 Reinicio automático para jugar continuamente
- 🌍 Gráficos Low Poly amigables y coloridos

## 🎯 Objetivo del Juego

Ayuda al panda a escalar la montaña en menos de 10 segundos soplando en el inspirómetro. Si logras llegar a la cima, el panda celebrará tu victoria. Si no lo logras a tiempo, el juego se reiniciará automáticamente para que lo intentes de nuevo.

## 💻 Requisitos del Sistema

### Requisitos Mínimos:
- **Sistema Operativo:** Windows 10 o superior (64-bit)
- **Procesador:** Intel Core i3 o equivalente
- **Memoria RAM:** 4 GB
- **Gráficos:** DirectX 11 compatible
- **Almacenamiento:** 500 MB de espacio disponible
- **Puerto USB:** Necesario para conectar el inspirómetro

### Hardware Adicional Requerido:
- **Inspirómetro casero** conectado al puerto COM4
- **Drivers:** CP210x USB to UART Bridge VCP Drivers

## 🔧 Instalación

### 1. Instalar Drivers del Inspirómetro

Antes de jugar, debes instalar los drivers necesarios para la comunicación con el inspirómetro:

1. Descarga los **CP210x USB to UART Bridge VCP Drivers** desde:
   - [Silicon Labs oficial](https://www.silabs.com/developers/usb-to-uart-bridge-vcp-drivers)
2. Ejecuta el instalador y sigue las instrucciones
3. Reinicia tu computadora después de la instalación

### 2. Instalar el Juego

1. Descarga el archivo `PandaGame.zip`
2. Extrae todos los archivos en una carpeta de tu preferencia
3. Busca el archivo `PandaGame.exe`
4. (Opcional) Crea un acceso directo en el escritorio

### 3. Conectar el Inspirómetro

1. Conecta el inspirómetro casero a un puerto USB de tu computadora
2. Verifica que Windows reconozca el dispositivo
3. Confirma que esté asignado al puerto **COM4**

## 🎮 Cómo Jugar

1. **Inicia el juego** haciendo doble clic en `PandaGame.exe`
2. **Asegúrate** de que el inspirómetro esté conectado antes de iniciar
3. **Sopla** en el inspirómetro para hacer que el panda escale
4. **Llega a la cima** antes de que pasen 10 segundos
5. Si llegas a tiempo, ¡el panda celebrará! 🎉
6. El juego se reinicia automáticamente para otra ronda

### Consejos:
- Mantén un soplido constante para un ascenso uniforme
- No es necesario soplar con máxima fuerza todo el tiempo
- Respira normalmente entre intentos

## 🛠️ Configuración del Inspirómetro

### Especificaciones Técnicas:
- **Puerto:** COM4
- **Velocidad (Baudios):** 115200
- **Formato de datos:** Texto plano
- **Rango de volumen:** 0 - 5000 ml
- **Protocolo:** Serial (UART)

## 📦 Créditos y Assets

Este juego fue desarrollado con:
- **Motor:** Unity 2022.3.62f1
- **Assets:**
  - StarterAssets - Third Person Controller (Unity Technologies)
  - Low Poly Environment Pack
- **Lenguaje:** C#

## 📄 Licencia

## 👥 Contacto y Soporte

Para reportar problemas o sugerencias:
- **Email:** [linnrodriguez25@gmail.com]
- **GitHub:** [https://github.com/liinarodriguez/PandaGame]

## 🔄 Historial de Versiones

### v1.0.0 (Fecha actual)
- Lanzamiento inicial
- Mecánica de escalada con inspirómetro
- Sistema de tiempo límite de 10 segundos
- Animación de celebración
- Reinicio automático

---

¡Disfruta escalando con tu panda! 🐼🏔️
