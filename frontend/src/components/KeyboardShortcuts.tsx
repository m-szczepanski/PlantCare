import { useEffect } from "react";
import { useNavigate } from "react-router-dom";

function isTypingTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) return false;
  return (
    target.isContentEditable ||
    ["INPUT", "TEXTAREA", "SELECT"].includes(target.tagName)
  );
}

export function KeyboardShortcuts() {
  const navigate = useNavigate();

  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (event.defaultPrevented || event.metaKey || event.ctrlKey || event.altKey) return;
      if (isTypingTarget(event.target)) return;

      switch (event.key) {
        case "n":
          event.preventDefault();
          navigate("/plants/new");
          break;
        case "/": {
          const search = document.getElementById("plant-search");
          if (search instanceof HTMLInputElement) {
            event.preventDefault();
            search.focus();
          }
          break;
        }
        case "w": {
          const card =
            document.activeElement instanceof HTMLElement
              ? document.activeElement.closest("[data-plant-card]")
              : null;
          const button = card?.querySelector<HTMLButtonElement>("[data-water-button]");
          if (button && !button.disabled) {
            event.preventDefault();
            button.click();
          }
          break;
        }
      }
    }

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [navigate]);

  return null;
}
