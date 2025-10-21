using System;
using System.Collections.Generic;
using demoVer.Interfaces;
using demoVer.Models;
using demoVer.Shared;

namespace demoVer.Services;

public sealed class RunningDiagramCardDefinition : ICardDefinition
{
    public CardType CardType => CardType.Status;
    public Type DisplayComponent => typeof(RunningDiagramCard);
    public Type EditorComponent => typeof(RunningDiagramCardEdit);
    public string DisplayName => "連接狀態";
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
