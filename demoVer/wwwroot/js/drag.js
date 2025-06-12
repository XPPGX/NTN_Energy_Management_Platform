window.initSortable = function () {
    const container = document.getElementById('sortable-modules');
    if (container) {
        new Sortable(container, {
            animation: 150,
            handle: '.module-card',
            touchStartThreshold: 10, // 避免誤觸
            // 可加入其他設定，例如 dragClass, ghostClass...
        });
    }
};