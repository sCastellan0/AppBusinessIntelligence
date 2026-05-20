using Windows.Storage.Pickers;
using WinRT.Interop;

namespace TFG_APPBusinessIntelligence.Services
{
    public class FolderPickerService : IFolderPickerService
    {
        public async Task<string?> PickFolderAsync()
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");

            var hwnd = ((MauiWinUIWindow)Application.Current!.Windows[0].Handler.PlatformView!).WindowHandle;
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            return folder?.Path;
        }
    }
}
