(() => {
  let theme = 'system';
  try { theme = localStorage.getItem('fincontrol.theme') || 'system'; } catch {}
  document.documentElement.dataset.theme = theme === 'system' ? (matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light') : theme;
})();
