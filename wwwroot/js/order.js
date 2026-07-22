/**
 * Order Management UI Interactions
 * BrewPoint Design System
 */

(function() {
    'use strict';

    // ====================================
    // SEARCH FUNCTIONALITY
    // ====================================

    function initSearch() {
        const searchInput = document.querySelector('.order-filter-input[type="text"]');
        if (!searchInput) return;

        let searchTimeout;
        searchInput.addEventListener('input', function() {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                // Trigger search if needed
                const searchEvent = new CustomEvent('orderSearch', {
                    detail: { query: this.value }
                });
                document.dispatchEvent(searchEvent);
            }, 300);
        });
    }

    // ====================================
    // FILTER FUNCTIONALITY
    // ====================================

    function initFilters() {
        const filterButtons = document.querySelectorAll('.order-btn-filter');
        const resetButtons = document.querySelectorAll('.order-btn-reset');

        filterButtons.forEach(btn => {
            btn.addEventListener('click', function(e) {
                e.preventDefault();
                const form = this.closest('form');
                if (form) {
                    form.submit();
                }
            });
        });

        resetButtons.forEach(btn => {
            btn.addEventListener('click', function(e) {
                e.preventDefault();
                // Reset all filters
                const filterInputs = document.querySelectorAll('.order-filter-input, .order-filter-select');
                filterInputs.forEach(input => {
                    if (input.type === 'text') {
                        input.value = '';
                    } else if (input.tagName === 'SELECT') {
                        input.selectedIndex = 0;
                    } else if (input.type === 'date') {
                        input.value = '';
                    }
                });

                // Navigate to index without filters
                const indexLink = document.querySelector('a[href*="Index"]');
                if (indexLink) {
                    window.location.href = indexLink.href.split('?')[0];
                }
            });
        });
    }

    // ====================================
    // PAGINATION
    // ====================================

    function initPagination() {
        const paginationButtons = document.querySelectorAll('.order-pagination-btn');

        paginationButtons.forEach(btn => {
            btn.addEventListener('click', function(e) {
                // Allow default link behavior
                if (this.tagName === 'A' && this.href) {
                    return;
                }
            });
        });
    }

    // ====================================
    // TABLE INTERACTIONS
    // ====================================

    function initTableInteractions() {
        const tableRows = document.querySelectorAll('.order-table tbody tr');

        tableRows.forEach(row => {
            // Make entire row clickable to detail page
            row.addEventListener('click', function(e) {
                // Don't trigger if clicking on a button or link
                if (e.target.tagName === 'A' || e.target.closest('button')) {
                    return;
                }

                // Find the detail link in the row
                const detailLink = this.querySelector('a.order-action-btn');
                if (detailLink) {
                    detailLink.click();
                }
            });

            // Add hover effect
            row.addEventListener('mouseenter', function() {
                this.style.cursor = 'pointer';
            });
        });
    }

    // ====================================
    // DETAIL PAGE TIMELINE
    // ====================================

    function initTimeline() {
        const timelineItems = document.querySelectorAll('.order-timeline-item');

        timelineItems.forEach((item, index) => {
            item.style.animationDelay = `${index * 0.1}s`;

            // Add expand functionality if needed
            const content = item.querySelector('.order-timeline-content');
            if (content && content.textContent.length > 100) {
                item.classList.add('expandable');
                item.addEventListener('click', function() {
                    this.classList.toggle('expanded');
                });
            }
        });
    }

    // ====================================
    // DETAIL PAGE INTERACTIONS
    // ====================================

    function initDetailPage() {
        const backBtn = document.querySelector('.order-back-btn');
        if (backBtn) {
            backBtn.addEventListener('click', function(e) {
                if (this.tagName !== 'A') {
                    e.preventDefault();
                    window.history.back();
                }
            });
        }

        // Print functionality
        const printBtn = document.querySelector('.order-btn-print');
        if (printBtn) {
            printBtn.addEventListener('click', function(e) {
                e.preventDefault();
                window.print();
            });
        }
    }

    // ====================================
    // TOOLTIP & POPOVER
    // ====================================

    function initTooltips() {
        const tooltips = document.querySelectorAll('[data-tooltip]');

        tooltips.forEach(element => {
            const tooltipText = element.getAttribute('data-tooltip');

            element.addEventListener('mouseenter', function() {
                const tooltip = document.createElement('div');
                tooltip.className = 'order-tooltip';
                tooltip.textContent = tooltipText;
                tooltip.style.position = 'absolute';
                tooltip.style.backgroundColor = 'var(--primary)';
                tooltip.style.color = 'white';
                tooltip.style.padding = '8px 12px';
                tooltip.style.borderRadius = '6px';
                tooltip.style.fontSize = '12px';
                tooltip.style.zIndex = '1000';
                tooltip.style.whiteSpace = 'nowrap';
                tooltip.style.pointerEvents = 'none';

                document.body.appendChild(tooltip);

                const rect = this.getBoundingClientRect();
                tooltip.style.left = rect.left + 'px';
                tooltip.style.top = (rect.top - tooltip.offsetHeight - 8) + 'px';
            });

            element.addEventListener('mouseleave', function() {
                const tooltip = document.querySelector('.order-tooltip');
                if (tooltip) {
                    tooltip.remove();
                }
            });
        });
    }

    // ====================================
    // EXPORT & PRINT
    // ====================================

    function initExportFunctionality() {
        const exportButtons = document.querySelectorAll('[data-export]');

        exportButtons.forEach(btn => {
            btn.addEventListener('click', function(e) {
                e.preventDefault();
                const format = this.getAttribute('data-export');

                if (format === 'pdf') {
                    window.print();
                } else if (format === 'excel') {
                    exportToExcel();
                } else if (format === 'csv') {
                    exportToCSV();
                }
            });
        });
    }

    function exportToExcel() {
        const table = document.querySelector('.order-table');
        if (!table) return;

        let csv = [];
        const rows = table.querySelectorAll('tr');

        rows.forEach(row => {
            let cells = [];
            row.querySelectorAll('th, td').forEach(cell => {
                cells.push('"' + cell.textContent.trim() + '"');
            });
            csv.push(cells.join(','));
        });

        const csvContent = 'data:text/csv;charset=utf-8,' + encodeURIComponent(csv.join('\n'));
        const link = document.createElement('a');
        link.setAttribute('href', csvContent);
        link.setAttribute('download', 'orders.csv');
        link.click();
    }

    function exportToCSV() {
        exportToExcel();
    }

    // ====================================
    // NOTIFICATION & ALERT
    // ====================================

    function showNotification(message, type = 'success', duration = 3000) {
        const notification = document.createElement('div');
        notification.className = `order-notification order-notification-${type}`;
        notification.textContent = message;
        notification.style.position = 'fixed';
        notification.style.top = '20px';
        notification.style.right = '20px';
        notification.style.zIndex = '9999';
        notification.style.padding = '16px 24px';
        notification.style.borderRadius = '8px';
        notification.style.boxShadow = 'var(--shadow-lg)';
        notification.style.animation = 'slideIn 0.3s ease-out';

        if (type === 'success') {
            notification.style.backgroundColor = 'var(--success)';
            notification.style.color = 'white';
        } else if (type === 'error') {
            notification.style.backgroundColor = 'var(--danger)';
            notification.style.color = 'white';
        } else if (type === 'warning') {
            notification.style.backgroundColor = 'var(--warning)';
            notification.style.color = 'white';
        } else {
            notification.style.backgroundColor = 'var(--info)';
            notification.style.color = 'white';
        }

        document.body.appendChild(notification);

        setTimeout(() => {
            notification.style.animation = 'slideOut 0.3s ease-out';
            setTimeout(() => {
                notification.remove();
            }, 300);
        }, duration);
    }

    // ====================================
    // STATUS FILTER HELPER
    // ====================================

    function filterByStatus(status) {
        const rows = document.querySelectorAll('.order-table tbody tr');
        let visibleCount = 0;

        rows.forEach(row => {
            const statusCell = row.querySelector('[data-status]');
            if (!statusCell) return;

            if (status === 'all' || statusCell.getAttribute('data-status') === status) {
                row.style.display = '';
                visibleCount++;
            } else {
                row.style.display = 'none';
            }
        });

        // Show empty state if no matches
        if (visibleCount === 0) {
            const tbody = document.querySelector('.order-table tbody');
            if (tbody) {
                const emptyRow = tbody.querySelector('.order-table-empty');
                if (!emptyRow) {
                    const row = document.createElement('tr');
                    row.className = 'order-table-empty';
                    row.innerHTML = '<td colspan="100%" class="order-text-center order-text-muted">Không có đơn hàng</td>';
                    tbody.appendChild(row);
                }
            }
        }
    }

    // ====================================
    // ANIMATION UTILITIES
    // ====================================

    function addAnimationStyles() {
        const style = document.createElement('style');
        style.textContent = `
            @keyframes slideIn {
                from {
                    transform: translateX(400px);
                    opacity: 0;
                }
                to {
                    transform: translateX(0);
                    opacity: 1;
                }
            }

            @keyframes slideOut {
                from {
                    transform: translateX(0);
                    opacity: 1;
                }
                to {
                    transform: translateX(400px);
                    opacity: 0;
                }
            }

            @keyframes fadeIn {
                from {
                    opacity: 0;
                }
                to {
                    opacity: 1;
                }
            }

            @keyframes fadeOut {
                from {
                    opacity: 1;
                }
                to {
                    opacity: 0;
                }
            }
        `;
        document.head.appendChild(style);
    }

    // ====================================
    // INITIALIZATION
    // ====================================

    function init() {
        // Add animation styles
        addAnimationStyles();

        // Initialize all components
        initSearch();
        initFilters();
        initPagination();
        initTableInteractions();
        initTimeline();
        initDetailPage();
        initTooltips();
        initExportFunctionality();
    }

    // Run on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Export functions for use in other scripts
    window.OrderUI = {
        showNotification,
        filterByStatus,
        exportToExcel,
        exportToCSV
    };

})();
