using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Reactive.Avalonia.Sample.ViewModels;

public class CommandViewModel : TabItemViewModelBase
{
    private CancellationTokenSource? _cts;
    private readonly CompositeDisposable _disposables = new();

    public CommandViewModel()
    {
        RunJobCommand = ReactiveCommand.CreateFromTask<decimal>(RunJobExecuteAsync);
        CancelJobCommand = ReactiveCommand.Create(CancelJobExecute, RunJobCommand.IsExecuting);

        _jobStatus = RunJobCommand.IsExecuting
            .Select(isExecuting => isExecuting ? "Executing" : "Stopped")
            .ToProperty(this, vm => vm.JobStatus, out _jobStatus);

        this.WhenAnyValue(vm => vm.IsActive)
            .Where(isActive => !isActive)
            .Subscribe(_ => _cts?.Cancel())
            .DisposeWith(_disposables);
    }

    public ReactiveCommand<decimal, Unit> RunJobCommand { get; }

    public ReactiveCommand<Unit, Unit> CancelJobCommand { get; }

    private readonly ObservableAsPropertyHelper<string> _jobStatus;
    public string JobStatus => _jobStatus.Value;

    public override string Title => "Command";

    private async Task RunJobExecuteAsync(
        decimal delaySeconds,
        CancellationToken commandToken)
    {
        _cts?.Cancel();

        var cts = CancellationTokenSource.CreateLinkedTokenSource(commandToken);
        _cts = cts;

        try
        {
            var seconds = int.CreateSaturating(delaySeconds);

            await Task.Delay(
                TimeSpan.FromSeconds(seconds),
                cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
        }
        finally
        {
            cts.Dispose();

            if (ReferenceEquals(_cts, cts))
                _cts = null;
        }
    }

    private void CancelJobExecute()
    {
        _cts?.Cancel();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _disposables.Dispose();
            _jobStatus.Dispose();

            RunJobCommand.Dispose();
            CancelJobCommand.Dispose();
        }

        base.Dispose(disposing);
    }
}