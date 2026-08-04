(function () {
    "use strict";

    if (!window.signalR) {
        console.warn("BrewPoint realtime: SignalR client is unavailable.");
        return;
    }

    const path = window.location.pathname.toLowerCase();
    let reloadTimer = null;
    let pendingWhileHidden = false;
    let formIsDirty = false;
    let suppressReloadUntil = 0;

    window.BrewPointRealtime = {
        suppressReloadFor(milliseconds = 5000) {
            const duration = Number.isFinite(milliseconds) ? Math.max(0, milliseconds) : 5000;
            suppressReloadUntil = Math.max(suppressReloadUntil, Date.now() + duration);
        }
    };

    const relevantTypes = () => {
        if (path.startsWith("/cashier")) return ["Order", "OrderDetail", "Payment", "RestaurantTable", "Reservation", "Product", "Customer", "Promotion"];
        if (path.startsWith("/customer/orders")) return ["Order", "OrderDetail", "Payment", "RestaurantTable", "Product", "Category", "Promotion", "PointHistory"];
        if (path.startsWith("/customer/reservations")) return ["Reservation", "RestaurantTable"];
        if (path === "/admin" || path.startsWith("/admin/dashboard")) return ["Order", "OrderDetail", "Payment", "RestaurantTable", "Reservation", "Product", "Customer", "PaymentAccountSetting", "PaymentGatewaySetting"];
        if (path.startsWith("/admin/orders")) return ["Order", "OrderDetail", "Payment"];
        if (path.startsWith("/admin/restauranttables")) return ["RestaurantTable", "Order", "Reservation"];
        if (path.startsWith("/admin/reservations")) return ["Reservation", "RestaurantTable"];
        if (path.startsWith("/admin/products")) return ["Product", "OrderDetail"];
        if (path.startsWith("/admin/categories")) return ["Category", "Product"];
        if (path.startsWith("/admin/customers")) return ["Customer", "Order", "Reservation", "PointHistory", "Review"];
        if (path.startsWith("/admin/employees")) return ["Employee", "User"];
        return [];
    };

    const watchedTypes = relevantTypes();
    if (watchedTypes.length === 0) return;

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
        return formIsDirty || /\/(create|edit)(\/|$)/.test(path);
    }

    function refreshPage(payload) {
        window.dispatchEvent(new CustomEvent("brewpoint:statechanged", { detail: payload }));

        // Mutations initiated by this tab can be echoed back through SignalR before
        // their modal/confirmation flow finishes. Other tabs still receive and act
        // on the event; only this tab skips its short-lived self-triggered reload.
        if (Date.now() < suppressReloadUntil) return;

        if (isEditing()) {
            showUpdateNotice();
            return;
        }
        if (document.hidden) {
            pendingWhileHidden = true;
            return;
        }

        window.clearTimeout(reloadTimer);
        reloadTimer = window.setTimeout(() => window.location.reload(), 700);
    }

    document.addEventListener("visibilitychange", () => {
        if (!document.hidden && pendingWhileHidden) {
            pendingWhileHidden = false;
            window.location.reload();
        }
    });

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/app-state")
        .withAutomaticReconnect([0, 1000, 3000, 5000, 10000])
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    connection.on("StateChanged", payload => {
        const changedTypes = payload && Array.isArray(payload.entityTypes) ? payload.entityTypes : [];
        if (changedTypes.some(type => watchedTypes.includes(type))) refreshPage(payload);
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
    connection.onreconnected(() => { document.documentElement.dataset.realtime = "connected"; });
    connection.onclose(() => {
        document.documentElement.dataset.realtime = "disconnected";
        window.setTimeout(start, 5000);
    });

    start();
})();
