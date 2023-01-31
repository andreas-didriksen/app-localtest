#nullable enable

namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// Model representation of Competent Authority part of the ServiceResource model
    /// </summary>
    public class CompetentAuthority
    {
        /// <summary>
        /// The organization number
        /// </summary>
        public string Organization { get; set; } = default!;

        /// <summary>
        /// The organization code
        /// </summary>
        public string Orgcode { get; set; } = default!;

        /// <summary>
        /// The organization name. If not set it will be retrived from register based on Organization number
        /// </summary>
        public Dictionary<string, string>? Name { get; set; }
    }
}
