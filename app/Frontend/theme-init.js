// Runs synchronously in <head> (before first paint) so the saved theme is applied
// without a flash. Kept as an external file because the CSP forbids inline scripts.
(function () {
  var theme = null;
  try { theme = localStorage.getItem("taskmate.theme"); } catch (e) { /* storage may be blocked */ }
  if (theme !== "light" && theme !== "dark") {
    theme = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  }
  document.documentElement.dataset.theme = theme;
})();
