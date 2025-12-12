/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./**/*.razor",
    "./**/*.html",
    "./**/*.cshtml"
  ],
  theme: {
    extend: {
      colors: {
        // Healthy wellness brand colors
        'healthy-primary': '#059669',      // Emerald green - main brand
        'healthy-secondary': '#10b981',    // Lighter emerald
        'healthy-accent': '#34d399',       // Mint accent
        'healthy-light': '#d1fae5',        // Very light green bg
        'healthy-dark': '#047857',         // Dark emerald
        'healthy-earth': '#92400e',        // Earth brown for protein
        'healthy-grain': '#b45309',        // Amber for carbs
        'healthy-water': '#0891b2',        // Cyan for water
        'healthy-fruit': '#dc2626',        // Red for fruits/veggies
        'healthy-bg': '#f0fdf4',           // Light green background
        'healthy-card': '#ffffff',         // White cards
        'healthy-text': '#1f2937',         // Dark text
        'healthy-muted': '#6b7280'         // Muted gray
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif']
      },
      boxShadow: {
        'healthy': '0 2px 8px rgba(5, 150, 105, 0.15)',
        'healthy-lg': '0 4px 16px rgba(5, 150, 105, 0.2)'
      }
    }
  },
  plugins: []
}
