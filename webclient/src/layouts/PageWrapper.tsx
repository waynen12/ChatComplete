// src/layouts/PageWrapper.tsx
import { useLocation } from "react-router-dom";
import { AnimatePresence, motion } from "framer-motion";

export function PageWrapper({ children }: { children: React.ReactNode }) {
  const { pathname } = useLocation();
  return (
    <AnimatePresence mode="wait">
      <motion.div
        key={pathname}
        initial={{ opacity: 0, y: 8 }}
        animate={{ opacity: 1, y: 0 }}
        exit={{ opacity: 0, y: -8 }}
        transition={{ duration: 0.15 }}
        className="h-full"
      >
        {children}
      </motion.div>
    </AnimatePresence>
  );
}
