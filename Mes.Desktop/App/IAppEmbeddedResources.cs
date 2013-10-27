namespace GitHub.Helpers
{
    public interface IAppEmbeddedResources
    {
        IEmbeddedResource Credits { get; }

        IEmbeddedResource GitHubDiffMustacheTemplateResource { get; }

        IEmbeddedResource GitHubShell { get; }

        IEmbeddedResource ReadMeTemplate { get; }
    }
}

