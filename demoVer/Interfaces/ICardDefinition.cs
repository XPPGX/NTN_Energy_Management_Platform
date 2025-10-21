using System;
using demoVer.Models;

namespace demoVer.Interfaces;

public interface ICardDefinition
{
    CardType CardType { get; }
    Type DisplayComponent { get; }
    Type EditorComponent { get; }
    string DisplayName { get; }
    string ThumbnailPath { get; }
    string DefaultWidthClass { get; }
    CardInfo CreateDefaultCard();


}