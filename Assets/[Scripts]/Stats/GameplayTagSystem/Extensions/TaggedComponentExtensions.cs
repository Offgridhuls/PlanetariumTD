using System.Linq;
using Planetarium.Stats;

namespace Planetarium.Stats
{
    public static class TaggedComponentExtensions
    {
        public static string GetTagsString(this TaggedComponent component)
        {
            if (component == null || component.Tags == null || !component.Tags.Any())
                return string.Empty;

            return string.Join("\n", component.Tags.Select(tag => 
                string.IsNullOrEmpty(tag.DevComment) ? tag.TagName : $"{tag.TagName} ({tag.DevComment})"));
        }
    }
}
