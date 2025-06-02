// src/context/ThemeContext.tsx
import { createContext, useContext, useEffect, useState } from "react";
type Theme = "light" | "dark";

const ThemeCtx = createContext<{ theme: Theme; toggle(): void }>({
  theme: "light",
  toggle: () => {},
});

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [theme, setTheme] = useState<Theme>(
    () => (localStorage.theme as Theme) || "light"
  );

  useEffect(() => {
    document.documentElement.classList.toggle("dark", theme === "dark");
    localStorage.theme = theme;
  }, [theme]);

  return (
    <ThemeCtx.Provider value={{ theme, toggle: () =>
      setTheme((t) => (t === "light" ? "dark" : "light")) }}>
      {children}
    </ThemeCtx.Provider>
  );
}
export const useTheme = () => useContext(ThemeCtx);
