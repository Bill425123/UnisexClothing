/**
 * UniStyle Admin Panel JavaScript
 * Main admin functionality
 */

// Toggle sidebar collapse
document.addEventListener('DOMContentLoaded', function () {
    const sidebarCollapse = document.getElementById('sidebarCollapse');
    const sidebar = document.getElementById('sidebar');

    if (sidebarCollapse && sidebar) {
        sidebarCollapse.addEventListener('click', function () {
            sidebar.classList.toggle('collapsed');
        });
    }

    // Auto-hide sidebar on mobile
    if (window.innerWidth <= 768) {
        if (sidebar) {
            sidebar.classList.add('collapsed');
        }
    }
});

// Toast notification function
function showAdminToast(message, type = 'success') {
    // Use showToast from site.js if available
    if (typeof showToast === 'function') {
        showToast(message);
        return;
    }

    // Fallback to alert
    alert(message);
}

// Confirm delete action
function confirmDelete(itemName, callback) {
    if (confirm(`Bạn có chắc chắn muốn xóa "${itemName}"?`)) {
        if (typeof callback === 'function') {
            callback();
        }
        return true;
    }
    return false;
}

// Format currency
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(amount);
}

// Format date
function formatDate(date) {
    return new Date(date).toLocaleDateString('vi-VN', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
    });
}

// Export table to CSV
function exportTableToCSV(tableId, filename = 'export.csv') {
    const table = document.getElementById(tableId);
    if (!table) return;

    let csv = [];
    const rows = table.querySelectorAll('tr');

    for (let i = 0; i < rows.length; i++) {
        const row = [], cols = rows[i].querySelectorAll('td, th');

        for (let j = 0; j < cols.length; j++) {
            let data = cols[j].innerText.replace(/(\r\n|\n|\r)/gm, '').replace(/(\s\s)/gm, ' ');
            data = data.replace(/"/g, '""');
            row.push('"' + data + '"');
        }

        csv.push(row.join(','));
    }

    downloadCSV(csv.join('\n'), filename);
}

function downloadCSV(csv, filename) {
    const csvFile = new Blob([csv], { type: 'text/csv' });
    const downloadLink = document.createElement('a');
    downloadLink.download = filename;
    downloadLink.href = window.URL.createObjectURL(csvFile);
    downloadLink.style.display = 'none';
    document.body.appendChild(downloadLink);
    downloadLink.click();
    document.body.removeChild(downloadLink);
}

// Print functionality
function printPage() {
    window.print();
}

// Search functionality
function setupTableSearch(inputId, tableId) {
    const input = document.getElementById(inputId);
    const table = document.getElementById(tableId);

    if (!input || !table) return;

    input.addEventListener('keyup', function () {
        const filter = this.value.toLowerCase();
        const rows = table.getElementsByTagName('tr');

        for (let i = 1; i < rows.length; i++) {
            const row = rows[i];
            const text = row.textContent || row.innerText;

            if (text.toLowerCase().indexOf(filter) > -1) {
                row.style.display = '';
            } else {
                row.style.display = 'none';
            }
        }
    });
}

// Initialize tooltips
function initTooltips() {
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl));
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    // Init tooltips if Bootstrap is loaded
    if (typeof bootstrap !== 'undefined') {
        initTooltips();
    }

    // Setup search for common tables
    setupTableSearch('searchProduct', 'productsTable');
    setupTableSearch('searchOrder', 'ordersTable');
    setupTableSearch('searchUser', 'usersTable');
});

// API Helper function
async function apiCall(url, method = 'GET', data = null) {
    try {
        const options = {
            method: method,
            headers: {
                'Content-Type': 'application/json',
            }
        };

        if (data && method !== 'GET') {
            options.body = JSON.stringify(data);
        }

        const response = await fetch(url, options);
        const result = await response.json();

        return result;
    } catch (error) {
        console.error('API Error:', error);
        return { success: false, message: error.message };
    }
}

// Image preview before upload
function previewImage(input, previewId) {
    if (input.files && input.files[0]) {
        const reader = new FileReader();

        reader.onload = function (e) {
            document.getElementById(previewId).src = e.target.result;
        };

        reader.readAsDataURL(input.files[0]);
    }
}

// Statistics counter animation
function animateCounter(element, target, duration = 1000) {
    let start = 0;
    const increment = target / (duration / 16);
    const timer = setInterval(function () {
        start += increment;
        if (start >= target) {
            element.textContent = target;
            clearInterval(timer);
        } else {
            element.textContent = Math.floor(start);
        }
    }, 16);
}

// Initialize counters on dashboard
document.addEventListener('DOMContentLoaded', function () {
    const counters = document.querySelectorAll('.stats-card h3');
    counters.forEach(counter => {
        const target = parseInt(counter.textContent.replace(/[^0-9]/g, ''));
        if (target) {
            counter.textContent = '0';
            animateCounter(counter, target);
        }
    });
});

console.log('✅ UniStyle Admin Panel JavaScript loaded successfully!');



