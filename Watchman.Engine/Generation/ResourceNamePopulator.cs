using System.Text.RegularExpressions;
using Watchman.AwsResources;
using Watchman.Configuration.Generic;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Generation
{
    public class ResourceNamePopulator<T, TConfig>
        where T: class
        where TConfig: class
    {
        private readonly IAlarmLogger _logger;
        private readonly IResourceSource<T>  _resourceSource;

        public ResourceNamePopulator(IAlarmLogger logger,
            IResourceSource<T> resourceSource)
        {
            _logger = logger;
            _resourceSource = resourceSource;
        }

        public async Task<PopulatedServiceAlertingGroup<TConfig, T>>
            PopulateResourceNames(ServiceAlertingGroup<TConfig> alertingGroup)
        {
           var items=
                await ExpandTablePatterns(alertingGroup.Service, alertingGroup.GroupParameters.Name);

           // TODO: maybe in mapper
           var result = new PopulatedServiceAlertingGroup<TConfig, T>()
           {
               GroupParameters = alertingGroup.GroupParameters,
               Service = new PopulatedServiceAlarms<TConfig, T>()
               {
                   ExcludeResourcesPrefixedWith = alertingGroup.Service.ExcludeResourcesPrefixedWith,
                   Options = alertingGroup.Service.Options,
                   Resources = items,
                   Values = alertingGroup.Service.Values
               }
           };

           return result;
        }

        private static IEnumerable<ResourceAndThresholdsPair<TConfig, T>>
            Distinct(List<ResourceAndThresholdsPair<TConfig, T>> input)
        {
            var names = new HashSet<string>();
            foreach (var resource in input)
            {
                // todo
                if (names.Add(resource.Resource.Name))
                {
                    yield return resource;
                }
            }
        }

        private async Task<List<ResourceAndThresholdsPair<TConfig, T>>> ExpandTablePatterns(
            AwsServiceAlarms<TConfig> service,
            string alertingGroupName
            )
        {
            var named = service.Resources
                .Where(t => string.IsNullOrWhiteSpace(t.Pattern))
                .Select(t => t.AsPattern())
                .ToList();

            var patterns = service.Resources
                .Where(t => !string.IsNullOrWhiteSpace(t.Pattern))
                .ToList();

            var matchedPatterns = new List<ResourceAndThresholdsPair<TConfig, T>>();

            var combined = named.Concat(patterns);

            foreach (var pattern in combined)
            {
                var matches = await GetPatternMatches(pattern, alertingGroupName);

                matchedPatterns.AddRange(matches);
            }

            var all = Distinct(matchedPatterns);

            if (service.ExcludeResourcesPrefixedWith == null)
            {
                return all.ToList();
            }

            return all.Where(
                a => !service.ExcludeResourcesPrefixedWith
                    .Any(prefix => a.Resource.Name.StartsWith(prefix))
            ).ToList();
        }


        private async Task<IList<ResourceAndThresholdsPair<TConfig, T>>> GetPatternMatches(
            ResourceThresholds<TConfig> resourcePattern,
            string alertingGroupName)
        {
            var tableNames = await _resourceSource.GetResourcesAsync();

            var regex = new Regex(resourcePattern.Pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var matches = tableNames
                .Where(table => regex.IsMatch(table.Name))
                .Select(tn => PatternToTable(resourcePattern, tn))
                .ToList();

            if (matches.Count == 0)
            {
                _logger.Info($"{alertingGroupName} pattern '{resourcePattern.Pattern}' matched no resource names");
            }
            else if (matches.Count == tableNames.Count)
            {
                _logger.Info($"{alertingGroupName} pattern '{resourcePattern.Pattern}' matched all resource names");
            }
            else
            {
                _logger.Info($"{alertingGroupName} pattern '{resourcePattern.Pattern}' matched {matches.Count} of {tableNames.Count} resource names");
            }

            return matches;
        }

        private static ResourceAndThresholdsPair<TConfig, T> PatternToTable(
            ResourceThresholds<TConfig> pattern, AwsResource<T> resource)
        {
            pattern = pattern.AsNamed(resource.Name);

            var matched = new ResourceAndThresholdsPair<TConfig, T>(pattern, resource);

            return matched;
        }
    }
}
