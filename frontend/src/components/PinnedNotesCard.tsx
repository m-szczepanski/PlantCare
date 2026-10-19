import { useTranslation } from "react-i18next";
import { Pin, PinOff } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { PlantNote } from "@/api/types";
import { formatInstant } from "@/lib/dates";

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

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Pin className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
          {t("detail.pinnedNotes")}
        </CardTitle>
      </CardHeader>
      <CardContent>
        <ul className="space-y-2 text-sm">
          {notes.map((note) => (
            <li key={note.id} className="flex items-start justify-between gap-3 rounded-md border p-3">
              <div>
                <p>{note.text}</p>
                <p className="mt-1 text-xs text-muted-foreground">
                  {formatInstant(note.createdAt)}
                </p>
              </div>
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
            </li>
          ))}
        </ul>
      </CardContent>
    </Card>
  );
}
