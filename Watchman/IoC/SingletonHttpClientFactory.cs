using System.Net.Http;
using Amazon.Runtime;

namespace Watchman.IoC
{
    class SingletonHttpClientFactory : HttpClientFactory
    {
        private readonly HttpClient _client;

        public SingletonHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public override HttpClient CreateHttpClient(IClientConfig clientConfig)
        {
            return _client;
        }

        public override bool UseSDKHttpClientCaching(IClientConfig clientConfig)
        {
            return false;
        }

        public override bool DisposeHttpClientsAfterUse(IClientConfig clientConfig)
        {
            return false;
        }
    }
}
