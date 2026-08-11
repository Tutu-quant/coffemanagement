/**
 * Kitchen Display System - Timer & Priority Management
 * Handles real-time elapsed time tracking and priority level updates
 */

(function() {
    'use strict';

    // ========================================
    // Constants
    // ========================================

    const TIMER_UPDATE_INTERVAL = 1000; // Update timer every 1 second

    // Priority thresholds in seconds
    const PRIORITY_THRESHOLDS = {
        WARNING: 10 * 60,      // 10:00
        URGENT: 15 * 60,       // 15:00
        OVERDUE: 20 * 60       // 20:00
    };

    // ========================================
    // Timer Calculation Functions
    // ========================================

    /**
     * Get elapsed seconds since order was created
     * @param {string} orderDateISO - ISO 8601 datetime string (e.g., "2026-08-11T14:00:00Z")
     * @returns {number} Elapsed seconds, minimum 0
     */
    function getElapsedSeconds(orderDateISO) {
        try {
            const orderDate = new Date(orderDateISO);
            const now = new Date();
            const elapsedMs = now - orderDate;
            const elapsedSeconds = Math.floor(elapsedMs / 1000);

            // Clamp to 0 if clock is slightly off
            return Math.max(0, elapsedSeconds);
        } catch (error) {
            console.error('Invalid order date:', orderDateISO, error);
            return 0;
        }
    }

    /**
     * Format elapsed seconds to hh:mm:ss format with leading zeros
     * @param {number} seconds - Total elapsed seconds
     * @returns {string} Formatted time string in hh:mm:ss
     */
    function formatElapsedTime(seconds) {
        if (seconds < 0) seconds = 0;

        // Calculate hours, minutes, seconds
        const hours = Math.floor(seconds / 3600);
        const minutes = Math.floor((seconds % 3600) / 60);
        const secs = seconds % 60;

        // Format with leading zeros: hh:mm:ss
        return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
    }

    /**
     * Determine priority level based on elapsed seconds
     * @param {number} seconds - Elapsed seconds
     * @returns {string} Priority level: 'normal', 'warning', 'urgent', or 'overdue'
     */
    function getPriorityLevel(seconds) {
        if (seconds < PRIORITY_THRESHOLDS.WARNING) {
            return 'normal';
        } else if (seconds < PRIORITY_THRESHOLDS.URGENT) {
            return 'warning';
        } else if (seconds < PRIORITY_THRESHOLDS.OVERDUE) {
            return 'urgent';
        } else {
            return 'overdue';
        }
    }

    /**
     * Get display text for priority level
     * @param {string} priority - Priority level
     * @returns {string} Display text
     */
    function getPriorityDisplayText(priority) {
        switch (priority) {
            case 'warning': return 'ƯU TIÊN';
            case 'urgent': return 'GẤP';
            case 'overdue': return 'QUÁ LÂU';
            case 'normal':
            default: return '';
        }
    }

    /**
     * Get warning icon for priority level
     * @param {string} priority - Priority level
     * @returns {string} Icon text
     */
    function getWarningIcon(priority) {
        switch (priority) {
            case 'warning': return '';
            case 'urgent': return '⚠';
            case 'overdue': return '⚠';
            default: return '';
        }
    }

    // ========================================
    // DOM Update Functions
    // ========================================

    /**
     * Update a single kitchen card's timer and priority
     * @param {HTMLElement} card - The order card element
     */
    function updateKitchenCardTimer(card) {
        const timerElement = card.querySelector('.kitchen-timer');
        const priorityBadge = card.querySelector('.kitchen-priority-badge');
        const warningIcon = card.querySelector('.kitchen-warning-icon');
        const orderStatus = card.getAttribute('data-status');

        if (!timerElement) return;

        // Get elapsed seconds
        const orderDateISO = timerElement.getAttribute('data-order-date');
        const elapsedSeconds = getElapsedSeconds(orderDateISO);

        // Format and update timer display
        const formattedTime = formatElapsedTime(elapsedSeconds);
        timerElement.textContent = formattedTime;

        // Determine priority level
        const priority = getPriorityLevel(elapsedSeconds);

        // Update classes and styling based on status
        // Only show aggressive priority warnings for Pending and Preparing
        if (orderStatus === 'Ready') {
            // For Ready orders, show timer but keep neutral styling
            timerElement.className = 'kitchen-timer';

            if (priorityBadge) {
                priorityBadge.className = 'kitchen-priority-badge';
            }
            if (warningIcon) {
                warningIcon.className = 'kitchen-warning-icon';
            }
        } else {
            // For Pending and Preparing, apply full priority styling

            // Update timer styling
            timerElement.className = 'kitchen-timer';
            if (priority !== 'normal') {
                timerElement.classList.add(`timer-${priority}`);
            }

            // Update priority badge
            if (priorityBadge) {
                priorityBadge.className = 'kitchen-priority-badge';
                if (priority !== 'normal') {
                    priorityBadge.classList.add(`priority-${priority}`);
                    priorityBadge.textContent = getPriorityDisplayText(priority);
                }
            }

            // Update warning icon
            if (warningIcon) {
                warningIcon.className = 'kitchen-warning-icon';
                const icon = getWarningIcon(priority);
                if (icon) {
                    warningIcon.classList.add(`icon-${priority}`);
                    warningIcon.textContent = icon;
                }
            }
        }
    }

    /**
     * Update all kitchen card timers
     */
    function updateAllKitchenTimers() {
        const cards = document.querySelectorAll('.order-card');
        cards.forEach(card => {
            updateKitchenCardTimer(card);
        });
    }

    // ========================================
    // Filter Management (Existing)
    // ========================================

    const ordersGrid = document.getElementById('ordersGrid');
    const filterButtons = document.querySelectorAll('.filter-btn');
    const orderCards = document.querySelectorAll('[data-order-id]');
    const actionButtons = document.querySelectorAll('.action-btn');

    function handleFilterClick(event) {
        const button = event.currentTarget;
        const filterValue = button.getAttribute('data-filter');

        filterButtons.forEach(btn => btn.classList.remove('active'));
        button.classList.add('active');

        filterCards(filterValue);
    }

    function filterCards(filterValue) {
        let visibleCount = 0;

        orderCards.forEach(card => {
            const status = card.getAttribute('data-status');
            let isVisible = false;

            if (filterValue === 'all') {
                isVisible = true;
            } else if (status === filterValue) {
                isVisible = true;
            }

            if (isVisible) {
                card.classList.remove('hidden');
                visibleCount++;
            } else {
                card.classList.add('hidden');
            }
        });

        const emptyState = document.querySelector('.empty-state');
        if (emptyState) {
            if (visibleCount === 0) {
                emptyState.classList.remove('hidden');
            } else {
                emptyState.classList.add('hidden');
            }
        }
    }

    // ========================================
    // AJAX Actions (Existing)
    // ========================================

    function getAntiForgeryToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }

    async function handleStartPreparing(button, orderId) {
        try {
            button.disabled = true;
            button.classList.add('loading');

            const token = getAntiForgeryToken();

            const response = await fetch('/Cashier/Kitchen/StartPreparing', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token,
                    'Content-Type': 'application/x-www-form-urlencoded'
                },
                body: new URLSearchParams({
                    orderId: orderId,
                    __RequestVerificationToken: token
                })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();

            if (result.success) {
                const card = document.querySelector(`[data-order-id="${orderId}"]`);
                if (card) {
                    card.setAttribute('data-status', 'Preparing');

                    const statusIndicator = card.querySelector('.status-indicator');
                    if (statusIndicator) {
                        statusIndicator.innerHTML = '<span class="status-dot"></span><span>ĐANG PHA</span>';
                    }

                    const footer = card.querySelector('.card-footer');
                    if (footer) {
                        footer.innerHTML = `<button class="action-btn btn-mark-ready" data-order-id="${orderId}" type="button">
                            <i class="fas fa-check"></i> Đánh dấu sẵn sàng
                        </button>`;

                        const newButton = footer.querySelector('.action-btn');
                        if (newButton) {
                            newButton.addEventListener('click', handleActionClick);
                        }
                    }

                    const header = card.querySelector('.card-header');
                    if (header) {
                        header.style.backgroundColor = '#F7F1E2';
                    }

                    // Timer will continue to update in the main interval
                    updateCounters();
                }
            } else {
                alert(result.message || 'Không thể bắt đầu pha chế. Vui lòng thử lại.');
                button.disabled = false;
                button.classList.remove('loading');
            }
        } catch (error) {
            console.error('Error starting preparation:', error);
            alert('Lỗi: ' + error.message);
            button.disabled = false;
            button.classList.remove('loading');
        }
    }

    async function handleMarkReady(button, orderId) {
        try {
            button.disabled = true;
            button.classList.add('loading');

            const token = getAntiForgeryToken();

            const response = await fetch('/Cashier/Kitchen/MarkReady', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token,
                    'Content-Type': 'application/x-www-form-urlencoded'
                },
                body: new URLSearchParams({
                    orderId: orderId,
                    __RequestVerificationToken: token
                })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();

            if (result.success) {
                const card = document.querySelector(`[data-order-id="${orderId}"]`);
                if (card) {
                    card.setAttribute('data-status', 'Ready');

                    const statusIndicator = card.querySelector('.status-indicator');
                    if (statusIndicator) {
                        statusIndicator.innerHTML = '<span class="status-dot"></span><span>XONG</span>';
                    }

                    const footer = card.querySelector('.card-footer');
                    if (footer) {
                        footer.innerHTML = `<div class="status-complete">
                            <i class="fas fa-check-circle"></i> Hoàn thành
                        </div>`;
                    }

                    const header = card.querySelector('.card-header');
                    if (header) {
                        header.style.backgroundColor = '#EAF7ED';
                    }

                    // Timer styling will update on next interval to neutral for Ready orders
                    updateCounters();
                }
            } else {
                alert(result.message || 'Không thể đánh dấu sẵn sàng. Vui lòng thử lại.');
                button.disabled = false;
                button.classList.remove('loading');
            }
        } catch (error) {
            console.error('Error marking ready:', error);
            alert('Lỗi: ' + error.message);
            button.disabled = false;
            button.classList.remove('loading');
        }
    }

    function handleActionClick(event) {
        const button = event.currentTarget;
        const orderId = button.getAttribute('data-order-id');

        if (button.classList.contains('btn-start-preparing')) {
            handleStartPreparing(button, orderId);
        } else if (button.classList.contains('btn-mark-ready')) {
            handleMarkReady(button, orderId);
        }
    }

    function updateCounters() {
        let pendingCount = 0;
        let preparingCount = 0;

        orderCards.forEach(card => {
            const status = card.getAttribute('data-status');
            const isHidden = card.classList.contains('hidden');

            if (!isHidden) {
                if (status === 'Pending') pendingCount++;
                if (status === 'Preparing') preparingCount++;
            }
        });

        const pendingDisplay = document.getElementById('pendingCountDisplay');
        const preparingDisplay = document.getElementById('preparingCountDisplay');

        if (pendingDisplay) {
            pendingDisplay.textContent = `${pendingCount} chờ`;
        }
        if (preparingDisplay) {
            preparingDisplay.textContent = `${preparingCount} đang pha`;
        }
    }

    // ========================================
    // Initialization
    // ========================================

    /**
     * Initialize all event listeners and timers
     */
    function init() {
        // Filter buttons
        filterButtons.forEach(button => {
            button.addEventListener('click', handleFilterClick);
        });

        // Action buttons
        actionButtons.forEach(button => {
            button.addEventListener('click', handleActionClick);
        });

        // Initialize timers immediately
        updateAllKitchenTimers();

        // Set up interval for timer updates (every 1 second)
        setInterval(updateAllKitchenTimers, TIMER_UPDATE_INTERVAL);

        // Initialize counters
        updateCounters();
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Export functions for external use (e.g., SignalR injection of new cards)
    window.KitchenDisplay = {
        updateKitchenCardTimer: updateKitchenCardTimer,
        updateAllKitchenTimers: updateAllKitchenTimers,
        getElapsedSeconds: getElapsedSeconds,
        formatElapsedTime: formatElapsedTime,
        getPriorityLevel: getPriorityLevel
    };
})();

