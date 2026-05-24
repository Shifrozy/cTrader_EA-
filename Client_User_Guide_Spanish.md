# 🤖 SuperTrend Bot v3.0 - Guía del Usuario

¡Bienvenido al **SuperTrend Auto-Trading Bot v3.0** para cTrader! Esta guía le ayudará a instalar, configurar y controlar su bot a través de Telegram.

## 📦 1. Instrucciones de Instalación

1. **Ubique el archivo del Bot**: Ha recibido un archivo `.algo`. Este es el robot compilado.
2. **Instalar**: Haga doble clic en el archivo `.algo`. cTrader se abrirá automáticamente y le pedirá confirmación para instalar el bot. Haga clic en **Sí** (Yes).
3. **Encuentre el Bot**: Abra cTrader, vaya al panel izquierdo y haga clic en la pestaña **Automate** (Automatizar). Busque `SuperTrendBot` en la sección **cBots**.
4. **Añadir al Gráfico**: Haga doble clic en el nombre `SuperTrendBot` o haga clic derecho y seleccione "Add Instance" (Añadir Instancia). Aparecerá una nueva ventana de parámetros.

---

## ⚙️ 2. Configuración y Parámetros

Al añadir el bot, verá una lista de parámetros divididos en grupos:

*   **1. SuperTrend**: Personalice los ajustes del indicador principal (`ATR Period` y `Multiplier`).
*   **2. EMA Filter**: (Opcional) Active el filtro de tendencia EMA 50/200 para asegurar que las operaciones se realicen solo a favor de la tendencia principal.
*   **3. EMA Slope**: (Opcional) Requiere que las líneas EMA tengan una inclinación específica (momentum) antes de abrir una operación.
*   **4. Trading**: 
    *   Establezca su **Trade Mode** (Modo de Operación): `1 = Auto` (opera automáticamente) o `2 = Manual` (solo envía alertas).
    *   Configure su **Volume** (Lotes), **Stop Loss** y **Take Profit**.
    *   Active/Desactive el **Trailing Stop Loss**.
*   **5. Telegram**: (CRÍTICO PARA EL CONTROL REMOTO)
    *   **Enable Telegram**: Configúrelo en `Yes` (Sí).
    *   **Bot Token**: Pegue su token del Bot de Telegram aquí (obtenido de `@BotFather`).
    *   **Chat ID**: Pegue su Chat ID personal aquí (obtenido de `@userinfobot`).
*   **6 & 7. Alerts / Display** (Alertas / Pantalla): Personalice los gráficos y las alertas de sonido.

**Iniciar el Bot:** Una vez configurados los parámetros, haga clic en **Apply** (Aplicar) y luego haga clic en el **botón de Inicio (Play ▶️)** junto a la instancia del bot.

---

## 📱 3. Control Remoto por Telegram

Una vez iniciado, el bot enviará un mensaje de `"✅ Bot connected!"` a su Telegram. Puede controlar el bot directamente desde su teléfono enviando los siguientes comandos:

| Comando | Acción | Descripción |
| :--- | :--- | :--- |
| `/start` | **Reanudar / Menú** | Despierta al bot si estaba pausado y muestra el menú de comandos. |
| `/auto` | **Activar Auto-Trading** | El bot abrirá y cerrará posiciones automáticamente basándose en las señales. |
| `/manual` | **Solo Alertas** | El bot SOLO le enviará alertas de señales en Telegram. NO realizará operaciones. |
| `/status` | **Ver Estado** | Envía un informe en vivo: Posiciones abiertas, Ganancias/Pérdidas, Símbolo, Precio y Tiempo de prueba restante. |
| `/stop` | **Pausar Bot** | Pausa todas las operaciones y alertas. El bot se pone en espera hasta que envíe `/start`. |

---

## ⌨️ 4. Teclas de Acceso Rápido en el Gráfico

Si está en su computadora, puede cambiar instantáneamente entre los modos **AUTO** y **MANUAL** presionando la tecla **`M`** en su teclado mientras ve el gráfico.

---

## ⏳ 5. Información de la Versión de Prueba

Este bot incluye un sistema de licencia de prueba incorporado.
*   La prueba comienza **la primera vez** que hace clic en Iniciar (Play ▶️).
*   La prueba tiene una duración de **3 Días** (72 horas).
*   Puede verificar cuánto tiempo queda en la esquina inferior derecha de su gráfico, o enviando el comando `/status` en Telegram.
*   Una vez que expire la prueba, el bot se bloqueará automáticamente y dejará de funcionar. Deberá comunicarse con el desarrollador para obtener la versión completa sin restricciones.

---
*¡Feliz Trading!*
