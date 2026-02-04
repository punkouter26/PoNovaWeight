/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./**/*.razor",
    "./**/*.html",
    "./**/*.cshtml"
  ],
  darkMode: 'class',
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
        'healthy-warning': '#f59e0b',      // Amber warning
        'healthy-danger': '#ef4444',       // Red danger/over
        'healthy-bg': '#f0fdf4',           // Light green background
        'healthy-card': '#ffffff',         // White cards
        'healthy-text': '#1f2937',         // Dark text
        'healthy-muted': '#6b7280',        // Muted gray
        // Dark mode variants
        'dark-bg': '#0f172a',
        'dark-card': '#1e293b',
        'dark-text': '#f1f5f9',
        'dark-muted': '#94a3b8',
        'dark-border': '#334155'
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif']
      },
      boxShadow: {
        // Layered elevation system
        'elevation-1': '0 1px 2px 0 rgb(0 0 0 / 0.03), 0 1px 3px 0 rgb(0 0 0 / 0.06)',
        'elevation-2': '0 2px 4px -1px rgb(0 0 0 / 0.04), 0 4px 6px -1px rgb(0 0 0 / 0.08)',
        'elevation-3': '0 4px 6px -2px rgb(0 0 0 / 0.05), 0 10px 15px -3px rgb(0 0 0 / 0.1)',
        'elevation-4': '0 10px 20px -5px rgb(0 0 0 / 0.08), 0 20px 25px -5px rgb(0 0 0 / 0.12)',
        // Brand shadows with color
        'healthy': '0 2px 8px rgba(5, 150, 105, 0.08), 0 4px 12px rgba(5, 150, 105, 0.04)',
        'healthy-lg': '0 4px 16px rgba(5, 150, 105, 0.12), 0 8px 24px rgba(5, 150, 105, 0.08)',
        'healthy-glow': '0 0 0 3px rgba(5, 150, 105, 0.15)',
        // Interactive shadows
        'btn': '0 1px 2px rgb(0 0 0 / 0.05)',
        'btn-hover': '0 4px 8px rgb(0 0 0 / 0.1)'
      },
      fontSize: {
        // Minimum 12px for accessibility
        'xxs': ['0.75rem', { lineHeight: '1rem' }],      // 12px - minimum
        'xs': ['0.8125rem', { lineHeight: '1.125rem' }], // 13px
        'sm': ['0.875rem', { lineHeight: '1.25rem' }],   // 14px
        'base': ['1rem', { lineHeight: '1.5rem' }],       // 16px
        'lg': ['1.125rem', { lineHeight: '1.75rem' }],   // 18px
        'xl': ['1.25rem', { lineHeight: '1.75rem' }],    // 20px
        '2xl': ['1.5rem', { lineHeight: '2rem' }],       // 24px
        '3xl': ['1.875rem', { lineHeight: '2.25rem' }],  // 30px
        '4xl': ['2.25rem', { lineHeight: '2.5rem' }],    // 36px
      },
      spacing: {
        // Touch target minimum 44px
        '11': '2.75rem',  // 44px
        '13': '3.25rem',  // 52px
      },
      borderRadius: {
        '2xl': '1rem',
        '3xl': '1.5rem'
      },
      animation: {
        'fade-in': 'fade-in 0.3s ease-out',
        'slide-up': 'slide-up 0.4s ease-out',
        'pulse-subtle': 'pulse-subtle 2s ease-in-out infinite',
        'shimmer': 'shimmer 1.5s infinite',
        'accordion-open': 'accordion-open 0.3s ease-out forwards',
        'accordion-close': 'accordion-close 0.3s ease-out forwards'
      },
      keyframes: {
        'fade-in': {
          '0%': { opacity: '0', transform: 'translateY(-8px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' }
        },
        'slide-up': {
          '0%': { opacity: '0', transform: 'translateY(20px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' }
        },
        'pulse-subtle': {
          '0%, 100%': { opacity: '1' },
          '50%': { opacity: '0.7' }
        },
        'shimmer': {
          '0%': { backgroundPosition: '-200% 0' },
          '100%': { backgroundPosition: '200% 0' }
        },
        'accordion-open': {
          '0%': { 'grid-template-rows': '0fr', opacity: '0' },
          '100%': { 'grid-template-rows': '1fr', opacity: '1' }
        },
        'accordion-close': {
          '0%': { 'grid-template-rows': '1fr', opacity: '1' },
          '100%': { 'grid-template-rows': '0fr', opacity: '0' }
        }
      }
    }
  },
  plugins: []
}

