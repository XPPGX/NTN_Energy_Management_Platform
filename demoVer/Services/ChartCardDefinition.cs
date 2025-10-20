using System;
using System.Collections.Generic;
using demoVer.Interfaces;
using demoVer.Models;
using demoVer.Shared;

namespace demoVer.Services;

public sealed class ChartCardDefinition : ICardDefinition
{
    public CardType CardType => CardType.Chart;
    public Type DisplayComponent => typeof(BlankChartCard);
    public string DisplayName => "圖表";
    public string ThumbnailPath => "images/cardSelection-chart.png";
    public string DefaultWidthClass => "w50";

    public CardInfo CreateDefaultCard()
    {
        var chartSetting = new CHART_SETTING
        {
            canvasID = $"Chart_{Guid.NewGuid():N}",
            ChartTitle = "NewChart",
            chart_single_data_lines = new List<CHART_SINGLE_DATA_LINE>()
        };

        return new CardInfo
        {
            card_Type = CardType.Chart,
            widthClass = DefaultWidthClass,
            cardName = chartSetting.ChartTitle,
            Chart_Setting = chartSetting
        };
    }
}
