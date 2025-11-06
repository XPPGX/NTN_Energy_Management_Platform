(function () {
    const SLIDER_SELECTOR = 'input[type="range"].custom-slider';
    const FLAG_KEY = 'sliderFillBound';

    function toNumber(value, fallback) {
        const parsed = Number(value);
        return Number.isFinite(parsed) ? parsed : fallback;
    }

    function applyFill(input) {
        const min = toNumber(input.min, 0);
        const max = toNumber(input.max, 100);
        const value = toNumber(input.value, min);
        const safeRange = max - min;
        const denominator = Number.isFinite(safeRange) && safeRange !== 0 ? safeRange : 1;
        const percent = ((value - min) / denominator) * 100;
        const clamped = Math.min(Math.max(percent, 0), 100);
        input.style.setProperty('--value', `${clamped}%`);
    }

    function bindSlider(input) {
        if (!(input instanceof HTMLInputElement)) {
            return;
        }

        if (input.dataset[FLAG_KEY] === 'true') {
            applyFill(input);
            return;
        }

        input.dataset[FLAG_KEY] = 'true';
        applyFill(input);

        const handler = (event) => {
            if (event.target instanceof HTMLInputElement) {
                applyFill(event.target);
            }
        };

        input.addEventListener('input', handler);
        input.addEventListener('change', handler);
    }

    function refresh(target) {
        const root = target ?? document;
        root.querySelectorAll(SLIDER_SELECTOR).forEach(bindSlider);
    }

    const observer = new MutationObserver((mutations) => {
        for (const mutation of mutations) {
            mutation.addedNodes.forEach((node) => {
                if (!(node instanceof HTMLElement)) {
                    return;
                }

                if (node.matches(SLIDER_SELECTOR)) {
                    bindSlider(node);
                    return;
                }

                node.querySelectorAll(SLIDER_SELECTOR).forEach(bindSlider);
            });
        }
    });

    function startObserving() {
        if (!document.body) {
            return;
        }

        refresh();
        observer.observe(document.body, { childList: true, subtree: true });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', startObserving, { once: true });
    } else {
        startObserving();
    }

    window.sliderFill = {
        refresh: () => refresh(),
        bind: (element) => {
            if (!element) {
                return;
            }
            bindSlider(element);
        },
        updateById: (id) => {
            const slider = document.getElementById(id);
            if (slider) {
                bindSlider(slider);
            }
        }
    };
})();