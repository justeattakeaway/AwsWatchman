using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;

namespace Watchman.AwsResources.Services.Lambda
{
    public class LambdaSource : ResourceSourceBase<FunctionConfiguration>
    {
        private readonly IAmazonLambda _client;

        public LambdaSource(IAmazonLambda client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _client = client;
        }

        protected override async Task<IEnumerable<FunctionConfiguration>> FetchResources()
        {
            var results = new List<IEnumerable<FunctionConfiguration>>();
            string marker = null;

            do
            {
                var response = await _client.ListFunctionsAsync(new ListFunctionsRequest
                {
                    Marker = marker
                });

                results.Add(response.Functions);
                marker = response.NextMarker;
            }
            while (!string.IsNullOrEmpty(marker));

            return results.SelectMany(x => x).ToList();
        }

        protected override string GetResourceName(FunctionConfiguration resource) => resource.FunctionName;
    }
}