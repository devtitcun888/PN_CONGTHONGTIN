/**
 * EV Rental — Public JS v2
 * Scroll header, reveal, counter, card interactions
 */

(function () {
  'use strict';

  /* ── Scroll Header (sticky with shadow) ── */
  function initScrollHeader(dotNetRef) {
    var header = document.getElementById('site-header');
    if (!header) return;

    var lastScrolled = false;
    window.addEventListener('scroll', function () {
      var scrolled = window.scrollY > 20;
      if (scrolled !== lastScrolled) {
        lastScrolled = scrolled;
        if (dotNetRef) {
          try { dotNetRef.invokeMethodAsync('OnScrollChange', scrolled); } catch (e) {}
        }
      }
    }, { passive: true });
  }

  /* ── Scroll Reveal ── */
  function initScrollReveal() {
    var els = document.querySelectorAll('.ev-reveal');
    if (!els.length) return;

    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) {
          entry.target.classList.add('visible');
          io.unobserve(entry.target);
        }
      });
    }, { threshold: 0.10, rootMargin: '0px 0px -30px 0px' });

    els.forEach(function (el) { io.observe(el); });
  }

  /* ── Sticky Header Shadow on Scroll ── */
  function initStickyHeader() {
    var header = document.querySelector('.ev-site-header');
    if (!header) return;

    window.addEventListener('scroll', function () {
      if (window.scrollY > 10) {
        header.style.boxShadow = '0 4px 30px rgba(0,0,0,0.6)';
      } else {
        header.style.boxShadow = '0 4px 20px rgba(0,0,0,0.4)';
      }
    }, { passive: true });
  }

  /* ── Counter Animation for Stats ── */
  function animateCounters() {
    var counters = document.querySelectorAll('.ev-stat__num[data-count]');
    if (!counters.length) return;

    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (!entry.isIntersecting) return;
        var el = entry.target;
        var target = parseInt(el.dataset.count, 10) || 0;
        var suffix = el.dataset.suffix || '';
        var start = 0;
        var duration = 1600;
        var startTime = null;

        function step(ts) {
          if (!startTime) startTime = ts;
          var progress = Math.min((ts - startTime) / duration, 1);
          var eased = 1 - Math.pow(1 - progress, 3);
          el.textContent = Math.floor(eased * target) + suffix;
          if (progress < 1) requestAnimationFrame(step);
          else el.textContent = target + suffix;
        }

        requestAnimationFrame(step);
        io.unobserve(el);
      });
    }, { threshold: 0.5 });

    counters.forEach(function (c) { io.observe(c); });
  }

  /* ── Pin Bar Animate ── */
  function animatePinBars() {
    var fills = document.querySelectorAll('.ev-pin-fill[data-width]');
    fills.forEach(function (el) {
      var w = el.dataset.width || '0';
      setTimeout(function () { el.style.width = w + '%'; }, 300);
    });
  }

  /* ── Vehicle Card tilt on hover ── */
  function initCardTilt() {
    document.querySelectorAll('.ev-vehicle-card').forEach(function (card) {
      card.addEventListener('mousemove', function (e) {
        var rect = card.getBoundingClientRect();
        var x = e.clientX - rect.left;
        var y = e.clientY - rect.top;
        var cx = rect.width / 2;
        var cy = rect.height / 2;
        var rx = ((y - cy) / cy) * 3;
        var ry = ((x - cx) / cx) * -3;
        card.style.transform = 'translateY(-6px) rotateX(' + rx + 'deg) rotateY(' + ry + 'deg)';
        card.style.transition = 'transform 0.1s ease';
      });

      card.addEventListener('mouseleave', function () {
        card.style.transform = '';
        card.style.transition = 'transform 0.4s ease';
      });
    });
  }

  /* ── Smooth scroll for anchor links ── */
  function initSmoothScroll() {
    document.querySelectorAll('a[href^="#"]').forEach(function (a) {
      a.addEventListener('click', function (e) {
        var target = document.querySelector(a.getAttribute('href'));
        if (!target) return;
        e.preventDefault();
        target.scrollIntoView({ behavior: 'smooth', block: 'start' });
      });
    });
  }

  /* ── Init all ── */
  function init() {
    initScrollReveal();
    initStickyHeader();
    animateCounters();
    animatePinBars();
    initCardTilt();
    initSmoothScroll();
  }

  // Run after DOM ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  // Blazor re-renders: re-run on navigation
  if (typeof Blazor !== 'undefined') {
    Blazor.addEventListener('enhancedload', function () {
      setTimeout(init, 100);
    });
  }

  // Expose for Blazor manual call
  window.evPublic = { init: init, initScrollHeader: initScrollHeader };

})();
