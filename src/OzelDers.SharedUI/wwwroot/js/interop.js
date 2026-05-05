// OzelDers Interop JS
window.ozelders = {
    scrollToTop: function () {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    },

    // Dark mode
    initTheme: function () {
        const saved = localStorage.getItem('theme') || 'light';
        document.documentElement.setAttribute('data-theme', saved);
        document.body.classList.toggle('dark', saved === 'dark');
        return saved;
    },

    toggleTheme: function () {
        const current = localStorage.getItem('theme') || 'light';
        const next = current === 'dark' ? 'light' : 'dark';
        localStorage.setItem('theme', next);
        document.documentElement.setAttribute('data-theme', next);
        document.body.classList.toggle('dark', next === 'dark');
        return next;
    },

    getTheme: function () {
        return localStorage.getItem('theme') || 'light';
    },

    /**
     * SweetAlert2 popup gösterici.
     * @param {string} icon - 'success' | 'error' | 'warning' | 'info' | 'question'
     * @param {string} title - Popup başlığı
     * @param {string} text - Popup açıklama metni
     * @param {number} timer - Otomatik kapanma süresi (ms). 0 ise otomatik kapanmaz.
     */
    showSwal: function (icon, title, text, timer) {
        if (typeof Swal === 'undefined') {
            alert(title + '\n' + text);
            return;
        }

        var options = {
            icon: icon,
            title: title,
            html: text,
            confirmButtonText: 'Tamam',
            confirmButtonColor: '#6C63FF',
            background: '#fff',
            customClass: {
                popup: 'animate__animated animate__fadeInDown'
            }
        };

        if (timer && timer > 0) {
            options.timer = timer;
            options.timerProgressBar = true;
            options.showConfirmButton = false;
        }

        Swal.fire(options);
    },

    /**
     * SweetAlert2 ile onay dialogu gösterir.
     * @returns {Promise<boolean>}
     */
    showConfirm: function (title, text, confirmText, cancelText) {
        if (typeof Swal === 'undefined') {
            return confirm(title + '\n' + text);
        }

        return Swal.fire({
            icon: 'warning',
            title: title,
            text: text,
            showCancelButton: true,
            confirmButtonText: confirmText || 'Evet, Sil',
            cancelButtonText: cancelText || 'İptal',
            confirmButtonColor: '#e74c3c',
            cancelButtonColor: '#6c757d',
            reverseButtons: true
        }).then(function (result) {
            return result.isConfirmed;
        });
    }
};
