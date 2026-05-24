# 🤖 SuperTrend Bot v3.0 - User Guide

Welcome to the **SuperTrend Auto-Trading Bot v3.0** for cTrader! This guide will help you install, configure, and control your bot via Telegram.

## 📦 1. Installation Instructions

1. **Locate the Bot File**: You have received a `.algo` file. This is the compiled robot.
2. **Install**: Double-click the `.algo` file. cTrader will open automatically and ask for confirmation to install the bot. Click **Yes**.
3. **Find the Bot**: Open cTrader, go to the left panel, and click on the **Automate** tab. Look for `SuperTrendBot` under the **cBots** section.
4. **Attach to Chart**: Double-click the `SuperTrendBot` name, or right-click it and select "Add Instance". A new parameters window will appear.

---

## ⚙️ 2. Configuration & Parameters

When you attach the bot, you will see a list of parameters divided into groups:

*   **1. SuperTrend**: Customize the core indicator settings (`ATR Period` and `Multiplier`).
*   **2. EMA Filter**: (Optional) Enable the 50/200 EMA trend filter to ensure trades are only taken in the direction of the macro trend.
*   **3. EMA Slope**: (Optional) Require the EMA lines to have a specific steepness (momentum) before entering a trade.
*   **4. Trading**: 
    *   Set your **Trade Mode**: `1 = Auto` (takes trades automatically) or `2 = Manual` (only sends alerts).
    *   Configure your **Volume**, **Stop Loss**, and **Take Profit**.
    *   Enable/Disable **Trailing Stop Loss**.
*   **5. Telegram**: (CRITICAL FOR REMOTE CONTROL)
    *   **Enable Telegram**: Set to `Yes`.
    *   **Bot Token**: Paste your Telegram Bot token here (from `@BotFather`).
    *   **Chat ID**: Paste your personal Chat ID here (from `@userinfobot`).
*   **6 & 7. Alerts / Display**: Customize chart visuals and sound alerts.

**Starting the Bot:** Once parameters are set, click **Apply**, and then click the **Start (Play ▶️) button** next to the bot instance.

---

## 📱 3. Telegram Remote Control

Once the bot is started, it will send a `"✅ Bot connected!"` message to your Telegram. You can control the bot directly from your phone by sending the following commands:

| Command | Action | Description |
| :--- | :--- | :--- |
| `/start` | **Resume / Menu** | Wakes up the bot if it was paused and shows the command menu. |
| `/auto` | **Enable Auto-Trading** | The bot will automatically open and close positions based on signals. |
| `/manual` | **Enable Alerts Only** | The bot will ONLY send you signal alerts on Telegram. It will NOT take trades. |
| `/status` | **View Status** | Sends a live report: Open positions, P/L, Symbol, Price, and Trial time remaining. |
| `/stop` | **Pause Bot** | Pauses all trading and alerts. The bot goes to sleep until you send `/start`. |

---

## ⌨️ 4. Chart Hotkeys

If you are at your computer, you can instantly switch between **AUTO** and **MANUAL** mode by pressing the **`M`** key on your keyboard while viewing the chart.

---

## ⏳ 5. Trial Version Info

This bot includes a built-in trial licensing system.
*   The trial begins the **very first time** you click Start (Play ▶️).
*   The trial lasts for **3 Days** (72 hours).
*   You can check how much time is left on the bottom right corner of your chart, or by sending the `/status` command on Telegram.
*   Once the trial expires, the bot will automatically lock itself and stop functioning. You will need to contact the developer for the full unrestricted version.

---
*Happy Trading!*
