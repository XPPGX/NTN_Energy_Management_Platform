using System;
using System.Collections.Generic;
using System.Linq;
using demoVer.Interfaces;
using demoVer.Models;

namespace demoVer.Services;

public interface ICardDefinitionRegistry
{
    IReadOnlyCollection<ICardDefinition> Definitions { get; }
    bool TryGet(CardType cardType, out ICardDefinition? definition);
    ICardDefinition Get(CardType cardType);
}

public sealed class CardDefinitionRegistry : ICardDefinitionRegistry
{
    private readonly IReadOnlyDictionary<CardType, ICardDefinition> _definitions;
    private readonly IReadOnlyCollection<ICardDefinition> _allDefinitions;

    // 當某個服務的建構子參數是IEnumerable<T>時，DI 會自動注入所有已註冊的 T 實作。\
    // 所以這裡會把所有在Program.cs註冊的 ICardDefinition 實作注入進來
    public CardDefinitionRegistry(IEnumerable<ICardDefinition> definitions)
    {
        if (definitions is null)
        {
            throw new ArgumentNullException(nameof(definitions));
        }

        var dict = new Dictionary<CardType, ICardDefinition>();
        foreach (var definition in definitions)
        {
            if (!dict.TryAdd(definition.CardType, definition))
            {
                throw new InvalidOperationException($"Duplicate card definition registered for type '{definition.CardType}'.");
            }
        }

        _definitions = dict;
        _allDefinitions = Array.AsReadOnly(dict.Values.ToArray());
    }

    public IReadOnlyCollection<ICardDefinition> Definitions => _allDefinitions;

    public bool TryGet(CardType cardType, out ICardDefinition? definition) => _definitions.TryGetValue(cardType, out definition);

    public ICardDefinition Get(CardType cardType)
    {
        if (!_definitions.TryGetValue(cardType, out var definition))
        {
            throw new KeyNotFoundException($"Card definition for type '{cardType}' was not found.");
        }

        return definition;
    }
}
