import { useTranslation } from "react-i18next";
import { Pin } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import type { PlantNote } from "@/api/types";
import { formatInstant } from "@/lib/dates";
import { cn } from "@/lib/utils";

interface PinnedNotesCardProps {
  notes: PlantNote[];
  onUnpin: (noteId: number) => void;
  unpinning: boolean;
}

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
        <Pin className="h-4 w-4 fill-current text-primary" aria-hidden="true" />
        {t("detail.pinnedNotes")}
      </h2>
      <ul className={cn("grid grid-cols-1 items-start gap-4", columns)}>
        {notes.map((note) => (
          <li key={note.id}>
            <Card className="group relative transition-colors hover:border-primary/40 hover:shadow-md">
              <CardContent className="p-4 pr-12">
                <p className="whitespace-pre-wrap break-words text-sm leading-relaxed sm:line-clamp-4 sm:group-hover:line-clamp-none sm:group-focus-within:line-clamp-none">
                  {note.text}
                </p>
                <p className="mt-2 text-xs text-muted-foreground">{formatInstant(note.createdAt)}</p>
              </CardContent>
              <Button
                type="button"
                variant="secondary"
                size="icon"
                className="absolute right-2 top-2 h-8 w-8"
                aria-label={t("detail.unpinNote")}
                aria-pressed={true}
                disabled={unpinning}
                onClick={() => onUnpin(note.id)}
              >
                <Pin className="h-4 w-4 fill-current" aria-hidden="true" />
              </Button>
            </Card>
          </li>
        ))}
      </ul>
    </section>
  );
}
