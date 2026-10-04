window.clipboardManager = {
    async copyText(text) {
        if (!navigator.clipboard) throw new Error('Clipboard API is unavailable.');
        await navigator.clipboard.writeText(text);
    }
};
window.reviewDraftStorage = {
    get: key => localStorage.getItem(key),
    set: (key, value) => localStorage.setItem(key, value),
    remove: key => localStorage.removeItem(key)
};
