# SuperTrend cTrader Bot

This is an automated trading bot for cTrader based on a customizable SuperTrend strategy. It comes bundled with built-in EMA filtering and advanced dynamic lot-sizing features.

## Features
- **SuperTrend Strategy:** Automated buying and selling upon trend flip.
- **EMA Filter Check:** Validates trades via Fast (50) and Slow (200) EMA indicators.
- **Dynamic Lot Sizing:** Computes the proper lot size automatically corresponding to a strict dollar risk parameter.
- **Telegram Integration:** Monitor your bot, check status, and receive entry/exit signals instantly.
- **Built-in Trial:** Pre-coded 3-day trial limit logic.
- **Optimized UI:** High-performance line/dot drawing on cTrader that completely prevents PC lag or freezing ("rayas locas").

## Installation
1. Compile `SuperTrendBot.cs` directly inside cTrader Automate.
2. Ensure you have the corresponding `SuperTrendExactAlertsV3.cs` indicator available.
3. Apply to chart and configure your strategy parameters.
