
let connection_Battery_Hub;
let connection_INV_Hub;
let connection_READ_DATA_Hub;

/**
 * @abstract Battery_setting page's dataHub and some helpful functions
 * @param connection_Battery_Hub
 */
window.startBatteryHub = async function(dotNetHelper) {
    connection_Battery_Hub = new signalR.HubConnectionBuilder()
        .withUrl("/datahub")
        .withAutomaticReconnect()
        .build();

    connection_Battery_Hub.on("BatteryUpdated", function(batteryData) {
        console.log("🔔 Received BatteryUpdated:", batteryData);
        
        //更新Slider與Input
        window.Sync_Sliders_And_Inputs(batteryData);

        //更新stage chart
        window.setStage(batteryData.curveStage);
        window.drawChart_Stage();
        

        dotNetHelper.invokeMethodAsync("OnBatteryUpdated", batteryData);
    });
    await connection_Battery_Hub.start();
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
/*******************************************************/

/**
 * @abstract INV_setting page's dataHub and some helpful functions
 * @param connection_INV_Hub
 */
window.startINVHub = async function(dotNetHelper)
{
    connection_INV_Hub = new signalR.HubConnectionBuilder()
        .withUrl("/datahub")
        .withAutomaticReconnect()
        .build();
    
    connection_INV_Hub.on("INVUpdated", function(INVData){
        console.log("🔔 Received INVUpdated:", INVData);
        
        drawSinChart_Helper(INVData);
        dotNetHelper.invokeMethodAsync("OnINVUpdated", INVData);
    });
    await connection_INV_Hub.start();
    console.log("✅ SignalR connected (non-module)");
}

window.drawSinChart_Helper = function(INV_setting)
{
    let waveCount;
    let period_computed;
    let peak_computed;
    //compute period
    switch(INV_setting.acf_display_str)
    {
        case "50Hz":
            waveCount = 4;
            period_computed = 720.0 / waveCount;
            break;
        case "60Hz":
            waveCount = 5;
            period_computed = 720.0 / waveCount;
            break;
        default:
            break;
    }
    //compute peak
    switch(INV_setting.acv_display_str)
    {
        case "100VAC": peak_computed = 100; break;
        case "110VAC": peak_computed = 110; break;
        case "115VAC": peak_computed = 115; break;
        case "120VAC": peak_computed = 120; break;
        case "200VAC": peak_computed = 200; break;
        case "220VAC": peak_computed = 220; break;
        case "230VAC": peak_computed = 230; break;
        case "240VAC": peak_computed = 240; break;
        default:
            break;
    }


    //pack config
    let config = {
        canvasID : "sinChart",
        peak : peak_computed,
        period : period_computed,
        chartType : "line",
        xLabel : "角度 (°)",
        yLabel : "輸出電壓(V)",
        yMin : -250,
        yMax : 250,
        totalDegrees : 720
    }

    drawChart_Sin(config);
    
}

/**
 * @abstract READ_DATA_Hub
 * @param connection_READ_DATA_Hub
 */

window.startSignalR = async function()
{
    connection_READ_DATA_Hub = new signalR.HubConnectionBuilder()
        .withUrl("/datahub")
        .build();
    
    connection_READ_DATA_Hub.on("UpdateCommandValue", (addr, cmd, value) =>{
        console.log(`[SignalR] ${cmd}@${addr} = ${value}`);

        if (cmd === "READ_VIN") {
            document.getElementById("vin-display").textContent = value;
        }
        if (cmd === "READ_IIN"){
            document.getElementById("iin-display").textContent = value;
        }
    });

    await connection_READ_DATA_Hub.start();
    return true;
}

window.subscribeCommand = async function (addr, cmd)
{
    if(connection_READ_DATA_Hub)
    {
        await connection_READ_DATA_Hub.invoke("SubscribeCommand", cmd, addr);
    }
}
window.subscribeCommands = async function(commandList)
{
    if(!connection_READ_DATA_Hub) return;

    if(!Array.isArray(commandList))
    {
        console.error("commandList is not an array:", commandList);
        return;
    }

    for(const item of commandList)
    {
        await connection_READ_DATA_Hub.invoke("SubscribeCommand", item.cmd, item.addr);
    }
}

window.unsubscribeAllCommands = async function()
{
    if(connection_READ_DATA_Hub)
    {
        await connection_READ_DATA_Hub.stop();
        console.log("🔌 SignalR connection stopped and unsubscribed all.");
    }
}