import { useTranslation } from "react-i18next";
import { Pin, PinOff } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { PlantNote } from "@/api/types";
import { formatInstant } from "@/lib/dates";
import { cn } from "@/lib/utils";

interface PinnedNotesCardProps {
  notes: PlantNote[];
  onUnpin: (noteId: number) => void;
  unpinning: boolean;
}

const rotations = ["sm:-rotate-1", "sm:rotate-1", "sm:rotate-0"];

export function PinnedNotesCard({ notes, onUnpin, unpinning }: PinnedNotesCardProps) {
  const { t } = useTranslation();

  if (notes.length === 0) {
    return null;
  }

  const columns =
    notes.length === 1
      ? "sm:grid-cols-1"
      : notes.length === 2
        ? "sm:grid-cols-2"
        : "sm:grid-cols-2 lg:grid-cols-3";

  return (
    <section aria-labelledby="pinned-notes-heading" className="space-y-3">
      <h2
        id="pinned-notes-heading"
        className="flex items-center gap-2 text-lg font-semibold leading-none tracking-tight"
      >
        <Pin className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
        {t("detail.pinnedNotes")}
      </h2>
      <ul className={cn("grid grid-cols-1 gap-4", columns)}>
        {notes.map((note, index) => (
          <li
            key={note.id}
            className={cn(
              "relative flex min-h-32 flex-col rounded-sm border border-sticky-border bg-sticky p-4 text-sticky-foreground shadow-md transition-transform hover:rotate-0",
              rotations[index % rotations.length],
            )}
          >
            <Pin
              className="absolute right-2 top-2 h-4 w-4 opacity-50"
              aria-hidden="true"
            />
            <p className="whitespace-pre-wrap break-words pr-6 text-sm">{note.text}</p>
            <div className="mt-auto flex items-end justify-between gap-2 pt-3">
              <p className="text-xs text-sticky-foreground/70">{formatInstant(note.createdAt)}</p>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                aria-label={t("detail.unpinNote")}
                disabled={unpinning}
                onClick={() => onUnpin(note.id)}
              >
                <PinOff className="h-4 w-4" aria-hidden="true" />
              </Button>
            </div>
          </li>
        ))}
      </ul>
    </section>
  );
}
