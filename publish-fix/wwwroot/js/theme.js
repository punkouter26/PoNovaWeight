// Theme management utilities
window.isDarkMode = function() {
    return localStorage.getItem('theme') === 'dark' || 
           (localStorage.getItem('theme') === null && 
            window.matchMedia('(prefers-color-scheme: dark)').matches);
};

window.toggleDarkMode = function() {
    const html = document.documentElement;
    const currentDark = html.classList.contains('dark');
    
    if (currentDark) {
        html.classList.remove('dark');
        localStorage.setItem('theme', 'light');
    } else {
        html.classList.add('dark');
        localStorage.setItem('theme', 'dark');
    }
    
    return !currentDark;
};

// Initialize theme on page load
function initializeTheme() {
    const html = document.documentElement;
    const savedTheme = localStorage.getItem('theme');
    
    if (savedTheme === 'dark' || 
        (savedTheme === null && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
        html.classList.add('dark');
    } else {
        html.classList.remove('dark');
    }
}

// Run initialization on DOM ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeTheme);
} else {
    initializeTheme();
}

// Listen for system theme changes
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
    if (localStorage.getItem('theme') === null) {
        const html = document.documentElement;
        if (e.matches) {
            html.classList.add('dark');
        } else {
            html.classList.remove('dark');
        }
    }
});
