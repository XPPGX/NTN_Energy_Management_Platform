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
                delay: 300,  // 減少延遲時間從 1000ms 到 300ms
                delayOnTouchOnly: true,
                animation: 150,
                handle: '.module-card',
                draggable: '.module-wrapper',
                touchStartThreshold: 20,    // 增加觸摸開始閾值，避免輕微移動就觸發
                preventOnFilter: false,     // 確保拖動不會被其他事件阻止
                scroll: false,              // 禁用 SortableJS 的自動滾動
                scrollSensitivity: 0,       // 設置滾動靈敏度為 0
                scrollSpeed: 0,             // 設置滾動速度為 0
                onStart: function(evt) {
                    console.log('🔒 拖動開始 - 阻止頁面滾動');
                    // 拖動開始時阻止頁面滾動
                    document.body.style.overflow = 'hidden';
                    document.body.style.touchAction = 'none';
                    
                    // 同時設置容器樣式
                    const container = document.getElementById('sortable-modules');
                    if (container) {
                        container.style.overflow = 'hidden';
                        container.style.touchAction = 'none';
                    }
                    
                    // 添加強力阻止頁面滾動的事件監聽器
                    const preventScroll = function(e) {
                        console.log('🚫 阻止滾動事件:', e.type);
                        e.preventDefault();
                        e.stopPropagation();
                        return false;
                    };
                    
                    // 阻止各種滾動事件
                    document.addEventListener('touchmove', preventScroll, { passive: false });
                    document.addEventListener('wheel', preventScroll, { passive: false });
                    document.addEventListener('scroll', preventScroll, { passive: false });
                    
                    // 將阻止函數保存到全域，以便稍後移除
                    window._sortablePreventScroll = preventScroll;
                },
                onEnd: function (evt) {     // 拖動完成後觸發
                    console.log('🔓 拖動結束 - 恢復頁面滾動');
                    // 恢復頁面滾動
                    document.body.style.overflow = '';
                    document.body.style.touchAction = '';
                    
                    // 恢復容器樣式
                    const container = document.getElementById('sortable-modules');
                    if (container) {
                        container.style.overflow = '';
                        container.style.touchAction = '';
                    }
                    
                    // 移除阻止滾動的事件監聽器
                    if (window._sortablePreventScroll) {
                        document.removeEventListener('touchmove', window._sortablePreventScroll);
                        document.removeEventListener('wheel', window._sortablePreventScroll);
                        document.removeEventListener('scroll', window._sortablePreventScroll);
                        window._sortablePreventScroll = null;
                        console.log('✅ 已移除滾動阻止監聽器');
                    }

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