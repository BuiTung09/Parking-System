// ===== PARKING MANAGEMENT SYSTEM - JavaScript =====

// Auto-refresh parking status mỗi 30 giây (cho Staff)
(function () {
    var statusContainer = document.getElementById('parkingStatusContainer');
    if (statusContainer) {
        setInterval(function () {
            fetch('/Staff/GetParkingStatus')
                .then(function (response) { return response.text(); })
                .then(function (html) { statusContainer.innerHTML = html; })
                .catch(function (err) { console.log('Auto-refresh error:', err); });
        }, 30000);
    }
})();

// Auto-dismiss alerts sau 5 giây
document.addEventListener('DOMContentLoaded', function () {
    var alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(function (alert) {
        setTimeout(function () {
            var bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            if (bsAlert) bsAlert.close();
        }, 5000);
    });
});

// Chống thao tác nhiều lần (Double-click / Spam submit)
document.addEventListener('submit', function (e) {
    var form = e.target;
    
    // Nếu có jQuery Validation, kiểm tra trước khi submit
    if (typeof $(form).valid === 'function' && !$(form).valid()) {
        return; 
    }

    // Nếu form đang submit rồi thì chặn các click tiếp theo
    if (form.classList.contains('is-submitting')) {
        e.preventDefault();
        return;
    }
    
    form.classList.add('is-submitting');
    
    // Tìm nút submit và đổi giao diện thành đang tải
    var btn = form.querySelector('button[type="submit"]');
    if (btn) {
        btn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang xử lý...';
        btn.classList.add('disabled');
        // Khóa nút lại sau 10ms (để form vẫn kịp gửi dữ liệu đi)
        setTimeout(function() { btn.disabled = true; }, 10);
    }
});
