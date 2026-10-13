(() => {
    const form = document.querySelector('[data-live-search]');
    const results = document.querySelector('[data-search-results]');
    const status = document.querySelector('[data-search-status]');
    if (!form || !results || !status) return;
    const input = form.querySelector('[name="search"]');
    let timer, controller, version = 0;
    const invalidate = () => {
        clearTimeout(timer);
        controller?.abort();
        return ++version;
    };
    const search = async (token) => {
        controller = new AbortController();
        const url = new URL(form.action || location.href);
        url.search = new URLSearchParams(new FormData(form)).toString();
        url.searchParams.delete('page');
        url.searchParams.delete('pageNumber');
        const displayUrl = new URL(url);
        url.searchParams.set('handler', 'Results');
        status.textContent = 'Searching…';
        results.setAttribute('aria-busy', 'true');
        try {
            const response = await fetch(url, { signal: controller.signal, headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            if (!response.ok || response.redirected) throw new Error('Search failed');
            const html = await response.text();
            if (token !== version) return;
            results.innerHTML = html;
            history.replaceState(null, '', displayUrl);
            status.textContent = results.querySelector('.record-count')?.textContent || 'Search complete.';
        } catch (error) {
            if (token === version && error.name !== 'AbortError') status.textContent = 'Search could not be completed. Try again.';
        } finally {
            if (token === version) results.removeAttribute('aria-busy');
        }
    };
    input.addEventListener('input', (event) => {
        const token = invalidate();
        if (event.isComposing) return;
        timer = setTimeout(() => search(token), 300);
    });
    input.addEventListener('compositionend', () => {
        const token = invalidate();
        timer = setTimeout(() => search(token), 300);
    });
    form.addEventListener('submit', () => invalidate());
})();
