
    $(document).ready(function () {
            var $mainImage = $('#mainQuickViewImage');
    var $colorName = $('#selectedColorName');
    var $quantityInput = $('#quantityInput');

    // --- ĐỒNG BỘ MÀU VÀ THUMBNAIL LOGIC ---

    // Khởi tạo trạng thái active cho ảnh preview dưới
    $('#thumbnailPreviewContainer .thumbnail-item[data-default="true"]').addClass('active-preview');

    // 1. Logic click Thumbnail (Ảnh phụ dưới) - Chỉ đồng bộ trạng thái active và chuyển sang logic click màu
    $('#thumbnailPreviewContainer').on('click', '.thumbnail-item', function () {
                var $this = $(this);
    var newImgSrc = $this.data('img');

    // Đồng bộ hóa bằng cách kích hoạt sự kiện click ở selector màu (bên phải)
    $('#colorSelectorContainer .color-variant-selector img[src="' + newImgSrc + '"]').parent().click();

    // Cập nhật active cho thanh preview dưới
    $('#thumbnailPreviewContainer .thumbnail-item').removeClass('active-preview');
    $this.addClass('active-preview');
            });

    // 2. Logic click Color Selector (bên phải) - Logic chính
    $('#colorSelectorContainer').on('click', '.color-variant-selector', function () {
                var $this = $(this);
    var newImgSrc = $this.data('img');
    var colorName = $this.data('color-name');

    // Cập nhật trạng thái Active
    $('.color-variant-selector').removeClass('active-color');
    $this.addClass('active-color');

    // Thay đổi ảnh chính (Quan trọng nhất)
    $mainImage.attr('src', newImgSrc);
    $colorName.text(colorName);

    // Đồng bộ hóa trạng thái ảnh preview dưới
    $('#thumbnailPreviewContainer .thumbnail-item').removeClass('active-preview');
    $('#thumbnailPreviewContainer .thumbnail-item img[src="' + newImgSrc + '"]').parent().addClass('active-preview');
            });

    // Khởi tạo trạng thái active ban đầu cho color selector bên phải
    setTimeout(function () {
                var defaultImageSrc = $mainImage.attr('src');
    $('#colorSelectorContainer img[src="' + defaultImageSrc + '"]').parent().addClass('active-color');
            }, 50);

    // --- LOGIC TĂNG GIẢM SỐ LƯỢNG ---

    $('.btn-quantity-minus').on('click', function () {
                var currentVal = parseInt($quantityInput.val());
                if (currentVal > 1) {
        $quantityInput.val(currentVal - 1);
                }
            });

    $('.btn-quantity-plus').on('click', function () {
                var currentVal = parseInt($quantityInput.val());
    $quantityInput.val(currentVal + 1);
            });

    // --- LOGIC THÊM VÀO GIỎ (SKELETON) ---
    $('.btn-add-to-cart').on('click', function () {
                var maspId = $(this).data('masp');
    var quantity = parseInt($quantityInput.val());
    var size = $('input[name="size"]:checked').attr('id');
    var colorName = $('#colorSelectorContainer .active-color').data('color-name');

    // TODO: Gửi yêu cầu AJAX đến CartController/AddToCart
    console.log('AJAX Data:', {MaspId: maspId, Quantity: quantity, Size: size, ColorName: colorName });
    alert("Sẵn sàng gửi dữ liệu vào giỏ hàng!");
            });
        });