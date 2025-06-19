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

let sort_object = null;
window.initSortable = function (enable_flag) {
    const container = document.getElementById('sortable-modules');
    if(container)
    {
        if(!sort_object)
        {
            sort_object = new Sortable(container, {
                delay: 1000,
                delayOnTouchOnly: true,
                animation: 150,
                handle: '.module-card',
                draggable: '.module-wrapper',
                touchStartThreshold: 10,    // 避免誤觸
                onEnd: function (evt) {     // 拖動完成後觸發
                    const wrappers = container.querySelectorAll('.module-wrapper');
                    let order = [];

                    wrappers.forEach((el, index) => {
                        const id = el.getAttribute("cardID_ForSorting");
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