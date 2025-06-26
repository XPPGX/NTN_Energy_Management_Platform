const labels = ["13:00", "13:10", "13:20", "13:30", "13:40", "13:50", "14:00", "14:10", "14:20", "14:30"];
const data = [120, 300, 500, 250, 600, 800, 400, 100, 700, 900];

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


window.chartInstances = new Map();

window.drawChart_Func = function(config)
{
    console.log("✅ JS drawChart_Func called", config);
    const canvas = document.getElementById(config.canvasID);
    canvas.width = canvas.parentElement.clientWidth;
    canvas.height = 300;

    const ctx = canvas.getContext('2d');

    if(chartInstances.has(config.canvasID))
    {
        chartInstances.get(config.canvasID).destroy();
    }

    const chart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: config.labels,
            datasets: [{
                label: config.yTitle,
                data: config.data,
                borderColor: 'rgba(54, 162, 235, 1)',
                backgroundColor: 'rgba(54, 162, 235, 0.2)',
                tension: 0.1,
                pointRadius: 3
            }]
        },
        options: {
            responsive: true,
            animation: false,
            plugins: {
                title: {
                    display: true,
                    text: config.chartTitle,
                    font: { size: 24 }
                }
            },
            scales: {
                y: {
                    min: config.yMin,
                    max: config.yMax,
                    title: {
                        display: true,
                        text: config.yTitle,
                        font: { size: 22 }
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: config.xTitle,
                        font: { size: 22 }
                    }
                }
            }
        }
    });

    chartInstances.set(config.canvasID, chart);
};

window.updateChartData = function (canvasID, label, value)
{
    const chart = chartInstances.get(canvasID);
    if(!chart)
    {
        console.warn("Chart not found:", canvasID);
        return;
    }

    chart.data.labels.push(label);
    chart.data.datasets[0].data.push(value);

    if(chart.data.labels.length > 10)
    {
        chart.data.labels.shift();
        chart.data.datasets[0].data.shift();
    }

    chart.update();
};

window.destroyChart = function (canvasID) {
    const chart = window.chartInstances.get(canvasID);
    if (chart) {
        chart.destroy();
        window.chartInstances.delete(canvasID);
        console.log(`[Chart] Destroyed chart with ID: ${canvasID}`);
    } else {
        console.warn(`[Chart] No chart found with ID: ${canvasID}`);
    }
};