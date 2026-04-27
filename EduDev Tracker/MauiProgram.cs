using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Microsoft.Maui.LifecycleEvents;

#if WINDOWS
using Microsoft.UI.Windowing;
using Microsoft.UI;
using WinRT.Interop;
#endif

namespace EduDev_Tracker
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Comfortaa.ttf", "Comfortaa");
                });

            //#if WINDOWS
            //            builder.ConfigureLifecycleEvents(events =>
            //            {
            //                events.AddWindows(windows =>
            //                {
            //                    windows.OnWindowCreated(window =>
            //                    {
            //                        window.ExtendsContentIntoTitleBar = false;

            //                        var handle = WindowNative.GetWindowHandle(window);
            //                        var id = Win32Interop.GetWindowIdFromWindow(handle);
            //                        var appWindow = AppWindow.GetFromWindowId(id);

            //                        if (appWindow.Presenter is OverlappedPresenter presenter)
            //                        {
            //                            presenter.SetBorderAndTitleBar(false, false);
            //                            presenter.Maximize();
            //                        }
            //                    });
            //                });
            //            });
            //#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif

            RegisterServices(builder.Services);
            return builder.Build();
        }

        static void RegisterServices(IServiceCollection services)
        {
        }
    }
}