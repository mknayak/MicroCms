/* ── MicroCMS Admin MVC — Client-side helpers ────────────────────────────────── */

'use strict';

(function () {
    // Auto-dismiss alerts after 5 seconds
    document.querySelectorAll('.alert-dismissible').forEach(function (el) {
        setTimeout(function () {
            var bsAlert = bootstrap.Alert.getOrCreateInstance(el);
            if (bsAlert) bsAlert.close();
        }, 5000);
    });

    // Auto-generate slug from display name / title fields
    var nameInputs = document.querySelectorAll('[data-slug-source]');
    nameInputs.forEach(function (src) {
        var targetId = src.dataset.slugSource;
        var target = document.getElementById(targetId);
        if (!target || target.value) return; // don't overwrite

        src.addEventListener('input', function () {
            target.value = src.value
                .toLowerCase()
                .trim()
                .replace(/[^a-z0-9_\-]+/g, '_')
                .replace(/^_+|_+$/g, '');
        });
    });
})();
