using CommunityToolkit.Maui;
using EduDev_Tracker.Data;
using EduDev_Tracker.Data.Repositories.Implementations;
using EduDev_Tracker.Features.Dashboard.ViewModels;
using EduDev_Tracker.Features.Dashboard.Views;
using EduDev_Tracker.Features.Habits.ViewModels;
using EduDev_Tracker.Features.Habits.Views;
using EduDev_Tracker.Features.Tasks.ViewModels;
using EduDev_Tracker.Features.Tasks.Views;
using EduDev_Tracker.Services.Habits;
using EduDev_Tracker.Services.Navigation;
using EduDev_Tracker.Services.Notification;
using EduDev_Tracker.Services.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Plugin.LocalNotification;

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
                .UseLocalNotification()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Comfortaa.ttf", "Comfortaa");
                });

            builder.Services.AddSingleton<DatabaseService>();

            builder.Services.AddSingleton<HabitRepository>();
            builder.Services.AddSingleton<TaskRepository>();
            builder.Services.AddSingleton<NoteRepository>();
            builder.Services.AddSingleton<PomodoroRepository>();
            builder.Services.AddSingleton<CheatsheetRepository>();
            builder.Services.AddSingleton<ProfileRepository>();

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
            services.AddSingleton<IHabitService, HabitService>();
            services.AddSingleton<ITaskService, TaskService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<Services.Notification.INotificationService, NotificationService>();
            services.AddSingleton<RecurrenceProcessorService>();

            services.AddTransient<CreateHabitViewModel>();
            services.AddTransient<CreateHabitPage>();

            services.AddTransient<HabitsPage>();
            services.AddTransient<HabitsViewModel>();

            services.AddTransient<HabitDetailsPage>();
            services.AddTransient<HabitDetailsViewModel>();

            services.AddTransient<ArchivedHabitsPage>();
            services.AddTransient<ArchivedHabitsViewModel>();

            services.AddTransient<DashboardPage>();
            services.AddTransient<DashboardViewModel>();

            services.AddTransient<TasksPage>();
            services.AddTransient<TasksViewModel>();

            services.AddTransient<AddTaskPage>();
            services.AddTransient<AddTaskViewModel>();

            services.AddTransient<TaskDetailsPage>();
            services.AddTransient<TaskDetailsViewModel>();
        }
    }
}