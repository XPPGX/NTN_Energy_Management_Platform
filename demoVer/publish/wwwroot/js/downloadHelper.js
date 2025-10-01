window.startDownload = function (url) {
    const a = document.createElement("a");
    a.href = url;
    a.target = "_blank"; // 或用 download 屬性
    a.click();
};
