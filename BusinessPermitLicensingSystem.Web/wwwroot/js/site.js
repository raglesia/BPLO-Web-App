document.addEventListener('DOMContentLoaded', () => {
  const toggle = document.getElementById('sidebar-toggle');
  const sidebar = document.getElementById('app-sidebar');
  const backdrop = document.querySelector('.sidebar-backdrop');
  if (toggle && sidebar && backdrop) {
    const setSidebar = open => {
      document.body.classList.toggle('sidebar-open', open);
      toggle.setAttribute('aria-expanded', String(open));
      toggle.setAttribute('aria-label', open ? 'Close navigation' : 'Open navigation');
      if (open) sidebar.querySelector('a')?.focus();
      else toggle.focus();
    };
    toggle.addEventListener('click', () => setSidebar(!document.body.classList.contains('sidebar-open')));
    backdrop.addEventListener('click', () => setSidebar(false));
    document.addEventListener('keydown', event => {
      if (event.key === 'Escape' && document.body.classList.contains('sidebar-open')) setSidebar(false);
    });
    sidebar.addEventListener('click', event => {
      if (event.target.closest('a') && window.matchMedia('(max-width: 991px)').matches)
        document.body.classList.remove('sidebar-open');
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
