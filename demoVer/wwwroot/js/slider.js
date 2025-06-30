let currentDrag = null;
let isDragging = false;

window.startVerticalSliderDrag = (fill, thumb, dotNetHelper, index) => {
    if(currentDrag)
    {
        currentDrag.stop();
    }
    const container = fill.parentElement;
    const rect = container.getBoundingClientRect();

    function getRect()
    {
        return container.getBoundingClientRect();
    }

    function updateFromY(y) {
        const rect = getRect(); // ← 注意這裡要即時取 rect
        const offset = Math.min(Math.max(y - rect.top, 0), rect.height);
        let rawValue = 100 - (offset / rect.height) * 100;
        
        // 四捨五入到 0.1 精度
        const step = 0.1;
        const value = Math.round(rawValue / step) * step;
        const send_value = Math.round(value * 10) / 10;
        fill.style.height = `${value}%`;
        thumb.style.bottom = `${value}%`;
    
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync("UpdateSliderValue", index, send_value);
        }
    }

    // ✅ 點擊事件處理（只觸發一次）
    container.addEventListener("click", (e) => {
        if(isDragging) return;
        fill.style.transition = "all 0.2s ease-in-out";
        thumb.style.transition = "all 0.2s ease-in-out";
        const clientY = e.touches ? e.touches[0].clientY : e.clientY;
        updateFromY(clientY);
    });


    function onMove(e) {
        isDragging = true;
        fill.style.transition = "none";
        thumb.style.transition = "none";
        const clientY = e.touches ? e.touches[0].clientY : e.clientY;
        updateFromY(clientY);
        
    }

    currentDrag = {stop};

    function stop() {
        document.removeEventListener("mousemove", onMove);
        document.removeEventListener("mouseup", stop);
        document.removeEventListener("touchmove", onMove);
        document.removeEventListener("touchend", stop);
        setTimeout(() => isDragging = false, 100);
        currentDrag = null;
    }
    document.addEventListener("mousemove", onMove);
    document.addEventListener("mouseup", stop);
    document.addEventListener("touchmove", onMove, { passive: false });
    document.addEventListener("touchend", stop, { passive: false });
};

window.updateSilderVisual = function(fill, thumb, value)
{
    value = Math.max(0, Math.min(value, 100));
    fill.style.height = `${value}%`;
    thumb.style.bottom = `${value}%`;
}