(function () {
    document.addEventListener('wheel', function (e) {
        var t = e.target;
        if (t && typeof t.closest === 'function' && t.closest('.maindemo-wheel-blocked')) {
            e.preventDefault();
            e.stopImmediatePropagation();
        }
    }, { capture: true, passive: false });
})();
