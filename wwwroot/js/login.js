// Password Show/Hide Toggle
document.addEventListener('DOMContentLoaded', function() {
    const passwordInput = document.getElementById('password');
    const passwordToggle = document.getElementById('passwordToggle');

    if (passwordToggle) {
        passwordToggle.addEventListener('click', function(e) {
            e.preventDefault();
            const isPassword = passwordInput.type === 'password';
            passwordInput.type = isPassword ? 'text' : 'password';
            this.classList.toggle('bi-eye');
            this.classList.toggle('bi-eye-slash');
        });
    }

    // Form submission - Loading state
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', function(e) {
            const loginBtn = document.querySelector('.login-btn');
            if (loginBtn) {
                loginBtn.disabled = true;
                loginBtn.classList.add('loading');
                loginBtn.innerHTML = 'Đang đăng nhập...';
            }
        });
    }

    // Auto-hide alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            const alertClone = alert.cloneNode(true);
            alert.style.animation = 'slideUp 0.3s ease-in-out forwards';
            setTimeout(() => {
                alert.remove();
            }, 300);
        }, 5000);
    });
});

// Keyboard shortcuts
document.addEventListener('keydown', function(e) {
    // Enter to submit form
    if (e.key === 'Enter') {
        const form = document.getElementById('loginForm');
        if (form && !e.shiftKey && !e.ctrlKey) {
            e.preventDefault();
            form.submit();
        }
    }
});
