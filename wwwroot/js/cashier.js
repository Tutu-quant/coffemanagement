/**
 * CASHIER DASHBOARD - JavaScript
 * Xử lý các tương tác, refresh dữ liệu, và thông báo real-time
 */

// Configuration
const DASHBOARD_CONFIG = {
    REFRESH_INTERVAL: 30000, // 30 seconds
    NOTIFICATION_TIMEOUT: 5000, // 5 seconds
    AUTO_REFRESH: false
};

// ================================================
// INITIALIZATION
// ================================================

document.addEventListener('DOMContentLoaded', function() {
    initializeDashboard();
});

function initializeDashboard() {
    if (!document.querySelector('.cashier-dashboard')) return;

    console.log('🎯 Initializing Cashier Dashboard...');

    // Add event listeners
    setupTableCardListeners();
    setupNotificationListeners();
    setupRefreshListeners();

    // Start auto-refresh if enabled
    if (DASHBOARD_CONFIG.AUTO_REFRESH) {
        startAutoRefresh();
    }

function openMergeModal(primaryTableId) {
    document.getElementById('mergePrimaryTableId').value = primaryTableId;
    // fetch candidate tables
    fetch(`/Cashier/POS/GetMergeCandidates?primaryTableId=${primaryTableId}`)
        .then(r => r.json())
        .then(data => {
            const list = document.getElementById('mergeTablesList');
            list.innerHTML = '';
            data.forEach(t => {
                const btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'btn btn-outline-primary btn-sm';
                btn.dataset.tableId = t.tableID;
                btn.textContent = `${t.tableNumber} (${t.capacity})`;
                btn.onclick = () => btn.classList.toggle('active');
                list.appendChild(btn);
            });
            var modal = new bootstrap.Modal(document.getElementById('mergeTablesModal'));
            modal.show();
        });
}

document.addEventListener('DOMContentLoaded', function () {
    const confirmBtn = document.getElementById('confirmMergeBtn');
    if (confirmBtn) confirmBtn.addEventListener('click', function () {
        const primaryId = document.getElementById('mergePrimaryTableId').value;
        const buttons = Array.from(document.querySelectorAll('#mergeTablesList button.active'));
        const ids = buttons.map(b => parseInt(b.dataset.tableId));
        fetch('/Cashier/POS/CreateTableGroup', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': getCsrfToken() },
            body: JSON.stringify({ primaryTableId: parseInt(primaryId), secondaryTableIds: ids })
        }).then(r => r.json()).then(res => {
            if (res.success) location.reload();
            else alert(res.message || 'Không thể ghép bàn');
        });
    });
});

function getCsrfToken() {
    const el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : '';
}

    console.log('✅ Dashboard initialized');
}
function setupTableCardListeners() {
    const tableCards = document.querySelectorAll('.table-card');

    tableCards.forEach(card => {
        if (card.hasAttribute('onclick')) return;
        // Hover effects
        card.addEventListener('mouseenter', function() {
            this.style.animation = 'pulse 1s ease-out';
        });

        // Click handler
        card.addEventListener('click', function() {
            const tableId = this.getAttribute('data-table-id');
            const status = this.className.match(/table-card-(\w+)/)[1];
            handleTableClick(tableId, capitalizeStatus(status));
        });
    });
}

function handleTableClick(tableId, status) {
    console.log(`Clicked table ${tableId} with status: ${status}`);

    const baseUrl = window.location.origin;
    const areaUrl = '/Cashier';

    switch (status) {
        case 'Empty':
        case 'Reserved':
        case 'Serving':
        case 'PendingPayment':
            redirectTo(`${areaUrl}/POS?tableId=${tableId}`);
            break;
        case 'Maintenance':
            redirectTo(`${areaUrl}/Tables`);
            break;
    }
}

function capitalizeStatus(status) {
    const statusMap = {
        'empty': 'Empty',
        'reserved': 'Reserved',
        'serving': 'Serving',
        'pendingpayment': 'PendingPayment'
    };
    return statusMap[status.toLowerCase()] || status;
}

function redirectTo(url) {
    window.location.href = url;
}

// ================================================
// NOTIFICATION SYSTEM
// ================================================

function setupNotificationListeners() {
    const notificationBtn = document.querySelector('.btn-notification');
    if (notificationBtn) {
        notificationBtn.addEventListener('click', toggleNotificationPanel);
    }
}

function toggleNotificationPanel() {
    const panel = document.getElementById('dashboardNotifications');
    if (!panel) return;
    panel.scrollIntoView({ behavior: 'smooth', block: 'start' });
    panel.focus({ preventScroll: true });
}

function showNotification(title, message, type = 'info') {
    // Tạo notification toast
    const notification = document.createElement('div');
    notification.className = `notification-toast notification-${type}`;
    notification.innerHTML = `
        <div class="notification-content">
            <strong>${title}</strong>
            <p>${message}</p>
        </div>
    `;

    document.body.appendChild(notification);

    // Auto remove after timeout
    setTimeout(() => {
        notification.remove();
    }, DASHBOARD_CONFIG.NOTIFICATION_TIMEOUT);
}

// ================================================
// REFRESH & AUTO-UPDATE
// ================================================

function setupRefreshListeners() {
    const refreshBtn = document.querySelector('.btn-refresh');
    if (refreshBtn) {
        refreshBtn.addEventListener('click', manualRefresh);
    }
}

function startAutoRefresh() {
    setInterval(() => {
        console.log('🔄 Auto-refreshing dashboard...');
        updateDashboardData();
    }, DASHBOARD_CONFIG.REFRESH_INTERVAL);
}

function manualRefresh() {
    console.log('🔄 Manual refresh');
    location.reload();
}

function updateDashboardData() {
    if (document.visibilityState === 'visible' && document.querySelector('.cashier-dashboard')) {
        location.reload();
    }
}

// ================================================
// REAL-TIME UPDATES
// ================================================

/**
 * Update statistics cards
 */
function updateStatistics(data) {
    if (!data || !data.stats) return;

    const stats = {
        'empty': data.stats.emptyTables,
        'reserved': data.stats.reservedTables,
        'serving': data.stats.servingTables,
        'pending': data.stats.pendingPaymentTables,
        'revenue': data.stats.todayRevenue
    };

    Object.keys(stats).forEach(key => {
        const element = document.querySelector(`[data-stat="${key}"]`);
        if (element) {
            element.textContent = stats[key];
        }
    });
}

/**
 * Update table cards
 */
function updateTables(data) {
    if (!data || !data.tables) return;

    const tablesContainer = document.querySelector('.tables-grid');
    if (!tablesContainer) return;

    // Update existing table cards
    data.tables.forEach(tableData => {
        const card = document.querySelector(`[data-table-id="${tableData.id}"]`);
        if (card) {
            updateTableCard(card, tableData);
        }
    });
}

/**
 * Update individual table card
 */
function updateTableCard(card, data) {
    // Update card class for status
    card.className = `table-card table-card-${data.status.toLowerCase()}`;

    // Update card content
    const bodyElement = card.querySelector('.table-body');
    if (bodyElement) {
        // Reconstruct based on status
        let html = '';

        switch (data.status) {
            case 'Empty':
                html = `<div class="status-indicator status-empty">
                    <i class="fas fa-check-circle"></i> Trống
                </div>`;
                break;

            case 'Reserved':
                html = `<div class="status-indicator status-reserved">
                    <i class="fas fa-calendar-check"></i> Đã Đặt
                </div>
                <p class="table-time"><i class="fas fa-clock"></i> ${data.reservationTime}</p>
                <p class="table-customer">${data.reservationCustomer}</p>
                <p class="table-guest">${data.reservationGuests} người</p>
                <p class="table-countdown">Còn ${data.minutesUntilReservation} phút</p>`;
                break;

            case 'Serving':
                html = `<div class="status-indicator status-serving">
                    <i class="fas fa-utensils"></i> Đang Phục Vụ
                </div>
                <p class="table-order-info">${data.orderItems} món - ${data.minutesUsed} phút</p>
                <p class="table-amount">${data.orderAmount}đ</p>`;
                break;

            case 'PendingPayment':
                html = `<div class="status-indicator status-pending">
                    <i class="fas fa-hourglass-end"></i> Chờ Thanh Toán
                </div>
                <p class="table-amount">${data.orderAmount}đ</p>
                <a class="btn btn-small btn-payment" href="/Cashier/POS?tableId=${encodeURIComponent(data.tableId)}">
                    Thanh Toán
                </a>`;
                break;
        }

        bodyElement.innerHTML = html;
    }
}

/**
 * Update notifications
 */
function updateNotifications(data) {
    if (!data || !data.notifications) return;

    const notificationsList = document.querySelector('.notifications-list');
    if (!notificationsList) return;

    // Clear existing notifications
    notificationsList.innerHTML = '';

    // Add new notifications
    data.notifications.slice(0, 5).forEach(notif => {
        const item = document.createElement('div');
        item.className = `notification-item notification-${notif.type}`;
        item.innerHTML = `
            <div class="notification-icon">
                <i class="fas ${notif.icon}"></i>
            </div>
            <div class="notification-body">
                <p class="notification-title">${notif.title}</p>
                <p class="notification-message">${notif.message}</p>
                <small class="notification-time">${notif.timeAgo}</small>
            </div>
        `;
        notificationsList.appendChild(item);
    });
}

// ================================================
// UTILITY FUNCTIONS
// ================================================

/**
 * Format currency
 */
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(amount);
}

/**
 * Format time
 */
function formatTime(date) {
    return new Date(date).toLocaleTimeString('vi-VN', {
        hour: '2-digit',
        minute: '2-digit'
    });
}

/**
 * Get badge color class based on status
 */
function getStatusBadgeClass(status) {
    const classMap = {
        'Empty': 'status-empty',
        'Reserved': 'status-reserved',
        'Serving': 'status-serving',
        'PendingPayment': 'status-pending'
    };
    return classMap[status] || 'status-empty';
}

// ================================================
// LOG HELPERS
// ================================================

function log(message, type = 'info') {
    const timestamp = new Date().toLocaleTimeString('vi-VN');
    console.log(`[${timestamp}] ${message}`);
}

// Export functions if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        handleTableClick,
        updateDashboardData
    };
}
