$(document).ready(function () {
    const canvas = document.getElementById('wheelCanvas');
    const ctx = canvas.getContext('2d');
    const btnSpin = document.getElementById('btn-spin');
    const resultDiv = document.getElementById('spin-result');
    const antiForgeryToken = $('input[name="__RequestVerificationToken"]').val();

    let prizes = [];
    let currentRotation = 0; // Góc quay hiện tại
    let isSpinning = false; // Cờ chặn bấm liên tục

    // 1. Khởi tạo: Lấy danh sách quà và vẽ vòng quay
    function initWheel() {
        $.getJSON('/Gift/GetPrizes', function (data) {
            if (data && data.length > 0) {
                prizes = data;
                drawWheel();
            } else {
                ctx.font = '30px Lato';
                ctx.fillText('Đang bảo trì...', canvas.width / 2 - 100, canvas.height / 2);
            }
        });
    }

    // 2. Hàm VẼ vòng quay bằng Canvas (Dựa trên số lượng ô)
    function drawWheel() {
        const numPrizes = prizes.length;
        const arc = Math.PI * 2 / numPrizes; // Góc của một ô (radian)
        const centerX = canvas.width / 2;
        const centerY = canvas.height / 2;
        const radius = centerX - 10;

        ctx.clearRect(0, 0, canvas.width, canvas.height); // Xóa cũ vẽ mới

        prizes.forEach((prize, i) => {
            const angle = arc * i;

            // Vẽ nền của ô
            ctx.beginPath();
            ctx.fillStyle = prize.fillColor || (i % 2 === 0 ? '#C1A35E' : '#333'); // Màu vàng xen kẽ màu đen
            ctx.moveTo(centerX, centerY);
            ctx.arc(centerX, centerY, radius, angle, angle + arc);
            ctx.fill();
            ctx.stroke();

            // Vẽ CHỮ hiển thị quà (Phải xoay chữ theo góc ô)
            ctx.save();
            ctx.translate(centerX, centerY);
            ctx.rotate(angle + arc / 2); // Xoay đến giữa ô
            ctx.textAlign = "right";
            ctx.fillStyle = "#fff"; // Màu chữ trắng
            ctx.font = 'bold 24px Lato';
            ctx.fillText(prize.name, radius - 30, 10); // Đặt chữ sát mép ngoài
            ctx.restore();
        });
    }

    // 3. Xử lý hành động BẤM QUAY
    btnSpin.addEventListener('click', function () {
        if (isSpinning) return;
        isSpinning = true;
        btnSpin.disabled = true; // Khóa nút
        resultDiv.classList.add('d-none'); // Ẩn kết quả cũ

        // Gọi Back-end để lấy kết quả
        $.ajax({
            url: '/Gift/Spin',
            method: 'POST',
            headers: { "RequestVerificationToken": antiForgeryToken }, // Gửi token bảo mật
            success: function (response) {
                if (response.success) {
                    // --- TÍNH TOÁN GÓC XOAY ĐỂ DỪNG ĐÚNG Ô ---
                    const numPrizes = prizes.length;
                    const prizeAngle = 360 / numPrizes; // Góc của 1 ô (độ)

                    // Góc để ô trúng thưởng nằm ở đỉnh Vòng quay (ngay dưới mũi tên)
                    // (Lưu ý: Canvas bắt đầu vẽ từ góc 3h, mũi tên nằm ở góc 12h)
                    const stopAngle = 270 - (prizeAngle * response.prizeIndex) - (prizeAngle / 2);

                    // Thêm 5 vòng quay toàn bộ (5 * 360) để tạo hiệu ứng quay nhanh
                    const finalRotation = 3600 + stopAngle;
                    currentRotation = finalRotation; // Cập nhật góc quay mới

                    // Áp dụng CSS để xoay (Transition đã cài trong CSS là 5 giây)
                    canvas.style.transform = `rotate(${finalRotation}deg)`;

                    // Đợi 5 giây (sau khi xoay xong) thì hiện kết quả
                    setTimeout(function () {
                        resultDiv.innerHTML = `<i class="fa fa-gift me-2 fs-5"></i>${response.message}`;
                        resultDiv.classList.remove('d-none');
                        isSpinning = false;
                        btnSpin.disabled = false; // Mở lại nút
                    }, 5500); // 5.5 giây (dư nửa giây cho chắc)

                } else {
                    alert(response.message);
                    isSpinning = false;
                    btnSpin.disabled = false;
                }
            },
            error: function () {
                alert("Đã xảy ra lỗi hệ thống, vui lòng thử lại sau.");
                isSpinning = false;
                btnSpin.disabled = false;
            }
        });
    });

    // Bắt đầu chạy
    initWheel();
});