document.addEventListener('DOMContentLoaded', function() {
    const passwordInput = document.getElementById('password');
    const passwordToggle = document.getElementById('passwordToggle');

    if (passwordToggle && passwordInput) {
        passwordToggle.addEventListener('click', function(e) {
            e.preventDefault();
            const isPassword = passwordInput.type === 'password';
            passwordInput.type = isPassword ? 'text' : 'password';

            const icon = this.querySelector('i');
            if (icon) {
                icon.classList.toggle('bi-eye');
                icon.classList.toggle('bi-eye-slash');
            }

            const pressed = this.getAttribute('aria-pressed') === 'true';
            this.setAttribute('aria-pressed', String(!pressed));
        });
    }

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

    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            alert.style.animation = 'slideUp 0.3s ease-in-out forwards';
            setTimeout(() => {
                alert.remove();
            }, 300);
        }, 5000);
    });
});

document.addEventListener('keydown', function(e) {

    if (e.key === 'Enter') {
        const form = document.getElementById('loginForm');
        if (form && !e.shiftKey && !e.ctrlKey) {
            e.preventDefault();
            form.submit();
        }
    }
});
