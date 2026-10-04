(() => {
    let initialized = false;
    let dialog = null;
    let returnFocus = null;
    let inertElements = [];
    const focusableSelector = 'button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), a[href], [tabindex]:not([tabindex="-1"])';

    function focusable(root) {
        return [...root.querySelectorAll(focusableSelector)]
            .filter(element => !element.closest('[inert]') && element.getClientRects().length > 0);
    }

    function focusFirst() {
        (focusable(dialog)[0] ?? dialog).focus();
    }

    function restoreBackground() {
        for (const element of inertElements) element.inert = false;
        inertElements = [];
    }

    window.desktopAccessibility = {
        initialize() {
            if (initialized) return;
            initialized = true;
            document.addEventListener('focusin', event => {
                if (dialog?.isConnected && !dialog.contains(event.target)) focusFirst();
            });
            document.addEventListener('keydown', event => {
                if (dialog?.isConnected) {
                    if (event.key === 'Escape') {
                        event.preventDefault();
                        event.stopPropagation();
                        dialog.querySelector('button[data-dialog-close]')?.click();
                        return;
                    }
                    if (event.key === 'Tab') {
                        const elements = focusable(dialog);
                        const index = elements.indexOf(document.activeElement);
                        if (elements.length === 0) {
                            event.preventDefault();
                            dialog.focus();
                        } else if (event.shiftKey && index <= 0) {
                            event.preventDefault();
                            elements.at(-1).focus();
                        } else if (!event.shiftKey && (index < 0 || index === elements.length - 1)) {
                            event.preventDefault();
                            elements[0].focus();
                        }
                    }
                }
                const radio = event.target.closest('[role="radio"]');
                const group = radio?.closest('[role="radiogroup"]');
                if (!group || !['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End'].includes(event.key)) return;
                const radios = [...group.querySelectorAll('[role="radio"]')].filter(element => !element.disabled);
                const index = radios.indexOf(radio);
                const next = event.key === 'Home' ? 0 : event.key === 'End' ? radios.length - 1 :
                    (index + (['ArrowLeft', 'ArrowUp'].includes(event.key) ? -1 : 1) + radios.length) % radios.length;
                event.preventDefault();
                radios[next]?.focus();
                radios[next]?.click();
            }, true);
        },
        syncDialog() {
            for (const group of document.querySelectorAll('[role="radiogroup"]')) {
                const radios = [...group.querySelectorAll('[role="radio"]')];
                const selected = radios.find(radio => radio.getAttribute('aria-checked') === 'true') ?? radios[0];
                for (const radio of radios) radio.tabIndex = radio === selected ? 0 : -1;
            }
            const next = [...document.querySelectorAll('[role="dialog"][aria-modal="true"]')].at(-1) ?? null;
            if (next === dialog) return;
            const previousFocus = returnFocus;
            restoreBackground();
            dialog = next;
            if (!dialog) {
                returnFocus = null;
                if (previousFocus?.isConnected) previousFocus.focus();
                return;
            }
            returnFocus = previousFocus?.isConnected ? previousFocus : document.activeElement;
            let ancestor = dialog;
            while (ancestor.parentElement && ancestor !== document.body) {
                for (const sibling of ancestor.parentElement.children) {
                    if (sibling !== ancestor && sibling instanceof HTMLElement && !sibling.inert) {
                        sibling.inert = true;
                        inertElements.push(sibling);
                    }
                }
                ancestor = ancestor.parentElement;
            }
            focusFirst();
        }
    };
})();
