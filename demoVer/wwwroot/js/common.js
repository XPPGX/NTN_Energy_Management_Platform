window.checkIsMobile = function () {
    return window.matchMedia("(max-width: 1200px)").matches;
}

window.scrollToBottom = function () {
    console.log("scroll to bottom !!!");
    window.scrollTo({
        top: document.body.scrollHeight,
        behavior: 'smooth'
    });
}

function jumpToApp(port) {
    const host = window.location.hostname;
    window.location.href = `http://${host}:${port}/`;
}


// window.registerGroupedTableTouchAndRightClick = function (containerId, dotNetHelper, pressTime = 800) {
//     const container = document.getElementById(containerId);
//     if (!container) return;

//     const isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
//     let timer = null;

//     function getElementInfo(el) {
//         const table = el.getAttribute("data-table");
//         const index = parseInt(el.getAttribute("data-index"));
//         const rect = el.getBoundingClientRect();
//         return { table, index, x: rect.right, y: rect.top + rect.height };
//     }

//     container.addEventListener("touchstart", function (e) {
//         if (!isMobile) return;
//         const el = e.target.closest(".long-press-row");
//         if (!el) return;
//         timer = setTimeout(() => {
//             const info = getElementInfo(el);
//             dotNetHelper.invokeMethodAsync("OnShowContextMenu", info.table, info.index, info.x, info.y);
//         }, pressTime);
//     });

//     container.addEventListener("touchend", () => clearTimeout(timer));
//     container.addEventListener("touchmove", () => clearTimeout(timer));

//     container.addEventListener("contextmenu", function (e) {
//         if (isMobile) return;
//         const el = e.target.closest(".long-press-row");
//         if (!el) return;
//         e.preventDefault();
//         const info = getElementInfo(el);
//         dotNetHelper.invokeMethodAsync("OnShowContextMenu", info.table, info.index, info.x, info.y);
//     });
// };

let longPressTriggered = false;
let suppressNextClick = false;
window.registerGroupedTableTouchAndRightClick = function (containerId, dotNetHelper, pressTime = 800) {
    const container = document.getElementById(containerId);
    if (!container) return;

    const isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
    let timer = null;

    function getElementInfo(el) {
        const table = el.getAttribute("data-table");
        const index = parseInt(el.getAttribute("data-index"));
        // const rect = el.getBoundingClientRect();
        return { table, index};
    }

    container.addEventListener("touchstart", function (e) {
        if (!isMobile) return;
        const el = e.target.closest(".long-press-row");
        const touch = e.touches[0];
        if (!el) return;
        timer = setTimeout(() => {
            longPressTriggered = true;
            suppressNextClick = true;
            const info = getElementInfo(el);
            const x = touch.clientX;
            const y = touch.clientY;
            dotNetHelper.invokeMethodAsync("OnShowContextMenu", info.table, info.index, x, y);
        }, pressTime);
        console.log("[touchstart]", e);
        dotNetHelper.invokeMethodAsync("JS_Console", "[touchstart]" + e);
    }, {passive:true});

    container.addEventListener("touchend", function(e)
    {
        if(!longPressTriggered){
            clearTimeout(timer);
        }
        longPressTriggered = false;
        // setTimeout(() => longPressTriggered = false, 500);
        // console.log("[touchend]", e);
        // dotNetHelper.invokeMethodAsync("JS_Console", "[touchend]" + e);
    });

    container.addEventListener("click", function(e){
        if(suppressNextClick)
        {
            e.stopPropagation();
            e.preventDefault();
            suppressNextClick = false;
            console.log("Click prevented after long press");
        }
        dotNetHelper.invokeMethodAsync("JS_Console", "[click]" + e);
    },true);

    container.addEventListener("touchmove", function(e)
    {
        clearTimeout(timer);
        console.log("[touchmove]", e);
        dotNetHelper.invokeMethodAsync("JS_Console", "[touchmove]" + e);
    });



    //For computer
    container.addEventListener("contextmenu", function (e) {
        if (isMobile) return;
        const el = e.target.closest(".long-press-row");
        if (!el) return;
        e.preventDefault();
        const info = getElementInfo(el);
        const x = e.clientX
        const y = e.clientY
        dotNetHelper.invokeMethodAsync("OnShowContextMenu", info.table, info.index, x, y);
    });
};