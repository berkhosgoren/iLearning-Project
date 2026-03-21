window.InventoryAutoSave = (function () {
    function init(options) {
        const form = document.getElementById(options.formId);
        if (!form) return;

        const statusEl = document.getElementById(options.statusElementId);
        const autosaveUrl = options.url;

        function setStatus(stateKey, suffix = '') {
            if (!statusEl) return;

            const prefix = statusEl.dataset.prefix || 'Auto-save:';
            const value = statusEl.dataset[stateKey] || stateKey;

            statusEl.textContent = suffix
                ? (prefix + ' ' + value + ' ' + suffix)
                : (prefix + ' ' + value);
        }

        function formatNow() {
            const now = new Date();
            const atText = statusEl?.dataset.at || 'at';
            const time = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
            return atText + ' ' + time;
        }

        function buildPayloadKey(fd) {
            return Array.from(fd.entries())
                .filter(([k]) => k !== '__RequestVerificationToken')
                .map(([k, v]) => k + '=' + String(v))
                .join('&');
        }

        const tokenEl = form.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenEl ? tokenEl.value : null;

        const idEl = form.querySelector(`input[name="${options.idFieldName}"]`);
        const versionEl = form.querySelector(`input[name="${options.versionFieldName}"]`);

        if (!token || !idEl || !versionEl) {
            setStatus('off');
            return;
        }

        let timer = null;
        let isSaving = false;
        let hasPending = false;
        let lastPayload = '';
        let stoppedByConflict = false;
        let manualSubmitInProgress = false;

        function serializeForm() {
            const fd = new FormData(form);

            if (options.mapFields && Array.isArray(options.mapFields)) {
                options.mapFields.forEach(function (m) {
                    const current = fd.get(m.from);
                    if (current !== null) {
                        fd.set(m.to, current);
                        if (m.removeOriginal) {
                            fd.delete(m.from);
                        }
                    }
                });
            }

            return fd;
        }

        async function doSave() {
            if (stoppedByConflict || manualSubmitInProgress) return;

            const fd = serializeForm();
            const payloadKey = buildPayloadKey(fd);

            if (payloadKey === lastPayload) {
                setStatus('saved', formatNow());
                return;
            }

            if (isSaving) {
                hasPending = true;
                return;
            }

            isSaving = true;
            setStatus('saving');

            try {
                const res = await fetch(autosaveUrl, {
                    method: 'POST',
                    body: fd,
                    headers: token ? { 'RequestVerificationToken': token } : {}
                });

                if (res.status === 409) {
                    stoppedByConflict = true;
                    setStatus('conflict');
                    return;
                }

                if (!res.ok) {
                    setStatus('error');
                    return;
                }

                const data = await res.json();

                if (data && data.newVersion) {
                    versionEl.value = String(data.newVersion);
                }

                lastPayload = buildPayloadKey(serializeForm());
                setStatus('saved', formatNow());
            } catch {
                setStatus('error');
            } finally {
                isSaving = false;

                if (hasPending && !manualSubmitInProgress && !stoppedByConflict) {
                    hasPending = false;
                    setTimeout(doSave, 50);
                }
            }
        }

        function scheduleSave() {
            if (stoppedByConflict || manualSubmitInProgress) return;

            setStatus('pending');

            if (timer) clearTimeout(timer);

            timer = setTimeout(doSave, options.delayMs || 800);
        }

        form.addEventListener('input', function (e) {
            const el = e.target;
            if (!el || manualSubmitInProgress) return;

            if (el.matches('input, textarea, select')) {
                scheduleSave();
            }
        });

        form.addEventListener('change', function (e) {
            const el = e.target;
            if (!el || manualSubmitInProgress) return;

            if (el.matches('input[type="checkbox"], input[type="radio"], select')) {
                scheduleSave();
            }
        });

        lastPayload = buildPayloadKey(serializeForm());
        setStatus('ready');
    }

    return {
        init: init
    };
})();

    