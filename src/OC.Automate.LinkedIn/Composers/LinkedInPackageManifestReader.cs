using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace OC.Automate.LinkedIn.Composers;

public class LinkedInPackageManifestReader : IPackageManifestReader
{
    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        var version = typeof(LinkedInPackageManifestReader).Assembly.GetName().Version?.ToString() ?? "1.0.0";

        return Task.FromResult<IEnumerable<PackageManifest>>(new[]
        {
            new PackageManifest
            {
                Id = "OC.Automate.LinkedIn",
                Name = "OC Automate LinkedIn",
                Version = version,
                AllowTelemetry = true,
                Extensions =
                [
                    new
                    {
                        name = "OC Automate LinkedIn Bundle",
                        alias = "OC.Automate.LinkedIn.Bundle",
                        type = "bundle",
                        js = "/App_Plugins/OC.Automate.LinkedIn/oc-automate-linkedin.js?v=" + version
                    }
                ]
            }
        });
    }
}
