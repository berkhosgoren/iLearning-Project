window.InventoryDiscussionPoll = (function () {
    function init(options) {
        const listEl = document.getElementById(options.listElementId);
        const emptyEl = document.getElementById(options.emptyElementId);
        if (!listEl || !emptyEl || !options.feedUrl) return;

        const pollMs = options.pollMs || 10000;
        let lastSnapshot = "";

        function escapeHtml(value) {
            return (value || "")
                .replaceAll("&", "&amp;")
                .replaceAll("<", "&lt;")
                .replaceAll(">", "&gt;")
                .replaceAll(`"`, "&quot;")
                .replaceAll("'", "&#39;");
        }

        function formatLocalDate(utcValue) {
            const date = new Date(utcValue);
            if (Number.isNaN(date.getTime())) return "";
            return date.toLocaleString([], {
                year: "numeric",
                month: "2-digit",
                day: "2-digit",
                hour: "2-digit",
                minute: "2-digit"
            });
        }

        function buildDeleteForm(commentId) {
            if (!options.inventoryId || !commentId) return "";

            const action = `/inventories/${encodeURIComponent(options.inventoryId)}/discussion/comments/${encodeURIComponent(commentId)}/delete`;

            return `
            <form method="post" action="${action}" class="m-0">
                <input name="__RequestVerificationToken" type="hidden" value="${escapeHtml(options.requestVerificationToken || "")}" />
                <button class="btn btn-outline-danger btn-sm" type="submit">${escapeHtml(options.deleteText || "Delete")}</button>
            </form>`;
        }

        function renderComments(comments) {
            if (!Array.isArray(comments) || comments.length === 0) {
                listEl.innerHTML = "";
                listEl.classList.add("d-none");
                emptyEl.classList.remove("d-none");
                return;
            }

            emptyEl.classList.add("d-none");
            listEl.classList.remove("d-none");

            const html = comments.map(function (c) {
                return `
                <div class="border rounded p-3">
                    <div class="d-flex align-items-start justify-content-between gap-2">
                       <div>
                           <div class="fw-semibold">${escapeHtml(c.userName)}</div>
                           <div class="text-muted small">${formatLocalDate(c.createdAtUtc)}</div>
                       </div>
                       ${c.canDelete ? buildDeleteForm(c.id) : ""}
                    </div>
                    <div class="mt-2">${escapeHtml(c.body)}</div>
                </div>`;
            }).join("");

            listEl.innerHTML = html;
        }

        async function refresh() {
            try {
                const res = await fetch(options.feedUrl, {
                    headers: {
                        "Accept": "application/json"
                    },
                    cache: "no-store"
                });

                if (!res.ok) return;

                const data = await res.json();
                const comments = data && Array.isArray(data.comments) ? data.comments : [];
                const snapshot = JSON.stringify(comments);

                if (snapshot === lastSnapshot) return;

                lastSnapshot = snapshot;
                renderComments(comments);
            } catch (err) {
                console.error("InventoryDiscussionPoll refresh failed:", err);
            }
        }

        refresh();
        window.setInterval(refresh, pollMs);
    }

    return {
        init: init
    };
})();