namespace Mes.Framework
{
    using Caliburn.Micro;
    using System;
    using System.ComponentModel.Composition;
    using System.Windows;

    [Export(typeof(IPresentationLocator))]
    public class GitHubPresentationLocator : IPresentationLocator
    {
        public void Bind(object viewModel, DependencyObject view)
        {
            ViewModelBinder.Bind(viewModel, view, null);
        }

        public object LocateModelForView(object view)
        {
            return ViewModelLocator.LocateForView(view);
        }

        public UIElement LocateViewForModel(object viewModel)
        {
            return ViewLocator.LocateForModel(viewModel, null, null);
        }
    }
}

