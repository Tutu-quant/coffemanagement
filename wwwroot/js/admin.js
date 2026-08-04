


document.addEventListener('DOMContentLoaded', function() {
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebar = document.querySelector('.admin-sidebar');

    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function() {
            sidebar.classList.toggle('show');
        });
    }


    document.addEventListener('click', function(event) {
        if (!event.target.closest('.admin-sidebar') &&
            !event.target.closest('.btn-sidebar-toggle')) {
            sidebar.classList.remove('show');
        }
    });


    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });
});


function confirmDelete(title = 'Xóa mục này?', message = 'Bạn không thể khôi phục dữ liệu bị xóa.') {
    return Swal.fire({
        title: title,
        text: message,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#8B6F47',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Xóa',
        cancelButtonText: 'Hủy',
        reverseButtons: true
    });
}


function showSuccess(title = 'Thành công!', message = 'Thao tác hoàn thành.') {
    Swal.fire({
        title: title,
        text: message,
        icon: 'success',
        confirmButtonColor: '#8B6F47',
        confirmButtonText: 'Đóng'
    });
}


function showError(title = 'Lỗi!', message = 'Có lỗi xảy ra. Vui lòng thử lại.') {
    Swal.fire({
        title: title,
        text: message,
        icon: 'error',
        confirmButtonColor: '#8B6F47',
        confirmButtonText: 'Đóng'
    });
}


function showInfo(title = 'Thông báo', message = 'Có thông tin cần bạn biết.') {
    Swal.fire({
        title: title,
        text: message,
        icon: 'info',
        confirmButtonColor: '#8B6F47',
        confirmButtonText: 'Đóng'
    });
}


function validateForm(formId) {
    const form = document.getElementById(formId);
    if (!form) return true;

    const inputs = form.querySelectorAll('[required]');
    let isValid = true;

    inputs.forEach(input => {
        if (!input.value.trim()) {
            input.classList.add('is-invalid');
            isValid = false;
        } else {
            input.classList.remove('is-invalid');
        }
    });

    return isValid;
}


document.addEventListener('click', function(e) {
    if (e.target.classList.contains('btn-delete-confirm')) {
        e.preventDefault();
        const url = e.target.getAttribute('href') || e.target.getAttribute('data-url');

        confirmDelete('Xóa danh mục?', 'Danh mục này sẽ bị xóa vĩnh viễn.').then((result) => {
            if (result.isConfirmed) {
                window.location.href = url;
            }
        });
    }
});


function formatCurrency(value) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(value);
}


function formatDate(date) {
    return new Date(date).toLocaleDateString('vi-VN');
}

