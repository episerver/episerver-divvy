using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

namespace Episerver.Labs.Divvy.Webhooks
{
    public class DivvyWebhookManager : Queue<DivvyWebhook>
    {
        private System.Timers.Timer timer;
        private bool working;

        public static DivvyWebhookManager Instance;

        static DivvyWebhookManager()
        {
            Instance = new DivvyWebhookManager();
        }

        public DivvyWebhookManager()
        {
            timer = new System.Timers.Timer
            {
                Interval = DivvyManager.Settings.WebhookTimerInterval
            };
            timer.Elapsed += (s, e) =>
            {
                if (!working)
                {
                    Process();
                }
            };
            timer.Start();
            DivvyLogManager.Log($"Webhook Manager Initialized", new { timer.Interval });
        }

        public void Add(DivvyWebhook webhook)
        {
            DivvyLogManager.Log($"Enqueing Webhook", webhook);
            Enqueue(webhook);
        }

        private void Process()
        {
            if (!this.Any())
            {
                return;
            }

            DivvyLogManager.Log($"Processing Webhook Queue. {this.Count()} item(s)");
            working = true;

            while (this.Any())
            {
                var webhook = Dequeue();
                Execute(webhook);
                Thread.Sleep(DivvyManager.Settings.WebhookTimerInterval);
            };

            working = false;
            DivvyLogManager.Log($"Webhook Queue Processing Complete");
        }

        private void Execute(DivvyWebhook webhook)
        {
            DivvyLogManager.Log($"Executing Webhook", webhook);

            var sw = Stopwatch.StartNew();
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", DivvyManager.Settings.AccessToken.ToString());
            var result = httpClient.PostAsJsonAsync<object>(webhook.Uri.AbsoluteUri, webhook.Data).Result;

            DivvyLogManager.Log($"Webhook Executed in {sw.ElapsedMilliseconds}ms", new { result.StatusCode });
        }
    }
}