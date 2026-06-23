using CommunityToolkit.Maui;
using EduDev_Tracker.Data;
using EduDev_Tracker.Data.Repositories.Implementations;
using EduDev_Tracker.Data.Seed;
using EduDev_Tracker.Features.Analytics.ViewModels;
using EduDev_Tracker.Features.Analytics.Views;
using EduDev_Tracker.Features.Auth.ViewModels;
using EduDev_Tracker.Features.Auth.Views;
using EduDev_Tracker.Features.Dashboard.ViewModels;
using EduDev_Tracker.Features.Dashboard.Views;
using EduDev_Tracker.Features.DevTools.ViewModels;
using EduDev_Tracker.Features.DevTools.Views;
using EduDev_Tracker.Features.Habits.ViewModels;
using EduDev_Tracker.Features.Habits.Views;
using EduDev_Tracker.Features.Notes.ViewModels;
using EduDev_Tracker.Features.Notes.Views;
using EduDev_Tracker.Features.Pomodoro.ViewModels;
using EduDev_Tracker.Features.Pomodoro.Views;
using EduDev_Tracker.Features.Profile.ViewModels;
using EduDev_Tracker.Features.Profile.Views;
using EduDev_Tracker.Features.Tasks.ViewModels;
using EduDev_Tracker.Features.Tasks.Views;
using EduDev_Tracker.Services.Analytics;
using EduDev_Tracker.Services.Audio;
using EduDev_Tracker.Services.Auth;
using EduDev_Tracker.Services.Converters;
using EduDev_Tracker.Services.Habits;
using EduDev_Tracker.Services.Navigation;
using EduDev_Tracker.Services.Notes;
using EduDev_Tracker.Services.Notification;
using EduDev_Tracker.Services.Pomodoro;
using EduDev_Tracker.Services.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.LifecycleEvents;
using Plugin.LocalNotification;
using Plugin.Maui.Audio;

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
            builder.Services.AddSingleton<ConversionRepository>();
            builder.Services.AddSingleton<ReminderRepository>();
            builder.Services.AddSingleton<DemoDataSeeder>();
            builder.Services.AddSingleton<ActivitySeeder>();
            builder.Services.AddSingleton(AudioManager.Current);
            builder.Services.AddSingleton<BackgroundTimerService>();

            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoBorderEntry", (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.SetBackground(null);

                var states = new int[][]
                {
        new int[] {  Android.Resource.Attribute.StateFocused  },
        new int[] { -Android.Resource.Attribute.StateFocused  },
        new int[] { }
                };
                var colors = new int[]
                {
        Android.Graphics.Color.Transparent,
        Android.Graphics.Color.Transparent,
        Android.Graphics.Color.Transparent
                };
                handler.PlatformView.BackgroundTintList =
                    new Android.Content.Res.ColorStateList(states, colors);

#elif IOS || MACCATALYST
    handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
    handler.PlatformView.Layer.BorderWidth = 0;
    handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;

#elif WINDOWS
    handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
    // Убираем фон и подсветку при фокусе
    handler.PlatformView.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
        Microsoft.UI.Colors.Transparent);
    handler.PlatformView.FocusVisualPrimaryThickness = new Microsoft.UI.Xaml.Thickness(0);
    handler.PlatformView.FocusVisualSecondaryThickness = new Microsoft.UI.Xaml.Thickness(0);
#endif
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
            services.AddSingleton<IOAuthService, OAuthService>();
            services.AddSingleton<IHabitService, HabitService>();
            services.AddSingleton<ITaskService, TaskService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<Services.Notification.INotificationService, NotificationService>();
            services.AddSingleton<RecurrenceProcessorService>();
            services.AddSingleton<INotesService, NotesService>();
            services.AddSingleton<IAuthService,  AuthService>();
            services.AddSingleton<IPomodoroService, PomodoroService>();
            services.AddSingleton<IAudioService, AudioService>();
            services.AddSingleton<IConversionService,  ConversionService>();
            services.AddSingleton<IAnalyticsService, AnalyticsService>();

            services.AddTransient<CreateHabitViewModel>();
            services.AddTransient<CreateHabitPage>();

            services.AddTransient<HabitsPage>();
            services.AddTransient<HabitsViewModel>();

            services.AddTransient<HabitDetailsPage>();
            services.AddTransient<HabitDetailsViewModel>();

            services.AddTransient<ArchivedHabitsPage>();
            services.AddTransient<ArchivedHabitsViewModel>();

            services.AddTransient<HabitDayDetailsPage>();
            services.AddTransient<HabitDayDetailsViewModel>();

            services.AddTransient<DashboardPage>();
            services.AddTransient<DashboardViewModel>();

            services.AddTransient<TasksPage>();
            services.AddTransient<TasksViewModel>();

            services.AddTransient<AddTaskPage>();
            services.AddTransient<AddTaskViewModel>();

            services.AddTransient<ReminderSettingsPage>();
            services.AddTransient<ReminderSettingsViewModel>();

            services.AddTransient<TaskDetailsPage>();
            services.AddTransient<TaskDetailsViewModel>();

            services.AddTransient<NotesViewModel>();
            services.AddTransient<NotesPage>();

            services.AddTransient<VersionsPopup>();

            services.AddTransient<AuthViewModel>();
            services.AddTransient<AuthPage>();

            services.AddTransient<ProfileViewModel>();
            services.AddTransient<ProfilePage>();

            services.AddTransient<PomodoroViewModel>();
            services.AddTransient<PomodoroPage>();
            services.AddTransient<TaskPickerBottomSheet>();
            services.AddTransient<CreatePresetViewModel>();
            services.AddTransient<CreatePresetPage>();

            services.AddTransient<ConvertersViewModel>();
            services.AddTransient<ConvertersPage>();

            services.AddTransient<AnalyticsViewModel>();
            services.AddTransient<AnalyticsPage>();
        }
    }
}