using System;
using System.Collections.Generic;
using demoVer.Interfaces;
using demoVer.Models;
using demoVer.Resources;
using demoVer.Shared;
using Microsoft.Extensions.Localization;

namespace demoVer.Services;

public sealed class RunningDiagramCardDefinition : ICardDefinition
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RunningDiagramCardDefinition(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public CardType CardType => CardType.Status;
    public Type DisplayComponent => typeof(RunningDiagramCard);
    public Type EditorComponent => typeof(RunningDiagramCardEdit);
    public string DisplayName => _localizer["CardType-Status"];
    public string ThumbnailPath => "images/cardSelection-status.png";
    public string DefaultWidthClass => "w50";

    public CardInfo CreateDefaultCard() => new()
    {
        card_Type = CardType.Status,
        widthClass = DefaultWidthClass,
        cardName = "RunningDiagram",
        Link_STATUS_Pairs = new List<LINK_STATUS_Pair>
        {
            new() { iconPath = "images/user_selections/transmission-tower.png", Label = string.Empty },
            new() { iconPath = "images/user_selections/fridge.png", Label = string.Empty },
            new() { iconPath = "images/user_selections/plug.png", Label = string.Empty },
            new() { iconPath = "images/user_selections/car-battery.png", Label = string.Empty }
        }
    };
}
