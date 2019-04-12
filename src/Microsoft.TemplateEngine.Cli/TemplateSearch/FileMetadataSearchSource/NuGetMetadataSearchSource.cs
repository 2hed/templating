using System.IO;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch.FileMetadataSearchSource
{
    public class NuGetMetadataSearchSource : FileMetadataSearchSource
    {
        private static readonly string _templateDiscoveryMetadataFile = "nugetTemplateSearchInfo.json";

        private readonly ISearchInfoFileProvider _searchInfoFileProvider;

        // TODO: get the ISearchInfoFileProvider finalized
        public NuGetMetadataSearchSource()
        {
            _searchInfoFileProvider = new TEMP_LocalSourceFileProvider();

            // This will become the real one once we're done testing / have the blob store automation setup.
            //_searchInfoFileProvider = new BlobStoreSourceFileProvider();
        }

        public override bool TryConfigure(IEngineEnvironmentSettings environmentSettings)
        {
            Paths paths = new Paths(environmentSettings);
            string searchMetadataFileLocation = Path.Combine(paths.User.BaseDir, _templateDiscoveryMetadataFile);

            if (!_searchInfoFileProvider.TryEnsureSearchFile(paths, searchMetadataFileLocation))
            {
                return false;
            }

            FileMetadataTemplateSearchCache searchCache = new FileMetadataTemplateSearchCache(environmentSettings, _templateDiscoveryMetadataFile);

            Configure(searchCache);

            return true;
        }

        public override string DisplayName => "NuGet";
    }
}
