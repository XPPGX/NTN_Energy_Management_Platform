window.applyDynamicCss = function(cssContent)
{
    let styleElement = document.getElementById('dynamic-theme-style');
    if(!styleElement)
    {
        styleElement = document.createElement('style');
        styleElement.id = 'dynamic-theme-style';
        document.head.appendChild(styleElement);
    }
    styleElement.innerHTML = cssContent;
    console.log("[applyDynamicCss] Dynamic Css Applied")
}