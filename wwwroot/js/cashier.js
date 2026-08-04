/**
 * CASHIER DASHBOARD - JavaScript
 * Xử lý các tương tác, refresh dữ liệu, và thông báo real-time
 */

// Configuration
const DASHBOARD_CONFIG = {
    REFRESH_INTERVAL: 30000, // 30 seconds
    NOTIFICATION_TIMEOUT: 5000, // 5 seconds
    AUTO_REFRESH: true
};

// ================================================
// INITIALIZATION
// ================================================

document.addEventListener('DOMContentLoaded', function() {
    initializeDashboard();
});

function initializeDashboard() {
    console.log('🎯 Initializing Cashier Dashboard...');

    // Add event listeners
    setupTableCardListeners();
    setupNotificationListeners();
    setupRefreshListeners();

    // Start auto-refresh if enabled
    if (DASHBOARD_CONFIG.AUTO_REFRESH) {
        startAutoRefresh();
    }

    console.log('✅ Dashboard initialized');
}

// ================================================
// TABLE CARD INTERACTIONS
// ================================================

function setupTableCardListeners() {
    const tableCards = document.querySelectorAll('.table-card');

    tableCards.forEach(card => {
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
            // Tạo order mới cho bàn trống
            redirectTo(`${areaUrl}/Orders/Create?tableId=${tableId}`);
            break;

        case 'Reserved':
            // Xem chi tiết reservation
            redirectTo(`${areaUrl}/Orders/Details?tableId=${tableId}`);
            break;

        case 'Serving':
            // Xem và chỉnh sửa order đang phục vụ
            redirectTo(`${areaUrl}/Orders/Details?tableId=${tableId}`);
            break;

        case 'Pendinypayment':
            // Xử lý thanh toán
            redirectTo(`${areaUrl}/Payments/Index?tableId=${tableId}`);
            break;
    }
}

function capitalizeStatus(status) {
    const statusMap = {
        'empty': 'Empty',
        'reserved': 'Reserved',
        'serving': 'Serving',
        'pendinypayment': 'PendingPayment'
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
    // Implementation để bật/tắt panel thông báo
    console.log('Toggle notification panel');
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
    // Gọi API để lấy dữ liệu mới
    // Có thể sử dụng AJAX/Fetch API
    console.log('Updating dashboard data...');

    // TODO: Implement API call
    // fetch('/Cashier/Dashboard/GetData')
    //     .then(response => response.json())
    //     .then(data => {
    //         updateStatistics(data);
    //         updateTables(data);
    //         updateNotifications(data);
    //     });
}

// ================================================
// TABLE ACTIONS
// ================================================

function handlePayment(orderId) {
    console.log(`Processing payment for order: ${orderId}`);
    window.location.href = `/Cashier/Payments/Index?orderId=${orderId}`;
}

function handlePrepareTable(reservationId) {
    console.log(`Preparing table for reservation: ${reservationId}`);

    if (confirm('Xác nhận chuẩn bị bàn?')) {
        // TODO: Gọi API để cập nhật trạng thái preparation
        showNotification('✅ Thành công', 'Bàn đã được chuẩn bị', 'success');
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
                <button class="btn btn-small btn-payment" onclick="handlePayment(${data.orderId})">
                    Thanh Toán
                </button>`;
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
        handlePayment,
        handlePrepareTable,
        updateDashboardData
    };
}
