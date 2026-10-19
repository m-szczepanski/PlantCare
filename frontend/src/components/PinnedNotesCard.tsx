import { useTranslation } from "react-i18next";
import { Pencil, Pin, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { NoteEditForm } from "@/components/NoteEditForm";
import type { PlantNote } from "@/api/types";
import { formatInstant } from "@/lib/dates";
import { cn } from "@/lib/utils";

interface PinnedNotesCardProps {
  notes: PlantNote[];
  editingId: number | null;
  saving: boolean;
  onEdit: (noteId: number) => void;
  onCancelEdit: () => void;
  onSaveEdit: (noteId: number, text: string) => void;
  onUnpin: (noteId: number) => void;
  unpinning: boolean;
  onDelete: (noteId: number) => void;
}

export function PinnedNotesCard({
  notes,
  editingId,
  saving,
  onEdit,
  onCancelEdit,
  onSaveEdit,
  onUnpin,
  unpinning,
  onDelete,
}: PinnedNotesCardProps) {
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
        {notes.map((note) => {
          const editing = editingId === note.id;
          return (
            <li key={note.id}>
              <Card className="group relative transition-colors hover:border-primary/40 hover:shadow-md">
                <CardContent className="p-4 pr-12">
                  {editing ? (
                    <NoteEditForm
                      text={note.text}
                      saving={saving}
                      onCancel={onCancelEdit}
                      onSave={(text) => onSaveEdit(note.id, text)}
                    />
                  ) : (
                    <>
                      <p className="whitespace-pre-wrap break-words text-sm leading-relaxed sm:line-clamp-4 sm:group-hover:line-clamp-none sm:group-focus-within:line-clamp-none">
                        {note.text}
                      </p>
                      <div className="mt-2 flex items-center justify-between gap-2">
                        <p className="text-xs text-muted-foreground">
                          {formatInstant(note.createdAt)}
                        </p>
                        <div className="flex items-center gap-1">
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8"
                            aria-label={t("detail.editNote")}
                            onClick={() => onEdit(note.id)}
                          >
                            <Pencil className="h-4 w-4" aria-hidden="true" />
                          </Button>
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8"
                            aria-label={t("detail.deleteNote")}
                            onClick={() => onDelete(note.id)}
                          >
                            <Trash2 className="h-4 w-4" aria-hidden="true" />
                          </Button>
                        </div>
                      </div>
                    </>
                  )}
                </CardContent>
                {editing ? null : (
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
                )}
              </Card>
            </li>
          );
        })}
      </ul>
    </section>
  );
}
