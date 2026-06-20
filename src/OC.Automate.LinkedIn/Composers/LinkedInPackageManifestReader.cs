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
                        name = "LinkedIn Authorize Button",
                        alias = "OC.Automate.LinkedIn.AuthorizeButton",
                        type = "propertyEditorUi",
                        element = "/App_Plugins/OC.Automate.LinkedIn/oc-automate-linkedin.js",
                        elementName = "linkedin-authorize-button",
                        meta = new
                        {
                            label = "LinkedIn Authorize Button",
                            icon = "icon-link",
                            group = "common",
                            propertyEditorSchemaAlias = "Umbraco.Plain.String",
                            supportsReadOnly = true
                        }
                    }
                ]
            }
        });
    }
}
