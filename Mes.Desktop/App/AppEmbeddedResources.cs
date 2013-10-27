namespace GitHub.Helpers
{
    using GitHub.IO;
    using System;
    using System.ComponentModel.Composition;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    [Export(typeof(IAppEmbeddedResources))]
    public class AppEmbeddedResources : IAppEmbeddedResources
    {
        private static readonly Assembly resourceAssembly = typeof(App).Assembly;

        [ImportingConstructor]
        public AppEmbeddedResources(IOperatingSystem operatingSystem)
        {
            this.ReadMeTemplate = Create("GitHub.data.readme-template.html", operatingSystem);
            this.Credits = Create("GitHub.data.CREDITS.md", operatingSystem);
            this.GitHubDiffMustacheTemplateResource = Create("GitHub.data.diff_xaml.mustache", operatingSystem);
            this.GitHubShell = Create("GitHub.shell.ps1", operatingSystem);
        }

        private static IEmbeddedResource Create(string resourceName, IOperatingSystem operatingSystem)
        {
            return new EmbeddedResource(resourceAssembly, resourceName, null, operatingSystem);
        }

        public IEmbeddedResource Credits { get; private set; }

        public IEmbeddedResource GitHubDiffMustacheTemplateResource { get; private set; }

        public IEmbeddedResource GitHubShell { get; private set; }

        public IEmbeddedResource ReadMeTemplate { get; private set; }
    }
}

