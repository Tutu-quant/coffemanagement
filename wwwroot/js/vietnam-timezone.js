/**
 * Vietnamese Timezone Utilities for Café Management System
 * All times in this system use Asia/Ho_Chi_Minh (UTC+7) timezone
 * 
 * Usage:
 * - Display UTC times: VietnamTimeUtil.formatTimeDisplay(utcIsoString)
 * - Convert to local: VietnamTimeUtil.toVietnamTime(utcIsoString)
 * - Get current Vietnam time: VietnamTimeUtil.now()
 */

const VietnamTimeUtil = (function () {
    // Vietnam timezone is UTC+7
    const VIETNAM_TIMEZONE = 'Asia/Ho_Chi_Minh';

    /**
     * Format a UTC ISO string to Vietnam local time display
     * @param {string} utcIsoString - ISO string in UTC (e.g., "2025-11-08T10:30:00.000Z")
     * @param {boolean} includeDate - Whether to include the date (default: true)
     * @returns {string} Formatted time string (e.g., "10:30" or "10:30, 08/11/2025")
     */
    function formatTimeDisplay(utcIsoString, includeDate = true) {
        if (!utcIsoString) return '';

        const date = new Date(utcIsoString);
        const vietnamTime = new Date(date.toLocaleString('en-US', { timeZone: VIETNAM_TIMEZONE }));

        const hours = String(vietnamTime.getHours()).padStart(2, '0');
        const minutes = String(vietnamTime.getMinutes()).padStart(2, '0');

        if (!includeDate) {
            return `${hours}:${minutes}`;
        }

        const day = String(vietnamTime.getDate()).padStart(2, '0');
        const month = String(vietnamTime.getMonth() + 1).padStart(2, '0');
        const year = vietnamTime.getFullYear();

        return `${hours}:${minutes}, ${day}/${month}/${year}`;
    }

    /**
     * Get current Vietnam time
     * @returns {Date} Current time in Vietnam timezone
     */
    function now() {
        const utcNow = new Date();
        return new Date(utcNow.toLocaleString('en-US', { timeZone: VIETNAM_TIMEZONE }));
    }

    /**
     * Convert UTC ISO string to Vietnam Date object
     * @param {string} utcIsoString - ISO string in UTC
     * @returns {Date} Date object in Vietnam timezone
     */
    function toVietnamTime(utcIsoString) {
        if (!utcIsoString) return null;
        const utcDate = new Date(utcIsoString);
        return new Date(utcDate.toLocaleString('en-US', { timeZone: VIETNAM_TIMEZONE }));
    }

    /**
     * Convert Vietnam local time to UTC ISO string
     * @param {Date} vietnamDate - Date object in Vietnam local time
     * @returns {string} ISO string in UTC
     */
    function toUtcIso(vietnamDate) {
        if (!vietnamDate) return '';

        // Calculate the offset between Vietnam time and UTC
        const vietnamTime = vietnamDate.getTime();
        const vietnamString = vietnamDate.toLocaleString('en-US', { timeZone: VIETNAM_TIMEZONE });
        const vietnamDateObj = new Date(vietnamString);
        const vietnamMs = vietnamDateObj.getTime();

        // Get UTC time by reversing the offset
        const offset = vietnamTime - vietnamMs;
        const utcDate = new Date(vietnamTime - offset);

        return utcDate.toISOString();
    }

    /**
     * Calculate minutes until a reservation time
     * @param {string} utcReservationTime - ISO string of reservation time in UTC
     * @returns {number} Minutes until reservation (negative if past)
     */
    function minutesUntil(utcReservationTime) {
        const currentVietnam = now();
        const reservationVietnam = toVietnamTime(utcReservationTime);

        const diff = reservationVietnam.getTime() - currentVietnam.getTime();
        return Math.floor(diff / (1000 * 60));
    }

    /**
     * Format countdown text
     * @param {string} utcReservationTime - ISO string of reservation time in UTC
     * @returns {string} Formatted countdown (e.g., "Còn 15 phút", "Đã quá giờ 5 phút")
     */
    function formatCountdown(utcReservationTime) {
        const minutes = minutesUntil(utcReservationTime);

        if (minutes < 0) {
            return `Đã quá giờ ${Math.abs(minutes)} phút`;
        }

        if (minutes === 0) {
            return 'Đến giờ';
        }

        if (minutes < 60) {
            return `Còn ${minutes} phút`;
        }

        const hours = Math.floor(minutes / 60);
        const mins = minutes % 60;
        return `Còn ${hours}h ${mins}p`;
    }

    /**
     * Format "time ago" text (for notifications)
     * @param {string} utcCreatedTime - ISO string when notification was created
     * @returns {string} Time ago text (e.g., "Vừa xong", "5m", "2h")
     */
    function formatTimeAgo(utcCreatedTime) {
        const currentVietnam = now();
        const createdVietnam = toVietnamTime(utcCreatedTime);

        const diffMs = currentVietnam.getTime() - createdVietnam.getTime();
        const diffSeconds = Math.floor(diffMs / 1000);
        const diffMinutes = Math.floor(diffSeconds / 60);
        const diffHours = Math.floor(diffMinutes / 60);
        const diffDays = Math.floor(diffHours / 24);

        if (diffSeconds < 60) return 'Vừa xong';
        if (diffMinutes < 60) return `${diffMinutes}m`;
        if (diffHours < 24) return `${diffHours}h`;
        return `${diffDays}d`;
    }

    /**
     * Initialize datetime-local input with current Vietnam time
     * @param {string} inputId - ID of the datetime-local input element
     * @param {number} minutesFromNow - Minutes from now to set as default
     */
    function initDatetimeInput(inputId, minutesFromNow = 0) {
        const input = document.getElementById(inputId);
        if (!input) return;

        const vietnamNow = now();
        vietnamNow.setMinutes(vietnamNow.getMinutes() + minutesFromNow);

        // Format as datetime-local requires: "2025-11-08T14:30"
        const year = vietnamNow.getFullYear();
        const month = String(vietnamNow.getMonth() + 1).padStart(2, '0');
        const day = String(vietnamNow.getDate()).padStart(2, '0');
        const hours = String(vietnamNow.getHours()).padStart(2, '0');
        const minutes = String(vietnamNow.getMinutes()).padStart(2, '0');

        input.value = `${year}-${month}-${day}T${hours}:${minutes}`;
        input.min = `${year}-${month}-${day}T${hours}:${minutes}`;
    }

    /**
     * Update a time display element continuously (countdown timer)
     * @param {string} elementSelector - CSS selector for the element to update
     * @param {string} utcTimeString - ISO string of the time to display
     * @param {string} format - Format type: 'countdown' or 'time-ago'
     */
    function startLiveUpdate(elementSelector, utcTimeString, format = 'countdown') {
        const element = document.querySelector(elementSelector);
        if (!element) return;

        const updateDisplay = () => {
            if (format === 'countdown') {
                element.textContent = formatCountdown(utcTimeString);
            } else if (format === 'time-ago') {
                element.textContent = formatTimeAgo(utcTimeString);
            } else {
                element.textContent = formatTimeDisplay(utcTimeString, true);
            }
        };

        updateDisplay();
        // Update every 30 seconds for countdown, every minute for time-ago
        const interval = format === 'countdown' ? 30000 : 60000;
        return setInterval(updateDisplay, interval);
    }

    // Public API
    return {
        formatTimeDisplay,
        toVietnamTime,
        toUtcIso,
        now,
        minutesUntil,
        formatCountdown,
        formatTimeAgo,
        initDatetimeInput,
        startLiveUpdate,
        TIMEZONE: VIETNAM_TIMEZONE
    };
})();

// Auto-update all elements with data-reservation attribute (countdown timers)
document.addEventListener('DOMContentLoaded', function () {
    // Update countdown timers
    document.querySelectorAll('[data-reservation]').forEach(element => {
        const utcTime = element.getAttribute('data-reservation');
        VietnamTimeUtil.startLiveUpdate('[data-reservation="' + utcTime + '"]', utcTime, 'countdown');
    });

    // Update order duration
    document.querySelectorAll('[data-order-start]').forEach(element => {
        const utcTime = element.getAttribute('data-order-start');
        // Update order duration every minute
        const updateDuration = () => {
            const vietnamStart = VietnamTimeUtil.toVietnamTime(utcTime);
            const vietnamNow = VietnamTimeUtil.now();
            const minutes = Math.floor((vietnamNow - vietnamStart) / (1000 * 60));
            element.textContent = minutes + ' phút';
        };
        updateDuration();
        setInterval(updateDuration, 60000);
    });
});
