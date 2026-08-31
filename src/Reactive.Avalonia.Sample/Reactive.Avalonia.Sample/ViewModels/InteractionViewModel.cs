using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Reactive.Avalonia.Sample.ViewModels;

public class InteractionViewModel : TabItemViewModelBase
{
    public InteractionViewModel()
    {
        SelectFileCommand = ReactiveCommand.CreateFromTask(SelectFileExecuteAsync);
    }

    public Interaction<Unit, string?> OpenFileInteraction { get; } = new();

    public ReactiveCommand<Unit, Unit> SelectFileCommand { get; }

    public string SelectedFile
    {
        get;
        private set => RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public override string Title => "Interaction";

    private async Task SelectFileExecuteAsync()
    {
        SelectedFile = await OpenFileInteraction.Handle(Unit.Default) ?? string.Empty;
    }
}