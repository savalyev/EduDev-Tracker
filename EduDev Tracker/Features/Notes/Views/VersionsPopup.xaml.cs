using CommunityToolkit.Maui.Views;
using EduDev_Tracker.Data.Models;

namespace EduDev_Tracker.Features.Notes.Views;

public partial class VersionsPopup : Popup
{

    private readonly TaskCompletionSource<NoteVersion?> _tcs = new();

    public Task<NoteVersion?> Result => _tcs.Task;
    public VersionsPopup(List<NoteVersion> versions)
	{
		InitializeComponent();
		VersionsList.ItemsSource = versions;
	}


    private async void OnRestoreClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is NoteVersion version)
        {
            _tcs.TrySetResult(version);
            await CloseAsync();
        }
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        await CloseAsync();
    }

}