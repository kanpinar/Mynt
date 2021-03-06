using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Mynt.Core.Strategies;
using Mynt.Core.TradeManagers;
using Mynt.Core.Binance;

namespace Mynt.Functions
{
    public static class BuyTimer
    {
        // This function runs every hour at 1 minute and 10 seconds past the hour (e.g. 14:01:10, 15:01:10).
        [FunctionName("BuyTimer")]
        public static async Task Run([TimerTrigger("10 3 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            try
            {
                log.Info("Starting processing...");

                // Call the trade manager with the strategy of our choosing.
                var manager = new GenericTradeManager(
                    new BinanceApi(), 
                    new BollingerAwe(), 
                    null, (a) => log.Info(a)
                );

                // Call the process method to start processing the current situation.
                await manager.CheckStrategySignals();

                log.Info("Done...");
            }
            catch (Exception ex)
            {
                // If anything goes wrong log an error to Azure.
                log.Error(ex.Message + ex.StackTrace);
            }
        }
    }
}
