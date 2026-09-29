document.addEventListener('DOMContentLoaded', () => {
  const toggle = document.getElementById('sidebar-toggle');
  const sidebar = document.getElementById('app-sidebar');
  const backdrop = document.querySelector('.sidebar-backdrop');
  if (toggle && sidebar && backdrop) {
    const mobile = window.matchMedia('(max-width: 991px)');
    const icon = toggle.querySelector('.toggle-icon');
    const focusableSidebarItems = () => [...sidebar.querySelectorAll('a[href], summary, button:not([disabled])')]
      .filter(item => item.getClientRects().length);
    const setSidebar = (open, returnFocus = true) => {
      open = open && mobile.matches;
      document.body.classList.toggle('sidebar-open', open);
      toggle.setAttribute('aria-expanded', String(open));
      toggle.setAttribute('aria-label', open ? 'Close navigation' : 'Open navigation');
      icon.textContent = open ? '×' : '☰';
      if (mobile.matches) sidebar.setAttribute('aria-hidden', String(!open));
      else sidebar.removeAttribute('aria-hidden');
      sidebar.inert = mobile.matches && !open;
      if (open) toggle.focus();
      else if (returnFocus && mobile.matches) toggle.focus();
    };
    setSidebar(false, false);
    toggle.addEventListener('click', () => setSidebar(!document.body.classList.contains('sidebar-open')));
    backdrop.addEventListener('pointerdown', event => event.preventDefault());
    backdrop.addEventListener('mousedown', event => event.preventDefault());
    backdrop.addEventListener('click', event => {
      event.preventDefault();
      setSidebar(false);
    });
    document.addEventListener('keydown', event => {
      if (!document.body.classList.contains('sidebar-open')) return;
      if (event.key === 'Escape') {
        setSidebar(false);
        return;
      }
      if (event.key !== 'Tab') return;
      const items = focusableSidebarItems();
      const first = items[0];
      const last = items[items.length - 1];
      if (document.activeElement === toggle) {
        event.preventDefault();
        (event.shiftKey ? last : first)?.focus();
      } else if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        toggle.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        toggle.focus();
      }
    });
    mobile.addEventListener('change', () => {
      if (!mobile.matches && document.activeElement === toggle) sidebar.querySelector('a')?.focus();
      setSidebar(false, false);
    });
  }
  const syncValidation = () => {
    document.querySelectorAll('[data-valmsg-for]').forEach(message => {
      const name = message.getAttribute('data-valmsg-for');
      const field = [...document.querySelectorAll('input, select, textarea')].find(input => input.name === name);
      if (!field) return;
      if (!message.id) message.id = `${field.id || 'field'}-error`;
      const hasError = message.classList.contains('field-validation-error') && message.textContent.trim();
      if (hasError) {
        field.setAttribute('aria-invalid', 'true');
        const descriptions = new Set((field.getAttribute('aria-describedby') || '').split(/\s+/).filter(Boolean));
        descriptions.add(message.id);
        field.setAttribute('aria-describedby', [...descriptions].join(' '));
      } else {
        field.removeAttribute('aria-invalid');
      }
    });
  };
  syncValidation();

  const firstInvalid = document.querySelector('.input-validation-error, [aria-invalid="true"]');
  const summary = document.querySelector('.validation-summary-errors');
  const pageError = document.querySelector('main [role="alert"]');
  const success = document.querySelector('main [role="status"].text-success');
  const focusTarget = firstInvalid || summary || pageError || success;
  if (focusTarget) {
    if (!focusTarget.matches('input, select, textarea, button, a')) focusTarget.setAttribute('tabindex', '-1');
    focusTarget.focus();
  }

  const observer = new MutationObserver(syncValidation);
  document.querySelectorAll('form').forEach(form => observer.observe(form, { subtree: true, childList: true, characterData: true, attributes: true, attributeFilter: ['class'] }));

  document.querySelectorAll('form[method="post"]').forEach(form => {
    form.addEventListener('submit', event => {
      setTimeout(() => {
        if (event.defaultPrevented || form.dataset.submitted === 'true') return;
        form.dataset.submitted = 'true';
        form.setAttribute('aria-busy', 'true');
        form.querySelectorAll('button[type="submit"]').forEach(button => {
          button.disabled = true;
          button.textContent = 'Processing…';
        });
      }, 0);
    });
  });
});

(() => {
    const clock = document.getElementById('philippine-clock');
    if (!clock) return;
    const dateFormat = new Intl.DateTimeFormat('en-PH', {
        timeZone: 'Asia/Manila', month: 'short', day: '2-digit', year: 'numeric'
    });
    const timeFormat = new Intl.DateTimeFormat('en-PH', {
        timeZone: 'Asia/Manila', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: true
    });
    const update = () => {
        const now = new Date();
        clock.dateTime = now.toISOString();
        clock.textContent = `${dateFormat.format(now)} · ${timeFormat.format(now)} PHT`;
    };
    update();
    setInterval(update, 1000);
})();
