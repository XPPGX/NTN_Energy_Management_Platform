window.getBoundingClientRect = (element, container) => {
    if (!element) return null;
    const rect = element.getBoundingClientRect();
    const containerRect = container.getBoundingClientRect();
    return {
        bottom: rect.bottom - containerRect.top,
        height: rect.height,
        left: rect.left - containerRect.left,
        right: rect.right - containerRect.left,
        top: rect.top - containerRect.top,
        width: rect.width
    };
};