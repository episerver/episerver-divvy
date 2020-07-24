using System;

namespace Episerver.Labs.Divvy.Webhooks
{
    public class DivvyWebhook
    {
        public string Key { get; private set; }
        public Uri Uri { get; set; }
        public object Data { get; set; }

        public DivvyWebhook()
        {
            Key = Guid.NewGuid().ToString();
        }
    }
}