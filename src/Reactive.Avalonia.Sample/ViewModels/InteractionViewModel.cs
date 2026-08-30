using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Reactive.Avalonia.Sample.ViewModels;

public class InteractionViewModel : TabItemViewModelBase
{
    public InteractionViewModel()
    {
        SelectFolderCommand = ReactiveCommand.CreateFromTask(SelectFolderExecuteAsync);
    }

    public Interaction<Unit, string?> OpenFolderInteraction { get; } = new();

    public ReactiveCommand<Unit, Unit> SelectFolderCommand { get; }

    public string SelectedFolder
    {
        get;
        private set => RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public override string Title => "Interaction";

    private async Task SelectFolderExecuteAsync()
    {
        SelectedFolder = await OpenFolderInteraction.Handle(Unit.Default) ?? string.Empty;
    }
}