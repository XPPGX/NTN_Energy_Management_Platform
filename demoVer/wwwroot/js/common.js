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
