﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mynt.Core.Enums;
using Mynt.Core.Indicators;
using Mynt.Core.Interfaces;
using Mynt.Core.Models;

namespace Mynt.Core.Strategies
{
    public class Wvf : BaseStrategy
    {
        public override string Name => "Williams Vix Fix";
        public override int MinimumAmountOfCandles => 40;
        public override Period IdealPeriod => Period.Hour;

        public override List<ITradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<ITradeAdvice>();

            var ao = candles.AwesomeOscillator();
            var close = candles.Select(x => x.Close).ToList();
            var high = candles.Select(x => x.High).ToList();
            var low = candles.Select(x => x.Low).ToList();
            var open = candles.Select(x => x.Open).ToList();

            var stochRsi = candles.StochRsi(14);

            var wvfs = new List<double>();
            var standardDevs = new List<double>();
            var rangeHighs = new List<double>();
            var upperRanges = new List<double?>();

            var pd = 22; // LookBack Period Standard Deviation High
            var bbl = 20; // Bollinger Band Length
            var mult = 2.0; // Bollinger Band Standard Deviation Up
            var lb = 50; // Look Back Period Percentile High
            var ph = .85; // Highest Percentile - 0.90=90%, 0.95=95%, 0.99=99%

            for (int i = 0; i < candles.Count; i++)
            {
                var itemsToPick = i < pd - 1 ? i + 1 : pd;
                var indexToStartFrom = i < pd - 1 ? 0 : i - pd;

                var highestClose = candles.Skip(indexToStartFrom).Take(itemsToPick).Select(x => x.Close).Max();
                var wvf = ((highestClose - candles[i].Low) / (highestClose)) * 100;

                // Calculate the WVF
                wvfs.Add(wvf);

                double standardDev = 0;

                if (wvfs.Count > 1)
                {
                    if (wvfs.Count < bbl)
                        standardDev = mult * GetStandardDeviation(wvfs.Take(bbl).ToList());
                    else
                        standardDev = mult * GetStandardDeviation(wvfs.Skip(wvfs.Count - bbl).Take(bbl).ToList());
                }

                // Also calculate the standard deviation.
                standardDevs.Add(standardDev);
            }

            var midLines = wvfs.Sma(bbl);

            for (int i = 0; i < candles.Count; i++)
            {
                var upperBand = midLines[i] + standardDevs[i];

                if (upperBand.HasValue)
                    upperRanges.Add(upperBand.Value);
                else
                    upperRanges.Add(null);

                var itemsToPickRange = i < lb - 1 ? i + 1 : lb;
                var indexToStartFromRange = i < lb - 1 ? 0 : i - lb;

                var rangeHigh = wvfs.Skip(indexToStartFromRange).Take(itemsToPickRange).Max() * ph;
                rangeHighs.Add(rangeHigh);
            }

            for (int i = 0; i < candles.Count; i++)
            {
                if (wvfs[i] >= upperRanges[i] || wvfs[i] >= rangeHighs[i] && ao[i] > 0 && ao[i - 1] < 0)
                    result.Add(new SimpleTradeAdvice(TradeAdvice.Buy));
                else if (stochRsi.K[i] > 80 && stochRsi.K[i] > stochRsi.D[i] && stochRsi.K[i - 1] < stochRsi.D[i - 1] && ao[i] < 0 && ao[i - 1] > 0)
                    result.Add(new SimpleTradeAdvice(TradeAdvice.Sell));
                else
                    result.Add(new SimpleTradeAdvice(TradeAdvice.Hold));
            }

            return result;
        }

        private double GetStandardDeviation(List<double> doubleList)
        {
            double average = doubleList.Average();
            double sumOfDerivation = 0;
            foreach (double value in doubleList)
            {
                sumOfDerivation += (value) * (value);
            }
            double sumOfDerivationAverage = sumOfDerivation / (doubleList.Count - 1);
            return Math.Sqrt(sumOfDerivationAverage - (average * average));
        }

        public override ITradeAdvice Forecast(List<Candle> candles)
        {
            return Prepare(candles).LastOrDefault();
        }
    }
}
