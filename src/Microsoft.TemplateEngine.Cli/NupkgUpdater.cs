// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplateUpdates;
using Microsoft.TemplateEngine.Edge.TemplateUpdates;
using Microsoft.TemplateEngine.Cli.TemplateSearch.FileMetadataSearchSource;
using Microsoft.TemplateEngine.Cli.TemplateSearch;
using Microsoft.TemplateSearch.Common;

namespace Microsoft.TemplateEngine.Cli
{
    internal class NupkgUpdater : IUpdater
    {
        public Guid Id { get; } = new Guid("DB5BF8D8-6181-496A-97DA-58616E135701");

        public Guid DescriptorFactoryId { get; } = NupkgInstallUnitDescriptorFactory.FactoryId;

        public string DisplayIdentifier { get; } = "Nupkg";

        private IEngineEnvironmentSettings _environmentSettings;
        //private readonly ISearchInfoFileProvider _searchInfoFileProvider;
        private bool _isInitialized = false;
        private IReadOnlyList<ITemplateSearchSource> _templateSearchSourceList;

        public NupkgUpdater()
        {
        }

        public void Configure(IEngineEnvironmentSettings environmentSettings)
        {
            _environmentSettings = environmentSettings;
        }

        //public NupkgUpdater(IEngineEnvironmentSettings environmentSettings)
        //{
        //    _environmentSettings = environmentSettings;
        //    //_searchInfoFileProvider = new TEMP_LocalSourceFileProvider();

        //    // This will become the real one once we're done testing / have the blob store automation setup.
        //    //_searchInfoFileProvider = new BlobStoreSourceFileProvider();
        //}

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            List<ITemplateSearchSource> searchSourceList = new List<ITemplateSearchSource>();

            foreach (ITemplateSearchSource searchSource in _environmentSettings.SettingsLoader.Components.OfType<ITemplateSearchSource>())
            {
                if (searchSource.TryConfigure(_environmentSettings))
                {
                    searchSourceList.Add(searchSource);
                }
            }

            _templateSearchSourceList = searchSourceList;

            _isInitialized = true;
        }

        public async Task<IReadOnlyList<IUpdateUnitDescriptor>> CheckForUpdatesAsync(IReadOnlyList<IInstallUnitDescriptor> descriptorsToCheck)
        {
            EnsureInitialized();

            IReadOnlyDictionary<string, IInstallUnitDescriptor> packToInstallDescriptorMap = descriptorsToCheck.ToDictionary(d => d.Identifier, d => d);

            List<IUpdateUnitDescriptor> updateList = new List<IUpdateUnitDescriptor>();

            foreach (ITemplateSearchSource searchSource in _templateSearchSourceList)
            {
                IReadOnlyDictionary<string, PackToTemplateEntry> packMatchList = await searchSource.CheckForTemplatePackMatchesAsync(packToInstallDescriptorMap.Keys.ToList());

                foreach (KeyValuePair<string, PackToTemplateEntry> packMatch in packMatchList)
                {
                    string packName = packMatch.Key;
                    PackToTemplateEntry packToUpdate = packMatch.Value;

                    if (packToInstallDescriptorMap.TryGetValue(packName, out IInstallUnitDescriptor installDescriptor))
                    {
                        IUpdateUnitDescriptor updateDescriptor = new UpdateUnitDescriptor(installDescriptor, packName, packName);
                        updateList.Add(updateDescriptor);
                    }
                }
            }

            return updateList;
        }

        public void ApplyUpdates(IInstaller installer, IReadOnlyList<IUpdateUnitDescriptor> updatesToApply)
        {
            IReadOnlyList<IUpdateUnitDescriptor> filteredUpdateToApply = updatesToApply.Where(x => x.InstallUnitDescriptor.FactoryId == DescriptorFactoryId).ToList();
            installer.InstallPackages(filteredUpdateToApply.Select(x => x.InstallString));
        }
    }
}
