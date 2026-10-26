document.addEventListener("DOMContentLoaded", function () {
    'use strict';

    /* =========================================
       1. VENDOR INITIALIZATIONS (AOS, Swiper, Magnific)
       ========================================= */
    if (typeof AOS !== 'undefined') {
        AOS.init({
            duration: 680,
            once: true,
            offset: 55
        });
    }

    if (typeof Swiper !== 'undefined' && document.querySelector('.tesSwiper')) {
        new Swiper('.tesSwiper', {
            slidesPerView: 1,
            spaceBetween: 22,
            loop: true,
            autoplay: { delay: 4000, disableOnInteraction: false },
            pagination: { el: '.swiper-pagination', clickable: true },
            breakpoints: {
                640: { slidesPerView: 2 },
                1024: { slidesPerView: 3 }
            }
        });
    }

    if (typeof jQuery !== 'undefined' && $('.magnific_popup').length) {
        $('.magnific_popup').magnificPopup({
            disableOn: 700,
            type: 'iframe',
            mainClass: 'mfp-fade',
            removalDelay: 160,
            preloader: false,
            fixedContentPos: false
        });
    }

    /* =========================================
       2. SCROLL EVENTS (Navbar & Back to Top)
       ========================================= */
    window.addEventListener('scroll', function () {
        const nav = document.getElementById('nav');
        const btt = document.getElementById('btt');
        
        if (nav) nav.classList.toggle('scrolled', window.scrollY > 60);
        if (btt) btt.classList.toggle('show', window.scrollY > 300);

        document.querySelectorAll('section[id]').forEach(function (sec) {
            var top = sec.offsetTop - 110,
                bot = top + sec.offsetHeight;
            if (window.scrollY >= top && window.scrollY < bot) {
                document.querySelectorAll('.nav-link').forEach(l => l.classList.remove('active'));
                var lnk = document.querySelector('.nav-link[href="#' + sec.id + '"]');
                if (lnk) lnk.classList.add('active');
            }
        });
    });


    // 2. Jab user kisi link par click kare ya form submit kare (naya page load ho raha ho), toh loader wapas show kar do
            window.addEventListener('beforeunload', function () {
                const loader = document.getElementById('canteenx-loader');
                if (loader) {
                    loader.classList.remove('hidden');
                }
            });


    /* =========================================
       3. SMOOTH SCROLL & MOBILE NAV CLOSE
       ========================================= */
    document.querySelectorAll('a[href^="#"]').forEach(function (a) {
        a.addEventListener('click', function (e) {
            var href = this.getAttribute('href');
            if (href === '#') return;
            var t = document.querySelector(href);
            if (t) {
                e.preventDefault();
                var navCollapse = document.getElementById('navmenu');
                if (navCollapse && navCollapse.classList.contains('show') && typeof bootstrap !== 'undefined') {
                    var bsCollapse = bootstrap.Collapse.getInstance(navCollapse);
                    if (bsCollapse) bsCollapse.hide();
                    else navCollapse.classList.remove('show');
                }
                setTimeout(function () {
                    window.scrollTo({ top: t.offsetTop - 78, behavior: 'smooth' });
                }, 50);
            }
        });
    });

    /* =========================================
       4. SEARCH OVERLAY
       ========================================= */
    const searchOv = document.getElementById('searchOv');
    const navSearchBtn = document.getElementById('navSearchBtn');
    const searchCloseBtn = document.getElementById('searchClose');
    const searchInput = document.getElementById('searchInput');

    function closeSearch() {
        if (searchOv) {
            searchOv.classList.remove('open');
            document.body.style.overflow = '';
        }
    }

    if (navSearchBtn && searchOv) {
        navSearchBtn.addEventListener('click', function () {
            searchOv.classList.add('open');
            document.body.style.overflow = 'hidden';
            setTimeout(() => { if (searchInput) searchInput.focus(); }, 220);
        });
    }

    if (searchCloseBtn) searchCloseBtn.addEventListener('click', closeSearch);
    if (searchOv) {
        searchOv.addEventListener('click', function (e) {
            if (e.target === searchOv) closeSearch();
        });
    }

    // Search Category & Trending Tags
    document.querySelectorAll('.sovcat').forEach(function (btn) {
        btn.addEventListener('click', function () {
            document.querySelectorAll('.sovcat').forEach(b => b.classList.remove('active'));
            this.classList.add('active');
            var f = this.getAttribute('data-cat');
            closeSearch();
            setTimeout(function () {
                filterMenu(f);
                const menuSec = document.getElementById('menu');
                if (menuSec) menuSec.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }, 300);
        });
    });

    document.querySelectorAll('.sovtrend .ttag').forEach(function (t) {
        t.addEventListener('click', function () {
            if (searchInput) {
                searchInput.value = this.textContent.trim();
                searchInput.focus();
            }
        });
    });

    /* =========================================
       5. MENU FILTERING
       ========================================= */
    function filterMenu(cat) {
        document.querySelectorAll('.filtbtn').forEach(b => b.classList.toggle('active', b.getAttribute('data-f') === cat));
        document.querySelectorAll('.catcard').forEach(c => c.classList.toggle('active', c.getAttribute('data-filter') === cat));
        
        document.querySelectorAll('.mwrap').forEach(function (w) {
            var c = w.getAttribute('data-c');
            if (cat === 'all' || c === cat) {
                w.classList.remove('gone');
                w.style.opacity = '0';
                w.style.transform = 'translateY(16px)';
                setTimeout(function () {
                    w.style.transition = 'opacity .38s,transform .38s';
                    w.style.opacity = '1';
                    w.style.transform = 'translateY(0)';
                }, 60);
            } else {
                w.classList.add('gone');
            }
        });
    }

    document.querySelectorAll('.filtbtn').forEach(btn => {
        btn.addEventListener('click', function () { filterMenu(this.getAttribute('data-f')); });
    });

    document.querySelectorAll('.catcard').forEach(card => {
        card.addEventListener('click', function () {
            var f = this.getAttribute('data-filter');
            const menuSec = document.getElementById('menu');
            if (menuSec) {
                window.scrollTo({ top: menuSec.offsetTop - 80, behavior: 'smooth' });
                setTimeout(() => filterMenu(f), 480);
            }
        });
    });

    /* =========================================
       6. MENU POPUP (QUICK VIEW)
       ========================================= */
    const menuPop = document.getElementById('menuPop');
    let mpQty = 1;

    function openMenuPop(card) {
        if (!menuPop) return;
        
        document.getElementById('mpImg').src = card.getAttribute('data-img') || '';
        document.getElementById('mpCat').textContent = card.getAttribute('data-cat') || '';
        document.getElementById('mpTitle').textContent = card.getAttribute('data-title') || '';
        document.getElementById('mpDesc').textContent = card.getAttribute('data-desc') || '';

        let rating = parseFloat(card.getAttribute('data-rating') || 0);
        let reviews = card.getAttribute('data-reviews') || '0';
        let full = Math.round(rating), empty = 5 - full;
        
        const mpStars = document.getElementById('mpStars');
        if (mpStars) {
            mpStars.innerHTML = '<i class="fas fa-star"></i>'.repeat(full) + '☆'.repeat(empty) + 
                ' <span style="color:#bbb;font-size:.78rem;">' + rating + ' (' + reviews + ' reviews)</span>';
        }

        let price = card.getAttribute('data-price') || '';
        let old = card.getAttribute('data-old') || '';
        const mpPrice = document.getElementById('mpPrice');
        if (mpPrice) {
            mpPrice.innerHTML = price + (old ? '<small style="color:#ccc;text-decoration:line-through;margin-left:8px;font-size:1rem;">' + old + '</small>' : '');
        }

        const mpMeta = document.getElementById('mpMeta');
        if (mpMeta) {
            mpMeta.innerHTML = 
                '<div class="mpm"><div class="mpmv">' + (card.getAttribute('data-cal') || '') + ' kcal</div><div class="mpml">Calories</div></div>' +
                '<div class="mpm"><div class="mpmv">' + (card.getAttribute('data-time') || '') + ' min</div><div class="mpml">Prep Time</div></div>' +
                '<div class="mpm"><div class="mpmv">' + rating + '/5</div><div class="mpml">Rating</div></div>';
        }

        const mpTags = document.getElementById('mpTags');
        if (mpTags) {
            let tags = card.getAttribute('data-tags') || '';
            mpTags.innerHTML = tags.split(',').filter(Boolean).map(t => '<span class="mptag">' + t.trim() + '</span>').join('');
        }

        mpQty = 1;
        const mpQnum = document.getElementById('mpQnum');
        const mpAddCart = document.getElementById('mpAddCart');
        if (mpQnum) mpQnum.textContent = 1;
        if (mpAddCart) {
            mpAddCart.innerHTML = '<i class="fas fa-shopping-cart"></i> Add to Cart';
            mpAddCart.style.background = '';
        }

        menuPop.classList.add('open');
        document.body.style.overflow = 'hidden';
    }

    function closeMenuPop() {
        if (menuPop) {
            menuPop.classList.remove('open');
            document.body.style.overflow = '';
        }
    }

    document.querySelectorAll('.mcard').forEach(card => {
        card.addEventListener('click', function () { openMenuPop(this); });
    });

    document.querySelectorAll('.madd').forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            openMenuPop(this.closest('.mcard'));
        });
    });

    document.querySelectorAll('.mhrt').forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            var ico = this.querySelector('i');
            if (ico) {
                ico.classList.toggle('far');
                ico.classList.toggle('fas');
                this.style.color = ico.classList.contains('fas') ? 'var(--primary)' : '#ccc';
            }
        });
    });

    const mpClose = document.getElementById('mpClose');
    if (mpClose) mpClose.addEventListener('click', closeMenuPop);
    
    if (menuPop) {
        menuPop.addEventListener('click', function (e) { if (e.target === this) closeMenuPop(); });
    }

    const mpPlus = document.getElementById('mpPlus');
    const mpMinus = document.getElementById('mpMinus');
    const mpQnum = document.getElementById('mpQnum');
    
    if (mpPlus && mpQnum) {
        mpPlus.addEventListener('click', () => { mpQnum.textContent = ++mpQty; });
    }
    if (mpMinus && mpQnum) {
        mpMinus.addEventListener('click', () => { if (mpQty > 1) mpQnum.textContent = --mpQty; });
    }

    const mpAddCartBtn = document.getElementById('mpAddCart');
    const cartCountEl = document.getElementById('cartCount');
    if (mpAddCartBtn && cartCountEl) {
        mpAddCartBtn.addEventListener('click', function () {
            var cnt = parseInt(cartCountEl.textContent || 0) + mpQty;
            cartCountEl.textContent = cnt;
            this.innerHTML = '<i class="fas fa-check"></i> Added to Cart!';
            this.style.background = 'linear-gradient(135deg,var(--green),#1a4a35)';
            setTimeout(() => {
                closeMenuPop();
                this.innerHTML = '<i class="fas fa-shopping-cart"></i> Add to Cart';
                this.style.background = '';
            }, 1000);
        });
    }

    /* =========================================
       7. BUTTON LOADERS (Reservation & Contact)
       ========================================= */
    const resBtn = document.getElementById('resBtn');
    if (resBtn) {
        resBtn.addEventListener('click', function () {
            var btn = this;
            btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Booking...';
            btn.disabled = true;
            setTimeout(function () {
                btn.innerHTML = '<i class="fas fa-calendar-check"></i> Confirm Reservation';
                btn.disabled = false;
                var ok = document.getElementById('resOk');
                if (ok) {
                    ok.style.display = 'block';
                    ok.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                }
            }, 1500);
        });
    }

    const ctcBtn = document.getElementById('ctcBtn');
    if (ctcBtn) {
        ctcBtn.addEventListener('click', function () {
            var btn = this;
            btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Sending...';
            btn.disabled = true;
            setTimeout(function () {
                btn.innerHTML = '<i class="fas fa-paper-plane"></i> Send Message';
                btn.disabled = false;
                var ok = document.getElementById('ctcOk');
                if (ok) {
                    ok.style.display = 'block';
                    ok.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                }
            }, 1500);
        });
    }

    /* =========================================
       8. GALLERY POPUP
       ========================================= */
    const galPop = document.getElementById('galPop');
    let galData = [];
    let galIdx = 0;

    document.querySelectorAll('.gitem').forEach(function (item) {
        galData.push({
            img: item.getAttribute('data-gimg'),
            title: item.getAttribute('data-gtitle'),
            desc: item.getAttribute('data-gdesc')
        });
        item.addEventListener('click', function () {
            openGal(parseInt(this.getAttribute('data-gi')));
        });
    });

    function openGal(i) {
        if (!galPop || galData.length === 0) return;
        galIdx = i;
        var g = galData[i];
        document.getElementById('gpImg').setAttribute('src', g.img);
        document.getElementById('gpTitle').textContent = g.title;
        document.getElementById('gpDesc').innerHTML = g.desc;
        galPop.classList.add('open');
        document.body.style.overflow = 'hidden';
    }

    function closeGal() {
        if (galPop) {
            galPop.classList.remove('open');
            document.body.style.overflow = '';
        }
    }

    const gpClose = document.getElementById('gpClose');
    const gpPrev = document.getElementById('gpPrev');
    const gpNext = document.getElementById('gpNext');

    if (gpClose) gpClose.addEventListener('click', closeGal);
    if (galPop) galPop.addEventListener('click', function (e) { if (e.target === this) closeGal(); });
    
    if (gpPrev) {
        gpPrev.addEventListener('click', () => openGal((galIdx - 1 + galData.length) % galData.length));
    }
    if (gpNext) {
        gpNext.addEventListener('click', () => openGal((galIdx + 1) % galData.length));
    }

    /* =========================================
       9. ESCAPE KEY (Close everything)
       ========================================= */
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeSearch();
            closeMenuPop();
            closeGal();
            if (typeof jQuery !== 'undefined' && typeof $.magnificPopup !== 'undefined') {
                $.magnificPopup.close();
            }
        }
    });

    /* =========================================
       10. COUNTDOWN TIMER
       ========================================= */
    if (document.getElementById('cdH')) {
        let cH = 8, cM = 45, cS = 30;
        setInterval(function () {
            cS--;
            if (cS < 0) { cS = 59; cM--; }
            if (cM < 0) { cM = 59; cH--; }
            if (cH < 0) { cH = 8; cM = 45; cS = 30; }
            
            document.getElementById('cdH').textContent = String(cH).padStart(2, '0');
            document.getElementById('cdM').textContent = String(cM).padStart(2, '0');
            document.getElementById('cdS').textContent = String(cS).padStart(2, '0');
        }, 1000);
    }

    /* =========================================
       11. NUMBER COUNTER ANIMATION
       ========================================= */
    let numAnimated = false;
    window.addEventListener('scroll', function () {
        const hero = document.getElementById('hero');
        if (!numAnimated && hero && window.scrollY > hero.offsetHeight - 300) {
            numAnimated = true;
            document.querySelectorAll('.snum').forEach(function (el) {
                let txt = el.textContent;
                let num = parseInt(txt);
                let suf = txt.replace(/[0-9]/g, '');
                if (isNaN(num)) return;
                
                let start = 0;
                let step = Math.ceil(num / 55);
                let iv = setInterval(function () {
                    start += step;
                    if (start >= num) {
                        start = num;
                        clearInterval(iv);
                    }
                    el.textContent = start + suf;
                }, 1400 / 55);
            });
        }
    });

});