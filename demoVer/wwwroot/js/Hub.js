
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
    // let RcvQueue = [];
    connection_READ_DATA_Hub = new signalR.HubConnectionBuilder()
        .withUrl("/datahub")
        .build();
    
    // window.INV_Info_Translator.Single.clearTempData();
    
    connection_READ_DATA_Hub.off("UpdateCommandValue");
    connection_READ_DATA_Hub.on("UpdateCommandValue", (addr, cmd, rcv_data) =>{
        //show addr on the info card
        // RcvQueue.push(cmd);
        // console.log(RcvQueue);
        
        try
        {
            document.getElementById("Address").textContent = "Address:" + addr;
            console.log("=================================================");
            console.log(`[SignalR] ${cmd}@${addr} :`);
            console.log(`[SignalR] ${cmd}@${addr} : ` + JSON.stringify(rcv_data, null, 2));
            let Translator = window.INV_Info_Translator.Single;
            let decode_data = Translator.decode(rcv_data);
            let temp_str = "";
            
            switch(cmd)
            {
                case "MFR_MODEL_B0B5" :
                    console.log("[SignalR][MFR_MODEL_B0B5] : decode_data = " + decode_data);
                    Translator.MFR_MODEL_B0B5_str = decode_data;
                    break;
                case "MFR_MODEL_B6B11" :
                    console.log("[SignalR][MFR_MODEL_B6B11] : decode_data = " + decode_data);
                    Translator.MFR_MODEL_B6B11_str = decode_data;
                    // if(Translator.MFR_MODEL_B0B5_str && Translator.MFR_MODEL_B6B11_str)
                    // {
                    //     console.log("MFR_MODEL = " + Translator.MFR_MODEL_B0B5_str + Translator.MFR_MODEL_B6B11_str);
                    // }
                    
                    document.getElementById("modelname-display").textContent = Translator.MFR_MODEL_B0B5_str + Translator.MFR_MODEL_B6B11_str;
                    break;
                case "INV_STATUS":
                    temp_str = Translator.Get_INV_Status(decode_data);

                    console.log("[INV_STATUS] : " + temp_str);
                    document.getElementById("InvSetting-display").textContent = temp_str;
                    break;

                case "INV_FAULT":
                    console.log("[INV_FAULT] : " + decode_data);
                    Translator.INV_FAULT_uint = Translator.Get_INV_Fault(decode_data);
                    break;

                case "MFR_REVISION_B0B5":
                    temp_str = Translator.Get_INV_MFR_REV(rcv_data);
                    console.log("[MFR_REVISION_B0B5] : " + temp_str);
                    document.getElementById("Revision-display").textContent = temp_str;
                    break;

                case "READ_FAN_SPEED_1":
                    temp_str = Translator.Get_INV_FAN_SPEED_1(decode_data);
                    console.log("[READ_FAN_SPEED_1] : " + temp_str);
                    document.getElementById("FAN_SPEED_1").textContent = temp_str;
                    break;
                case "READ_FAN_SPEED_2":
                    temp_str = Translator.Get_INV_FAN_SPEED_2(decode_data);
                    console.log("[READ_FAN_SPEED_2] : " + temp_str);
                    document.getElementById("FAN_SPEED_2").textContent = temp_str; 
                    break;
                case "READ_VBAT":
                    temp_str = Translator.Get_INV_BAT_V_Data(decode_data, rcv_data.scaling);
                    console.log("[READ_VBAT] : " + temp_str);
                    document.getElementById("V_BAT").textContent = temp_str;
                    break;

                case "READ_AC_VOUT":
                    Translator.Save_AC_VOUT(decode_data, rcv_data.scaling);
                    if(Translator.OP_VA_HI_uint != 0 || Translator.OP_VA_LO_uint != 0)
                    {
                        temp_str = Translator.Get_INV_Current_Data();
                        document.getElementById("Load_Current").textContent = temp_str;
                    }
                    break;
                case "READ_OP_VA_HI":
                    Translator.Save_OP_VA_HI(decode_data, rcv_data.scaling);
                    //get Load Power
                    temp_str = Translator.Get_INV_VA_Data();
                    document.getElementById("Load_Power").textContent = temp_str;
                    //get Load Current
                    temp_str = Translator.Get_INV_Current_Data();
                    document.getElementById("Load_Current").textContent = temp_str;
                    break;
                case "READ_OP_VA_LO":
                    Translator.Save_OP_VA_LO(decode_data, rcv_data.scaling);
                    //get Load Power
                    temp_str = Translator.Get_INV_VA_Data();
                    document.getElementById("Load_Power").textContent = temp_str;
                    //get Load Current
                    temp_str = Translator.Get_INV_Current_Data();
                    document.getElementById("Load_Current").textContent = temp_str;
                    break;
                case "READ_CHG_CURR":
                    temp_str = Translator.Get_INV_Current_DC_Data(decode_data, rcv_data.scaling);
                    document.getElementById("DC_Current").textContent = temp_str;
                    break;

                case "READ_TEMPERATURE_1":
                    temp_str = Translator.Get_INV_Temperatures_Data(decode_data, rcv_data.scaling);
                    document.getElementById("Temperature_display").textContent = temp_str;
                    break;
                default :
                    console.log(`[SignalR] Unknown command received: "${cmd}"`);
                    
                    break;
            }

        }
        catch(err)
        {
            console.error(`Error decoding ${cmd}@${addr}`, err);
        }
    });

    connection_READ_DATA_Hub.onclose(err => {
        console.warn("SignalR connection closed", err);
    })

    await connection_READ_DATA_Hub.start();
    console.log("[SignalR] connection started");

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

    if(connection_READ_DATA_Hub.state !== "Connected")
    {
        console.warn("SignalR not connected. Skipping subscribe");
        return;
    }

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

window.subscribeCommands_fixedCommand_dynamicAddr = async function(commandArray, addr)
{   
    if(!connection_READ_DATA_Hub) return;

    if(!Array.isArray(commandArray))
    {
        console.error("commandList is not an array:", commandArray);
        return;
    }

    for(const item of commandArray)
    {
        await connection_READ_DATA_Hub.invoke("SubscribeCommand", item, addr);
    }
}

window.unsubscribeAllCommands = async function()
{
    if(connection_READ_DATA_Hub)
    {
        await connection_READ_DATA_Hub.invoke("unsubscribeAllCommands");
        console.log("📭 All command subscriptions removed.");
    }
}

window.hubDisconnect = async function()
{
    if(connection_READ_DATA_Hub)
    {
        await connection_READ_DATA_Hub.stop();
        console.log("🔌 SignalR connection stopped and unsubscribed all.");
    }
}