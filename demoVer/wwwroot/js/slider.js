let currentDrag = null;
let isDragging = false;

window.sliderBars_Info =
{
    CC : {
        selfMax: 0,
        selfMin: 0,
        currentVal: 0,
        fill: null,
        thumb : null,
    },
    TC : {
        selfMax: 0,
        selfMin: 0,
        currentVal: 0,
        fill: null,
        thumb : null,
    },
    CV : {
        selfMax: 0,
        selfMin: 0,
        currentVal: 0,
        fill: null,
        thumb : null,
    },
    FV : {
        selfMax: 0,
        selfMin: 0,
        currentVal: 0,
        fill: null,
        thumb : null,
    },
};

//從C#傳入JS的東西一律小寫，較不容易出錯
window.setSliderBars_Info = function(config)
{   
    console.log("CONFIG : ", config);

    sliderBars_Info["CC"].currentVal    = config.cc_val;
    sliderBars_Info["CC"].selfMax       = config.cc_controlMax;
    sliderBars_Info["CC"].selfMin       = config.cc_minValue;
    sliderBars_Info["CC"].UI_max        = config.cc_UI_MAX;
    sliderBars_Info["CC"].fill          = config.cc_fill;
    sliderBars_Info["CC"].thumb         = config.cc_thumb;
    

    sliderBars_Info["TC"]["currentVal"] = config.tc_val;
    sliderBars_Info["TC"].selfMax       = config.tc_controlMax;
    sliderBars_Info["TC"].selfMin       = config.tc_minValue;
    sliderBars_Info["TC"].UI_max        = config.tc_UI_MAX;
    sliderBars_Info["TC"].fill          = config.tc_fill;
    sliderBars_Info["TC"].thumb         = config.tc_thumb;


    sliderBars_Info["CV"]["currentVal"] = config.cv_val;
    sliderBars_Info["CV"].selfMax       = config.cv_controlMax;
    sliderBars_Info["CV"].selfMin       = config.cv_minValue;
    sliderBars_Info["CV"].UI_max        = config.cv_UI_MAX;
    sliderBars_Info["CV"].fill          = config.cv_fill;
    sliderBars_Info["CV"].thumb         = config.cv_thumb;

    sliderBars_Info["FV"]["currentVal"] = config.fv_val;
    sliderBars_Info["FV"].selfMax       = config.fv_controlMax;
    sliderBars_Info["FV"].selfMin       = config.fv_minValue;
    sliderBars_Info["FV"].UI_max        = config.fv_UI_MAX;
    sliderBars_Info["FV"].fill          = config.fv_fill;
    sliderBars_Info["FV"].thumb         = config.fv_thumb;

    console.log(sliderBars_Info["CC"].currentVal);
    initializeSliderInputs();
}

window.initializeSliderInputs = function() {
    for (let i = 0; i < 4; i++) {
        const input = document.getElementById(`slider-input-${i}`);
        if (!input) continue;

        const label = get_slider_Label(i);
        const val = sliderBars_Info[label].currentVal;
        window.drawChart_Stage();
        input.value = val.toFixed(1);
    }
}


window.sliderUpdateFromInput = function(dotNetHelper)
{
    for(let i = 0 ; i < 4 ; i ++)
    {
        const input = document.getElementById(`slider-input-${i}`);
        if(!input) continue;

        input.addEventListener('change', () => {
            const value = parseFloat(input.value);
            if (!isNaN(value)) {
                // 更新 JS 記憶體
                const label = get_slider_Label(i);
                const Final_Val = window.check_Over_Mix_Max(label, value);
                input.value = Final_Val;
                sliderBars_Info[label].currentVal = Final_Val;

                window.CV_Value_to_FV(label, Final_Val); //check 是否為 2階段時的CV拖動

                window.drawChart_Stage();
                limitOthersMax(label, dotNetHelper);

                // 更新 slider UI
                let displayMax;
                switch(label)
                {
                    case "CC":
                        displayMax = sliderBars_Info["CC"].selfMax;
                        break;
                    case "TC":
                        displayMax = sliderBars_Info["CC"].selfMax;
                        break;
                    case "CV":
                        displayMax = sliderBars_Info["CV"].selfMax;
                        break;
                    case "FV":
                        displayMax = sliderBars_Info["CV"].selfMax;
                        break;
                    default:
                        break;
                }

                const percent = (Final_Val / displayMax) * 100;
                sliderBars_Info[label].fill.style.height = `${percent}%`;
                sliderBars_Info[label].thumb.style.bottom = `${percent}%`;
            }
        });

    }
}

window.updateInputFromSlider = function(index, value)
{
    const input = document.getElementById(`slider-input-${index}`);
    if(input)
    {
        input.value = value.toFixed(1);
    }
}

window.startVerticalSliderDrag = (config) => {
    if(currentDrag)
    {
        currentDrag.stop();
    }
    const container = config.fill.parentElement;
    const rect = container.getBoundingClientRect();

    function getRect()
    {
        return container.getBoundingClientRect();
    }

    function isTouchDevice()
    {
        return 'ontouchstart' in window || navigator.maxTouchPoints > 0;
    }

    function updateFromY(y) {
        const rect = getRect(); // ← 注意這裡要即時取 rect
        const offset = Math.min(Math.max(y - rect.top, 0), rect.height);
        
        //百分比(反轉Y軸)
        const percentage = 1 - (offset / rect.height);
        //實際值
        // let rawValue = 100 - (offset / rect.height) * 100;
        const rawValue = config.minValue + percentage * (config.displayMaxValue  - config.minValue);
        const clampedValue = Math.min(Math.max(rawValue, config.minValue), config.displayMaxValue );

        // 四捨五入到 0.1 精度
        const step = 0.1;
        const value = Math.round(clampedValue / step) * step;
        const send_value = Math.round(value * 10) / 10;
        console.log(send_value);
        //取label
        const label = window.get_slider_Label(config.index);
        
        const Final_Val = window.check_Over_Mix_Max(label, send_value);
        console.log(`Final = ${Final_Val}, send_val = ${send_value}`);
        sliderBars_Info[label].currentVal = Final_Val;
        window.CV_Value_to_FV(label, Final_Val); //check 是否為 2階段時的CV拖動

        window.drawChart_Stage();
        window.limitOthersMax(label, config.dotNetHelper);
        // 顯示 config.fill/slider 在百分比位置（但使用 0~100% 表示）
        // const visualPercent = ((value - config.minValue) / (config.maxValue - config.minValue)) * 100;
        
        const visualPercent = (Final_Val / config.displayMaxValue) * 100;
        config.fill.style.height = `${visualPercent}%`;
        config.thumb.style.bottom = `${visualPercent}%`;
    

        updateInputFromSlider(config.index, Final_Val);

        
    }

    function preventTouchScroll(e)
    {
        e.preventDefault();
    }

    if(isTouchDevice())
    {
        console.log("is Mobile !!");
        container.addEventListener("touchstart", (e) => {
            config.fill.style.transition = "all 0.2s ease-in-out";
            config.thumb.style.transition = "all 0.2s ease-in-out";
            const clientY = e.touches[0].clientY;
            updateFromY(clientY);
            e.preventDefault();
        }, {passive: false});
    }
    else
    {
        // ✅ 點擊事件處理（只觸發一次）
        container.addEventListener("click", (e) => {
            if(isDragging) return;
            config.fill.style.transition = "all 0.2s ease-in-out";
            config.thumb.style.transition = "all 0.2s ease-in-out";
            const clientY = e.touches ? e.touches[0].clientY : e.clientY;
            updateFromY(clientY);
        });
    }
    


    function onMove(e) {
        isDragging = true;
        config.fill.style.transition = "none";
        config.thumb.style.transition = "none";
        const clientY = e.touches ? e.touches[0].clientY : e.clientY;
        updateFromY(clientY);
        
    }

    currentDrag = {stop};

    function stop() {
        document.removeEventListener("mousemove", onMove);
        document.removeEventListener("mouseup", stop);
        document.removeEventListener("touchmove", onMove);
        document.removeEventListener("touchend", stop);

        document.removeEventListener("touchmove", preventTouchScroll);

        setTimeout(() => isDragging = false, 100);
        currentDrag = null;
    }
    document.addEventListener("mousemove", onMove);
    document.addEventListener("mouseup", stop);
    document.addEventListener("touchmove", onMove, { passive: false });

    document.addEventListener("touchmove", preventTouchScroll, {passive: false});
    document.addEventListener("touchend", stop, { passive: false });
};

window.updateSliderVisual = function(config)
{
    value = Math.max(0, Math.min(config.value, config.displayMaxValue));

    const visualPercent = (value / config.displayMaxValue) * 100;
    config.fill.style.height = `${visualPercent}%`;
    config.thumb.style.bottom = `${visualPercent}%`;
}

window.limitOthersMax = function(label, dotNetHelper)
{
    if(label === "CC")
    {
        //查看當前TC值是否超過CC當前值，有的話則 TC_val = CC_val
        if(sliderBars_Info["TC"]["currentVal"] > sliderBars_Info["CC"]["currentVal"])
        {
            sliderBars_Info["TC"]["currentVal"] = sliderBars_Info["CC"]["currentVal"];
            
            const visualPercent = (sliderBars_Info["TC"].currentVal / sliderBars_Info["CC"].selfMax) * 100;
            sliderBars_Info["TC"].fill.style.height = `${visualPercent}%`;
            sliderBars_Info["TC"].thumb.style.bottom = `${visualPercent}%`;

            updateInputFromSlider(1, sliderBars_Info["TC"].currentVal);
            // dotNetHelper.invokeMethodAsync("UpdateSliderValue", 1, sliderBars_Info["TC"].currentVal);

        }
    }
    else if(label == "CV")
    {
        if(sliderBars_Info["FV"]["currentVal"] > sliderBars_Info["CV"]["currentVal"])
        {
            sliderBars_Info["FV"]["currentVal"] = sliderBars_Info["CV"]["currentVal"];

            const visualPercent = (sliderBars_Info["FV"].currentVal / sliderBars_Info["CV"].selfMax) * 100;
            sliderBars_Info["FV"].fill.style.height = `${visualPercent}%`;
            sliderBars_Info["FV"].thumb.style.bottom = `${visualPercent}%`;

            updateInputFromSlider(3, sliderBars_Info["FV"].currentVal);
            // dotNetHelper.invokeMethodAsync("UpdateSliderValue", 3, sliderBars_Info["FV"].currentVal);
        }
    }
}

window.check_Over_Mix_Max = function(label, tempVal)
{
    let temp_return;
    switch(label)
    {
        case "CC":
            if(tempVal > sliderBars_Info["CC"].selfMax){temp_return = sliderBars_Info["CC"].selfMax;}
            else if(tempVal < sliderBars_Info["CC"].selfMin){temp_return = sliderBars_Info["CC"].selfMin;}
            else{temp_return = tempVal;}
            
            break;
        case "TC":
            tempTC_upperBound = Math.min(sliderBars_Info["CC"]["currentVal"], sliderBars_Info["TC"]["selfMax"]);
            console.log(`tempTC_upperBound = ${tempTC_upperBound}`);
            if(tempVal > tempTC_upperBound)
            {
                temp_return = tempTC_upperBound;
            }
            else if(tempVal < sliderBars_Info["TC"].selfMin)
            {
                temp_return = sliderBars_Info["TC"].selfMin;
            }
            else
            {
                temp_return = tempVal;
            }
            break;
        case "CV":
            if(tempVal > sliderBars_Info["CV"].selfMax){temp_return = sliderBars_Info["CV"].selfMax;}
            else if(tempVal < sliderBars_Info["CV"].selfMin){temp_return = sliderBars_Info["CV"].selfMin;}
            else{temp_return = tempVal;}

            break;
        case "FV":
            tempFV_upperBound = Math.min(sliderBars_Info["CV"]["currentVal"], sliderBars_Info["FV"]["selfMax"]);
            console.log(`tempFV_upperBound = ${tempFV_upperBound}`);
            if(tempVal > tempFV_upperBound)
            {
                temp_return = tempFV_upperBound;
            }
            else if(tempVal < sliderBars_Info["FV"].selfMin)
            {
                temp_return = sliderBars_Info["FV"].selfMin;
            }
            else
            {
                temp_return = tempVal;
            }
            break;
        default:
            break;
    }
    return temp_return;
}

window.get_slider_Label = function(index)
{
    switch(index)
    {
        case 0:
            return "CC";
        case 1:
            return "TC";
        case 2:
            return "CV";
        case 3:
            return "FV";
    }
}

window.CV_Value_to_FV = function(label, value)
{
    if(window.stageChartStorage.stageOption == 0)
    {
        console.log("CV_Value_to_FV");
        if(label === "CV")
        {
            const FV_val = value;
            sliderBars_Info["FV"].currentVal = FV_val;
            
            const fvPercent = (FV_val / sliderBars_Info["CV"].selfMax) * 100;
            sliderBars_Info["FV"].fill.style.height = `${fvPercent}%`;
            sliderBars_Info["FV"].thumb.style.bottom = `${fvPercent}%`;
            
            updateInputFromSlider(3, FV_val);
        }
    }
}

window.transferSliderValue = function(){
    return [sliderBars_Info["CC"].currentVal, sliderBars_Info["TC"].currentVal, sliderBars_Info["CV"].currentVal, sliderBars_Info["FV"].currentVal];
};