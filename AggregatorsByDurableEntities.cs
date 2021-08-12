using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFunctionsDocumentation
{
    public static class AggregatorsByDurableEntities
    {
        [FunctionName("Function1")]
        public static void Counter(
            [EntityTrigger] IDurableEntityContext ctx)
        {
            int currentValue = ctx.GetState<int>();
            switch (ctx.OperationName.ToLowerInvariant())
            {
                case "add":
                    int amount = ctx.GetInput<int>();
                    ctx.SetState(currentValue + amount);
                    break;
                case "reset":
                    ctx.SetState(0);
                    break;
                case "get":
                    ctx.Return(currentValue);
                    break;
            }
        }

        public class CounterModel
        {
            [JsonProperty("value")]
            public int CurrentValue { get; set; }

            public void Add(int amount) => this.CurrentValue += amount;

            public void Reset() => this.CurrentValue = 0;

            public int Get() => this.CurrentValue;

            [FunctionName(nameof(Counter))]
            public static Task Run([EntityTrigger] IDurableEntityContext ctx)
                => ctx.DispatchAsync<CounterModel>();
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