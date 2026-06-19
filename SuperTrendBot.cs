// SuperTrend Bot v3.1 - With Telegram + Trial System + Hybrid BE/ST
// cTrader cBot | SuperTrend + EMA Filter + Telegram Control + 3-Day Trial
// Changelog v3.1:
//   - Fixed BreakEven/TrailingSL coordination (SL no longer jumps ahead of SuperTrend)
//   - Added MaxLots safety cap to prevent unexpectedly large positions
//   - Improved Telegram reliability (retry, error logging, HTML escaping)
//   - Optimized chart drawing performance (MaxChartBars parameter)
//   - NEW: Hybrid BreakEven + SuperTrend trailing mode

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class SuperTrendBot : Robot
    {
        // --- SUPERTREND ---
        [Parameter("ATR Period", Group = "1. SuperTrend", DefaultValue = 14, MinValue = 1)]
        public int AtrPeriod { get; set; }
        [Parameter("Multiplier", Group = "1. SuperTrend", DefaultValue = 3.0, MinValue = 0.1, Step = 0.1)]
        public double Multiplier { get; set; }

        // --- EMA FILTER ---
        [Parameter("Enable EMA Filter", Group = "2. EMA Filter", DefaultValue = true)]
        public bool EnableEmaFilter { get; set; }
        [Parameter("Use EMA 200 (Slow)", Group = "2. EMA Filter", DefaultValue = true)]
        public bool UseEma200 { get; set; }
        [Parameter("Fast EMA Period", Group = "2. EMA Filter", DefaultValue = 50, MinValue = 1)]
        public int FastEmaPeriod { get; set; }
        [Parameter("Slow EMA Period", Group = "2. EMA Filter", DefaultValue = 200, MinValue = 1)]
        public int SlowEmaPeriod { get; set; }

        // --- EMA SLOPE ---
        [Parameter("Enable Slope Filter", Group = "3. EMA Slope", DefaultValue = false)]
        public bool EnableSlopeFilter { get; set; }
        [Parameter("Slope Lookback", Group = "3. EMA Slope", DefaultValue = 5, MinValue = 1)]
        public int SlopeLookback { get; set; }
        [Parameter("Min Slope (pips)", Group = "3. EMA Slope", DefaultValue = 0.5, MinValue = 0.0, Step = 0.1)]
        public double MinSlopeAngle { get; set; }

        // --- ATR RANGE FILTER ---
        [Parameter("Enable ATR Range Filter", Group = "3b. Range Filter", DefaultValue = true)]
        public bool EnableAtrRangeFilter { get; set; }
        [Parameter("ATR Range Lookback", Group = "3b. Range Filter", DefaultValue = 20, MinValue = 5)]
        public int AtrRangeLookback { get; set; }
        [Parameter("Min ATR Multiplier", Group = "3b. Range Filter", DefaultValue = 0.7, MinValue = 0.1, Step = 0.1)]
        public double MinAtrMultiplier { get; set; }

        // --- TRADING ---
        [Parameter("Trade Mode (1=Auto,2=Manual)", Group = "4. Trading", DefaultValue = 1)]
        public int TradeModeInput { get; set; }
        
        [Parameter("Use Fixed Lots Instead of Risk", Group = "4. Trading", DefaultValue = false)]
        public bool UseFixedLots { get; set; }
        [Parameter("Volume (Lots)", Group = "4. Trading", DefaultValue = 0.01, MinValue = 0.01, Step = 0.01)]
        public double VolumeLots { get; set; }
        [Parameter("Risk Amount ($)", Group = "4. Trading", DefaultValue = 10, MinValue = 1)]
        public double RiskAmount { get; set; }
        [Parameter("Max Lots", Group = "4. Trading", DefaultValue = 1.0, MinValue = 0.01, Step = 0.01)]
        public double MaxLots { get; set; }
        
        [Parameter("Stop Loss (Pips, 0=off)", Group = "4. Trading", DefaultValue = 0, MinValue = 0)]
        public double FixedSL { get; set; }
        [Parameter("Take Profit (Pips, 0=off)", Group = "4. Trading", DefaultValue = 0, MinValue = 0)]
        public double FixedTP { get; set; }
        [Parameter("Use Risk:Reward TP", Group = "4. Trading", DefaultValue = true)]
        public bool UseRRTP { get; set; }
        [Parameter("Risk:Reward Ratio", Group = "4. Trading", DefaultValue = 1.0, MinValue = 0.5, Step = 0.5)]
        public double RiskRewardRatio { get; set; }
        
        // --- BREAK-EVEN ---
        [Parameter("Enable Break-Even", Group = "4b. Break Even", DefaultValue = true)]
        public bool EnableBreakEven { get; set; }
        [Parameter("Trigger Break-Even (Pips)", Group = "4b. Break Even", DefaultValue = 10.0, MinValue = 1.0)]
        public double BreakEvenTriggerPips { get; set; }
        [Parameter("Lock Profit (Pips)", Group = "4b. Break Even", DefaultValue = 2.0, MinValue = 0.0)]
        public double BreakEvenLockPips { get; set; }

        // --- HYBRID BREAK-EVEN + SUPERTREND ---
        [Parameter("Enable Hybrid BE+ST", Group = "4c. Hybrid BE+ST", DefaultValue = false)]
        public bool EnableHybridBEST { get; set; }
        [Parameter("Hybrid Trigger (R multiple)", Group = "4c. Hybrid BE+ST", DefaultValue = 0.5, MinValue = 0.1, Step = 0.1)]
        public double HybridTriggerR { get; set; }
        [Parameter("Hybrid Lock (R multiple)", Group = "4c. Hybrid BE+ST", DefaultValue = 0.4, MinValue = 0.0, Step = 0.1)]
        public double HybridLockR { get; set; }

        [Parameter("Trailing SL (SuperTrend)", Group = "4. Trading", DefaultValue = true)]
        public bool UseTrailingSL { get; set; }
        [Parameter("Trailing SL Buffer (Pips)", Group = "4. Trading", DefaultValue = 5.0, MinValue = 0.0)]
        public double SLBufferPips { get; set; }
        [Parameter("Manage Manual Trades", Group = "4. Trading", DefaultValue = true)]
        public bool ManageManualTrades { get; set; }
        [Parameter("Max Positions", Group = "4. Trading", DefaultValue = 1, MinValue = 1)]
        public int MaxPositions { get; set; }
        [Parameter("Bot Label", Group = "4. Trading", DefaultValue = "STBot")]
        public string BotLabel { get; set; }
 
        // --- TELEGRAM ---
        [Parameter("Enable Telegram", Group = "5. Telegram", DefaultValue = true)]
        public bool EnableTelegram { get; set; }
        [Parameter("Bot Token", Group = "5. Telegram", DefaultValue = "")]
        public string TelegramToken { get; set; }
        [Parameter("Chat ID", Group = "5. Telegram", DefaultValue = "")]
        public string TelegramChatId { get; set; }
        [Parameter("Telegram Control (commands)", Group = "5. Telegram", DefaultValue = true)]
        public bool EnableTelegramControl { get; set; }

        // --- ALERTS ---
        [Parameter("Enable Sound", Group = "6. Alerts", DefaultValue = true)]
        public bool EnableSound { get; set; }
        [Parameter("Sound File", Group = "6. Alerts", DefaultValue = "Alert2.wav")]
        public string SoundFile { get; set; }
        [Parameter("Show on Chart", Group = "6. Alerts", DefaultValue = true)]
        public bool EnableVisual { get; set; }
        [Parameter("Enable Touch Alert", Group = "6. Alerts", DefaultValue = true)]
        public bool EnableTouchAlert { get; set; }

        // --- DISPLAY ---
        [Parameter("Show SuperTrend", Group = "7. Display", DefaultValue = true)]
        public bool ShowST { get; set; }
        [Parameter("Show EMA Lines", Group = "7. Display", DefaultValue = true)]
        public bool ShowEma { get; set; }
        [Parameter("Max Chart Bars", Group = "7. Display", DefaultValue = 500, MinValue = 50)]
        public int MaxChartBars { get; set; }

        // Trial duration hardcoded (not visible to user)
        private const int TrialDays = 3;

        // PRIVATE
        private AverageTrueRange _atr;
        private ExponentialMovingAverage _emaF, _emaS;
        private IndicatorDataSeries _bU, _bL, _fU, _fL, _st, _td;
        private bool _isAuto;
        private long _lastUpdateId;
        private string _trialFile;
        private DateTime _trialExpiry;
        private HttpClient _httpClient;
        private DateTime _lastTelegramCheck = DateTime.MinValue;
        private bool _isPaused = false;
        private bool _touchAlertFired = false;

        // --- Hybrid BE+ST state tracking ---
        // Tracks the initial SL distance (in price) for each position, keyed by position ID
        private Dictionary<long, double> _positionInitialRisk = new Dictionary<long, double>();
        // Tracks which positions have had BreakEven activated
        private HashSet<long> _breakEvenActivated = new HashSet<long>();
        // Hybrid state: 0=not triggered, 1=locked at R-level, 2=handed off to SuperTrend
        private Dictionary<long, int> _hybridState = new Dictionary<long, int>();

        protected override void OnStart()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _lastUpdateId = 0;

            // --- IMMEDIATE TELEGRAM TEST ---
            Print("=== BOT STARTING (v3.1) ===");
            Print("TG Check: Enable={0}, TokenLength={1}, ChatIDLength={2}", 
                EnableTelegram, TelegramToken?.Length ?? 0, TelegramChatId?.Length ?? 0);

            if (EnableTelegram && !string.IsNullOrEmpty(TelegramToken) && !string.IsNullOrEmpty(TelegramChatId))
            {
                Print("TG: Token={0}..., ChatID={1}", TelegramToken.Substring(0, Math.Min(10, TelegramToken.Length)), TelegramChatId);
                try
                {
                    string testUrl = string.Format("https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}",
                        TelegramToken, TelegramChatId, Uri.EscapeDataString("✅ Bot connected! Starting..."));
                    Print("TG: Sending test...");
                    string response = _httpClient.GetStringAsync(testUrl).Result;
                    Print("TG: SUCCESS! Response: " + response.Substring(0, Math.Min(100, response.Length)));
                }
                catch (Exception ex)
                {
                    Print("TG: FAILED! Error: " + ex.Message);
                    if (ex.InnerException != null) Print("TG: Inner: " + ex.InnerException.Message);
                }
            }
            else
            {
                Print("TG: Not sending test. Condition failed (Enable={0}, TokenEmpty={1}, ChatIDEmpty={2})", 
                    EnableTelegram, string.IsNullOrEmpty(TelegramToken), string.IsNullOrEmpty(TelegramChatId));
            }

            // --- TRIAL CHECK ---
            _trialFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "STBot_trial.dat");
            
            if (System.IO.File.Exists(_trialFile))
            {
                try
                {
                    string data = System.IO.File.ReadAllText(_trialFile).Trim();
                    DateTime firstRun = DateTime.Parse(data);
                    if (Server.Time > firstRun.AddDays(TrialDays))
                    {
                        Print("Trial was expired - resetting for testing");
                        System.IO.File.Delete(_trialFile);
                    }
                }
                catch { }
            }
            
            if (!CheckTrial())
            {
                Print("TRIAL EXPIRED! Bot stopped.");
                SendTelegram("TRIAL EXPIRED!");
                Chart.DrawStaticText("Trial", "TRIAL EXPIRED\nContact seller",
                    VerticalAlignment.Center, HorizontalAlignment.Center, Color.Red);
                Stop();
                return;
            }

            // --- INIT ---
            _atr = Indicators.AverageTrueRange(AtrPeriod, MovingAverageType.Exponential);
            _emaF = Indicators.ExponentialMovingAverage(Bars.ClosePrices, FastEmaPeriod);
            _emaS = Indicators.ExponentialMovingAverage(Bars.ClosePrices, SlowEmaPeriod);
            _bU = CreateDataSeries(); _bL = CreateDataSeries();
            _fU = CreateDataSeries(); _fL = CreateDataSeries();
            _st = CreateDataSeries(); _td = CreateDataSeries();
            _isAuto = (TradeModeInput == 1);

            for (int i = 0; i < Bars.Count; i++) CalcST(i);

            // Only draw the last MaxChartBars bars on startup to prevent lag
            int drawStart = Math.Max(AtrPeriod + 1, Bars.Count - MaxChartBars);
            if (ShowST) for (int i = drawStart; i < Bars.Count; i++) DrawSTBar(i);
            if (ShowEma) for (int i = Math.Max(2, drawStart); i < Bars.Count; i++) DrawEma(i);

            Chart.KeyDown += OnKey;
            UpdateDisplay();

            Timer.Start(2);

            string startMsg = string.Format(
                "\ud83e\udd16 SuperTrend Bot v3.1\n" +
                "{0} | {1}\n" +
                "Mode: {2}\n" +
                "Trial: {3:yyyy-MM-dd HH:mm}\n" +
                "/auto /manual /status /stop",
                SymbolName, TimeFrame, _isAuto ? "AUTO" : "MANUAL", _trialExpiry);
            Print(startMsg);
            SendTelegram(startMsg);

            if (EnableSound) try { Notifications.PlaySound(SoundFile); } catch { }
        }

        protected override void OnTimer()
        {
            if (!EnableTelegram || !EnableTelegramControl) return;
            CheckTelegramCommands();
        }

        // --- TRIAL SYSTEM ---
        private bool CheckTrial()
        {
            try
            {
                if (System.IO.File.Exists(_trialFile))
                {
                    string data = System.IO.File.ReadAllText(_trialFile).Trim();
                    DateTime firstRun = DateTime.Parse(data);
                    _trialExpiry = firstRun.AddDays(TrialDays);
                }
                else
                {
                    DateTime now = Server.Time;
                    System.IO.File.WriteAllText(_trialFile, now.ToString("o"));
                    _trialExpiry = now.AddDays(TrialDays);
                }
                TimeSpan remaining = _trialExpiry - Server.Time;
                if (remaining.TotalSeconds <= 0) return false;
                Print("⏳ Trial: {0:F1} hours remaining", remaining.TotalHours);
                return true;
            }
            catch (Exception ex)
            {
                Print("Trial check error: " + ex.Message);
                _trialExpiry = Server.Time.AddDays(TrialDays);
                return true;
            }
        }

        // --- TELEGRAM SEND (with retry and HTML escaping) ---
        private void SendTelegram(string msg)
        {
            if (!EnableTelegram || string.IsNullOrEmpty(TelegramToken) || string.IsNullOrEmpty(TelegramChatId)) return;
            
            // Escape HTML entities to prevent parse failures
            string safeMsg = msg.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
            
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    string url = string.Format("https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}&parse_mode=HTML",
                        TelegramToken, TelegramChatId, Uri.EscapeDataString(safeMsg));
                    var task = _httpClient.GetStringAsync(url);
                    task.Wait(5000);
                    return; // Success, exit
                }
                catch (Exception ex)
                {
                    Print("TG Send Error (attempt {0}): {1}", attempt + 1, ex.Message);
                    if (ex.InnerException != null) Print("TG Inner: {0}", ex.InnerException.Message);
                    if (attempt == 0)
                    {
                        // Wait briefly before retry
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }
        }

        private void CheckTelegramCommands()
        {
            if (!EnableTelegram || !EnableTelegramControl || string.IsNullOrEmpty(TelegramToken)) return;
            try
            {
                string url = string.Format("https://api.telegram.org/bot{0}/getUpdates?offset={1}&limit=5&timeout=0",
                    TelegramToken, _lastUpdateId + 1);
                var task = _httpClient.GetStringAsync(url);
                task.Wait(5000);
                string json = task.Result;

                int idx = 0;
                while ((idx = json.IndexOf("\"update_id\":", idx)) >= 0)
                {
                    int numStart = idx + 12;
                    int numEnd = json.IndexOfAny(new[] { ',', '}' }, numStart);
                    long uid = long.Parse(json.Substring(numStart, numEnd - numStart).Trim());
                    if (uid > _lastUpdateId) _lastUpdateId = uid;

                    int textIdx = json.IndexOf("\"text\":\"", idx);
                    int nextUpdate = json.IndexOf("\"update_id\":", idx + 12);
                    if (textIdx > 0 && (nextUpdate < 0 || textIdx < nextUpdate))
                    {
                        int tStart = textIdx + 8;
                        int tEnd = json.IndexOf("\"", tStart);
                        if (tEnd > tStart)
                        {
                            string cmd = json.Substring(tStart, tEnd - tStart).Trim().ToLower();
                            Print("TG Command received: " + cmd);
                            ProcessCommand(cmd);
                        }
                    }
                    idx = numEnd;
                }
            }
            catch (Exception ex)
            {
                // Log instead of silently suppressing
                Print("TG Commands Error: {0}", ex.Message);
            }
        }

        private void ProcessCommand(string cmd)
        {
            if (cmd.Contains("@")) cmd = cmd.Split('@')[0];

            if (cmd == "/auto")
            {
                _isAuto = true; _isPaused = false;
                UpdateDisplay();
                SendTelegram("✅ Mode: AUTOMATIC\nBot will execute trades automatically.");
            }
            else if (cmd == "/manual")
            {
                _isAuto = false; _isPaused = false;
                UpdateDisplay();
                SendTelegram("✅ Mode: MANUAL\nBot will only send alerts, no auto trading.");
            }
            else if (cmd == "/status")
            {
                var pos = Positions.FindAll(BotLabel, SymbolName);
                double pnl = pos.Sum(p => p.NetProfit);
                TimeSpan rem = _trialExpiry - Server.Time;
                string status = string.Format(
                    "📊 Status Report\n" +
                    "Symbol: {0} | TF: {1}\n" +
                    "Mode: {2}\n" +
                    "Open Positions: {3}\n" +
                    "Total P/L: {4:F2}\n" +
                    "Trial: {5:F1} hours left\n" +
                    "Price: {6}",
                    SymbolName, TimeFrame, _isAuto ? "AUTO" : "MANUAL",
                    pos.Length, pnl, rem.TotalHours, Symbol.Bid);
                SendTelegram(status);
            }
            else if (cmd == "/start")
            {
                _isPaused = false;
                SendTelegram("✅ Bot Resumed!\n\n🤖 SuperTrend Bot v3.1\nCommands:\n/auto - Automatic trading\n/manual - Manual mode (alerts only)\n/status - Show status\n/stop - Pause bot");
            }
            else if (cmd == "/stop")
            {
                _isPaused = true;
                SendTelegram("⏸ Bot Paused! (Send /start to resume)");
                UpdateDisplay();
            }
        }

        private void OnKey(ChartKeyboardEventArgs a)
        {
            if (a.Key == Key.M)
            {
                _isAuto = !_isAuto;
                UpdateDisplay();
                Print("⚡ Mode: {0}", _isAuto ? "AUTO" : "MANUAL");
                SendTelegram("⚡ Mode switched to: " + (_isAuto ? "AUTO" : "MANUAL"));
            }
        }

        private void UpdateDisplay()
        {
            string txt = _isPaused 
                ? "⏸ PAUSED  |  Send /start" 
                : (_isAuto ? "🤖 AUTO  |  Press M or /manual" : "👤 MANUAL  |  Press M or /auto");
            Color c = _isPaused ? Color.Red : (_isAuto ? Color.FromHex("#00C853") : Color.FromHex("#FF6D00"));
            Chart.DrawStaticText("Mode", txt, VerticalAlignment.Top, HorizontalAlignment.Left, c);

            TimeSpan rem = _trialExpiry - Server.Time;
            Chart.DrawStaticText("Trial", string.Format("⏳ Trial: {0:F1}h left", rem.TotalHours),
                VerticalAlignment.Bottom, HorizontalAlignment.Right, Color.Gray);
        }

        protected override void OnTick()
        {
            if (Bars.Count < AtrPeriod + 1) return;
            
            // Re-calculate live bar
            int currentBar = Bars.Count - 1;
            CalcST(currentBar);
            if (ShowST) DrawSTBar(currentBar);
            if (ShowEma) DrawEma(currentBar);

            // Live touch alerts
            if (EnableTouchAlert && !_touchAlertFired)
            {
                double superValue = _st[currentBar];
                double bid = Bars.ClosePrices[currentBar];

                if (System.Math.Abs(bid - superValue) <= Symbol.PipSize)
                {
                    _touchAlertFired = true;
                    if (EnableSound) try { Notifications.PlaySound(SoundFile); } catch { }
                    if (EnableVisual)
                    {
                        Chart.DrawText("Touch_" + currentBar, "⚡ ST Touched", currentBar, bid, Color.Orange);
                    }
                }
            }
            
            // BreakEven runs on tick for responsiveness, but now coordinated with SuperTrend
            if (EnableBreakEven && !EnableHybridBEST) CheckBreakEven();
            
            // Hybrid BE+ST also runs on tick for the trigger check
            if (EnableHybridBEST) CheckHybridBEST();
        }

        protected override void OnBar()
        {
            _touchAlertFired = false; // reset for new bar
            if (Server.Time >= _trialExpiry)
            {
                SendTelegram("❌ TRIAL EXPIRED!");
                Chart.DrawStaticText("Trial", "❌ TRIAL EXPIRED", VerticalAlignment.Center, HorizontalAlignment.Center, Color.Red);
                Stop(); return;
            }

            int closedBar = Bars.Count - 2;
            if (closedBar < 0) return;

            // Finalize calculation for closed bar
            CalcST(closedBar);
            if (ShowST) DrawSTBar(closedBar);
            if (ShowEma) DrawEma(closedBar);

            if (_isPaused) return;

            int minP = Math.Max(AtrPeriod, Math.Max(FastEmaPeriod, SlowEmaPeriod)) + 2;
            if (closedBar < minP) return;

            int cur = (int)_td[closedBar], prev = (int)_td[closedBar - 1];

            if (cur != prev && prev != 0)
            {
                double eF = _emaF.Result[closedBar], eS = _emaS.Result[closedBar], cl = Bars.ClosePrices[closedBar];

                if (cur == 1)
                {
                    // Always close SELL positions when ST flips BULLISH, regardless of EMA filter
                    if (_isAuto) ClosePosType(TradeType.Sell);
                    
                    if (CheckFilters(closedBar, "BUY", cl, eF, eS))
                    {
                        string m = _isAuto
                            ? string.Format("📈 BUY SIGNAL\n{0} @ {1}\nST flipped BULLISH\nMode: AUTO ✅\nTrade opened automatically", SymbolName, cl)
                            : string.Format("📈 BUY SIGNAL\n{0} @ {1}\nST flipped BULLISH\n⚡ ACTION REQUIRED: Open BUY now!\nSL: {2:F5}\nTrailing: SuperTrend", SymbolName, cl, _st[closedBar] - SLBufferPips * Symbol.PipSize);
                        Print("📈 BUY @ " + cl); SendAlerts(m, "BUY", closedBar); SendTelegram(m);
                        if (_isAuto) OpenOrder(TradeType.Buy, closedBar);
                    }
                }
                else if (cur == -1)
                {
                    // Always close BUY positions when ST flips BEARISH, regardless of EMA filter
                    if (_isAuto) ClosePosType(TradeType.Buy);
                    
                    if (CheckFilters(closedBar, "SELL", cl, eF, eS))
                    {
                        string m = _isAuto
                            ? string.Format("📉 SELL SIGNAL\n{0} @ {1}\nST flipped BEARISH\nMode: AUTO ✅\nTrade opened automatically", SymbolName, cl)
                            : string.Format("📉 SELL SIGNAL\n{0} @ {1}\nST flipped BEARISH\n⚡ ACTION REQUIRED: Open SELL now!\nSL: {2:F5}\nTrailing: SuperTrend", SymbolName, cl, _st[closedBar] + SLBufferPips * Symbol.PipSize);
                        Print("📉 SELL @ " + cl); SendAlerts(m, "SELL", closedBar); SendTelegram(m);
                        if (_isAuto) OpenOrder(TradeType.Sell, closedBar);
                    }
                }
            }
            
            // Trail stop loss ONLY on confirmed closed candle (not on live tick wicks)
            if (UseTrailingSL) TrailSL(closedBar);

            // Clean up tracking for closed positions
            CleanupPositionTracking();

            UpdateDisplay();
        }

        private bool CheckFilters(int i, string dir, double cl, double eF, double eS)
        {
            if (EnableEmaFilter)
            {
                if (UseEma200)
                {
                    if (dir == "BUY" && !(cl > eS && cl >= eF)) return false;
                    if (dir == "SELL" && !(cl < eS && cl <= eF)) return false;
                }
                else
                {
                    if (dir == "BUY" && cl < eF) return false;
                    if (dir == "SELL" && cl > eF) return false;
                }
            }
            if (EnableSlopeFilter && i > SlopeLookback)
            {
                double fSlope = (_emaF.Result[i] - _emaF.Result[i - SlopeLookback]) / Symbol.PipSize;
                double sSlope = (_emaS.Result[i] - _emaS.Result[i - SlopeLookback]) / Symbol.PipSize;
                if (dir == "BUY" && (fSlope < MinSlopeAngle || sSlope < MinSlopeAngle * 0.5)) return false;
                if (dir == "SELL" && (fSlope > -MinSlopeAngle || sSlope > -MinSlopeAngle * 0.5)) return false;
            }
            // ATR Range Filter: Block trades when volatility is too low (choppy/range market)
            if (EnableAtrRangeFilter && i > AtrRangeLookback)
            {
                double currentAtr = _atr.Result[i];
                double avgAtr = 0;
                for (int k = i - AtrRangeLookback; k < i; k++) avgAtr += _atr.Result[k];
                avgAtr /= AtrRangeLookback;
                if (currentAtr < avgAtr * MinAtrMultiplier) return false;
            }
            return true;
        }

        private void CalcST(int i)
        {
            if (i < AtrPeriod) { _td[i] = 0; return; }
            double h = Bars.HighPrices[i], l = Bars.LowPrices[i], c = Bars.ClosePrices[i];
            double med = (h + l) / 2.0, atr = _atr.Result[i];
            _bU[i] = med + Multiplier * atr; _bL[i] = med - Multiplier * atr;

            if (i == AtrPeriod)
            { _fU[i] = _bU[i]; _fL[i] = _bL[i]; _st[i] = _bU[i]; _td[i] = -1; }
            else
            {
                double pFU = _fU[i-1], pFL = _fL[i-1], pC = Bars.ClosePrices[i-1], pS = _st[i-1];
                _fU[i] = (_bU[i] < pFU || pC > pFU) ? _bU[i] : pFU;
                _fL[i] = (_bL[i] > pFL || pC < pFL) ? _bL[i] : pFL;
                _st[i] = (pS == pFU) ? (c <= _fU[i] ? _fU[i] : _fL[i]) : (c >= _fL[i] ? _fL[i] : _fU[i]);
            }
            _td[i] = _st[i] == _fL[i] ? 1 : -1;
        }

        private Position[] GetManagedPositions()
        {
            return Positions.Where(p => p.SymbolName == SymbolName && 
                (p.Label == BotLabel || (ManageManualTrades && string.IsNullOrEmpty(p.Label)))).ToArray();
        }

        private void OpenOrder(TradeType tt, int i)
        {
            if (GetManagedPositions().Length >= MaxPositions) return;
            
            double? sl = FixedSL > 0 ? FixedSL : (double?)null;
            double? tp = FixedTP > 0 ? FixedTP : (double?)null;
            
            if (UseTrailingSL && sl == null)
            {
                double bufferPrice = SLBufferPips * Symbol.PipSize;
                double adjustedSl = (tt == TradeType.Buy) ? _st[i] - bufferPrice : _st[i] + bufferPrice;
                double d = Math.Abs((tt == TradeType.Buy ? Symbol.Ask : Symbol.Bid) - adjustedSl) / Symbol.PipSize;
                if (d > 0) sl = Math.Round(d, 1);
            }
            
            // Enforce minimum SL distance for safe risk calculation (2 pips minimum)
            if (sl != null && sl < 2.0)
            {
                Print("⚠️ SL distance too small ({0:F1} pips), clamping to 2.0 pips for safety.", sl);
                sl = 2.0;
            }
            
            // Risk:Reward TP - calculate TP based on SL distance
            if (UseRRTP && sl != null && sl > 0 && tp == null)
            {
                tp = Math.Round(sl.Value * RiskRewardRatio, 1);
            }
            
            double vol = Symbol.QuantityToVolumeInUnits(VolumeLots);
            if (!UseFixedLots && sl != null && sl > 0)
            {
                double pipValue = Symbol.PipValue;
                if (pipValue > 0)
                {
                    double units = RiskAmount / (sl.Value * pipValue);
                    vol = Symbol.NormalizeVolumeInUnits(units, RoundingMode.Down);
                }
            }

            // Apply MaxLots safety cap
            double maxVol = Symbol.QuantityToVolumeInUnits(MaxLots);
            if (vol > maxVol)
            {
                Print("⚠️ Volume clamped from {0} to {1} (MaxLots={2})", vol, maxVol, MaxLots);
                vol = maxVol;
            }

            // Ensure minimum volume
            if (vol < Symbol.VolumeInUnitsMin)
            {
                vol = Symbol.VolumeInUnitsMin;
                Print("⚠️ Volume set to minimum: {0}", vol);
            }

            var r = ExecuteMarketOrder(tt, SymbolName, vol, BotLabel, sl, tp);
            string msg = r.IsSuccessful
                ? string.Format("✅ {0} opened @ {1} (Vol: {2}, SL: {3} pips)", tt, r.Position.EntryPrice, vol, sl ?? 0)
                : string.Format("❌ {0} failed: {1}", tt, r.Error);
            Print(msg);
            if (r.IsSuccessful)
            {
                // Track initial risk for this position (used by BreakEven coordination and Hybrid BE+ST)
                if (sl != null && sl > 0)
                {
                    _positionInitialRisk[r.Position.Id] = sl.Value * Symbol.PipSize;
                }
                SendTelegram(msg);
            }
        }

        private void ClosePosType(TradeType tt)
        {
            foreach (var p in GetManagedPositions().Where(x => x.TradeType == tt))
            {
                var r = ClosePosition(p);
                if (r.IsSuccessful)
                {
                    string msg = string.Format("🔒 Closed {0} #{1} P/L: {2:F2}", tt, p.Id, p.NetProfit);
                    Print(msg); SendTelegram(msg);
                    // Clean up tracking
                    _positionInitialRisk.Remove(p.Id);
                    _breakEvenActivated.Remove(p.Id);
                    _hybridState.Remove(p.Id);
                }
            }
        }

        private void TrailSL(int i)
        {
            int t = (int)_td[i];
            double bufferPrice = SLBufferPips * Symbol.PipSize;
            double svBuy = _st[i] - bufferPrice;
            double svSell = _st[i] + bufferPrice;

            foreach (var p in GetManagedPositions())
            {
                // If Hybrid BE+ST is active and position is in "locked" state (state=1),
                // check if SuperTrend has reached the lock level before allowing trail
                if (EnableHybridBEST && _hybridState.ContainsKey(p.Id) && _hybridState[p.Id] == 1)
                {
                    double lockLevel = GetHybridLockPrice(p);
                    if (p.TradeType == TradeType.Buy)
                    {
                        // SuperTrend (with buffer) must be at or above the lock level to hand off
                        if (svBuy >= lockLevel)
                        {
                            _hybridState[p.Id] = 2; // Hand off to SuperTrend
                            Print("🔄 Hybrid: Position #{0} handed off to SuperTrend trailing (ST={1:F5} >= Lock={2:F5})", p.Id, svBuy, lockLevel);
                        }
                        else
                        {
                            continue; // Keep SL frozen at lock level, don't trail
                        }
                    }
                    else // Sell
                    {
                        if (svSell <= lockLevel)
                        {
                            _hybridState[p.Id] = 2;
                            Print("🔄 Hybrid: Position #{0} handed off to SuperTrend trailing (ST={1:F5} <= Lock={2:F5})", p.Id, svSell, lockLevel);
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                // Standard BreakEven coordination: if BE has fired but we're NOT in hybrid mode,
                // don't let TrailSL move the stop to a worse level than what BE set
                if (!EnableHybridBEST && _breakEvenActivated.Contains(p.Id))
                {
                    // BreakEven already set the SL. Only trail if SuperTrend is BETTER than current SL.
                    // The check below (svBuy > p.StopLoss for Buy) already ensures this,
                    // so BE coordination is inherently handled. No extra logic needed.
                }

                if (p.TradeType == TradeType.Buy && t == 1 && (p.StopLoss == null || svBuy > p.StopLoss))
                    p.ModifyStopLossPrice(Math.Round(svBuy, Symbol.Digits));
                else if (p.TradeType == TradeType.Sell && t == -1 && (p.StopLoss == null || svSell < p.StopLoss))
                    p.ModifyStopLossPrice(Math.Round(svSell, Symbol.Digits));
            }
        }

        private void CheckBreakEven()
        {
            foreach (var p in GetManagedPositions())
            {
                // Skip if already activated
                if (_breakEvenActivated.Contains(p.Id)) continue;

                if (p.Pips >= BreakEvenTriggerPips)
                {
                    double newSl = p.TradeType == TradeType.Buy 
                        ? p.EntryPrice + (BreakEvenLockPips * Symbol.PipSize)
                        : p.EntryPrice - (BreakEvenLockPips * Symbol.PipSize);

                    // COORDINATION FIX: If trailing SL is enabled, check that the BE level
                    // doesn't jump ahead of where SuperTrend would place the SL.
                    // Take the MORE CONSERVATIVE (wider) of the two levels.
                    if (UseTrailingSL && Bars.Count > AtrPeriod + 1)
                    {
                        int lastBar = Bars.Count - 2; // Use last closed bar's SuperTrend
                        int trend = (int)_td[lastBar];
                        double bufferPrice = SLBufferPips * Symbol.PipSize;
                        
                        if (p.TradeType == TradeType.Buy && trend == 1)
                        {
                            double stSl = _st[lastBar] - bufferPrice;
                            // Use the wider (lower for BUY) of BE and ST levels
                            if (newSl > stSl && stSl > p.EntryPrice)
                            {
                                // BE would jump ahead of ST — cap it at ST level
                                newSl = stSl;
                                Print("🔧 BE capped at SuperTrend level ({0:F5}) for BUY #{1}", stSl, p.Id);
                            }
                        }
                        else if (p.TradeType == TradeType.Sell && trend == -1)
                        {
                            double stSl = _st[lastBar] + bufferPrice;
                            // Use the wider (higher for SELL) of BE and ST levels
                            if (newSl < stSl && stSl < p.EntryPrice)
                            {
                                newSl = stSl;
                                Print("🔧 BE capped at SuperTrend level ({0:F5}) for SELL #{1}", stSl, p.Id);
                            }
                        }
                    }
                        
                    if (p.TradeType == TradeType.Buy && (p.StopLoss == null || newSl > p.StopLoss))
                    {
                        p.ModifyStopLossPrice(Math.Round(newSl, Symbol.Digits));
                        _breakEvenActivated.Add(p.Id);
                        Print("🔒 BE activated for BUY #{0}: SL → {1:F5}", p.Id, newSl);
                    }
                    else if (p.TradeType == TradeType.Sell && (p.StopLoss == null || newSl < p.StopLoss))
                    {
                        p.ModifyStopLossPrice(Math.Round(newSl, Symbol.Digits));
                        _breakEvenActivated.Add(p.Id);
                        Print("🔒 BE activated for SELL #{0}: SL → {1:F5}", p.Id, newSl);
                    }
                }
            }
        }

        // --- HYBRID BREAK-EVEN + SUPERTREND ---
        // Logic:
        // 1. Trade opens with normal SL
        // 2. When profit reaches HybridTriggerR × initialRisk → move SL to entry ± HybridLockR × initialRisk
        // 3. Keep SL fixed at that level (state=1)
        // 4. Wait until SuperTrend reaches the lock level
        // 5. Once SuperTrend >= lock level → hand off to SuperTrend trailing (state=2)
        // 6. If SuperTrend never reaches, keep SL fixed
        private void CheckHybridBEST()
        {
            foreach (var p in GetManagedPositions())
            {
                // Initialize state if not tracked
                if (!_hybridState.ContainsKey(p.Id))
                    _hybridState[p.Id] = 0;

                // Skip if already triggered (state 1 or 2)
                if (_hybridState[p.Id] != 0) continue;

                // Need initial risk to calculate R multiples
                if (!_positionInitialRisk.ContainsKey(p.Id)) continue;

                double initialRiskPrice = _positionInitialRisk[p.Id]; // in price units
                double currentProfitPrice = 0;

                if (p.TradeType == TradeType.Buy)
                    currentProfitPrice = Symbol.Bid - p.EntryPrice;
                else
                    currentProfitPrice = p.EntryPrice - Symbol.Ask;

                // Check if profit has reached the trigger level
                if (currentProfitPrice >= HybridTriggerR * initialRiskPrice)
                {
                    // Calculate lock level
                    double lockPrice = HybridLockR * initialRiskPrice;
                    double newSl = p.TradeType == TradeType.Buy
                        ? p.EntryPrice + lockPrice
                        : p.EntryPrice - lockPrice;

                    if (p.TradeType == TradeType.Buy && (p.StopLoss == null || newSl > p.StopLoss))
                    {
                        p.ModifyStopLossPrice(Math.Round(newSl, Symbol.Digits));
                        _hybridState[p.Id] = 1; // Locked
                        Print("🔐 Hybrid BE: BUY #{0} locked at +{1:F1}R (SL={2:F5})", p.Id, HybridLockR, newSl);
                        SendTelegram(string.Format("🔐 Hybrid BE: BUY #{0} profit reached {1:F1}R - SL locked at +{2:F1}R ({3:F5})", 
                            p.Id, HybridTriggerR, HybridLockR, newSl));
                    }
                    else if (p.TradeType == TradeType.Sell && (p.StopLoss == null || newSl < p.StopLoss))
                    {
                        p.ModifyStopLossPrice(Math.Round(newSl, Symbol.Digits));
                        _hybridState[p.Id] = 1;
                        Print("🔐 Hybrid BE: SELL #{0} locked at +{1:F1}R (SL={2:F5})", p.Id, HybridLockR, newSl);
                        SendTelegram(string.Format("🔐 Hybrid BE: SELL #{0} profit reached {1:F1}R - SL locked at +{2:F1}R ({3:F5})",
                            p.Id, HybridTriggerR, HybridLockR, newSl));
                    }
                }
            }
        }

        private double GetHybridLockPrice(Position p)
        {
            if (!_positionInitialRisk.ContainsKey(p.Id)) return p.StopLoss ?? p.EntryPrice;
            double lockPrice = HybridLockR * _positionInitialRisk[p.Id];
            return p.TradeType == TradeType.Buy
                ? p.EntryPrice + lockPrice
                : p.EntryPrice - lockPrice;
        }

        private void CleanupPositionTracking()
        {
            // Remove tracking entries for positions that no longer exist
            var activeIds = new HashSet<long>(GetManagedPositions().Select(p => (long)p.Id));
            var staleIds = _positionInitialRisk.Keys.Where(id => !activeIds.Contains(id)).ToList();
            foreach (var id in staleIds)
            {
                _positionInitialRisk.Remove(id);
                _breakEvenActivated.Remove(id);
                _hybridState.Remove(id);
            }
        }

        private void SendAlerts(string msg, string dir, int index)
        {
            if (EnableSound) try { Notifications.PlaySound(SoundFile); } catch { }
            if (EnableVisual)
            {
                Color c = dir == "BUY" ? Color.Lime : Color.Red;
                Chart.DrawIcon("Sig_" + index, dir == "BUY" ? ChartIconType.UpArrow : ChartIconType.DownArrow,
                    index, dir == "BUY" ? Bars.LowPrices[index] : Bars.HighPrices[index], c);
                Chart.DrawText("ST_" + index, dir == "BUY" ? "▲ BUY" : "▼ SELL", index,
                    dir == "BUY" ? Bars.LowPrices[index] - Symbol.PipSize * 20 : Bars.HighPrices[index] + Symbol.PipSize * 20, c);
            }
        }

        private void DrawSTBar(int i)
        {
            if (i < AtrPeriod + 1) return;
            int t = (int)_td[i], p = (int)_td[i-1];
            Color c = t == 1 ? Color.Lime : Color.Red;
            
            // VIP visual: Clean solid line for continuous trend, dot for flip
            if (t == p)
                Chart.DrawTrendLine("L_" + i, i-1, _st[i-1], i, _st[i], c, 3, LineStyle.Solid);
            else
                Chart.DrawIcon("D_" + i, ChartIconType.Circle, i, _st[i], c);
            
            // Clean up old objects using MaxChartBars threshold
            if (i > MaxChartBars)
            {
                Chart.RemoveObject("L_" + (i - MaxChartBars));
                Chart.RemoveObject("D_" + (i - MaxChartBars));
            }
        }

        private void DrawEma(int i)
        {
            if (i < 2) return;
            
            // Fast EMA (Orange)
            Chart.DrawTrendLine("EF_" + i, i-1, _emaF.Result[i-1], i, _emaF.Result[i], Color.Orange, 2, LineStyle.Solid);
            
            // Slow EMA (Cyan)
            if (UseEma200)
            {
                Chart.DrawTrendLine("ES_" + i, i-1, _emaS.Result[i-1], i, _emaS.Result[i], Color.Cyan, 2, LineStyle.Solid);
            }
            
            // Cleanup using MaxChartBars threshold
            if (i > MaxChartBars)
            {
                Chart.RemoveObject("EF_" + (i - MaxChartBars));
                Chart.RemoveObject("ES_" + (i - MaxChartBars));
            }
        }

        protected override void OnStop()
        {
            Chart.KeyDown -= OnKey;
            SendTelegram("🛑 SuperTrend Bot Stopped.");
            Print("Bot Stopped.");
        }
    }
}
