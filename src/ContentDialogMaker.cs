using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

public static class ContentDialogMaker
{
    public static async void CreateContentDialog(ContentDialog dialog, bool awaitPreviousDialog)
    {
        await CreateDialog(dialog, awaitPreviousDialog);
    }

    public static async Task CreateContentDialogAsync(ContentDialog dialog, bool awaitPreviousDialog)
    {
        await CreateDialog(dialog, awaitPreviousDialog);
    }

    static async Task CreateDialog(ContentDialog dialog, bool awaitPreviousDialog)
    {
        if (ActiveDialog != null)
        {
            if (awaitPreviousDialog)
            {
                await DialogAwaiter.Task;
                DialogAwaiter = new TaskCompletionSource<bool>();
            }
            else
            {
                ActiveDialog.Hide();
            }
        }
        ActiveDialog = dialog;
        ActiveDialog.Closed += ActiveDialog_Closed;
        await ActiveDialog.ShowAsync();
        ActiveDialog.Closed -= ActiveDialog_Closed;
    }

    public static ContentDialog ActiveDialog;
    static TaskCompletionSource<bool> DialogAwaiter = new TaskCompletionSource<bool>();

    private static void ActiveDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        DialogAwaiter.SetResult(true);
    }
}