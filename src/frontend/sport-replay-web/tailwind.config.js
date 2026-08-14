/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        ink: '#07131f',
        panel: '#0e1d2e',
        line: '#1e3a52',
        accent: '#3ee0a2',
        warn: '#ffb020',
        danger: '#ff5d73'
      },
      fontFamily: {
        sans: ['Manrope', 'Segoe UI', 'sans-serif'],
        display: ['Oswald', 'Impact', 'sans-serif']
      },
      boxShadow: {
        glow: '0 0 40px rgba(62, 224, 162, 0.18)'
      }
    }
  },
  plugins: []
};
