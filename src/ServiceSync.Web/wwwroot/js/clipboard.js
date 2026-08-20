window.serviceSync = window.serviceSync || {};
window.serviceSync.copyText = async function (value) {
    await navigator.clipboard.writeText(value);
};
