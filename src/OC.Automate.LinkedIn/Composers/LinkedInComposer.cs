using Microsoft.Extensions.DependencyInjection;
using OC.Automate.LinkedIn.Composers;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Connections;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Manifest;

namespace OC.Automate.LinkedIn;

public class LinkedInComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddOptions<LinkedInSettings>()
            .BindConfiguration("OwainCodes:Automate:LinkedIn");

        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<LinkedInTokenService>();

        builder.WithCollectionBuilder<ConnectionTypeCollectionBuilder>()
            .Add<LinkedInConnectionType>();

        builder.WithCollectionBuilder<ActionCollectionBuilder>()
            .Add<SendLinkedInPostAction>();

        builder.Services.AddSingleton<IPackageManifestReader, LinkedInPackageManifestReader>();
    }
}
