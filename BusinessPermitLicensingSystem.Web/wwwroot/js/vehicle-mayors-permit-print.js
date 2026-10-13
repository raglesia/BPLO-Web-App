(() => {
    const paper = document.querySelector('.mayor-paper[data-prepared="true"]');
    if (!paper) return;
    window.addEventListener('beforeprint', () => {
        paper.style.zoom = '1';
        paper.classList.add('mayor-print-sizing');
        // A4 minus two 8 mm margins, with 2 mm pagination clearance.
        const availableHeight = 279 * 96 / 25.4;
        const naturalHeight = Math.max(paper.scrollHeight, paper.getBoundingClientRect().height);
        const scale = Math.min(1, availableHeight / naturalHeight);
        paper.style.zoom = String(scale);
        paper.dataset.printHeight = String(naturalHeight * scale);
        paper.dataset.printScale = String(scale);
    });
    window.addEventListener('afterprint', () => {
        paper.classList.remove('mayor-print-sizing');
        paper.style.removeProperty('zoom');
    });
})();
