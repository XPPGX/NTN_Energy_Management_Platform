window.initSortableIfDesktop = function () {
    // 偵測是不是桌面裝置（簡易檢查）
    let isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
    if (!isMobile) {
        new Sortable(document.getElementById('sortable-modules'), {
            animation: 150,
            handle: '.module-card-dragable',   // 可自訂拖曳的部位
            // 其他 options...
        });
    }
    // 如果是手機，什麼都不做
};