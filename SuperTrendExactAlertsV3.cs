using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, AccessRights = AccessRights.None)]
    public class SuperTrendExactAlertsV3 : Indicator
    {
        [Parameter("ATR Period", DefaultValue = 10)]
        public int AtrPeriod { get; set; }

        [Parameter("Multiplier", DefaultValue = 3.0, MinValue = 0.1, Step = 0.1)]
        public double Multiplier { get; set; }

        [Parameter("Enable Alerts", DefaultValue = true)]
        public bool EnableAlerts { get; set; }

        [Parameter("Enable Sound", DefaultValue = true)]
        public bool EnableSound { get; set; }

        [Parameter("Enable Visual Alerts", DefaultValue = true)]
        public bool EnableVisual { get; set; }

        [Parameter("Enable Touch Alert", DefaultValue = true)]
        public bool EnableTouchAlert { get; set; }

        [Parameter("Sound File", DefaultValue = "Alert2.wav")]
        public string SoundFile { get; set; }

        [Output("UpTrend", LineColor = "Lime", PlotType = PlotType.DiscontinuousLine, Thickness = 2)]
        public IndicatorDataSeries UpTrend { get; set; }

        [Output("DownTrend", LineColor = "Red", PlotType = PlotType.DiscontinuousLine, Thickness = 2)]
        public IndicatorDataSeries DownTrend { get; set; }

        private AverageTrueRange _atr;
        private IndicatorDataSeries _basicUpper;
        private IndicatorDataSeries _basicLower;
        private IndicatorDataSeries _finalUpper;
        private IndicatorDataSeries _finalLower;
        private IndicatorDataSeries _superTrend;

        private int _lastTrend; // 1 = alcista, -1 = bajista, 0 = sin definir

        protected override void Initialize()
        {
            _atr = Indicators.AverageTrueRange(AtrPeriod, MovingAverageType.Exponential);

            _basicUpper = CreateDataSeries();
            _basicLower = CreateDataSeries();
            _finalUpper = CreateDataSeries();
            _finalLower = CreateDataSeries();
            _superTrend = CreateDataSeries();

            _lastTrend = 0;

            // --- Sonido inmediato al cargar ---
            if (EnableSound)
                Notifications.PlaySound(SoundFile);
        }

        public override void Calculate(int index)
        {
            if (index < AtrPeriod)
            {
                UpTrend[index] = double.NaN;
                DownTrend[index] = double.NaN;
                return;
            }

            double high = Bars.HighPrices[index];
            double low = Bars.LowPrices[index];
            double close = Bars.ClosePrices[index];
            double median = (high + low) / 2.0;
            double atrVal = _atr.Result[index];

            _basicUpper[index] = median + Multiplier * atrVal;
            _basicLower[index] = median - Multiplier * atrVal;

            if (index == AtrPeriod)
            {
                _finalUpper[index] = _basicUpper[index];
                _finalLower[index] = _basicLower[index];
                _superTrend[index] = _basicUpper[index];
            }
            else
            {
                double prevFinalUpper = _finalUpper[index - 1];
                double prevFinalLower = _finalLower[index - 1];
                double prevClose = Bars.ClosePrices[index - 1];
                double prevSuper = _superTrend[index - 1];

                // Final Upper
                if ((_basicUpper[index] < prevFinalUpper) || (prevClose > prevFinalUpper))
                    _finalUpper[index] = _basicUpper[index];
                else
                    _finalUpper[index] = prevFinalUpper;

                // Final Lower
                if ((_basicLower[index] > prevFinalLower) || (prevClose < prevFinalLower))
                    _finalLower[index] = _basicLower[index];
                else
                    _finalLower[index] = prevFinalLower;

                // SuperTrend
                if (prevSuper == prevFinalUpper)
                {
                    _superTrend[index] = close <= _finalUpper[index] ? _finalUpper[index] : _finalLower[index];
                }
                else
                {
                    _superTrend[index] = close >= _finalLower[index] ? _finalLower[index] : _finalUpper[index];
                }
            }

            // Determinar tendencia actual
            int currentTrend = _superTrend[index] == _finalLower[index] ? 1 : -1;
            UpTrend[index] = currentTrend == 1 ? _superTrend[index] : double.NaN;
            DownTrend[index] = currentTrend == -1 ? _superTrend[index] : double.NaN;

            // --- ALERTAS POR CAMBIO DE TENDENCIA ---
            if (EnableAlerts && index > AtrPeriod && _lastTrend != 0 && currentTrend != _lastTrend)
            {
                string msg = currentTrend == 1 ? "📈 BUY Signal" : "📉 SELL Signal";

                if (EnableSound)
                    Notifications.PlaySound(SoundFile);

                if (EnableVisual)
                {
                    Chart.DrawText(
                        "Alert_" + index,
                        msg,
                        index,
                        Bars.ClosePrices[index],
                        Color.Yellow
                    );
                }
            }

            // --- ALERTA POR TOQUE DE SUPER TREND ---
            if (EnableTouchAlert)
            {
                double superValue = _superTrend[index];
                double bid = Bars.ClosePrices[index];

                if (System.Math.Abs(bid - superValue) <= Symbol.PipSize)
                {
                    if (EnableSound)
                        Notifications.PlaySound(SoundFile);

                    if (EnableVisual)
                    {
                        Chart.DrawText(
                            "Touch_" + index,
                            "⚡ SuperTrend Touched",
                            index,
                            bid,
                            Color.Orange
                        );
                    }
                }
            }

            _lastTrend = currentTrend;
        }
    }
}

