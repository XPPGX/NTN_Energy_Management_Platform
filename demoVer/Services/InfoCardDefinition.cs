using System;
using System.Collections.Generic;
using demoVer.Interfaces;
using demoVer.Models;
using demoVer.Shared;

namespace demoVer.Services;

public sealed class InfoCardDefinition : ICardDefinition
{
    public CardType CardType => CardType.Info;
    public Type DisplayComponent => typeof(BlankInfoCard);
    public string DefaultWidthClass => "w25";

    public CardInfo CreateDefaultCard() => new()
    {
        card_Type = CardType.Info,
        widthClass = DefaultWidthClass,
        cardName = string.Empty,
        Info_DataItems = new List<INFO_DataItem>()
    };
}
