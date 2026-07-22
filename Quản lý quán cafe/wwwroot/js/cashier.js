// BrewPoint Cashier - JavaScript

// Update time display
function updateTime() {
    const now = new Date();
    const timeDisplay = document.getElementById('current-time');
    if (timeDisplay) {
        timeDisplay.textContent = now.toLocaleTimeString('vi-VN');
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    setInterval(updateTime, 1000);
});

// Show notification
function showNotification(message, type = 'success') {
    const alertClass = type === 'success' ? 'alert-success' : 
                       type === 'error' ? 'alert-danger' : 'alert-info';

    const alert = document.createElement('div');
    alert.className = `alert ${alertClass} alert-dismissible fade show`;
    alert.style.position = 'fixed';
    alert.style.top = '20px';
    alert.style.right = '20px';
    alert.style.zIndex = '9999';
    alert.style.minWidth = '300px';
    alert.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

    document.body.appendChild(alert);

    setTimeout(() => {
        alert.remove();
    }, 5000);
}

// Format currency
function formatCurrency(value) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND',
        minimumFractionDigits: 0
    }).format(value);
}

// Table operations
function selectTable(tableId) {
    window.location.href = `/Cashier/POS?tableId=${tableId}`;
}

function callCustomer(tableId) {
    // Sound/notification for calling customer
    playSound('/sounds/call.mp3');
    showNotification(`Gọi khách bàn ${tableId}`);
}

function playSound(soundPath) {
    const audio = new Audio(soundPath);
    audio.play().catch(() => {
        console.log('Could not play sound');
    });
}
