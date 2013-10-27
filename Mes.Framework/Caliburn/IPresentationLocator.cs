namespace Mes.Framework
{
    using System;
    using System.Windows;

    public interface IPresentationLocator
    {
        void Bind(object model, DependencyObject view);
        object LocateModelForView(object view);
        UIElement LocateViewForModel(object model);
    }
}

