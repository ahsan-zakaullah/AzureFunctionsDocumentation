using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace AzureFunctionsDocumentation
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task Run([EventHubTrigger("samples-workitems", Connection = "")] EventData[] events, ILogger log)
        {
            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                    // Replace these two lines with your processing logic.
                    log.LogInformation($"C# Event Hub trigger function processed a message: {messageBody}");
                    await Task.Yield();
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }

        [FunctionName("EventHubTriggerCSharp")]
        public static async Task Run(
            [EventHubTrigger("device-sensor-events")] EventData eventData,
            [DurableClient] IDurableEntityClient entityClient)
        {
            var metricType = (string)eventData.Properties["metric"];
            var delta = BitConverter.ToInt32(eventData.Body.Array, eventData.Body.Offset);

            // The "Counter/{metricType}" entity is created on-demand.
            var entityId = new EntityId("Counter", metricType);
            await entityClient.SignalEntityAsync(entityId, "add", delta);
        }
    }
}
