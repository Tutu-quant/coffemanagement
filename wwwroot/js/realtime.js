(function () {
    "use strict";

    const path = window.location.pathname.toLowerCase();
    const pageConfiguration = document.documentElement.dataset;
    const configuredArea = (pageConfiguration.brewpointArea || "").toLowerCase();
    const configuredController = (pageConfiguration.brewpointController || "").toLowerCase();
    const configuredAction = (pageConfiguration.brewpointAction || "").toLowerCase();
    const routePath = configuredArea && configuredController
        ? `/${configuredArea}/${configuredController}/${configuredAction}`
        : path;
    const staffArea = configuredArea === "admin" || configuredArea === "cashier"
        ? configuredArea
        : path.startsWith("/admin")
            ? "admin"
            : path.startsWith("/cashier")
                ? "cashier"
                : null;
    const staffNotificationsEnabled = staffArea !== null && !routePath.startsWith("/cashier/orders/print");
    const pendingReservationEndpoint = pageConfiguration.brewpointPendingReservationUrl
        || "/api/notifications/reservations/pending-count";
    const reservationDetailsBase = pageConfiguration.brewpointReservationDetailsBase || "";
    const realtimeHubUrl = pageConfiguration.brewpointHubUrl || "/hubs/app-state";
    const queuedNotificationsKey = "brewpoint:reservation-notifications";
    const shownReservationIds = new Set();
    const notificationsWaitingForReload = new Set();

    let reloadTimer = null;
    let pendingWhileHidden = false;
    let formIsDirty = false;
    let suppressReloadUntil = 0;
    let suppressedPayload = null;
    let suppressedTimer = null;
    let pendingCountRequest = null;
    let pendingCountRefreshQueued = false;

    window.BrewPointRealtime = {
        suppressReloadFor(milliseconds = 5000) {
            const duration = Number.isFinite(milliseconds) ? Math.max(0, milliseconds) : 5000;
            suppressReloadUntil = Math.max(suppressReloadUntil, Date.now() + duration);
        },
        markDirty() { formIsDirty = true; },
        clearDirty() { formIsDirty = false; },
        refreshReservationNotifications() { return refreshPendingReservationCount(); }
    };

    const relevantTypes = () => {
        if (routePath.startsWith("/cashier/orders/print")) return [];
        if (routePath.startsWith("/cashier/pos")) return ["Order", "OrderDetail", "Payment", "RestaurantTable", "Product", "Customer", "Promotion", "PointHistory", "OrderPointRedemption", "Voucher"];
        if (routePath.startsWith("/cashier/payments")) return ["Order", "OrderDetail", "Payment", "RestaurantTable", "Customer", "Promotion", "PointHistory", "OrderPointRedemption", "Voucher"];
        if (routePath.startsWith("/cashier/orders")) return ["Order", "OrderDetail", "Payment"];
        if (routePath.startsWith("/cashier/reservations")) return ["Reservation", "RestaurantTable", "Customer"];
        if (routePath.startsWith("/cashier/tables")) return ["RestaurantTable", "Order", "Reservation"];
        if (routePath.startsWith("/cashier")) return ["Order", "OrderDetail", "Payment", "RestaurantTable", "Reservation", "Product", "Customer", "Promotion"];
        if (routePath.startsWith("/customer/orders")) return ["Order", "OrderDetail", "Payment", "RestaurantTable", "Product", "Category", "Promotion", "PointHistory", "OrderPointRedemption", "Voucher", "Customer"];
        if (routePath.startsWith("/customer/reservations")) return ["Reservation", "RestaurantTable"];
        if (routePath === "/admin" || routePath.startsWith("/admin/dashboard")) return ["Order", "OrderDetail", "Payment", "RestaurantTable", "Reservation", "Product", "Customer", "PaymentAccountSetting", "PaymentGatewaySetting"];
        if (routePath.startsWith("/admin/orders")) return ["Order", "OrderDetail", "Payment"];
        if (routePath.startsWith("/admin/restauranttables")) return ["RestaurantTable", "Order", "Reservation"];
        if (routePath.startsWith("/admin/reservations")) return ["Reservation", "RestaurantTable"];
        if (routePath.startsWith("/admin/products")) return ["Product", "OrderDetail"];
        if (routePath.startsWith("/admin/categories")) return ["Category", "Product"];
        if (routePath.startsWith("/admin/customers")) return ["Customer", "Order", "Reservation", "Review", "PointHistory", "OrderPointRedemption"];
        if (routePath.startsWith("/admin/employees")) return ["Employee", "User"];
        return [];
    };

    const watchedTypes = relevantTypes();

    function getChangeValue(change, camelCaseName, pascalCaseName) {
        if (!change || typeof change !== "object") return null;
        return change[camelCaseName] ?? change[pascalCaseName] ?? null;
    }

    function updateReservationBadges(count) {
        const safeCount = Number.isFinite(count) ? Math.max(0, Math.trunc(count)) : 0;
        const displayCount = safeCount > 99 ? "99+" : String(safeCount);
        const accessibleLabel = `${safeCount} lịch đặt bàn đang chờ`;

        document.querySelectorAll("[data-reservation-notification-count]").forEach(badge => {
            badge.textContent = displayCount;
            badge.hidden = safeCount === 0;
            badge.setAttribute("aria-label", accessibleLabel);
        });

        document.querySelectorAll(".staff-notification-shortcut").forEach(link => {
            link.setAttribute("title", accessibleLabel);
            link.setAttribute("aria-label", safeCount > 0
                ? `Mở danh sách ${accessibleLabel}`
                : "Mở danh sách lịch đặt bàn");
        });
    }

    async function refreshPendingReservationCount() {
        if (!staffNotificationsEnabled) return null;
        if (pendingCountRequest) {
            pendingCountRefreshQueued = true;
            return pendingCountRequest;
        }

        pendingCountRequest = fetch(pendingReservationEndpoint, {
            credentials: "same-origin",
            cache: "no-store",
            headers: { "Accept": "application/json" }
        })
            .then(async response => {
                if (!response.ok || response.redirected) return null;
                const contentType = response.headers.get("content-type") || "";
                if (!contentType.includes("application/json")) return null;

                const data = await response.json();
                const count = Number(data && data.pendingReservations);
                if (!Number.isFinite(count)) return null;
                updateReservationBadges(count);
                return count;
            })
            .catch(error => {
                console.warn("BrewPoint notifications: could not refresh the pending reservation count.", error);
                return null;
            })
            .finally(() => {
                pendingCountRequest = null;
                if (pendingCountRefreshQueued) {
                    pendingCountRefreshQueued = false;
                    refreshPendingReservationCount();
                }
            });

        return pendingCountRequest;
    }

    function notificationTarget(reservationId) {
        if (reservationId > 0 && reservationDetailsBase) {
            const normalizedBase = reservationDetailsBase.endsWith("/")
                ? reservationDetailsBase
                : `${reservationDetailsBase}/`;
            return `${normalizedBase}${encodeURIComponent(reservationId)}`;
        }
        if (staffArea === "admin" && reservationId > 0)
            return `/Admin/Reservations/Details/${encodeURIComponent(reservationId)}`;
        if (reservationId > 0)
            return `/Cashier/Reservations/Details/${encodeURIComponent(reservationId)}`;
        return reservationDetailsBase || "/Cashier/Reservations?status=Pending";
    }

    function ensureToastRegion() {
        let region = document.getElementById("staff-reservation-toast-region");
        if (region) return region;

        region = document.createElement("div");
        region.id = "staff-reservation-toast-region";
        region.className = "staff-reservation-toast-region";
        region.setAttribute("aria-live", "polite");
        region.setAttribute("aria-label", "Thông báo đặt bàn");
        document.body.appendChild(region);
        return region;
    }

    function dismissToast(toast) {
        if (!toast || !toast.isConnected || toast.classList.contains("is-leaving")) return;
        toast.classList.add("is-leaving");
        window.setTimeout(() => toast.remove(), 220);
    }

    function showReservationNotification(reservationId) {
        if (!staffNotificationsEnabled) return;

        const numericId = Number(reservationId);
        const safeId = Number.isInteger(numericId) && numericId > 0 ? numericId : 0;
        if (safeId > 0 && shownReservationIds.has(safeId)) return;
        if (safeId > 0) shownReservationIds.add(safeId);

        const toast = document.createElement("section");
        toast.className = "staff-reservation-toast";
        toast.setAttribute("role", "status");
        if (safeId > 0) toast.dataset.reservationId = String(safeId);

        const icon = document.createElement("span");
        icon.className = "staff-reservation-toast__icon";
        icon.setAttribute("aria-hidden", "true");
        icon.textContent = "📅";

        const content = document.createElement("div");
        content.className = "staff-reservation-toast__content";

        const title = document.createElement("h2");
        title.className = "staff-reservation-toast__title";
        title.textContent = safeId > 0 ? `Có yêu cầu đặt bàn mới #${safeId}` : "Có yêu cầu đặt bàn mới";

        const message = document.createElement("p");
        message.className = "staff-reservation-toast__message";
        message.textContent = staffArea === "admin"
            ? "Khách hàng vừa gửi yêu cầu. Vui lòng kiểm tra và xác nhận lịch đặt bàn."
            : "Khách hàng vừa gửi yêu cầu đặt bàn. Bạn có thể xem đầy đủ thông tin trong danh sách lịch đặt.";

        const action = document.createElement("a");
        action.className = "staff-reservation-toast__action";
        action.href = notificationTarget(safeId);
        action.textContent = staffArea === "admin" ? "Xem chi tiết" : "Xem lịch đặt";

        const close = document.createElement("button");
        close.className = "staff-reservation-toast__close";
        close.type = "button";
        close.setAttribute("aria-label", "Đóng thông báo");
        close.textContent = "×";
        close.addEventListener("click", () => dismissToast(toast));

        content.append(title, message, action);
        toast.append(icon, content, close);
        ensureToastRegion().prepend(toast);

        let dismissTimer = window.setTimeout(() => dismissToast(toast), 12000);
        const pauseDismiss = () => window.clearTimeout(dismissTimer);
        const resumeDismiss = () => {
            window.clearTimeout(dismissTimer);
            dismissTimer = window.setTimeout(() => dismissToast(toast), 5000);
        };
        toast.addEventListener("mouseenter", pauseDismiss);
        toast.addEventListener("mouseleave", resumeDismiss);
        toast.addEventListener("focusin", pauseDismiss);
        toast.addEventListener("focusout", resumeDismiss);
        toast.addEventListener("keydown", event => {
            if (event.key === "Escape") {
                dismissToast(toast);
            }
        });
    }

    function readQueuedNotifications() {
        try {
            const raw = window.sessionStorage.getItem(queuedNotificationsKey);
            const parsed = raw ? JSON.parse(raw) : [];
            return Array.isArray(parsed) ? parsed : [];
        } catch {
            return [];
        }
    }

    function queueNotificationForReload(reservationId) {
        const numericId = Number(reservationId);
        if (!Number.isInteger(numericId) || numericId <= 0) return;

        try {
            const cutoff = Date.now() - 2 * 60 * 1000;
            const queued = readQueuedNotifications()
                .filter(item => item && Number(item.id) !== numericId && Number(item.createdAt) >= cutoff)
                .slice(-4);
            queued.push({ id: numericId, createdAt: Date.now() });
            window.sessionStorage.setItem(queuedNotificationsKey, JSON.stringify(queued));
            notificationsWaitingForReload.add(numericId);
        } catch {
            // Without storage the staff event still renders the toast immediately.
        }
    }

    function restoreQueuedNotifications() {
        let queued = [];
        try {
            queued = readQueuedNotifications();
            window.sessionStorage.removeItem(queuedNotificationsKey);
        } catch {
            return;
        }

        const cutoff = Date.now() - 2 * 60 * 1000;
        queued
            .filter(item => item && Number(item.createdAt) >= cutoff)
            .slice(-5)
            .forEach(item => showReservationNotification(Number(item.id)));
    }

    function initializeStaffNotifications() {
        if (!staffNotificationsEnabled) return;
        restoreQueuedNotifications();
        refreshPendingReservationCount();
        window.setInterval(refreshPendingReservationCount, 60000);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initializeStaffNotifications, { once: true });
    } else {
        initializeStaffNotifications();
    }

    if (!window.signalR) {
        if (watchedTypes.length > 0 || staffNotificationsEnabled) {
            console.warn("BrewPoint realtime: SignalR client is unavailable.");
        }
        return;
    }

    if (watchedTypes.length === 0 && !staffNotificationsEnabled) return;

    document.querySelectorAll('form[method="post"], form[method="POST"]').forEach(form => {
        form.addEventListener("input", () => { formIsDirty = true; });
        form.addEventListener("change", () => { formIsDirty = true; });
        form.addEventListener("submit", () => { formIsDirty = false; });
    });

    function showUpdateNotice() {
        let notice = document.getElementById("realtime-update-notice");
        if (notice) return;

        notice = document.createElement("div");
        notice.id = "realtime-update-notice";
        notice.setAttribute("role", "status");
        notice.innerHTML = '<span>Dữ liệu vừa được cập nhật ở màn hình khác.</span><button type="button">Tải dữ liệu mới</button>';
        Object.assign(notice.style, {
            position: "fixed",
            right: "20px",
            bottom: "20px",
            zIndex: "10000",
            display: "flex",
            gap: "12px",
            alignItems: "center",
            maxWidth: "430px",
            padding: "12px 14px",
            color: "#fff",
            background: "#2f241f",
            borderRadius: "10px",
            boxShadow: "0 8px 28px rgba(0,0,0,.25)",
            fontSize: "14px"
        });
        const button = notice.querySelector("button");
        Object.assign(button.style, {
            border: "0",
            borderRadius: "7px",
            padding: "7px 10px",
            background: "#f4b860",
            color: "#241a16",
            fontWeight: "600",
            whiteSpace: "nowrap"
        });
        button.addEventListener("click", () => window.location.reload());
        document.body.appendChild(notice);
    }

    function isEditing() {
        return formIsDirty || document.querySelector('.modal.show') !== null || /\/(create|edit)(\/|$)/.test(routePath);
    }

    function refreshPage(payload) {
        window.dispatchEvent(new CustomEvent("brewpoint:statechanged", { detail: payload }));

        // Mutations initiated by this tab can be echoed back through SignalR before
        // their modal/confirmation flow finishes. Other tabs still receive and act
        // on the event; only this tab skips its short-lived self-triggered reload.
        if (Date.now() < suppressReloadUntil) {
            suppressedPayload = payload;
            window.clearTimeout(suppressedTimer);
            suppressedTimer = window.setTimeout(() => {
                const queuedPayload = suppressedPayload;
                suppressedPayload = null;
                if (queuedPayload) refreshPage(queuedPayload);
            }, Math.max(0, suppressReloadUntil - Date.now()) + 50);
            return "suppressed";
        }

        if (isEditing()) {
            showUpdateNotice();
            return "notice";
        }
        if (document.hidden) {
            pendingWhileHidden = true;
            return "hidden";
        }

        window.clearTimeout(reloadTimer);
        reloadTimer = window.setTimeout(() => window.location.reload(), 700);
        return "scheduled";
    }

    document.addEventListener("visibilitychange", () => {
        if (!document.hidden && pendingWhileHidden) {
            pendingWhileHidden = false;
            window.location.reload();
        }
    });

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(realtimeHubUrl)
        .withAutomaticReconnect([0, 1000, 3000, 5000, 10000])
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    connection.on("ReservationCreated", payload => {
        if (!staffNotificationsEnabled) return;

        const reservationIds = payload && Array.isArray(payload.reservationIds)
            ? payload.reservationIds
            : payload && Array.isArray(payload.ReservationIds)
                ? payload.ReservationIds
                : [];
        refreshPendingReservationCount();
        window.dispatchEvent(new CustomEvent("brewpoint:reservationcreated", { detail: payload }));
        reservationIds
            .map(Number)
            .filter(id => !notificationsWaitingForReload.has(id))
            .forEach(showReservationNotification);
    });

    connection.on("StateChanged", payload => {
        const changedTypes = payload && Array.isArray(payload.entityTypes) ? payload.entityTypes : [];
        const changes = payload && Array.isArray(payload.changes) ? payload.changes : [];
        const reservationChanged = changedTypes.includes("Reservation") || changes.some(change =>
            getChangeValue(change, "entityType", "EntityType") === "Reservation");
        const addedReservationIds = staffNotificationsEnabled
            ? changes
                .filter(change =>
                    getChangeValue(change, "entityType", "EntityType") === "Reservation" &&
                    getChangeValue(change, "changeType", "ChangeType") === "Added")
                .map(change => Number(getChangeValue(change, "entityId", "EntityId")))
                .filter(id => Number.isInteger(id) && id > 0)
            : [];
        const reservationAdded = addedReservationIds.length > 0;
        const reservationViewCanReload = [
            "/admin/dashboard",
            "/admin/reservations",
            "/admin/restauranttables",
            "/cashier/dashboard",
            "/cashier/reservations",
            "/cashier/tables"
        ].some(prefix => routePath.startsWith(prefix));

        if (reservationChanged && staffNotificationsEnabled) {
            refreshPendingReservationCount();
        }

        const refreshableTypes = reservationAdded && !reservationViewCanReload
            ? changedTypes.filter(type => type !== "Reservation")
            : changedTypes;
        let refreshDisposition = null;
        if (refreshableTypes.some(type => watchedTypes.includes(type))) {
            refreshDisposition = refreshPage(payload);
        } else if (reservationAdded) {
            // Booking notifications must never reload POS/payment/QR screens.
            // The dedicated staff event renders the toast and updates the badge.
            window.dispatchEvent(new CustomEvent("brewpoint:statechanged", { detail: payload }));
        }

        if (["scheduled", "hidden", "suppressed"].includes(refreshDisposition)) {
            addedReservationIds.forEach(queueNotificationForReload);
        }
    });

    async function start() {
        try {
            await connection.start();
            document.documentElement.dataset.realtime = "connected";
        } catch (error) {
            document.documentElement.dataset.realtime = "disconnected";
            console.warn("BrewPoint realtime: reconnecting...", error);
            window.setTimeout(start, 5000);
        }
    }

    connection.onreconnecting(() => { document.documentElement.dataset.realtime = "reconnecting"; });
    connection.onreconnected(() => {
        document.documentElement.dataset.realtime = "connected";
        refreshPendingReservationCount();
    });
    connection.onclose(() => {
        document.documentElement.dataset.realtime = "disconnected";
        window.setTimeout(start, 5000);
    });

    start();
})();
