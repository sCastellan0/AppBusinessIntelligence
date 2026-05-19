using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace TFG_APPBusinessIntelligence.Platforms.Android
{
    /// <summary>
    /// Handler que hace transparente el fondo nativo de la Activity
    /// para que las páginas modales muestren el contenido subyacente.
    /// </summary>
    public class TransparentModalPageHandler : PageHandler
    {
        protected override void ConnectHandler(ContentViewGroup platformView)
        {
            base.ConnectHandler(platformView);
            platformView.SetBackgroundColor(global::Android.Graphics.Color.Transparent);

            if (MauiContext?.Context is global::Android.App.Activity activity)
            {
                activity.Window?.SetBackgroundDrawableResource(global::Android.Resource.Color.Transparent);
            }
        }
    }
}
