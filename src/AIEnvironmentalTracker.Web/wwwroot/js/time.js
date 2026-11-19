// Gives Blazor the browser's IANA time zone (for example "Asia/Jakarta").
window.oxygenai = window.oxygenai || {};
window.oxygenai.getTimeZone = () => Intl.DateTimeFormat().resolvedOptions().timeZone;
