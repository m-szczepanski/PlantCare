import { useEffect } from "react";
import { useLocation } from "react-router-dom";

const scrollPositions = new Map<string, number>();

// Remembers window scroll per history entry (router location.key) and puts it
// back when the user navigates forward or back to that entry.
export function ScrollRestoration() {
  const { key } = useLocation();

  useEffect(() => {
    const saved = scrollPositions.get(key);
    if (saved && typeof window.scrollTo === "function") {
      window.scrollTo(0, saved);
    }

    return () => {
      scrollPositions.set(key, window.scrollY ?? 0);
    };
  }, [key]);

  return null;
}
