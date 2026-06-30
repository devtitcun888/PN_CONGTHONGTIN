/**
 * EV Rental — Public JS v3
 * Scroll header, reveal, counter, card interactions, particles
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

  /* ── Scroll Reveal with direction ── */
  function initScrollReveal() {
    var els = document.querySelectorAll('.ev-reveal');
    if (!els.length) return;

    // Add ready class to enable initial hidden state
    document.body.classList.add('ev-anim-ready');

    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) {
          var el = entry.target;
          var revealType = el.getAttribute('data-reveal');
          
          // Remove transform based on direction for final state
          if (revealType === 'left') el.classList.add('visible', 'fade-in-left');
          else if (revealType === 'right') el.classList.add('visible', 'fade-in-right');
          else if (revealType === 'scale') el.classList.add('visible', 'scale-in');
          else el.classList.add('visible');
          
          io.unobserve(el);
        }
      });
    }, { threshold: 0.12, rootMargin: '0px 0px -40px 0px' });

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
    var counters = document.querySelectorAll('.ev-stat-item__num');
    if (!counters.length) return;

    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (!entry.isIntersecting) return;
        var el = entry.target;
        var text = el.textContent || '';
        var match = text.match(/(\d+)/);
        if (!match) { io.unobserve(el); return; }
        
        var target = parseInt(match[1], 10);
        var suffix = text.replace(match[1], '');
        var start = 0;
        var duration = 1800;
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
      if (card.getAttribute('data-tilt') !== 'true') return;
      
      card.addEventListener('mousemove', function (e) {
        var rect = card.getBoundingClientRect();
        var x = e.clientX - rect.left;
        var y = e.clientY - rect.top;
        var cx = rect.width / 2;
        var cy = rect.height / 2;
        var rx = ((y - cy) / cy) * 4;
        var ry = ((x - cx) / cx) * -4;
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

  /* ── Floating particles ── */
  function initParticles() {
    var container = document.getElementById('hero-particles');
    if (!container) return;
    
    // Particles are already in DOM, just add random animation for variety
    var particles = container.querySelectorAll('.ev-particle');
    particles.forEach(function (p, i) {
      var delay = Math.random() * 10;
      var dur = 20 + Math.random() * 20;
      p.style.animationDelay = delay + 's';
      p.style.animationDuration = dur + 's';
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
    initParticles();
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
