using System;
using System.Collections.Generic;
using demoVer.Interfaces;
using demoVer.Models;
using demoVer.Resources;
using demoVer.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace demoVer.Services;

public sealed class InfoCardDefinition : ICardDefinition
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public InfoCardDefinition(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public CardType CardType => CardType.Info;
    public Type DisplayComponent => typeof(BlankInfoCard);
    public Type EditorComponent => typeof(InfoCardEdit);
    public string DisplayName => _localizer["CardType-Info"];
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
