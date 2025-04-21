/**
 * ui.js - Các chức năng liên quan đến giao diện người dùng chung
 */

/**
 * Xóa bỏ inline style của các nav-link active
 */
function cleanupActiveLinks() {
    document.querySelectorAll('.nav-link.active, .sidebar-nav-link.active').forEach(link => {
        // Xóa bỏ thuộc tính style color inline nếu có
        if (link.style.color) {
            link.style.removeProperty('color');
        }
    });
}

/**
 * Khởi tạo tooltips cho các phần tử có thuộc tính data-bs-toggle="tooltip"
 */
function initTooltips() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

/**
 * Khởi tạo lazy loading cho hình ảnh
 */
function initLazyLoading() {
    // Kiểm tra nếu trình duyệt hỗ trợ Intersection Observer
    if ('IntersectionObserver' in window) {
        const lazyImages = document.querySelectorAll('img[loading="lazy"]');
        
        const imageObserver = new IntersectionObserver(function(entries, observer) {
            entries.forEach(function(entry) {
                if (entry.isIntersecting) {
                    const lazyImage = entry.target;
                    lazyImage.src = lazyImage.dataset.src || lazyImage.src;
                    lazyImage.classList.remove('lazy');
                    imageObserver.unobserve(lazyImage);
                }
            });
        });
        
        lazyImages.forEach(function(lazyImage) {
            imageObserver.observe(lazyImage);
        });
    }
}

/**
 * Khởi tạo nút back-to-top
 */
function initBackToTop() {
    const backToTopBtn = document.getElementById('backToTopBtn');
    if (!backToTopBtn) return;
    
    // Hiển thị nút khi cuộn xuống một khoảng cách nhất định
    window.addEventListener('scroll', function() {
        if (window.pageYOffset > 300) {
            backToTopBtn.classList.remove('d-none');
            backToTopBtn.classList.add('d-flex', 'justify-content-center', 'align-items-center');
        } else {
            backToTopBtn.classList.remove('d-flex', 'justify-content-center', 'align-items-center');
            backToTopBtn.classList.add('d-none');
        }
    });
    
    // Xử lý sự kiện khi click nút
    backToTopBtn.addEventListener('click', function() {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    });
}

/**
 * Khởi tạo các chức năng responsive
 */
function initResponsive() {
    // Đóng navbar khi bấm vào liên kết (trên mobile)
    const navLinks = document.querySelectorAll('.navbar-nav .nav-link');
    const navbarToggler = document.querySelector('.navbar-toggler');
    const navbarCollapse = document.querySelector('.navbar-collapse');
    
    if (navLinks && navbarToggler && navbarCollapse) {
        navLinks.forEach(function(link) {
            link.addEventListener('click', function() {
                if (window.innerWidth < 992 && navbarCollapse.classList.contains('show')) {
                    // Sử dụng Bootstrap API để đóng navbar
                    bootstrap.Collapse.getInstance(navbarCollapse).hide();
                }
            });
        });
    }
    
    // Xử lý sự kiện cuộn trang
    let lastScrollTop = 0;
    const navbar = document.querySelector('.navbar');
    
    if (navbar) {
        window.addEventListener('scroll', function() {
            const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
            
            // Thêm box-shadow khi cuộn xuống
            if (scrollTop > 10) {
                navbar.classList.add('navbar-scrolled');
            } else {
                navbar.classList.remove('navbar-scrolled');
            }
            
            // Ẩn/hiện navbar khi cuộn (chỉ khi ở chế độ sticky)
            if (window.getComputedStyle(document.querySelector('.site-header')).position === 'sticky') {
                if (scrollTop > lastScrollTop && scrollTop > 150) {
                    // Cuộn xuống và đã cuộn quá 150px
                    navbar.classList.add('navbar-hidden');
                } else {
                    // Cuộn lên
                    navbar.classList.remove('navbar-hidden');
                }
            }
            
            lastScrollTop = scrollTop;
            
            // Kiểm tra footer khi cuộn
            adjustFooterPosition();
        });
    }
    
    // Kiểm tra lại vị trí footer khi cửa sổ thay đổi kích thước
    window.addEventListener('resize', adjustFooterPosition);
}

/**
 * Khắc phục vấn đề với accordion
 */
function fixAccordionIssues() {
    // Điều chỉnh z-index cho các thành phần accordion
    const accordions = document.querySelectorAll('.accordion');
    
    accordions.forEach(function(accordion) {
        // Lắng nghe sự kiện khi accordion được mở
        accordion.addEventListener('shown.bs.collapse', function(event) {
            const collapsedElement = event.target;
            
            // Đảm bảo rằng phần tử mở ra hiển thị đầy đủ
            setTimeout(function() {
                // Cuộn đến phần tử đã mở nếu nó bị che khuất
                const headerHeight = document.querySelector('.site-header').offsetHeight || 0;
                const elementTop = collapsedElement.getBoundingClientRect().top;
                
                if (elementTop < headerHeight + 20) {
                    window.scrollBy({
                        top: elementTop - headerHeight - 20,
                        behavior: 'smooth'
                    });
                }
                
                // Kiểm tra xem phần tử có bị che khuất bởi footer không
                const elementBottom = collapsedElement.getBoundingClientRect().bottom;
                const windowHeight = window.innerHeight;
                const footerHeight = document.querySelector('.site-footer').offsetHeight || 0;
                
                if (elementBottom > windowHeight - footerHeight - 20) {
                    // Điều chỉnh cuộn để hiển thị đầy đủ phần tử mở
                    collapsedElement.scrollIntoView({
                        behavior: 'smooth',
                        block: 'center'
                    });
                }
            }, 350); // Đợi để animation hoàn thành
        });
    });
    
    // Đặc biệt xử lý cho accordion trong chương
    const chapterAccordions = document.querySelectorAll('.chapters-container .accordion');
    
    chapterAccordions.forEach(function(accordion) {
        const buttons = accordion.querySelectorAll('.accordion-button');
        
        buttons.forEach(function(button) {
            button.addEventListener('click', function() {
                // Đảm bảo rằng không có phần tử bị che khuất
                setTimeout(function() {
                    const isExpanded = button.getAttribute('aria-expanded') === 'true';
                    
                    if (isExpanded) {
                        const headerHeight = document.querySelector('.site-header').offsetHeight || 0;
                        const buttonRect = button.getBoundingClientRect();
                        
                        if (buttonRect.top < headerHeight) {
                            window.scrollBy({
                                top: buttonRect.top - headerHeight - 20,
                                behavior: 'smooth'
                            });
                        }
                    }
                }, 350);
            });
        });
    });
    
    // Đảm bảo footer luôn ở dưới cùng
    adjustFooterPosition();
    window.addEventListener('resize', adjustFooterPosition);
    document.addEventListener('DOMContentLoaded', adjustFooterPosition);
}

/**
 * Tự động điều chỉnh vị trí footer
 */
function adjustFooterPosition() {
    const content = document.querySelector('.content-wrapper');
    const footer = document.querySelector('.site-footer');
    
    if (!content || !footer) return;
    
    function adjust() {
        const windowHeight = window.innerHeight;
        const contentHeight = content.getBoundingClientRect().height;
        
        // Đảm bảo footer luôn nằm dưới nội dung
        if (contentHeight < windowHeight - footer.offsetHeight) {
            footer.style.marginTop = 'auto';
            content.style.minHeight = `calc(100vh - ${footer.offsetHeight}px)`;
        } else {
            footer.style.marginTop = '0';
        }
    }
    
    // Điều chỉnh ngay khi trang tải xong
    adjust();
    
    // Điều chỉnh khi thay đổi kích thước cửa sổ
    window.addEventListener('resize', adjust);
    
    // Điều chỉnh khi nội dung trang thay đổi
    window.addEventListener('load', adjust);
    document.addEventListener('DOMContentLoaded', adjust);
}

/**
 * Tạo ảnh mặc định nếu không tìm thấy
 */
function createDefaultImage() {
    // Kiểm tra xem ảnh mặc định đã tồn tại trong localStorage chưa
    if (!localStorage.getItem('defaultCoverImage')) {
        // Tạo canvas để vẽ ảnh mặc định
        const canvas = document.createElement('canvas');
        canvas.width = 320;
        canvas.height = 450;
        const ctx = canvas.getContext('2d');
        
        // Vẽ nền
        ctx.fillStyle = document.documentElement.getAttribute('data-bs-theme') === 'dark' ? '#2c2c2c' : '#f8f9fa';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        
        // Vẽ biểu tượng sách
        ctx.fillStyle = '#6c757d';
        ctx.font = 'bold 100px Bootstrap-icons';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText('\uf02d', canvas.width / 2, canvas.height / 2 - 50);
        
        // Vẽ text
        ctx.fillStyle = '#6c757d';
        ctx.font = 'bold 24px Arial, sans-serif';
        ctx.fillText('No Cover', canvas.width / 2, canvas.height / 2 + 50);
        ctx.font = '18px Arial, sans-serif';
        ctx.fillText('Image Not Available', canvas.width / 2, canvas.height / 2 + 90);
        
        // Lưu ảnh vào localStorage
        try {
            localStorage.setItem('defaultCoverImage', canvas.toDataURL('image/png'));
        } catch (e) {
            console.error('Không thể lưu ảnh mặc định vào localStorage:', e);
        }
    }
}

/**
 * Tự động điều chỉnh kích thước chữ cho tiêu đề manga khi vượt quá 2 dòng
 * @param {HTMLElement|null} container - Container chứa các phần tử cần kiểm tra (hoặc null để kiểm tra toàn bộ trang)
 */
function adjustMangaTitles(container = null) {
    // Lấy tất cả các phần tử tiêu đề manga cần kiểm tra
    const titleElements = container 
        ? container.querySelectorAll('.details-manga-title') 
        : document.querySelectorAll('.details-manga-title');
    
    if (!titleElements || titleElements.length === 0) return;
    
    console.log(`Đang kiểm tra ${titleElements.length} tiêu đề manga để điều chỉnh kích thước chữ`);
    
    titleElements.forEach(titleElement => {
        // Lấy kích thước chữ ban đầu
        const computedStyle = window.getComputedStyle(titleElement);
        const originalFontSize = parseFloat(computedStyle.fontSize);
        
        // Tính chiều cao tối đa cho 2 dòng
        const lineHeight = parseFloat(computedStyle.lineHeight);
        const maxHeight = lineHeight * 2; // 2 dòng
        // Thêm dung sai 1px để xử lý sai số làm tròn số
        const heightTolerance = 1;
        
        // Lưu trữ nội dung gốc và độ rộng để xử lý kiểm tra
        const originalText = titleElement.textContent;
        const originalWidth = titleElement.offsetWidth;
        
        // Tạo một phần tử kiểm tra mà không thêm vào DOM
        const testDiv = document.createElement('div');
        // Sao chép các thuộc tính CSS quan trọng từ phần tử gốc
        testDiv.style.cssText = `
            position: absolute; 
            visibility: hidden; 
            font-family: ${computedStyle.fontFamily};
            font-weight: ${computedStyle.fontWeight};
            text-transform: ${computedStyle.textTransform};
            letter-spacing: ${computedStyle.letterSpacing};
            white-space: ${computedStyle.whiteSpace};
            width: ${originalWidth}px;
            line-height: ${lineHeight}px;
        `;
        testDiv.textContent = originalText;
        document.body.appendChild(testDiv);
        
        // Danh sách các kích thước font sẽ thử lần lượt (từ lớn đến nhỏ)
        // Đảm bảo danh sách chỉ chứa các kích thước từ originalFontSize trở xuống
        const fontSizesToTry = [
            originalFontSize,
            Math.min(originalFontSize, 48),
            Math.min(originalFontSize, 40),
            Math.min(originalFontSize, 36),
            Math.min(originalFontSize, 32),
            Math.min(originalFontSize, 28),
            Math.min(originalFontSize, 24),
            Math.min(originalFontSize, 20),
            Math.min(originalFontSize, 18),
            Math.min(originalFontSize, 16),
            Math.min(originalFontSize, 14),
            12 // Kích thước nhỏ nhất
        ].filter((size, index, array) => {
            // Loại bỏ các giá trị trùng lặp
            return index === 0 || size < array[index - 1];
        });
        
        // Thông tin debug
        console.log(`Tiêu đề '${originalText.trim().substring(0, 30)}...': Tối đa ${maxHeight}px, Font-size ban đầu: ${originalFontSize}px`);
        
        // Kiểm tra từng kích thước cho đến khi tìm thấy kích thước phù hợp
        let fontSizeFound = false;
        let selectedFontSize = 12; // Giá trị mặc định nếu không tìm thấy kích thước phù hợp
        
        for (const fontSize of fontSizesToTry) {
            testDiv.style.fontSize = `${fontSize}px`;
            const height = testDiv.offsetHeight;
            
            console.log(`  Thử kích thước: ${fontSize}px -> Chiều cao: ${height}px (tối đa ${maxHeight}px)`);
            
            // Thêm dung sai khi so sánh để xử lý sai số làm tròn số
            if (height <= maxHeight + heightTolerance) {
                selectedFontSize = fontSize;
                fontSizeFound = true;
                console.log(`  ✓ Tìm thấy kích thước phù hợp: ${fontSize}px (trong dung sai ${heightTolerance}px)`);
                break;
            }
        }
        
        // Xóa phần tử kiểm tra sau khi sử dụng
        document.body.removeChild(testDiv);
        
        // Áp dụng kích thước chữ đã tìm thấy vào phần tử thực
        if (fontSizeFound) {
            if (selectedFontSize < originalFontSize) {
                titleElement.style.fontSize = `${selectedFontSize}px`;
                titleElement.classList.add('title-size-adjusted');
                console.log(`Đã điều chỉnh font-size từ ${originalFontSize}px xuống ${selectedFontSize}px`);
            } else {
                console.log(`Tiêu đề đã vừa vặn với font-size hiện tại: ${originalFontSize}px`);
                if (titleElement.classList.contains('title-size-adjusted')) {
                    titleElement.style.fontSize = '';
                    titleElement.classList.remove('title-size-adjusted');
                }
            }
        } else {
            console.log(`Không tìm thấy kích thước phù hợp, sử dụng kích thước tối thiểu: 12px`);
            titleElement.style.fontSize = '12px';
            titleElement.classList.add('title-size-adjusted');
        }
    });
}

export {
    adjustFooterPosition, adjustMangaTitles, cleanupActiveLinks, createDefaultImage, fixAccordionIssues, initBackToTop, initLazyLoading, initResponsive, initTooltips
};
