(() => {
    const form = document.querySelector('[data-live-search]');
    const results = document.querySelector('[data-search-results]');
    const status = document.querySelector('[data-search-status]');
    if (!form || !results || !status) return;
    const input = form.querySelector('[name="search"]');
    const clear = form.querySelector('[data-clear-search]');
    let timer, controller, version = 0;
    const invalidate = () => {
        if (clear) clear.hidden = !input.value.trim();
        clearTimeout(timer);
        controller?.abort();
        results.removeAttribute('aria-busy');
        status.textContent = '';
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
            const partial = document.createElement('template');
            partial.innerHTML = html;
            const count = partial.content.querySelector('.record-count');
            if (!count) throw new Error('Results unavailable');
            const restoreSearchFocus = results.contains(document.activeElement);
            results.replaceChildren(partial.content);
            if (restoreSearchFocus) input.focus({ preventScroll: true });
            history.replaceState(history.state, '', displayUrl);
            status.textContent = results.querySelector('.empty-state')?.textContent.trim() || count.textContent.trim();
        } catch (error) {
            if (token === version && error.name !== 'AbortError') status.textContent = 'Search could not be completed. Previous results are still shown. Choose Search to retry.';
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
    form.addEventListener('submit', invalidate);
    window.addEventListener('pagehide', invalidate);
})();
