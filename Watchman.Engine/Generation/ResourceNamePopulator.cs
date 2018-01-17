using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Watchman.AwsResources;
using Watchman.Configuration.Generic;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Generation
{
    public class ResourceNamePopulator<T> where T: class
    {
        private readonly IAlarmLogger _logger;
        private readonly IResourceSource<T>  _resourceSource;

        public ResourceNamePopulator(IAlarmLogger logger,
            IResourceSource<T> resourceSource)
        {
            _logger = logger;
            _resourceSource = resourceSource;
        }

        public async Task PopulateResourceNames(ServiceAlertingGroup alertingGroup)
        {
            alertingGroup.Service.Resources = await ExpandTablePatterns(alertingGroup.Service, alertingGroup.GroupParameters.Name);
        }

        private static IEnumerable<ResourceThresholds> Distinct(IEnumerable<ResourceThresholds> input)
        {
            var names = new HashSet<string>();
            foreach (var resource in input)
            {
                if (names.Add(resource.Name))
                {
                    yield return resource;
                }
            }
        }

        private async Task<List<ResourceThresholds>> ExpandTablePatterns(AwsServiceAlarms service, string alertingGroupName)
        {
            var named = service.Resources
                .Where(t => string.IsNullOrWhiteSpace(t.Pattern))
                .Select(NameToPattern)
                .ToList();

            var patterns = service.Resources
                .Where(t => !string.IsNullOrWhiteSpace(t.Pattern))
                .ToList();

            var matchedPatterns = new List<ResourceThresholds>();

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
                a => !service.ExcludeResourcesPrefixedWith.Any(prefix => a.Name.StartsWith(prefix))
            ).ToList();
        }

        private static ResourceThresholds NameToPattern(ResourceThresholds named)
        {
            var name = Regex.Escape(named.Name);

            return new ResourceThresholds()
            {
                Pattern = $"^{name}$",
                Values = named.Values
            };
        }

        private async Task<IList<ResourceThresholds>> GetPatternMatches(ResourceThresholds resourcePattern, string alertingGroupName)
        {
            var tableNames = await _resourceSource.GetResourceNamesAsync();

            var matches = tableNames
                .WhereRegexIsMatch(resourcePattern.Pattern)
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

        private static ResourceThresholds PatternToTable(ResourceThresholds pattern, string tableName)
        {
            return new ResourceThresholds
            {
                Name = tableName,
                Pattern = null,
                Values = pattern.Values
            };
        }
    }
}
