let sort_object = null;
window.initSortable = function (enable_flag) {
    const container = document.getElementById('sortable-modules');
    let isMobile = /Android|iPhone|iPad|iPod/i.test(navigator.userAgent);
    let mobile_sortable = null;
    
    // if(isMobile)
    // {   
    //     console.log("Mobile");
    //     let pressTimer;
    //     if(container)
    //     {
    //         container.querySelectorAll('.module-card').forEach(el => {
    //             el.addEventListener('pointerdown', e => {
    //                 pressTimer = setTimeout(() => {
    //                     console.log("mobile press 1s");
                        
    //                     if(!mobile_sortable)
    //                     {
    //                         mobile_sortable = new Sortable(container, {
    //                             animation: 150,
    //                             handle: '.module-card',
    //                             draggable: '.module-wrapper',
    //                             touchStartThreshold: 10, // 避免誤觸
    //                             onEnd:() =>{
    //                                 mobile_sortable.option("disabled", true);
    //                             }
    //                             // 可加入其他設定，例如 dragClass, ghostClass...
    //                         });
    //                     }
    //                     else
    //                     {
    //                         mobile_sortable.option("disabled", false);
    //                     }
    //                 }, 1000);
    //             });
    
    //             el.addEventListener('pointerup', () => {

    //                 clearTimeout(pressTimer);
    //             });
    
    //             el.addEventListener('pointerleave', () => {
    //                 clearTimeout(pressTimer);
    //             })
    //         })
    //     }
    // }
    // else
    // {
        if(container)
        {
            if(!sort_object)
            {
                sort_object = new Sortable(container, {
                    animation: 150,
                    handle: '.module-card',
                    draggable: '.module-wrapper',
                    touchStartThreshold: 10, // 避免誤觸
                    // 可加入其他設定，例如 dragClass, ghostClass...
                });
            }
            
            if(sort_object)
            {
                sort_object.option("disabled", !enable_flag);
            }
        }
    // }
};