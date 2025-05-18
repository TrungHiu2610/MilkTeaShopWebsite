// Đảm bảo rằng JavaScript chạy sau khi trang đã được tải hoàn toàn
    document.addEventListener('DOMContentLoaded', function() {
        // Lấy tất cả các div có lớp 'btn_size'
        const toppingButtons = document.querySelectorAll('.btn_size');
        
        // Lặp qua tất cả các topping để thêm sự kiện click
        toppingButtons.forEach(function(button) {
            button.addEventListener('click', function() {
                // Toggle lớp 'selected' khi nhấn vào
                this.classList.toggle('selected');
                
                // Nếu bạn cần lưu trạng thái các topping đã chọn, có thể làm như sau:
                const selectedToppings = [];
                document.querySelectorAll('.btn_size.selected').forEach(function(selectedButton) {
                    selectedToppings.push(selectedButton.getAttribute('data-topping'));
                });

                // Log hoặc gửi danh sách topping đã chọn (có thể xử lý thêm ở đây)
                console.log(selectedToppings);
            });
        });
    });

//RADIO BUTTON
    document.addEventListener('DOMContentLoaded', function () {
        // Lấy tất cả các div có lớp 'btn_size_radio'
        const sizeButtons = document.querySelectorAll('.btn_size_radio');

        // Lặp qua tất cả các size để thêm sự kiện click
        sizeButtons.forEach(function (button) {
            button.addEventListener('click', function () {
                // Xóa lớp 'selected' khỏi tất cả các button khác
                sizeButtons.forEach(function (otherButton) {
                    otherButton.classList.remove('selected');
                });

                // Thêm lớp 'selected' vào button đang được chọn
                this.classList.add('selected');

                // Nếu bạn cần lưu trạng thái size đã chọn, có thể làm như sau:
                const selectedSize = this.getAttribute('data-size');
                const selectedPrice = this.getAttribute('data-price');

                // Log hoặc xử lý size đã chọn (có thể gửi về server)
                console.log('Size đã chọn: ' + selectedSize + ', Giá thêm: ' + selectedPrice);
            });
        });
    });