using System;
using System.Collections.Generic;
using demoVer.Interfaces;
using demoVer.Models;
using demoVer.Shared;
using Microsoft.AspNetCore.Components;

namespace demoVer.Services;

public sealed class InfoCardDefinition : ICardDefinition
{
    public CardType CardType => CardType.Info;
    public Type DisplayComponent => typeof(BlankInfoCard);
    public Type EditorComponent => typeof(InfoCardEdit);
    public string DisplayName => "資訊";
    public string ThumbnailPath => "images/cardSelection-info.png";
    public string DefaultWidthClass => "w25";

    public CardInfo CreateDefaultCard() => new()
    {
        card_Type = CardType.Info,
        widthClass = DefaultWidthClass,
        cardName = string.Empty,
        Info_DataItems = new List<INFO_DataItem>()
    };


}
