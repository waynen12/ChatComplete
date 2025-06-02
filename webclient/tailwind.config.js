// tailwind.config.js
/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  darkMode: "class",
  theme: {
    extend: {
      colors: {
        primary:   "hsl(var(--primary))",
        secondary: "hsl(var(--secondary))",
        ring:      "hsl(var(--ring))",
        background:"hsl(var(--background))",
        foreground:"hsl(var(--foreground))",
        accent: "hsl(var(--accent))",
      },
      fontFamily: {
        heading: ["Inter", "sans-serif"],
      },
      borderRadius: { lg: "var(--radius)" },
    },
  },
  plugins: [require("tailwindcss-animate")],

};
