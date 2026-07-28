using Library.Light;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Library.SpeedDice;

public sealed class LibrarySpeedDiceRegistrationBuilder<TCharacter>
    where TCharacter : CharacterModel
{
    private readonly string _id;
    private readonly LibrarySpeedDiceOptions _options;
    private readonly List<ILibrarySpeedDiceModule> _modules = [];
    private LibraryEmotionConfig _emotion = new();
    private LibraryLightOptions? _light;
    private LibraryLightStoreFactory? _lightStoreFactory;
    private bool _registered;

    internal LibrarySpeedDiceRegistrationBuilder(
        string id,
        LibrarySpeedDiceOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(options);
        _id = id;
        _options = options;
    }

    public LibrarySpeedDiceRegistrationBuilder<TCharacter> WithEmotion(
        LibraryEmotionConfig emotion)
    {
        ArgumentNullException.ThrowIfNull(emotion);
        EnsureNotRegistered();
        _emotion = emotion;
        return this;
    }

    public LibrarySpeedDiceRegistrationBuilder<TCharacter> WithLight(
        LibraryLightOptions options,
        LibraryLightStoreFactory? storeFactory = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        EnsureNotRegistered();
        _light = options;
        _lightStoreFactory = storeFactory;
        return this;
    }

    public LibrarySpeedDiceRegistrationBuilder<TCharacter> UseModule(
        ILibrarySpeedDiceModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        EnsureNotRegistered();
        _modules.Add(module);
        return this;
    }

    public void Register()
    {
        EnsureNotRegistered();

        Func<Player, bool> predicate =
            static player => player.Character is TCharacter;
        LibraryEmotionConfig frozenEmotion =
            LibrarySpeedDiceRegistration.FreezeEmotion(_emotion);
        var compatibilityParticipant = new LibrarySpeedDiceParticipant
        {
            Id = _id,
            IsEnabledForPlayer = predicate,
            BaseSpeedDiceCount = _options.BaseCount,
            MinSpeed = _options.MinRoll,
            MaxSpeed = _options.MaxRoll,
            Emotion = frozenEmotion,
        };
        var registration = new LibrarySpeedDiceRegistration(
            _id,
            predicate,
            _options,
            frozenEmotion,
            _light,
            _lightStoreFactory,
            _modules,
            compatibilityParticipant);
        LibrarySpeedDiceService.RegisterRegistration(
            registration,
            replaceExisting: false);
        _registered = true;
    }

    private void EnsureNotRegistered()
    {
        if (_registered)
        {
            throw new InvalidOperationException(
                $"Speed-dice registration '{_id}' has already been registered.");
        }
    }
}
