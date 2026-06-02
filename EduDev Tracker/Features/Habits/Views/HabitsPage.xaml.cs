using EduDev_Tracker.Features.Habits.ViewModels;

namespace EduDev_Tracker.Features.Habits.Views;

public partial class HabitsPage : ContentPage
{
    private readonly HabitsViewModel _vm;
    public HabitsPage(HabitsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _vm = viewModel;

        _vm.Habits.CollectionChanged += OnHabitsCollectionChanged;

#if WINDOWS
        HabitsCollectionView.ChildAdded += OnHabitCardAdded;
#endif

    }

    private bool _isFading = false;

    private async void OnHabitsCollectionChanged(object sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
        {
            _isFading = true;
            await HabitsCollectionView.FadeToAsync(0, 120);
        }
        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add
                 && _isFading && _vm.Habits.Count > 0)
        {
            _isFading = false;
            await HabitsCollectionView.FadeToAsync(1, 180);
        }
    }

    private void OnMenuTapped(object? sender, EventArgs e)
    {
        if (Shell.Current != null)
        {
            Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if(BindingContext is HabitsViewModel vm)
        {
            vm.LoadCommand.Execute(null);
        }
    }

#if WINDOWS
    private void OnHabitCardAdded(object sender, ElementEventArgs e)
    {
        if (e.Element is Border card)
        {
            var pointer = new PointerGestureRecognizer();
            pointer.PointerEntered += OnCardPointerEntered;
            pointer.PointerExited += OnCardPointerExited;
            card.GestureRecognizers.Add(pointer);
        }
    }

    private void OnCardPointerEntered(object sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer { Parent: Border card })
        {
            card.ScaleToAsync(1.02, 150, Easing.CubicOut);
            card.Stroke = new SolidColorBrush(Color.FromArgb("#886EE7B7"));
        }
    }

    private void OnCardPointerExited(object sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer { Parent: Border card })
        {
            card.ScaleToAsync(1.0, 150, Easing.CubicOut);
            card.Stroke = new SolidColorBrush(Color.FromArgb("#495061"));
        }
    }
#endif
}