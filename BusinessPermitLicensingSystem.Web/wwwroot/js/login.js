(() => {
    const toggle = document.getElementById('password-toggle');
    const password = document.getElementById('Input_Password');
    if (!toggle || !password) return;
    toggle.addEventListener('click', () => {
        const show = password.type === 'password';
        password.type = show ? 'text' : 'password';
        toggle.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
        toggle.setAttribute('aria-pressed', String(show));
        toggle.querySelector('.password-eye-slash').toggleAttribute('hidden', !show);
    });
})();
