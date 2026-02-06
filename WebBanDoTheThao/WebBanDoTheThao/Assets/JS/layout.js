function closeTooltip() {
    const bubble = document.getElementById('bubbleTooltip');
    if (bubble) {
        bubble.style.display = 'none';
    }
}
const backToTop = document.getElementById("backToTop");

// Hiện nút khi scroll xuống 200px
window.addEventListener("scroll", () => {
    if (window.scrollY > 250) {
        backToTop.classList.add("show");
    } else {
        backToTop.classList.remove("show");
    }
});

// Click -> cuộn mượt lên đầu
backToTop.addEventListener("click", () => {
    window.scrollTo({
        top: 0,
        behavior: "smooth"
    });
});
document.querySelectorAll('.favorite-btn').forEach(btn => {
    btn.addEventListener('click', function () {
        const icon = this.querySelector('i');
        // Nếu chưa đỏ -> đổi sang đỏ, ngược lại thì bỏ đỏ
        icon.classList.toggle('fas'); // đổi viền sang tim đặc
        icon.classList.toggle('text-danger'); // thêm màu đỏ
    });
});
// Chạy sau khi DOM load
document.addEventListener("DOMContentLoaded", function () {
    // Lấy tất cả thẻ sản phẩm
    document.querySelectorAll(".product-card").forEach(function (card) {
        const mainImage = card.querySelector(".card-img-top"); // ảnh chính
        const thumbnails = card.querySelectorAll(".thumbnail-img"); // thumbnail list

        thumbnails.forEach(thumb => {
            thumb.addEventListener("click", function () {
                // đổi src ảnh chính
                mainImage.src = this.src;

                // highlight thumbnail đang chọn
                thumbnails.forEach(t => t.classList.remove("active"));
                this.classList.add("active");
            });
        });
    });
});