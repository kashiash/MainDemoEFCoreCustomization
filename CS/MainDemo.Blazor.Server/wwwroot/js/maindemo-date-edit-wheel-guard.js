let registered = false;

export function ensureRegistered() {
    if (registered) {
        return;
    }

    registered = true;
    document.addEventListener('wheel', function (e) {
        const target = e.target;
        if (!target || typeof target.closest !== 'function') {
            return;
        }

        if (target.closest('.maindemo-dateedit-wheel-allowed')) {
            return;
        }

        if (target.closest('.maindemo-dateedit-wheel-blocked')) {
            e.preventDefault();
            e.stopImmediatePropagation();
        }
    }, { capture: true, passive: false });
}
