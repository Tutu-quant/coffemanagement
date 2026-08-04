const ReservationManager = {
    selectedTableId: null,
    selectedTableNumber: null,
    selectedTableCapacity: 0,

    init() {
        this.setupEventListeners();
    },

    setupEventListeners() {
        const dateInput = document.getElementById('ReservationDate');
        const guestInput = document.getElementById('NumberOfGuests');
        const confirmBtn = document.getElementById('confirmBtn');

        if (dateInput) {
            dateInput.addEventListener('change', () => {
                this.validateForm();
                this.updateSummary();
            });
        }

        if (guestInput) {
            guestInput.addEventListener('change', () => {
                this.validateForm();
                this.updateSummary();
            });
        }

        document.addEventListener('click', (e) => {
            if (e.target.closest('.table-card')) {
                this.selectTable(e.target.closest('.table-card'));
            }
        });

        const form = document.getElementById('reservationForm');
        if (form) {
            form.addEventListener('submit', (e) => {
                if (!this.selectedTableId) {
                    e.preventDefault();
                    alert('Vui lòng chọn một bàn.');
                }
            });
        }
    },

    validateForm() {
        const dateInput = document.getElementById('ReservationDate');
        const guestInput = document.getElementById('NumberOfGuests');
        const confirmBtn = document.getElementById('confirmBtn');

        if (!dateInput || !dateInput.value) return false;

        const reservationDate = new Date(dateInput.value);
        const guestCount = parseInt(guestInput.value) || 0;

        if (reservationDate <= new Date()) {
            if (confirmBtn) confirmBtn.disabled = true;
            return false;
        }

        if (guestCount <= 0 || guestCount > 50) {
            if (confirmBtn) confirmBtn.disabled = true;
            return false;
        }

        if (confirmBtn) confirmBtn.disabled = !this.selectedTableId;
        return true;
    },

    async searchAvailableTables() {
        const dateInput = document.getElementById('ReservationDate');
        const guestInput = document.getElementById('NumberOfGuests');

        if (!dateInput?.value || !guestInput?.value) {
            document.getElementById('tablesError').classList.remove('d-none');
            return;
        }

        const reservationDate = new Date(dateInput.value);
        if (reservationDate <= new Date()) {
            alert('Thời gian đặt phải ở tương lai.');
            return;
        }

        const guestCount = parseInt(guestInput.value);
        if (guestCount <= 0 || guestCount > 50) {
            alert('Số khách phải từ 1 đến 50.');
            return;
        }

        const loadingEl = document.getElementById('tablesLoading');
        const resultsEl = document.getElementById('tablesResults');
        const errorEl = document.getElementById('tablesError');

        if (loadingEl) loadingEl.classList.remove('d-none');
        if (resultsEl) resultsEl.innerHTML = '';
        if (errorEl) errorEl.classList.add('d-none');

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            const response = await fetch('/Customer/Reservations/SearchAvailableTables', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': token || ''
                },
                body: JSON.stringify({
                    reservationDate: dateInput.value,
                    numberOfGuests: guestCount,
                    durationMinutes: 120
                })
            });

            if (!response.ok) {
                throw new Error('Lỗi tìm kiếm bàn.');
            }

            const data = await response.json();

            if (loadingEl) loadingEl.classList.add('d-none');

            if (data.success && data.data.tables.length > 0) {
                this.renderAvailableTables(data.data.tables);
            } else {
                if (resultsEl) {
                    resultsEl.innerHTML = `<div class="alert alert-info mb-0">
                        <i class="bi bi-info-circle"></i> 
                        ${data.data.tables.length === 0 ? 'Không có bàn còn trống cho khung giờ này.' : data.message || 'Không tìm thấy bàn.'}
                    </div>`;
                }
            }
        } catch (error) {
            console.error('Lỗi:', error);
            if (loadingEl) loadingEl.classList.add('d-none');
            if (resultsEl) {
                resultsEl.innerHTML = `<div class="alert alert-danger mb-0">
                    <i class="bi bi-exclamation-circle"></i> 
                    Lỗi: ${error.message}
                </div>`;
            }
        }
    },

    renderAvailableTables(tables) {
        const resultsEl = document.getElementById('tablesResults');
        if (!resultsEl || !tables || tables.length === 0) return;

        let html = '';
        tables.forEach(table => {
            const isSelected = this.selectedTableId === table.tableID;
            const isAvailable = !table.isBooked;
            const statusIcon = isAvailable ? '<i class="bi bi-check-circle-fill" style="color: #4CAF50;"></i>' : '<i class="bi bi-x-circle-fill" style="color: #dc3545;"></i>';
            const statusText = isAvailable ? 'Còn trống' : 'Đầy';
            const statusClass = isAvailable ? 'available' : 'booked';

            // Extract floor number from location if available
            const floorText = table.location || 'Tầng 1';

            html += `
                <div class="table-card ${isSelected ? 'active' : ''}" 
                     data-table-id="${table.tableID}" 
                     data-table-number="${table.tableNumber}" 
                     data-table-capacity="${table.capacity}">
                    <div class="table-card-icon"><i class="bi bi-cup-hot" style="font-size: 2rem; color: #8B6F47;"></i></div>
                    <h6 class="table-card-title">Bàn ${table.tableNumber}</h6>
                    <p class="table-card-info"><i class="bi bi-people-fill" style="font-size: 0.9rem; margin-right: 0.3rem;"></i>${table.capacity} khách</p>
                    <p class="table-card-info"><i class="bi bi-building" style="font-size: 0.9rem; margin-right: 0.3rem;"></i>${floorText}</p>
                    <div class="table-card-status ${statusClass}">
                        ${statusIcon} ${statusText}
                    </div>
                </div>
            `;
        });

        resultsEl.innerHTML = html;
    },

    selectTable(cardElement) {
        const tableId = parseInt(cardElement.getAttribute('data-table-id'));
        const tableNumber = cardElement.getAttribute('data-table-number');
        const capacity = parseInt(cardElement.getAttribute('data-table-capacity'));

        this.selectedTableId = tableId;
        this.selectedTableNumber = tableNumber;
        this.selectedTableCapacity = capacity;

        document.getElementById('TableID').value = tableId;

        document.querySelectorAll('.table-card').forEach(card => {
            card.classList.remove('active');
        });
        cardElement.classList.add('active');

        this.validateForm();
        this.updateSummary();
    },

    updateSummary() {
        const summaryEl = document.getElementById('reservationSummary');
        if (!summaryEl) return;

        const dateInput = document.getElementById('ReservationDate');
        const guestInput = document.getElementById('NumberOfGuests');
        const confirmBtn = document.getElementById('confirmBtn');
        const confirmMsg = document.getElementById('confirmDisabledMsg');

        if (!this.selectedTableId || !dateInput?.value || !guestInput?.value) {
            summaryEl.innerHTML = `
                <p class="text-muted text-center py-4">
                    <i class="bi bi-info-circle"></i> Chọn bàn để xem tóm tắt
                </p>
            `;
            if (confirmMsg) confirmMsg.style.display = 'block';
            if (confirmBtn) confirmBtn.style.display = 'none';
            return;
        }

        if (confirmMsg) confirmMsg.style.display = 'none';
        if (confirmBtn) confirmBtn.style.display = 'block';

        const reservationDate = new Date(dateInput.value);
        const time = reservationDate.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
        const date = reservationDate.toLocaleDateString('vi-VN');
        const guestCount = parseInt(guestInput.value);

        let html = `
            <div class="summary-item">
                <span class="label">Bàn:</span>
                <span class="value">Bàn ${this.selectedTableNumber}</span>
            </div>
            <div class="summary-item">
                <span class="label">Sức chứa:</span>
                <span class="value">${this.selectedTableCapacity} khách</span>
            </div>
            <div class="summary-item">
                <span class="label">Tầng:</span>
                <span class="value">Tầng 1</span>
            </div>
            <div class="summary-item">
                <span class="label">Ngày:</span>
                <span class="value">${date}</span>
            </div>
            <div class="summary-item">
                <span class="label">Giờ:</span>
                <span class="value">${time}</span>
            </div>
            <div class="summary-item">
                <span class="label">Khách:</span>
                <span class="value">${guestCount}</span>
            </div>
        `;

        summaryEl.innerHTML = html;
    }
};

document.addEventListener('DOMContentLoaded', () => {
    ReservationManager.init();
});
