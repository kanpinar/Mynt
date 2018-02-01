using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Mynt.Core;
using Mynt.Core.Bittrex;
using Mynt.Core.NotificationManagers;
using Mynt.Core.Strategies;
using Mynt.Core.TradeManagers;
using Mynt.Core.Binance;

namespace Mynt.Functions
{
    public static class TradeTimer
    {
        // This function triggers every 5 minutes on the 2nd second so e.g. 14:05:02, 14:10:02 etc.
        [FunctionName("TradeTimer")]
        public static async Task Run([TimerTrigger("10 0 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            try
            {
                log.Info("Starting processing...");

                // Call the Bittrex Trade manager with the strategy of our choosing.
                var manager = new GenericTradeManager(
                    new BittrexApi(), 
                    new BigThree(), 
                    new TelegramNotificationManager(), (a) => log.Info(a)
                );

                // Call the process method to start processing the current situation.
                await manager.Process();
            }
            catch (Exception ex)
            {
                // If anything goes wrong log an error to Azure.
                log.Error(ex.Message + ex.StackTrace);
            }
        }
    }
}
