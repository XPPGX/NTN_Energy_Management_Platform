window.initSortableRows = (dotNetHelper) => {
    ['row1', 'row2'].forEach(id => {
        const el = document.getElementById(id);
        if (el) {
            new Sortable(el, {
                group: 'shared-row',
                animation: 150,
                onEnd: function () {
                    const row1Order = Array.from(document.getElementById('row1').children)
                        .map(el => el.getAttribute('data-id'));
                    const row2Order = Array.from(document.getElementById('row2').children)
                        .map(el => el.getAttribute('data-id'));
                    dotNetHelper.invokeMethodAsync('UpdateRowOrders', row1Order, row2Order);
                }
            });
        }
    });
};