const labels = ["13:00", "13:10", "13:20", "13:30", "13:40", "13:50", "14:00", "14:10", "14:20", "14:30"];
const data = [120, 300, 500, 250, 600, 800, 400, 100, 700, 900];
window.stageCanvasID = "stageChart";

function drawPowerChart()
{
    const canvas = document.getElementById('powerchart');
    
    //控制Canvas解析度(必要)，Css調整可視區域，但實際顯示是這裡控制
    canvas.width = canvas.parentElement.clientWidth;
    canvas.height = 300;
    
    const ctx = document.getElementById('powerchart').getContext('2d');
    
    
    const chart = new Chart(ctx, {
    type: 'line',
    data: {
        labels: labels,
        datasets: [{
            label: '功率 (kW)',
            data: data,
            borderColor: 'rgba(54, 162, 235, 1)',
            backgroundColor: 'rgba(54, 162, 235, 0.2)',
            tension: 0.1, // 線條平滑程度
            pointRadius: 3,
        }]
    },
    options: {
        responsive: true,
        plugins: {
            title: {
                display: true,
                text: '負載即時功率',
                font:{
                    size:24
                }
            }
        },
        scales: {
        y: {
            title: {
                display: true,
                text: '功率 (kW)',
                font:{
                    size:22
                }
            },
            min: 0,
            max: 1000
        },
        x: {
            title: {
                display: true,
                text: '時間',
                font:{
                    size:22
                }
            }
        }
        }
    }
    });

}

const week_labels = ["周一", "周二", "周三", "周四", "周五", "周六", "周日"];
const battery_data = [120, 300, 500, 250, 600, 800, 400];

function drawBatteryChart()
{
    const canvas = document.getElementById('batterychart');
    
    //控制Canvas解析度(必要)，Css調整可視區域，但實際顯示是這裡控制
    canvas.width = canvas.parentElement.clientWidth;
    canvas.height = 300;
    
    const ctx = document.getElementById('batterychart').getContext('2d');
    
    
    const chart = new Chart(ctx, {
    type: 'line',
    data: {
        labels: week_labels,
        datasets: [{
            label: '收益',
            data: battery_data,
            borderColor: 'rgba(54, 162, 235, 1)',
            backgroundColor: 'rgba(54, 162, 235, 0.2)',
            fill: true,
            tension: 0.1, // 線條平滑程度
            pointRadius: 3,
        }]
    },
    options: {
        responsive: true,
        plugins: {
            legend:{
                display: false
            },
            title: {
                display: true,
                text: '電池充放電量',
                font:{
                    size:24
                }
            }
        },
        scales: {
        y: {
            title: {
                display: false,
                text: '收益',
                font:{
                    size:22
                }
            },
            min: 0,
            max: 1000
        },
        x: {
            title: {
                display: false,
                text: '時間',
                font:{
                    size:22
                }
            }
        }
        }
    }
    });

}



window.chartInstances = window.chartInstances || new Map();

window.drawChart_Func = function(config, webFirstRend = false) {

    console.log("✅ JS drawChart_Func called", config);

    const canvas = document.getElementById(config.canvasID);
    if (!canvas) {
        console.warn("Canvas not found:", config.canvasID);
        return;
    }

    canvas.width = canvas.parentElement.clientWidth;
    canvas.height = 300;

    if(!window.chartInstances)
    {
        window.chartInstances = {};
    }

    // 獲取 CSS 變數的顏色值
    const root = document.body;
    const textPrimary = getComputedStyle(root).getPropertyValue('--text-primary').trim();
    const textSecondary = getComputedStyle(root).getPropertyValue('--text-secondary').trim();
    const borderColor = getComputedStyle(root).getPropertyValue('--border-color').trim();

    console.log(`Text Primary: ${textPrimary}, Text Secondary: ${textSecondary}, Border Color: ${borderColor}`);

    const existingChart = window.chartInstances[config.canvasID];
    
    const datasets = config.chart_single_data_lines.map((line, index) => ({
        label: line.cmd || `Line ${index + 1}`,
        data: line.data,
        borderColor: line.color || 'rgba(54, 162, 235, 1)',
        backgroundColor: (line.color || 'rgba(54, 162, 235, 1)') + '33',
        tension: 0.1,
        pointRadius: 3,
        yAxisID: line.y_axis_selection ? 'y' : 'y1'  // true=右, false=左
    }));

    const hasLeftData = datasets.some(ds => ds.yAxisID === "y");
    const hasrightData = datasets.some(ds => ds.yAxisID === "y1");

    if(existingChart && webFirstRend == false)
    {   
        // 重新獲取當前主題的顏色
        const currentTextPrimary = getComputedStyle(root).getPropertyValue('--text-primary').trim();
        const currentBorderColor = getComputedStyle(root).getPropertyValue('--border-color').trim();
        
        existingChart.options.plugins.title.text = config.chartTitle;
        existingChart.options.plugins.title.color = currentTextPrimary;
        existingChart.options.plugins.legend.labels.color = currentTextPrimary;
        
        existingChart.options.scales.x.title.color = currentTextPrimary;
        existingChart.options.scales.x.grid.color = currentBorderColor;
        existingChart.options.scales.x.ticks.color = currentTextPrimary;
        
        existingChart.options.scales.y.title.color = currentTextPrimary;
        existingChart.options.scales.y.grid.color = currentBorderColor;
        existingChart.options.scales.y.ticks.color = currentTextPrimary;
        
        existingChart.options.scales.y1.title.color = currentTextPrimary;
        existingChart.options.scales.y1.grid.color = currentBorderColor;
        existingChart.options.scales.y1.ticks.color = currentTextPrimary;
        
        existingChart.data.labels = config.labels;
        existingChart.data.datasets = datasets;

        existingChart.options.scales.y.display = hasLeftData;
        existingChart.options.scales.y.title.text = config.y_left_Title;
        
        existingChart.options.scales.y1.display = hasrightData;
        existingChart.options.scales.y1.title.text = config.y_right_Title;

        existingChart.update();
    }
    else
    {
        if (window.chartInstances[config.canvasID]) {
            window.chartInstances[config.canvasID].destroy();
            delete window.chartInstances[config.canvasID];
        }
        const ctx = canvas.getContext("2d");
        window.chartInstances[config.canvasID] = new Chart(ctx, {
            type: "line",  // ✅ 支援選擇圖表類型
            data: {
                labels: config.labels,
                datasets: datasets
            },
            options: {
                animation: false,
                responsive: true,
                plugins: {
                    title: {
                        display: true,
                        text: config.chartTitle || '',
                        font: {size : 26},
                        color: textPrimary  // 使用主題文字顏色
                    },
                    legend: {
                        display: true,
                        labels: {
                            color: textPrimary  // 使用主題文字顏色
                        }
                    }
                },
                scales: {
                    x: {
                        type:'category',
                        title: {
                            display: true,
                            text: "時間",
                            font: {size : 16},
                            color: textPrimary  // 使用主題文字顏色
                        },
                        grid: {
                            color: borderColor  // 使用主題邊框顏色
                        },
                        ticks: {
                            color: textPrimary  // 使用主題文字顏色
                        }
                    },
                    y: {
                        type: "linear",
                        position: "left",
                        display: hasLeftData,
                        title:{
                            display: hasLeftData && !!config.y_left_Title,
                            text: hasLeftData ? config.y_left_Title : "",
                            font: {size : 16},
                            color: textPrimary  // 使用主題文字顏色
                        },
                        grid: {
                            color: borderColor  // 使用主題邊框顏色
                        },
                        ticks: {
                            color: textPrimary  // 使用主題文字顏色
                        }
                    },
                    y1: {
                        type: "linear",
                        position: "right",
                        display: hasrightData,
                        title: {
                            display: hasrightData && !!config.y_right_Title,
                            text: hasrightData ? config.y_right_Title : "",
                            font: {size: 16},
                            color: textPrimary  // 使用主題文字顏色
                        },
                        grid: {
                            drawOnChartArea: false,
                            color: borderColor  // 使用主題邊框顏色
                        },
                        ticks: {
                            color: textPrimary  // 使用主題文字顏色
                        }
                    }
                }
            }
        });
    }
   
};

// window.drawChart_Stage = function(config)
// {
//     const canvas = document.getElementById(config.canvasID);
//     if (!canvas) {
//         console.warn("找不到 canvas:", config.canvasID);
//         return;
//     }

//     const ctx = canvas.getContext("2d");

//     if (!window.chartInstances) {
//         window.chartInstances = {};
//     }

//     if (window.chartInstances[config.canvasID]) {
//         window.chartInstances[config.canvasID].destroy();
//     }

//     window.chartInstances[config.canvasID] = new Chart(ctx, {
//         type: config.chartType || "line",
//         data: {
//             labels: config.labels,
//             datasets: [
//                 {
//                     label: config.y1Label,
//                     data: config.y1Data,
//                     yAxisID: "y1",
//                     borderWidth: 2,
//                     borderColor: "rgba(255, 99, 132, 1)",
//                     backgroundColor: "rgba(255, 99, 132, 0.2)",
//                     fill: false
//                 },
//                 {
//                     label: config.y2Label,
//                     data: config.y2Data,
//                     yAxisID: "y2",
//                     borderWidth: 2,
//                     borderColor: "rgba(54, 162, 235, 1)",
//                     backgroundColor: "rgba(54, 162, 235, 0.2)",
//                     fill: false
//                 }
//             ]
//         },
//         options: {
//             responsive: true,
//             plugins: {
//                 legend: {
//                     display: true
//                 }
//             },
//             scales: {
//                 x: {
//                     title: {
//                         display: true,
//                         text: config.xLabel || "時間"
//                     }
//                 },
//                 y1: {
//                     type: "linear",
//                     position: "left",
//                     title: {
//                         display: true,
//                         text: config.y1Label
//                     },
//                     suggestedMin: 0,
//                     suggestedMax: 50
//                 },
//                 y2: {
//                     type: "linear",
//                     position: "right",
//                     title: {
//                         display: true,
//                         text: config.y2Label
//                     },
//                     suggestedMin: 90,
//                     suggestedMax: 110,
//                     grid: {
//                         drawOnChartArea: false
//                     }
//                 }
//             }
//         }
//     });
// };

let oldStageOption = -1;



window.stageChartStorage = {
    stageOption : -1,    //0 for 2_stage, 1 for 3_stage

    stage2:{
        y1Data : [],
        y2Data : [],
        labels : []
    },
    stage3:{
        y1Data : [],
        y2Data : [],
        labels : []
    }
};

// //[DEBUG] 可能可以解掉 拉動slider時，canvas不斷重繪的問題 (只更新部分點)，目前是把動畫效果關掉(可以解決)
// window.updateStageData_AfterInit = function(_existingChart)
// {
//     const showY1Data = _existingChart.data.datasets[0];
//     const showY2Data = _existingChart.data.datasets[1];

//     if(stageChartStorage.stageOption == 0) // 2 stage
//     {
//         showY1Data.data.length = 3;
//         showY2Data.data.length = 3;
        
//         showY1Data.data[0] = sliderBars_Info["CC"].currentVal;
//         showY1Data.data[1] = sliderBars_Info["CC"].currentVal;
//         showY1Data.data[2] = sliderBars_Info["TC"].currentVal;

//         showY2Data.data[0] = sliderBars_Info["CV"].selfMin;
//         showY2Data.data[1] = sliderBars_Info["CV"].currentVal;
//         showY2Data.data[2] = sliderBars_Info["CV"].currentVal;
//     }
//     else if(stageChartStorage.stageOption == 1) // 3 stage
//     {
//         showY1Data.data.length = 5;
//         showY2Data.data.length = 5;

//         showY1Data.data[0] = sliderBars_Info["CC"].currentVal;
//         showY1Data.data[1] = sliderBars_Info["CC"].currentVal;
//         showY1Data.data[2] = sliderBars_Info["TC"].currentVal;
//         showY1Data.data[3] = 0;
//         showY1Data.data[4] = 0;

//         showY2Data.data[0] = sliderBars_Info["CV"].selfMin;
//         showY2Data.data[1] = sliderBars_Info["CV"].currentVal;
//         showY2Data.data[2] = sliderBars_Info["CV"].currentVal;
//         showY2Data.data[3] = sliderBars_Info["FV"].currentVal;
//         showY2Data.data[4] = sliderBars_Info["FV"].currentVal;
//     }
// }

window.updateStageData_Init = function()
{
    if(stageChartStorage.stageOption == 0) // 2 stage
    {
        //clear the lists
        window.stageChartStorage.stage2.y1Data.length = 0;
        window.stageChartStorage.stage2.y2Data.length = 0;
        //current
        window.stageChartStorage.stage2.y1Data.push(...[
            {x:1, y:sliderBars_Info["CC"].currentVal},
            {x:2, y:sliderBars_Info["CC"].currentVal},
            {x:3, y:sliderBars_Info["TC"].currentVal}]);

        //voltage
        window.stageChartStorage.stage2.y2Data.push(...[
            {x:1, y:sliderBars_Info["CV"].selfMin},
            {x:2, y:sliderBars_Info["CV"].currentVal},
            {x:3, y:sliderBars_Info["CV"].currentVal}]);
    }
    else if(stageChartStorage.stageOption == 1) // 3 stage
    {
        //clear the lists
        window.stageChartStorage.stage3.y1Data.length = 0;
        window.stageChartStorage.stage3.y2Data.length = 0;
        //current
        window.stageChartStorage.stage3.y1Data.push(...[
            {x : 1, y : sliderBars_Info["CC"].currentVal},
            {x : 2, y : sliderBars_Info["CC"].currentVal},
            {x : 3, y : sliderBars_Info["TC"].currentVal},
            {x : 4, y : 0}]);
        //voltage
        window.stageChartStorage.stage3.y2Data.push(...[
            {x:1, y:sliderBars_Info["CV"].selfMin},
            {x:2, y:sliderBars_Info["CV"].currentVal},
            {x:3, y:sliderBars_Info["CV"].currentVal},
            {x:3, y:sliderBars_Info["FV"].currentVal},
            {x:4, y:sliderBars_Info["FV"].currentVal}]);
    } 
}

window.setStage = function(option)
{
    console.log(`[Receive] option = ${option}`);
    if(window.stageChartStorage.stageOption != option)
    {
        window.stageChartStorage.stageOption = option;
        oldStageOption = option;
        // updateStageData();
        if(window.stageChartStorage.stageOption == 1) // 2 stage 
        {
            //clear the lists
            window.stageChartStorage.stage2.labels.length = 3;
            //fake x-axis data
            window.stageChartStorage.stage2.labels[0] = "1";
            window.stageChartStorage.stage2.labels[1] = "2";
            window.stageChartStorage.stage2.labels[2] = "3";

            window.CV_Value_to_FV("CV", window.sliderBars_Info["CV"].currentVal);
        }
        else if(window.stageChartStorage.stageOption == 0) // 3 stage
        {
            //clear the lists
            window.stageChartStorage.stage3.labels.length = 5;

            //fake x-axis data
            window.stageChartStorage.stage3.labels[0] = "1";
            window.stageChartStorage.stage3.labels[1] = "2";
            window.stageChartStorage.stage3.labels[2] = "3";
            window.stageChartStorage.stage3.labels[3] = "4";
            window.stageChartStorage.stage3.labels[4] = "5";
        }
    }


    
}

window.drawChart_Stage = function (webFirstRend = false) {
    console.log("[drawChart_Stage]");
    const canvas = document.getElementById(stageCanvasID);
    if (!canvas) {
        console.warn("找不到 canvas:", stageCanvasID);
        return;
    }
    
    const stageData = (stageChartStorage.stageOption == 0) ? stageChartStorage.stage2 : stageChartStorage.stage3;
    console.log(stageData);

    const rect = canvas.getBoundingClientRect();
    canvas.width = rect.width;
    canvas.height = rect.height;

    
    if (!window.chartInstances) {
        window.chartInstances = {};
    }

    // 獲取 CSS 變數的顏色值
    const root = document.body;
    const textPrimary = getComputedStyle(root).getPropertyValue('--text-primary').trim();
    const borderColor = getComputedStyle(root).getPropertyValue('--border-color').trim();

    console.log(`[drawChart_Stage] Text Primary: ${textPrimary}, Border Color: ${borderColor}`);

    const existingChart = window.chartInstances[stageCanvasID];
    // if (window.chartInstances[config.canvasID]) {
    //     window.chartInstances[config.canvasID].destroy();
    // }
    updateStageData_Init();
    if(existingChart && webFirstRend == false)
    {
        console.log(existingChart);
        
        // 更新現有圖表的顏色
        existingChart.options.plugins.legend.labels.color = textPrimary;
        
        existingChart.options.scales.x.title.color = textPrimary;
        existingChart.options.scales.x.grid.color = borderColor;
        existingChart.options.scales.x.ticks.color = textPrimary;
        
        existingChart.options.scales.y1.title.color = textPrimary;
        existingChart.options.scales.y1.grid.color = borderColor;
        existingChart.options.scales.y1.ticks.color = textPrimary;
        
        existingChart.options.scales.y2.title.color = textPrimary;
        existingChart.options.scales.y2.grid.color = borderColor;
        existingChart.options.scales.y2.ticks.color = textPrimary;
        
        existingChart.data.labels = stageData.labels;
        existingChart.data.datasets[0].data = stageData.y1Data;
        existingChart.data.datasets[1].data = stageData.y2Data;
        // updateStageData_AfterInit(existingChart);
        existingChart.update();
    }
    else
    {
        if (window.chartInstances[stageCanvasID]) {
            window.chartInstances[stageCanvasID].destroy();
            delete window.chartInstances[stageCanvasID];
        }
        const ctx = canvas.getContext("2d");
        window.chartInstances[stageCanvasID] = new Chart(ctx, {
            type: "line",  // ✅ 支援選擇圖表類型
            data: {
                datasets: [
                    {
                        label: "電流 (A)",
                        data: stageData.y1Data,
                        yAxisID: "y1",
                        borderWidth: 2,
                        borderColor: "rgba(255, 99, 132, 1)",
                        backgroundColor: "rgba(255, 99, 132, 0.2)",
                        fill: false
                    },
                    {
                        label: "電壓 (V)",
                        data: stageData.y2Data,
                        yAxisID: "y2",
                        borderWidth: 2,
                        borderColor: "rgba(54, 162, 235, 1)",
                        backgroundColor: "rgba(54, 162, 235, 0.2)",
                        fill: false
                    }
                ]
            },
            options: {
                animation: false,
                responsive: true,
                plugins: {
                    legend: {
                        display: true,
                        labels: {
                            color: textPrimary
                        }
                    }
                },
                scales: {
                    x: {
                        type:'linear',
                        title: {
                            display: true,
                            text: "",
                            color: textPrimary
                        },
                        grid: {
                            color: borderColor
                        },
                        ticks: {
                            color: textPrimary
                        }
                    },
                    y1: {
                        type: "linear",
                        position: "left",
                        title: {
                            display: true,
                            text: "電流 (A)",
                            color: textPrimary
                        },
                        suggestedMin: 0,
                        suggestedMax: 100,
                        grid: {
                            color: borderColor
                        },
                        ticks: {
                            color: textPrimary
                        }
                    },
                    y2: {
                        type: "linear",
                        position: "right",
                        title: {
                            display: true,
                            text: "電壓 (V)",
                            color: textPrimary
                        },
                        suggestedMin: 0,
                        suggestedMax: 100,
                        grid: {
                            drawOnChartArea: false,
                            color: borderColor
                        },
                        ticks: {
                            color: textPrimary
                        }
                    }
                }
            }
        });
    }
    
};



// window.chartInstances = new Map();
// window.drawChart_Func = function(config)
// {
//     console.log("✅ JS drawChart_Func called", config);
//     const canvas = document.getElementById(config.canvasID);
//     canvas.width = canvas.parentElement.clientWidth;
//     canvas.height = 300;

//     const ctx = canvas.getContext('2d');

//     if(chartInstances.has(config.canvasID))
//     {
//         chartInstances.get(config.canvasID).destroy();
//     }

//     const chart = new Chart(ctx, {
//         type: 'line',
//         data: {
//             labels: config.labels,
//             datasets: [{
//                 label: config.yTitle,
//                 data: config.data,
//                 borderColor: 'rgba(54, 162, 235, 1)',
//                 backgroundColor: 'rgba(54, 162, 235, 0.2)',
//                 tension: 0.1,
//                 pointRadius: 3
//             }]
//         },
//         options: {
//             responsive: true,
//             animation: false,
//             plugins: {
//                 title: {
//                     display: true,
//                     text: config.chartTitle,
//                     font: { size: 24 }
//                 }
//             },
//             scales: {
//                 y: {
//                     min: config.yMin,
//                     max: config.yMax,
//                     title: {
//                         display: true,
//                         text: config.yTitle,
//                         font: { size: 22 }
//                     }
//                 },
//                 x: {
//                     title: {
//                         display: true,
//                         text: config.xTitle,
//                         font: { size: 22 }
//                     }
//                 }
//             }
//         }
//     });

//     chartInstances.set(config.canvasID, chart);
// };

// window.updateChartData = function (canvasID, label, value)
// {
//     const chart = chartInstances.get(canvasID);
//     if(!chart)
//     {
//         console.warn("Chart not found:", canvasID);
//         return;
//     }

//     chart.data.labels.push(label);
//     chart.data.datasets[0].data.push(value);

//     if(chart.data.labels.length > 10)
//     {
//         chart.data.labels.shift();
//         chart.data.datasets[0].data.shift();
//     }

//     chart.update();
// };

// window.destroyChart = function (canvasID) {
//     const chart = window.chartInstances.get(canvasID);
//     if (chart) {
//         chart.destroy();
//         window.chartInstances.delete(canvasID);
//         console.log(`[Chart] Destroyed chart with ID: ${canvasID}`);
//     } else {
//         console.warn(`[Chart] No chart found with ID: ${canvasID}`);
//     }
// };

window.drawChart_Sin = function(config)
{
    const canvas = document.getElementById(config.canvasID);
    if (!canvas) {
        console.warn("找不到 canvas:", config.canvasID);
        return;
    }

    const rect = canvas.getBoundingClientRect();
    canvas.width = rect.width;
    canvas.height = rect.height;

    const ctx = canvas.getContext("2d");

    if (!window.chartInstances) {
        window.chartInstances = {};
    }

    // 獲取 CSS 變數的顏色值
    const root = document.body;
    const textPrimary = getComputedStyle(root).getPropertyValue('--text-primary').trim();
    const borderColor = getComputedStyle(root).getPropertyValue('--border-color').trim();

    console.log(`[drawChart_Sin] Text Primary: ${textPrimary}, Border Color: ${borderColor}`);

    if (window.chartInstances[config.canvasID]) {
        window.chartInstances[config.canvasID].destroy();
    }

    // 產生 Sin 資料
    const labels = [];
    const data = [];
    const peakLineData = [];
    const peak = config.peak || 1;
    const period = config.period || 360;
    const totalDegrees = config.totalDegrees || 360;
    for (let x = 0; x <= totalDegrees; x += 5) {
        const radians = (x * Math.PI) / 180;
        labels.push(x + "°");
        data.push(peak * Math.sin((radians * 360) / period));
        peakLineData.push(peak);
    }

    window.chartInstances[config.canvasID] = new Chart(ctx, {
        type: config.chartType || "line",
        data: {
            labels: labels,
            datasets: [
                {
                label: config.yLabel || "輸出電壓",
                data: data,
                borderWidth: 2,
                borderColor: "rgba(75, 192, 192, 1)",
                backgroundColor: "rgba(75, 192, 192, 0.2)",
                fill: false,
                tension: 0.2,
                pointRadius: 0,
                pointHoverRadius: 0,
                },
                // {
                // label: "峰值",
                // data: Array(100).fill(peak), // ⬅ 長度符合 labels
                // borderColor: "red",
                // borderDash: [5, 5],
                // borderWidth: 2,
                // fill: false,
                // tension: 0,
                // pointRadius: 0

                // }
            ]
        },
        options: {
            responsive: true,
            plugins: {
                legend: {
                    display: true,
                    labels: {
                        color: textPrimary  // 使用主題文字顏色
                    }
                }
            },
            scales: {
                x: {
                    title: {
                        display: false,
                        text: config.xLabel || "角度 (°)",
                        color: textPrimary  // 使用主題文字顏色
                    },
                    ticks:{
                        display: false,
                        color: textPrimary  // 使用主題文字顏色
                    },
                    grid:{
                        display: false
                    }
                },
                y: {
                    title: {
                        display: false,
                        text: config.yLabel || "值",
                        color: textPrimary  // 使用主題文字顏色
                    },
                    min: config.yMin !== undefined ? config.yMin : -peak,
                    max: config.yMax !== undefined ? config.yMax : peak,
                    // suggestedMin: -peak,
                    // suggestedMax: peak
                    grid:{
                        color: borderColor  // 使用主題邊框顏色
                    },
                    ticks:{
                        stepSize: 20,
                        color: textPrimary  // 使用主題文字顏色
                    }
                }
            }
        }
    });
}

window.destroyAllCharts = function () {
    console.log("[destoryAllCharts]");
    console.log(window.chartInstances);
};

window.redrawAllCharts = function () {
    console.log("[redrawAllCharts] 重新渲染所有圖表以應用新主題");
    
    if (!window.chartInstances) {
        console.log("沒有圖表實例");
        return;
    }

    const root = document.body;
    
    // 獲取當前主題的顏色
    const textPrimary = getComputedStyle(root).getPropertyValue('--text-primary').trim();
    const borderColor = getComputedStyle(root).getPropertyValue('--border-color').trim();
    
    console.log(`[redrawAllCharts] 當前主題顏色 - 文字: ${textPrimary}, 邊框: ${borderColor}`);

    // 遍歷所有圖表實例並更新顏色
    for (const [canvasID, chart] of Object.entries(window.chartInstances)) {
        if (chart && !chart.destroyed) {
            console.log(`更新圖表: ${canvasID}`);
            
            // 更新插件顏色
            if (chart.options.plugins) {
                if (chart.options.plugins.title) {
                    chart.options.plugins.title.color = textPrimary;
                }
                if (chart.options.plugins.legend && chart.options.plugins.legend.labels) {
                    chart.options.plugins.legend.labels.color = textPrimary;
                }
            }
            
            // 更新軸顏色
            if (chart.options.scales) {
                // X軸
                if (chart.options.scales.x) {
                    if (chart.options.scales.x.title) {
                        chart.options.scales.x.title.color = textPrimary;
                    }
                    if (chart.options.scales.x.grid) {
                        chart.options.scales.x.grid.color = borderColor;
                    }
                    if (chart.options.scales.x.ticks) {
                        chart.options.scales.x.ticks.color = textPrimary;
                    }
                }
                
                // Y軸
                if (chart.options.scales.y) {
                    if (chart.options.scales.y.title) {
                        chart.options.scales.y.title.color = textPrimary;
                    }
                    if (chart.options.scales.y.grid) {
                        chart.options.scales.y.grid.color = borderColor;
                    }
                    if (chart.options.scales.y.ticks) {
                        chart.options.scales.y.ticks.color = textPrimary;
                    }
                }
                
                // Y1軸 (右側Y軸)
                if (chart.options.scales.y1) {
                    if (chart.options.scales.y1.title) {
                        chart.options.scales.y1.title.color = textPrimary;
                    }
                    if (chart.options.scales.y1.grid) {
                        chart.options.scales.y1.grid.color = borderColor;
                    }
                    if (chart.options.scales.y1.ticks) {
                        chart.options.scales.y1.ticks.color = textPrimary;
                    }
                }
                
                // Y2軸 (另一個右側Y軸，用於 stage chart)
                if (chart.options.scales.y2) {
                    if (chart.options.scales.y2.title) {
                        chart.options.scales.y2.title.color = textPrimary;
                    }
                    if (chart.options.scales.y2.grid) {
                        chart.options.scales.y2.grid.color = borderColor;
                    }
                    if (chart.options.scales.y2.ticks) {
                        chart.options.scales.y2.ticks.color = textPrimary;
                    }
                }
            }
            
            // 重新渲染圖表
            chart.update();
            console.log(`圖表 ${canvasID} 已更新`);
        }
    }
};