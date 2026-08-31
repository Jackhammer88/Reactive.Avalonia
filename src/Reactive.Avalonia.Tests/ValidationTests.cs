using System.Collections;
using System.ComponentModel;

namespace Reactive.Avalonia.Tests;

/// <summary>
/// Covers <see cref="ReactiveValidationObject"/>, <see cref="ValidationContext"/> and
/// <see cref="ValidationMixins"/>.
/// </summary>
/// <remarks>
/// ReactiveUI keeps validation in a separate repository, so these are written against this library's own
/// behaviour rather than ported.
/// </remarks>
[TestFixture]
public class ValidationTests : ReactiveTestBase
{
    [Test]
    public void ARuleFailsWhenItsPredicateFails()
    {
        using var viewModel = new PersonViewModel();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasErrors, Is.True);
            Assert.That(Errors(viewModel, nameof(PersonViewModel.Name)), Is.EqualTo(new[] { "Name is required." }));
        });
    }

    [Test]
    public void ARuleClearsOnceTheValueIsAcceptable()
    {
        using var viewModel = new PersonViewModel();

        viewModel.Name = "Alice";

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasErrors, Is.False);
            Assert.That(Errors(viewModel, nameof(PersonViewModel.Name)), Is.Empty);
        });
    }

    [Test]
    public void SeveralRulesOnOnePropertyAllReport()
    {
        using var viewModel = new PersonViewModel();

        viewModel.Name = "a name that is far too long to be acceptable";

        Assert.That(Errors(viewModel, nameof(PersonViewModel.Name)), Is.EqualTo(new[] { "Name is too long." }));
    }

    [Test]
    public void MessagesCanBeBuiltFromTheValue()
    {
        using var viewModel = new MessageFromValueViewModel();

        viewModel.Value = "abcd";

        Assert.That(Errors(viewModel, nameof(MessageFromValueViewModel.Value)), Is.EqualTo(new[] { "4 is too many." }));
    }

    [Test]
    public void ErrorsChangedNamesTheAffectedProperty()
    {
        using var viewModel = new PersonViewModel();
        var affected = new List<string?>();
        ((INotifyDataErrorInfo)viewModel).ErrorsChanged += (_, args) => affected.Add(args.PropertyName);

        viewModel.Name = "Alice";

        Assert.That(affected, Is.All.EqualTo(nameof(PersonViewModel.Name)));
        Assert.That(affected, Is.Not.Empty);
    }

    [Test]
    public void HasErrorsRaisesAChangeNotification()
    {
        using var viewModel = new PersonViewModel();
        var raised = new List<string?>();
        viewModel.Changed.Subscribe(args => raised.Add(args.PropertyName));

        viewModel.Name = "Alice";

        Assert.That(raised, Does.Contain(nameof(PersonViewModel.HasErrors)));
    }

    [Test]
    public void GetErrorsWithoutAPropertyNameReturnsEverything()
    {
        using var viewModel = new PersonViewModel();

        Assert.That(Errors(viewModel, null), Is.EqualTo(new[] { "Name is required." }));
    }

    [Test]
    public void ARuleCanSpanSeveralProperties()
    {
        using var viewModel = new PasswordViewModel();

        viewModel.Password = "secret";
        viewModel.Confirmation = "different";

        Assert.That(
            Errors(viewModel, nameof(PasswordViewModel.Confirmation)),
            Is.EqualTo(new[] { "The passwords do not match." }));

        viewModel.Confirmation = "secret";

        Assert.That(viewModel.HasErrors, Is.False);
    }

    [Test]
    public void AModelWideRuleReportsAgainstNoProperty()
    {
        using var viewModel = new ModelWideViewModel();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasErrors, Is.True);
            Assert.That(Errors(viewModel, null), Is.EqualTo(new[] { "Something is wrong." }));
            Assert.That(
                Errors(viewModel, nameof(ModelWideViewModel.Value)),
                Is.Empty,
                "Entity-level errors belong to the object, not to any one property, which is what "
                + "INotifyDataErrorInfo consumers expect.");
        });
    }

    [Test]
    public void ARuleCanTakeItsOutcomeAndMessageFromASequence()
    {
        using var viewModel = new PolicyViewModel();

        Assert.That(viewModel.HasErrors, Is.False, "An empty password is not checked against the policy yet.");

        viewModel.Password = "short";
        Assert.That(
            Errors(viewModel, nameof(PolicyViewModel.Password)),
            Is.EqualTo(new[] { "5 characters is not enough." }),
            "The message comes from the state, so it can describe why this particular value failed.");

        viewModel.Password = "short!";
        Assert.That(
            Errors(viewModel, nameof(PolicyViewModel.Password)),
            Is.EqualTo(new[] { "6 characters is not enough." }));

        viewModel.Password = "long enough";
        Assert.That(viewModel.HasErrors, Is.False);
    }

    [Test]
    public void ASequenceBackedRuleFollowsEveryPropertyItDependsOn()
    {
        using var viewModel = new PolicyViewModel();
        viewModel.Password = "short";
        Assert.That(viewModel.HasErrors, Is.True);

        // The rule only applies in one mode, and switching mode has to re-evaluate it.
        viewModel.Mode = PolicyViewModel.SetupMode.Existing;

        Assert.That(viewModel.HasErrors, Is.False);
    }

    [Test]
    public void AModelWideRuleCanTakeItsMessageFromASequence()
    {
        var states = new BehaviorSubject<ValidationState>(new ValidationState(false, "Not ready."));
        using var viewModel = new SequenceOnlyViewModel(states);

        Assert.That(Errors(viewModel, null), Is.EqualTo(new[] { "Not ready." }));

        states.OnNext(ValidationState.Valid);
        Assert.That(viewModel.HasErrors, Is.False);
    }

    [Test]
    public void ValidationStateAcceptsASingleMessage()
    {
        var state = new ValidationState(false, "Nope.");

        Assert.Multiple(() =>
        {
            Assert.That(state.IsValid, Is.False);
            Assert.That(state.Messages, Is.EqualTo(new[] { "Nope." }));
        });
    }

    [Test]
    public void TheContextRaisesChangeNotificationsForIsValid()
    {
        using var viewModel = new PersonViewModel();
        var raised = new List<string?>();
        viewModel.ValidationContext.Changed.Subscribe(args => raised.Add(args.PropertyName));

        viewModel.Name = "Alice";

        Assert.That(raised, Does.Contain(nameof(ValidationContext.IsValid)));
    }

    [Test]
    public void TheContextRaisesChangeNotificationsForText()
    {
        using var viewModel = new PersonViewModel();
        var raised = new List<string?>();
        viewModel.ValidationContext.Changed.Subscribe(args => raised.Add(args.PropertyName));

        viewModel.Name = "Alice";

        Assert.That(raised, Does.Contain(nameof(ValidationContext.Text)));
    }

    [Test]
    public void TheContextIsObservableWithWhenAnyValue()
    {
        // Code carried over from ReactiveUI observes the context directly. Before the context raised
        // notifications this produced one value and then sat silent, which looked like nothing was wrong.
        using var viewModel = new PersonViewModel();
        var seen = new List<bool>();

        using (viewModel.ValidationContext.WhenAnyValue(x => x.IsValid).Subscribe(seen.Add))
        {
            viewModel.Name = "Alice";
            viewModel.Name = string.Empty;
        }

        Assert.That(seen, Is.EqualTo(new[] { false, true, false }));
    }

    [Test]
    public void TheContextDoesNotRaiseWhenNothingChanged()
    {
        using var viewModel = new PersonViewModel();
        viewModel.Name = "Alice";

        var raised = new List<string?>();
        viewModel.ValidationContext.Changed.Subscribe(args => raised.Add(args.PropertyName));

        viewModel.Name = "Bob";

        Assert.That(raised, Is.Empty, "Both names are valid, so neither IsValid nor Text moved.");
    }

    [Test]
    public void AHelperRaisesChangeNotifications()
    {
        using var viewModel = new RemovableRuleViewModel();
        var raised = new List<string?>();
        viewModel.Rule.Changed.Subscribe(args => raised.Add(args.PropertyName));

        viewModel.Value = "set";

        Assert.Multiple(() =>
        {
            Assert.That(raised, Does.Contain(nameof(ValidationHelper.IsValid)));
            Assert.That(raised, Does.Contain(nameof(ValidationHelper.Message)));
        });
    }

    [Test]
    public void IsValidDrivesACommandsCanExecute()
    {
        using var viewModel = new PersonViewModel();
        var canExecute = false;
        using var command = ReactiveCommand.Create(static () => { }, viewModel.IsValid());
        command.CanExecute.Subscribe(value => canExecute = value);

        Assert.That(canExecute, Is.False);

        viewModel.Name = "Alice";
        Assert.That(canExecute, Is.True);

        viewModel.Name = string.Empty;
        Assert.That(canExecute, Is.False);
    }

    [Test]
    public void TheContextExposesEveryFailingMessage()
    {
        using var viewModel = new PasswordViewModel();
        viewModel.Password = "a";
        viewModel.Confirmation = "b";

        Assert.That(viewModel.ValidationContext.Text, Does.Contain("The passwords do not match."));
    }

    [Test]
    public void DisposingARuleRemovesIt()
    {
        using var viewModel = new RemovableRuleViewModel();

        Assert.That(viewModel.HasErrors, Is.True);

        viewModel.Rule.Dispose();

        Assert.That(viewModel.HasErrors, Is.False);
    }

    [Test]
    public void AHelperReportsItsOwnState()
    {
        using var viewModel = new RemovableRuleViewModel();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Rule.IsValid, Is.False);
            Assert.That(viewModel.Rule.Message, Is.EqualTo("Value is required."));
        });

        viewModel.Value = "set";

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Rule.IsValid, Is.True);
            Assert.That(viewModel.Rule.Message, Is.Empty);
        });
    }

    [Test]
    public void ValidationRuleRejectsAChainedExpression()
    {
        using var viewModel = new PersonViewModel();

        Assert.Throws<ArgumentException>(() =>
            viewModel.ValidationRule(static x => x.Nested!.Name, static _ => true, "nope"));
    }

    private static string[] Errors(INotifyDataErrorInfo viewModel, string? propertyName) =>
        [.. ((IEnumerable)viewModel.GetErrors(propertyName)).Cast<string>()];

    private sealed class PersonViewModel : ReactiveValidationObject
    {
        private string _name = string.Empty;

        public PersonViewModel()
        {
            this.ValidationRule(x => x.Name, static name => !string.IsNullOrWhiteSpace(name), "Name is required.");
            this.ValidationRule(x => x.Name, static name => name.Length <= 10, "Name is too long.");
        }

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public PersonViewModel? Nested => null;
    }

    private sealed class MessageFromValueViewModel : ReactiveValidationObject
    {
        private string _value = string.Empty;

        public MessageFromValueViewModel() =>
            this.ValidationRule(
                x => x.Value,
                static value => value.Length < 4,
                static value => $"{value.Length} is too many.");

        public string Value
        {
            get => _value;
            set => this.RaiseAndSetIfChanged(ref _value, value);
        }
    }

    private sealed class PasswordViewModel : ReactiveValidationObject
    {
        private string _password = string.Empty;
        private string _confirmation = string.Empty;

        public PasswordViewModel() =>
            this.ValidationRule(
                x => x.Confirmation,
                this.WhenAnyValue(x => x.Password, x => x.Confirmation, static (a, b) => a == b),
                "The passwords do not match.");

        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }

        public string Confirmation
        {
            get => _confirmation;
            set => this.RaiseAndSetIfChanged(ref _confirmation, value);
        }
    }

    private sealed class PolicyViewModel : ReactiveValidationObject
    {
        private SetupMode _mode = SetupMode.New;
        private string _password = string.Empty;

        public PolicyViewModel()
        {
            var policy = this.WhenAnyValue(
                x => x.Mode,
                x => x.Password,
                static (mode, password) =>
                {
                    if (mode != SetupMode.New || string.IsNullOrWhiteSpace(password))
                    {
                        return ValidationState.Valid;
                    }

                    return password.Length >= 8
                        ? ValidationState.Valid
                        : new ValidationState(false, $"{password.Length} characters is not enough.");
                });

            this.ValidationRule(x => x.Password, policy);
        }

        public enum SetupMode
        {
            New,
            Existing,
        }

        public SetupMode Mode
        {
            get => _mode;
            set => this.RaiseAndSetIfChanged(ref _mode, value);
        }

        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }
    }

    private sealed class SequenceOnlyViewModel : ReactiveValidationObject
    {
        public SequenceOnlyViewModel(IObservable<ValidationState> states) => this.ValidationRule(states);
    }

    private sealed class ModelWideViewModel : ReactiveValidationObject
    {
        public ModelWideViewModel() =>
            this.ValidationRule(Observable.Return(false), "Something is wrong.");

        public string Value => string.Empty;
    }

    private sealed class RemovableRuleViewModel : ReactiveValidationObject
    {
        private string _value = string.Empty;

        public RemovableRuleViewModel() =>
            Rule = this.ValidationRule(
                x => x.Value,
                static value => !string.IsNullOrEmpty(value),
                "Value is required.");

        public ValidationHelper Rule { get; }

        public string Value
        {
            get => _value;
            set => this.RaiseAndSetIfChanged(ref _value, value);
        }
    }
}