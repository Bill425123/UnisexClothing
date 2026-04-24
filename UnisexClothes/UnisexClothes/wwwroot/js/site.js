// Function to show toast notification
function showToast(message) {
    const toastElement = document.getElementById('toast');
    const toastMessage = document.getElementById('toastMessage');
    
    if (toastMessage) {
        toastMessage.textContent = message;
    }
    
    const toast = new bootstrap.Toast(toastElement);
    toast.show();
}

// Cart Management using localStorage
function getCart() {
    const cart = localStorage.getItem('unistyleCart');
    return cart ? JSON.parse(cart) : [];
}

function saveCart(cart) {
    localStorage.setItem('unistyleCart', JSON.stringify(cart));
}

function getCartCount() {
    const cart = getCart();
    return cart.reduce((total, item) => total + item.quantity, 0);
}

function updateCartCount() {
    // Gọi hàm updateCartBadge để lấy từ server
    updateCartBadge();
}

// Product data for cart
const productData = {
    1: { name: "Áo Phông UniStyle", image: "Áo Phông UniStyle.jpg", price: 224000 },
    2: { name: "Áo Phông Channel", image: "Áo phông channel.jpg", price: 399000 },
    3: { name: "Áo Chanel", image: "Áo channe.jpg", price: 629000 },
    4: { name: "Áo Hoodie UniStyle", image: "Áo Hoodie UniStyle.jpg", price: 382000 },
    5: { name: "Áo Khoác UniStyle", image: "Áo Khoác UniStyle.jpeg", price: 491000 },
    6: { name: "Quần Short UniStyle", image: "Quần Short UniStyle.jpg", price: 311000 },
    7: { name: "Quần Gucci UniStyle", image: "Quần Gucchi.jpg", price: 520000 },
    8: { name: "Quần Jean UniStyle", image: "Quần Jean UniStyle.jpeg", price: 431000 },
    9: { name: "Túi Xách Da UniStyle", image: "Túi xách da.jpg", price: 779000 },
    10: { name: "Túi Xách Gucci", image: "Túi xách Gucchi.jpg", price: 1999000 },
    11: { name: "Đồng Hồ Moonphase", image: "Đồng hồ oonphase.jpg", price: 2099000 },
    12: { name: "Đồng Hồ Titan MC", image: "Đồng Hồ titan MC.jpg", price: 2749000 }
};

const productDetailCache = {};

// Helper function để xử lý image path - tối ưu và tái sử dụng
function normalizeImagePath(imagePath) {
    if (!imagePath) return 'default.jpg';
    
    // Remove path prefix if exists
    if (imagePath.includes('/images/HinhSanPhamUnisex/')) {
        imagePath = imagePath.replace('/images/HinhSanPhamUnisex/', '');
    }
    // Remove leading slash if exists
    if (imagePath.startsWith('/')) {
        imagePath = imagePath.substring(1);
    }
    // If imagePath is empty or still contains path, use default
    if (!imagePath || imagePath.includes('/')) {
        return 'default.jpg';
    }
    
    return imagePath;
}

// Debounce helper function - tối ưu performance
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Add to cart function - Sử dụng API server-side
async function addToCart(productId, quantity = 1, variantId = null) {
    try {
        console.log('Adding to cart:', productId, quantity, variantId);
        
        // Gọi API thêm vào giỏ hàng trên server
        const response = await fetch('/Cart/AddToCart', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                productId: productId,
                quantity: quantity,
                variantId: variantId
            })
        });
        
        const result = await response.json();
        console.log('Add to cart result:', result);
        
        if (result.success) {
            // Cập nhật badge giỏ hàng
            if (typeof updateCartBadge === 'function') {
                updateCartBadge();
            } else {
                updateCartCount();
            }
            
            // Hiển thị thông báo thành công
            showToast(result.message || 'Đã thêm sản phẩm vào giỏ hàng!');
        } else {
            // Kiểm tra nếu cần đăng nhập
            if (result.requiresLogin) {
                if (confirm('Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng. Bạn có muốn chuyển đến trang đăng nhập không?')) {
                    window.location.href = '/Account/Login';
                }
            } else {
            // Hiển thị lỗi
            alert(result.message || 'Không thể thêm sản phẩm vào giỏ hàng!');
            }
        }
        
    } catch (error) {
        console.error('Error adding to cart:', error);
        alert('Có lỏi xảy ra khi thêm sản phẩm vào giỏ hàng!');
    }
}

// Hàm cập nhật badge giỏ hàng từ server
function updateCartBadge() {
    fetch('/Cart/GetCartCount')
        .then(res => res.json())
        .then(data => {
            const badge = document.getElementById('cartBadge');
            if (badge) {
                badge.textContent = data.count;
                badge.style.display = data.count > 0 ? 'inline-block' : 'none';
            }
        })
        .catch(error => {
            console.error('Error updating cart badge:', error);
        });
}

// Buy now function - only buy this product, ignore cart - chuyển thẳng đến thanh toán
async function buyNow(productId, quantity = 1) {
    
    try {
        // Load product info from API
        const response = await fetch(`/Product/GetProductDetail?id=${productId}`);
        const result = await response.json();
        
        let product;
        
        if (!result.success || !result.data) {
            // Fallback to hardcoded data if API fails
            const fallbackProduct = productData[productId];
            if (!fallbackProduct) {
                showToast('Không thể tải thông tin sản phẩm!');
                console.error('Product not found:', productId);
                return;
            }
            product = {
                id: productId,
                name: fallbackProduct.name,
                image: fallbackProduct.image,
                price: fallbackProduct.price,
                salePrice: fallbackProduct.price
            };
        } else {
            product = result.data;
        }

        const availableStock = typeof product.stock === 'number' ? product.stock : null;
        if (availableStock !== null && availableStock <= 0) {
            showToast('Sản phẩm đã hết hàng.');
            return;
        }
        let allowedQuantity = Number(quantity) || 1;
        if (availableStock !== null && allowedQuantity > availableStock) {
            allowedQuantity = availableStock;
            showToast(`Chỉ còn ${availableStock} sản phẩm trong kho, đã điều chỉnh số lượng.`);
        }
        
        // Create a cart with ONLY this product for checkout - sử dụng helper function
        const checkoutOnlyCart = [{
            productId: Number(product.id),
            name: product.name,
            image: normalizeImagePath(product.image), // Lưu chỉ tên file, không có path
            price: product.salePrice || product.price,
            quantity: allowedQuantity,
            color: "Mặc định",
            size: "M",
            stock: availableStock
        }];
        
        // Save ONLY this product to localStorage for checkout (don't affect cart)
        localStorage.setItem('unistyleSelectedProducts', JSON.stringify(checkoutOnlyCart));
        
        // Show toast
        showToast(`Đang chuyển đến thanh toán ${product.name}...`);
        
        // Redirect to checkout immediately
        window.location.href = '/Checkout/Index';
        
    } catch (error) {
        console.error('Error loading product for buy now:', error);
        // Fallback to hardcoded data - sử dụng helper function
        const fallbackProduct = productData[productId];
        if (fallbackProduct) {
            const checkoutOnlyCart = [{
                productId: productId,
                name: fallbackProduct.name,
                image: normalizeImagePath(fallbackProduct.image), // Lưu chỉ tên file, không có path
                price: fallbackProduct.price,
                quantity: quantity,
                color: "Mặc định",
                size: "M",
                stock: typeof fallbackProduct.stock === 'number' ? fallbackProduct.stock : null
            }];
            
            localStorage.setItem('unistyleSelectedProducts', JSON.stringify(checkoutOnlyCart));
            showToast(`Đang chuyển đến thanh toán ${fallbackProduct.name}...`);
            window.location.href = '/Checkout/Index';
        } else {
            showToast('Không thể mua sản phẩm này!');
        }
    }
}

// Remove from cart
function removeFromCart(productId) {
    if (confirm('Bạn có chắc chắn muốn xóa sản phẩm này khỏi giỏ hàng?')) {
        const cart = getCart();
        const removeId = Number(productId);
        
        console.log('Removing product ID:', removeId);
        console.log('Current cart before removal:', cart);
        
        // Filter out the item with matching productId (convert both to numbers)
        const filteredCart = cart.filter(item => {
            const itemId = Number(item.productId);
            const keep = itemId !== removeId;
            if (!keep) {
                console.log('Removing item:', item);
            }
            return keep;
        });
        
        console.log('Cart after removal:', filteredCart);
        
        // Lưu vào localStorage ngay lập tức
        saveCart(filteredCart);
        
        // Kiểm tra lại sau khi lưu
        const verifyCart = getCart();
        console.log('Verified cart from localStorage:', verifyCart);
        
        // Update UI
        const cartItem = document.getElementById('cart-item-' + productId);
        if (cartItem) {
            cartItem.remove();
        }
        
        updateCartCount();
        
        // Update cart total if function exists
        if (typeof updateCartTotal === 'function') {
            updateCartTotal();
        }
        
        // Reload cart items if on cart page (quan trọng để refresh UI)
        if (typeof loadCartItems === 'function') {
            loadCartItems();
        }
        
        showToast('Đã xóa sản phẩm khỏi giỏ hàng');
    }
}

// Update cart total
function updateCartTotal() {
    const cart = getCart();
    let total = 0;
    
    cart.forEach(item => {
        total += item.price * item.quantity;
    });
    
    const totalElement = document.getElementById('cartTotal');
    if (totalElement) {
        totalElement.textContent = formatPrice(total);
    }
}

// Format price
function formatPrice(price) {
    return new Intl.NumberFormat('vi-VN').format(price) + ' ₫';
}

// Update quantity
function updateQuantity(productId, newQuantity) {
    const cart = getCart();
    const item = cart.find(i => i.productId === productId);
    
    if (item && newQuantity > 0) {
        item.quantity = newQuantity;
        saveCart(cart);
        updateCartTotal();
        updateCartCount();
    }
}

// Process checkout function
function processCheckout(event) {
    event.preventDefault();
    
    showToast('Đơn hàng đã được đặt thành công!');
    
    // Clear cart after checkout
    setTimeout(() => {
        localStorage.removeItem('unistyleCart');
        window.location.href = '/Account/MyOrders';
    }, 2000);
}

// Đảm bảo các function là global ngay sau khi định nghĩa
// (Chuyển đến cuối file sau khi tất cả functions được định nghĩa)

// Initialize cart count on page load
document.addEventListener('DOMContentLoaded', function() {
    // Xóa dữ liệu giả trong localStorage - tự động xóa một lần khi trang load
    // Kiểm tra xem có đã xóa chưa (dùng flag trong localStorage)
    const hasClearedFakeData = sessionStorage.getItem('hasClearedFakeCartData');
    if (!hasClearedFakeData) {
        localStorage.removeItem('unistyleCart');
        sessionStorage.setItem('hasClearedFakeCartData', 'true');
        console.log('Đã xóa dữ liệu giả trong giỏ hàng!');
    }
    
    // Không tạo dữ liệu giả - để giỏ hàng rỗng cho người dùng tự thêm sản phẩm
    updateCartCount();
});

// Size selection
function selectSize(element) {
    const siblings = element.parentElement.children;
    for (let i = 0; i < siblings.length; i++) {
        siblings[i].classList.remove('selected');
    }
    element.classList.add('selected');
}

// Color selection
function selectColor(element) {
    const siblings = element.parentElement.children;
    for (let i = 0; i < siblings.length; i++) {
        siblings[i].classList.remove('selected');
    }
    element.classList.add('selected');
}

// Quantity controls
function decreaseQuantity(element) {
    const input = element.nextElementSibling;
    let value = parseInt(input.value);
    if (value > 1) {
        input.value = value - 1;
    }
}

function increaseQuantity(element) {
    const input = element.previousElementSibling;
    let value = parseInt(input.value);
    input.value = value + 1;
}

// Helper functions để gọi từ onclick - đơn giản hơn và đáng tin cậy hơn
window.handleAddToCart = function(productId) {
    console.log('handleAddToCart called with productId:', productId);
    if (typeof window.addToCart === 'function') {
        window.addToCart(productId);
    } else if (typeof addToCart === 'function') {
        addToCart(productId);
    } else {
        console.error('addToCart function not found');
        alert('Lỗi: Không thể thêm sản phẩm vào giỏ hàng! Vui lòng refresh trang.');
    }
};

window.handleBuyNow = function(productId) {
    console.log('handleBuyNow called with productId:', productId);
    if (typeof window.buyNow === 'function') {
        window.buyNow(productId);
    } else if (typeof buyNow === 'function') {
        buyNow(productId);
    } else {
        console.error('buyNow function not found');
        alert('Lỗi: Không thể mua ngay sản phẩm này! Vui lòng refresh trang.');
    }
};

// Login function
async function submitLogin(event) {
    event.preventDefault();
    
    const email = document.getElementById('email')?.value;
    const password = document.getElementById('password')?.value;
    
    if (!email || !password) {
        alert('Vui lòng điền đầy đủ thông tin!');
        return;
    }
    
    try {
        const formData = new FormData();
        formData.append('email', email);
        formData.append('password', password);
        
        const response = await fetch('/Account/Login', {
            method: 'POST',
            body: formData
        });
        
        const result = await response.json();
        
        if (result.success) {
            showToast('Đăng nhập thành công!');
            // Redirect về trang trước đó hoặc trang chủ
            const redirectUrl = result.redirectUrl || '/';
            setTimeout(() => {
                window.location.href = redirectUrl;
            }, 1000);
        } else {
            alert(result.message || 'Đăng nhập thất bại! Vui lòng kiểm tra lại email và mật khẩu.');
        }
    } catch (error) {
        console.error('Login error:', error);
        alert('Có lỗi xảy ra khi đăng nhập! Vui lòng thử lại.');
    }
}

// Register function
async function submitRegister(event) {
    event.preventDefault();
    
    const firstName = document.getElementById('firstName')?.value;
    const lastName = document.getElementById('lastName')?.value;
    const email = document.getElementById('email')?.value;
    const phone = document.getElementById('phone')?.value;
    const password = document.getElementById('password')?.value;
    const confirmPassword = document.getElementById('confirmPassword')?.value;
    
    if (!firstName || !lastName || !email || !phone || !password || !confirmPassword) {
        alert('Vui lòng điền đầy đủ thông tin!');
        return;
    }
    
    if (password !== confirmPassword) {
        alert('Mật khẩu xác nhận không khớp!');
        return;
    }
    
    try {
        const formData = new FormData();
        formData.append('name', firstName + ' ' + lastName);
        formData.append('email', email);
        formData.append('phone', phone);
        formData.append('password', password);
        
        const response = await fetch('/Account/Register', {
            method: 'POST',
            body: formData
        });
        
        const result = await response.json();
        
        if (result.success) {
            showToast('Đăng ký thành công!');
            setTimeout(() => {
                window.location.href = '/Account/Login';
            }, 1000);
        } else {
            alert(result.message || 'Đăng ký thất bại! Vui lòng thử lại.');
        }
    } catch (error) {
        console.error('Register error:', error);
        alert('Có lỗi xảy ra khi đăng ký! Vui lòng thử lại.');
    }
}

// Logout function
function logoutUser() {
    if (confirm('Bạn có chắc chắn muốn đăng xuất?')) {
        window.location.href = '/Account/Logout';
    }
}

// ================== FAVORITES FUNCTIONALITY ==================
// Toggle favorite (add/remove)
async function toggleFavorite(productId, heartElement = null) {
    try {
        const response = await fetch('/Favorites/ToggleFavorite', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                productId: productId
            })
        });
        
        const result = await response.json();
        
        if (result.success) {
            // Update heart icon if element provided
            if (heartElement) {
                if (result.isAdded) {
                    heartElement.classList.remove('far');
                    heartElement.classList.add('fas', 'text-danger');
                } else {
                    heartElement.classList.remove('fas', 'text-danger');
                    heartElement.classList.add('far');
                }
            }
            
            // Update favorites badge
            if (typeof updateFavoritesBadge === 'function') {
                updateFavoritesBadge();
            } else if (typeof window.updateFavoritesBadge === 'function') {
                window.updateFavoritesBadge();
            }
            
            // Show toast notification
            showToast(result.message);
        } else {
            alert(result.message || 'Không thể thực hiện thao tác!');
        }
    } catch (error) {
        console.error('Error toggling favorite:', error);
        alert('Có lỗi xảy ra!');
    }
}

// Check if product is in favorites
async function checkFavorite(productId) {
    try {
        const response = await fetch(`/Favorites/CheckFavorite?productId=${productId}`);
        const result = await response.json();
        return result.isFavorite || false;
    } catch (error) {
        console.error('Error checking favorite:', error);
        return false;
    }
}

// Update favorites badge
function updateFavoritesBadge() {
    fetch('/Favorites/GetFavoritesCount')
        .then(res => res.json())
        .then(data => {
            const badge = document.getElementById('favoritesBadge');
            if (badge) {
                badge.textContent = data.count;
                badge.style.display = data.count > 0 ? 'inline-block' : 'none';
            }
        })
        .catch(error => {
            console.error('Error updating favorites badge:', error);
        });
}

// Đảm bảo các function quan trọng là global để có thể gọi từ mọi nơi
window.addToCart = addToCart;
window.buyNow = buyNow;
window.removeFromCart = removeFromCart;
window.getCart = getCart;
window.saveCart = saveCart;
window.updateCartCount = updateCartCount;
window.updateCartBadge = updateCartBadge;
window.showToast = showToast;
window.submitLogin = submitLogin;
window.submitRegister = submitRegister;
window.logoutUser = logoutUser;
window.productDetailCache = productDetailCache;
window.toggleFavorite = toggleFavorite;
window.checkFavorite = checkFavorite;
window.updateFavoritesBadge = updateFavoritesBadge;
