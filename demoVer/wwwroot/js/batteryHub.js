
let connection;

window.startBatteryHub = async function(dotNetHelper) {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/datahub")
        .withAutomaticReconnect()
        .build();

    connection.on("BatteryUpdated", function(batteryData) {
        console.log("🔔 Received BatteryUpdated:", batteryData);
        
        //更新Slider與Input
        window.Sync_Sliders_And_Inputs(batteryData);

        //更新stage chart
        window.setStage(batteryData.curveStage);
        window.drawChart_Stage();
        

        dotNetHelper.invokeMethodAsync("OnBatteryUpdated", batteryData);
    });

    await connection.start();
    console.log("✅ SignalR connected (non-module)");
}

window.Sync_Sliders_And_Inputs = function(batteryData)
{
    //更新資料(Slider, input, stageChart 都會參照這邊的資料)
    window.sliderBars_Info["CC"].currentVal = batteryData.cc_Display;
    window.sliderBars_Info["TC"].currentVal = batteryData.tc_Display;
    window.sliderBars_Info["CV"].currentVal = batteryData.cv_Display;
    window.sliderBars_Info["FV"].currentVal = batteryData.fv_Display;
    
    //更新UI
    window.SyncSliderUI("CC", 0);
    window.SyncSliderUI("TC", 1);
    window.SyncSliderUI("CV", 2);
    window.SyncSliderUI("FV", 3);

}

window.SyncSliderUI = function(label, index)
{
    const info = sliderBars_Info[label];

    const minValue = info.selfMin;
    const displayMax = info.UI_max;
    // const clampedValue = Math.min(Math.max(info.currentVal, minValue), displayMax);

    // const step = 0.1;
    // const value = Math.round(clampedValue / step) * step;
    // const send_value = Math.round(value * 10) / 10;
    // console.log(send_value);

    //不用計算 Final_Val，因為在別的web送出資料時，就是計算完的(CC, TC, CV, FV正確數值)

    //更新 slider UI視覺
    const percent = (info.currentVal / displayMax) * 100;
    info.fill.style.height = `${percent}%`;
    info.thumb.style.bottom = `${percent}%`;
    
    //更新 input 數值
    // window.updateInputFromSlider(index, info.currentVal);
    
    const input = document.getElementById(`slider-input-${index}`);
    console.log(input);
    if(input)
    {
        input.value = info.currentVal.toFixed(1);
    }
}