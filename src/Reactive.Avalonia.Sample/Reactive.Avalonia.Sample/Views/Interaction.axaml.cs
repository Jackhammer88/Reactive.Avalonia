using System.Linq;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Platform.Storage;

using Reactive.Avalonia.Sample.ViewModels;

namespace Reactive.Avalonia.Sample.Views;

public partial class Interaction : ReactiveView<InteractionViewModel>
{
    public Interaction()
    {
        InitializeComponent();

        // Registering inside WhenActivated is what keeps the handler tied to this view: it goes away with the
        // control, so a view model outliving its view can never call into a dead TopLevel.
        this.WhenActivated(disposables =>
        {
            if (Design.IsDesignMode || ViewModel is null)
                return;

            ViewModel.OpenFileInteraction
                .RegisterHandler(OnOpenFileAsync)
                .DisposeWith(disposables);
        });
    }

    private async Task OnOpenFileAsync(InteractionContext<Unit, string?> context)
    {
        if (TopLevel.GetTopLevel(this) is not { } topLevel)
        {
            context.SetOutput(null);
            return;
        }

        var result = await topLevel.StorageProvider.OpenFilePickerAsync(new()
        {
            Title = "Select a file",
            AllowMultiple = false,
        });

        var file = result.FirstOrDefault();

        context.SetOutput(file is null
            ? null
            : file.TryGetLocalPath() ?? file.Name);
    }
}