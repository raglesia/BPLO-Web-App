document.addEventListener('DOMContentLoaded', () => {
  const retry = document.getElementById('dashboard-retry');
  if (retry) {
    const status = document.getElementById('dashboard-refresh-status');
    retry.addEventListener('click', event => {
      if (!event.ctrlKey && !event.metaKey && !event.shiftKey && !event.altKey) {
        status.textContent = 'Refreshing dashboard…';
      }
    });
    window.addEventListener('pageshow', () => { status.textContent = ''; });
  }
  const rentalEstimate = document.getElementById('rental-estimate');
  if (rentalEstimate?.form) {
    const section = rentalEstimate.form.elements.namedItem('Input.BusinessSection');
    const size = rentalEstimate.form.elements.namedItem('Input.StallSize');
    const peso = new Intl.NumberFormat('en-PH', { style: 'currency', currency: 'PHP' });
    const showRent = () => {
      const rate = section.selectedOptions[0]?.dataset;
      const area = Number(size.value.trim());
      const estimate = rate?.flat ? Number(rate.flat)
        : area > 0 && rate?.perArea ? area * Number(rate.perArea) : NaN;
      const next = Number.isFinite(estimate) ? `Estimated base rent: ${peso.format(estimate)} per month.`
        : rate?.perArea ? 'Enter the stall size for a base rent estimate.'
          : 'Select a section to preview base rent.';
      if (rentalEstimate.textContent !== next) rentalEstimate.textContent = next;
    };
    section.addEventListener('change', showRent);
    size.addEventListener('input', showRent);
    showRent();
  }
  const chart = document.querySelector('[data-collection-chart]');
  if (chart) {
    const breakdown = document.getElementById('collection-breakdown');
    const showMonth = event => {
      const month = event.target.closest('[data-collection-month]');
      if (month) breakdown.textContent = month.dataset.collectionMonth;
    };
    chart.addEventListener('pointerover', showMonth);
    chart.addEventListener('focusin', showMonth);
    chart.addEventListener('click', showMonth);
  }

  const toggle = document.getElementById('sidebar-toggle');
  const sidebar = document.getElementById('app-sidebar');
  // Give the navigation disclosures native one-at-a-time behavior.
  sidebar?.querySelectorAll('details.sidebar-disclosure').forEach(menu => {
    menu.setAttribute('name', 'bplo-navigation');
  });
  const backdrop = document.querySelector('.sidebar-backdrop');
  if (toggle && sidebar && backdrop) {
    const mobile = window.matchMedia('(max-width: 1250px)');
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
      if (event.key === 'Escape') {
        const disclosure = document.activeElement?.closest('details[open]');
        if (disclosure && sidebar.contains(disclosure)) {
          disclosure.open = false;
          disclosure.querySelector('summary')?.focus();
          return;
        }
      }
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
      const field = message.closest('form')?.elements.namedItem(name);
      if (!(field instanceof HTMLElement)) return;
      if (!message.id) message.id = `${field.id || 'field'}-error`;
      const hasError = message.classList.contains('field-validation-error') && message.textContent.trim();
      if (hasError) {
        field.setAttribute('aria-invalid', 'true');
        const descriptions = new Set((field.getAttribute('aria-describedby') || '').split(/\s+/).filter(Boolean));
        descriptions.add(message.id);
        field.setAttribute('aria-describedby', [...descriptions].join(' '));
      } else {
        field.removeAttribute('aria-invalid');
        const descriptions = (field.getAttribute('aria-describedby') || '').split(/\s+/)
          .filter(id => id && id !== message.id);
        if (descriptions.length) field.setAttribute('aria-describedby', descriptions.join(' '));
        else field.removeAttribute('aria-describedby');
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

  const pendingSubmissions = new Map();
  const submissionStatus = document.getElementById('submission-status');
  document.addEventListener('submit', event => {
    if (pendingSubmissions.has(event.target)) event.preventDefault();
  }, true);

  // Run after form validation and the document-level confirmation handler.
  window.addEventListener('submit', event => {
    const form = event.target;
    if (event.defaultPrevented || !(form instanceof HTMLFormElement) ||
        form.method !== 'post' || event.submitter?.formTarget === '_blank' || form.target === '_blank') return;

    const button = event.submitter || form.querySelector('button[type="submit"]:enabled');
    const original = button instanceof HTMLButtonElement ? [...button.childNodes] : [];
    const controls = [...form.elements].filter(control => control.type === 'submit' && !control.disabled);
    const label = form.dataset.processing || 'Submitting…';
    const timer = setTimeout(() => {
      // Leave the submitter enabled until the browser has serialized its name and value.
      controls.forEach(control => { control.disabled = true; });
      if (button instanceof HTMLButtonElement) button.textContent = label;
    });
    pendingSubmissions.set(form, { button, original, controls, timer });
    form.setAttribute('aria-busy', 'true');
    if (submissionStatus) submissionStatus.textContent = label;
  });

  window.addEventListener('pageshow', () => {
    for (const [form, { button, original, controls, timer }] of pendingSubmissions) {
      clearTimeout(timer);
      controls.forEach(control => { control.disabled = false; });
      if (button instanceof HTMLButtonElement) button.replaceChildren(...original);
      form.removeAttribute('aria-busy');
    }
    pendingSubmissions.clear();
    if (submissionStatus) submissionStatus.textContent = '';
  });

  const trackedForms = [...document.querySelectorAll('form[data-warn-unsaved]')];
  const snapshot = form => JSON.stringify([...new FormData(form)].map(([name, value]) => {
    if (typeof value === 'string' && (name.startsWith('FeeAmounts[') || name === 'PermitDetails.Capital')) {
      const decimal = value.trim().replace(/[₱,]/g, '');
      if (/^\d+(\.\d{1,2})?$/.test(decimal)) {
        const [whole, fraction = ''] = decimal.split('.');
        value = `${BigInt(whole)}.${fraction.padEnd(2, '0')}`;
      }
    }
    return [name, value];
  }));
  // Fee widgets format their initial values during DOMContentLoaded.
  const baselines = new Map();
  setTimeout(() => trackedForms.forEach(form => baselines.set(form, snapshot(form))), 0);
  const warnUnsaved = event => {
    if (trackedForms.some(form => !pendingSubmissions.has(form) && snapshot(form) !== baselines.get(form))) {
      event.preventDefault();
      event.returnValue = true;
    }
  };
  const updateUnsavedWarning = () => {
    window.removeEventListener('beforeunload', warnUnsaved);
    if (trackedForms.some(form => baselines.has(form) && snapshot(form) !== baselines.get(form))) {
      window.addEventListener('beforeunload', warnUnsaved);
    }
  };
  trackedForms.forEach(form => {
    form.addEventListener('change', updateUnsavedWarning);
    form.addEventListener('input', updateUnsavedWarning);
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

(() => {
    const dialog = document.getElementById('bplo-confirm');
    if (!dialog?.showModal) return;

    const title = document.getElementById('bplo-confirm-title');
    const message = document.getElementById('bplo-confirm-message');
    const action = document.getElementById('bplo-confirm-action');
    let pending;
    let approvedForm;

    document.addEventListener('submit', event => {
        const form = event.target;
        if (event.defaultPrevented || !(form instanceof HTMLFormElement) ||
            !form.hasAttribute('data-confirm-message') || form === approvedForm) return;

        event.preventDefault();
        if (pending) return;

        pending = { form, submitter: event.submitter };
        title.textContent = form.dataset.confirmTitle || 'Confirm action';
        message.textContent = form.dataset.confirmMessage;
        action.textContent = form.dataset.confirmAction || 'Confirm';
        dialog.returnValue = '';
        dialog.showModal();
    });

    dialog.addEventListener('close', () => {
        const submission = pending;
        pending = undefined;
        if (!submission || dialog.returnValue !== 'confirm' || !submission.form.isConnected) return;

        const { form, submitter } = submission;
        approvedForm = form;
        try {
            form.requestSubmit(submitter?.form === form ? submitter : undefined);
        } finally {
            approvedForm = undefined;
        }
    });
})();
