document.addEventListener('DOMContentLoaded', () => {
  const form = document.querySelector('[data-fee-assessment]');
  if (!form) return;

  const fields = [...form.querySelectorAll('.fee-amount')];
  const total = form.querySelector('#fee-total');
  const maximum = 999999999999999999n;

  function parseCents(value) {
    const raw = value.trim().replace(/^₱\s*/, '');
    if (!/^(?:\d+|\d{1,3}(?:,\d{3})+)(?:\.\d{0,2})?$/.test(raw)) return null;
    const [whole, fraction = ''] = raw.replaceAll(',', '').split('.');
    const cents = BigInt(whole) * 100n + BigInt((fraction + '00').slice(0, 2));
    return cents <= maximum ? cents : null;
  }

  function money(cents) {
    const whole = (cents / 100n).toString().replace(/\B(?=(\d{3})+(?!\d))/g, ',');
    return `₱${whole}.${(cents % 100n).toString().padStart(2, '0')}`;
  }

  function validate(field) {
    const cents = parseCents(field.value);
    const message = cents === null ? 'Enter a non-negative amount with at most two decimal places.' : '';
    field.setCustomValidity(message);
    const error = document.getElementById(`fee-error-${field.id.slice(4)}`);
    if (error) error.textContent = message;
    return cents;
  }

  function updateTotal() {
    let sum = 0n;
    let valid = true;
    for (const field of fields) {
      const cents = validate(field);
      if (cents === null) valid = false;
      else sum += cents;
    }
    if (sum > maximum) valid = false;
    total.textContent = valid ? money(sum) : 'Check fee amounts';
    total.classList.toggle('text-danger', !valid);
  }

  for (const field of fields) {
    const initial = parseCents(field.value);
    if (initial !== null) field.value = money(initial);
    field.addEventListener('focus', () => {
      const cents = parseCents(field.value);
      if (cents !== null) field.value = `${cents / 100n}.${(cents % 100n).toString().padStart(2, '0')}`;
      field.select();
    });
    field.addEventListener('input', updateTotal);
    field.addEventListener('blur', () => {
      const cents = validate(field);
      if (cents !== null) field.value = money(cents);
      updateTotal();
    });
  }

  form.addEventListener('submit', event => {
    updateTotal();
    const invalid = fields.find(field => !field.checkValidity());
    if (invalid || total.classList.contains('text-danger')) {
      event.preventDefault();
      (invalid ?? fields[0]).focus();
      (invalid ?? fields[0]).reportValidity();
      return;
    }
    for (const field of fields) {
      const cents = parseCents(field.value);
      field.value = `${cents / 100n}.${(cents % 100n).toString().padStart(2, '0')}`;
    }
  });

  updateTotal();
});
