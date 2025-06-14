window.isMobileDevice = function() {
    return /Android|iPhone|iPad|iPod/i.test(navigator.userAgent);
}


let sort_object = null;
window.initGridUI = function()
{
    if(window.isMobileDevice){
        console.log("Using SortableJS for mobile");
        const grid = GridStack.init({
            cellHeight: 100,
            float: true,
            resizable: false,
            margin: 5,
        });
        Sortable.create(document.getElementById('grid-stack-container'), {
            animation: 150,
            delay: 200,  // 手機長按觸發拖曳
            delayOnTouchOnly: true,
            draggable: '.grid-stack-item',
            ghostClass: 'sortable-ghost',
            onEnd: function (evt) {
                console.log(`Moved from ${evt.oldIndex} to ${evt.newIndex}`);
                // 這裡可以儲存排序順序
            }
        });
    }
    else{
        console.log("Using GridStack for desktop");
        const grid = GridStack.init({
            cellHeight: 100,
            float: true,
            resizable: false,
            margin: 5,
            // maxRow: 9,
            draggable: {
                handle: '.module-card',
                appendTo: '#grid-stack-container',
                scroll: false
            }
        });
    
        grid.on('dragstop', function (event, el) {
            compressGridBothAxis(grid);
            // grid.compact();
        });
    
        window.gridInstance = grid;
    }
}

function compressGridBothAxis(grid) {
    let nodes = [...grid.engine.nodes];

    // 依照 y 值排序，然後群組每一列
    let rows = {};
    nodes.forEach(n => {
        if (!rows[n.y]) rows[n.y] = [];
        rows[n.y].push(n);
    });

    // 對每一列做 x 軸壓縮
    Object.values(rows).forEach(row => {
        row.sort((a, b) => a.x - b.x); // 確保順序
        let currentX = 0;
        row.forEach(n => {
            if (n.x !== currentX) {
                grid.update(n.el, { x: currentX });
            }
            currentX += n.w;
        });
    });

    // 再往上壓縮
    grid.compact();
}
window.initGridStack = function () {
    const grid = GridStack.init({
        cellHeight: 100,
        float: true,
        resizable: false,
        margin: 5,
        // maxRow: 9,
        draggable: {
            handle: '.module-card',
            appendTo: '#grid-stack-container',
            scroll: false
        }
    });

    grid.on('dragstop', function (event, el) {
        compressGridBothAxis(grid);
        // grid.compact();
    });

    window.gridInstance = grid;
};


window.DotNetHelperRegister = function(helper){
    window.DotNetHelper = helper;
};

window.getCardOrder = function(){
    const container = document.getElementById('sortable-modules');
    const wrappers = container.querySelectorAll('.module-wrapper');
    let order = [];

    wrappers.forEach((el, index) => {
        const id = el.getAttribute("cardID");
        order.push({ id, position: index });
    });

    console.log("New order:", order);

    // 如果要傳回 Razor 端，可以透過 JS interop
    if (window.DotNetHelper) {
        window.DotNetHelper.invokeMethodAsync('UpdateCardOrderFromJS', order.map(o => o.id));
    }
}


window.initSortable = function (enable_flag) {
    const container = document.getElementById('sortable-modules');
    if(container)
    {
        if(!sort_object)
        {
            sort_object = new Sortable(container, {
                animation: 150,
                handle: '.module-card',
                draggable: '.module-wrapper',
                touchStartThreshold: 10,    // 避免誤觸
                onEnd: function (evt) {     // 拖動完成後觸發
                    const wrappers = container.querySelectorAll('.module-wrapper');
                    let order = [];

                    wrappers.forEach((el, index) => {
                        const id = el.getAttribute("cardID");
                        order.push({ id, position: index });
                    });

                    console.log("New order:", order);

                    // 如果要傳回 Razor 端，可以透過 JS interop
                    if (window.DotNetHelper) {
                        window.DotNetHelper.invokeMethodAsync('UpdateCardOrderFromJS', order.map(o => o.id));
                    }
                }
                // 可加入其他設定，例如 dragClass, ghostClass...
            });
        }
        
        if(sort_object)
        {
            sort_object.option("disabled", !enable_flag);
        }
    }
};