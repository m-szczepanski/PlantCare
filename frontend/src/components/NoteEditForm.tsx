import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/ui/button";
import { touchTextarea } from "@/lib/ui";

const NOTE_MAX_LENGTH = 2000;

interface NoteEditFormProps {
  text: string;
  saving: boolean;
  onSave: (text: string) => void;
  onCancel: () => void;
}

export function NoteEditForm({ text, saving, onSave, onCancel }: NoteEditFormProps) {
  const { t } = useTranslation();
  const [draft, setDraft] = useState(text);
  const trimmed = draft.trim();

  return (
    <div className="space-y-2">
      <textarea
        aria-label={t("detail.noteEditorAria")}
        value={draft}
        onChange={(event) => setDraft(event.target.value)}
        rows={3}
        maxLength={NOTE_MAX_LENGTH}
        autoFocus
        className={touchTextarea}
      />
      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          size="sm"
          disabled={saving || trimmed === ""}
          onClick={() => onSave(trimmed)}
        >
          {saving ? t("common.saving") : t("common.save")}
        </Button>
        <Button type="button" size="sm" variant="ghost" disabled={saving} onClick={onCancel}>
          {t("common.cancel")}
        </Button>
      </div>
    </div>
  );
}
